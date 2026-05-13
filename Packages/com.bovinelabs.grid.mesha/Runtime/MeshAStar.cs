using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using BovineLabs.Grid;

namespace BovineLabs.Grid.MeshA
{
    [BurstCompile]
    public struct MeshAStarJob : IJob
    {
        public NativeGrid2D Grid;
        public MeshGraphData MeshGraph;
        public PrimitiveSet PrimSet;
        public int2 Start;
        public int2 Goal;
        public int StartTheta;
        public float Weight;

        public NativeList<int2> Path;
        public NativeReference<bool> Found;
        public NativeReference<float> PathCost;
        public NativeReference<int> NodesExplored;

        public void Execute()
        {
            int gridW = Grid.Width;
            int gridH = Grid.Height;
            int numConfigs = MeshGraphBuilder.NumHeadings;
            int totalStates = gridW * gridH * numConfigs;

            // Flat arrays replace NativeHashMap — O(1) access, no hashing overhead
            float* gCosts = (float*)UnsafeUtility.Malloc(
                UnsafeUtility.SizeOf<float>() * totalStates, UnsafeUtility.AlignOf<float>(), Allocator.Temp);
            int* parentMap = (int*)UnsafeUtility.Malloc(
                UnsafeUtility.SizeOf<int>() * totalStates, UnsafeUtility.AlignOf<int>(), Allocator.Temp);
            // Bit-packed closed set: 1 bit per state
            int closedWords = (totalStates + 31) >> 5;
            uint* closed = (uint*)UnsafeUtility.Malloc(
                UnsafeUtility.SizeOf<uint>() * closedWords, UnsafeUtility.AlignOf<uint>(), Allocator.Temp);

            // Initialize: gCosts = MaxValue, parentMap = -1, closed = 0
            for (int i = 0; i < totalStates; i++) gCosts[i] = float.MaxValue;
            UnsafeUtility.MemSet(parentMap, 0xFF, (long)totalStates * UnsafeUtility.SizeOf<int>()); // -1
            UnsafeUtility.MemSet(closed, 0, (long)closedWords * UnsafeUtility.SizeOf<uint>());

            var heap = new MinHeap();
            if (!MinHeap.TryCreate(totalStates, Allocator.Temp, out heap))
            {
                UnsafeUtility.Free(gCosts, Allocator.Temp);
                UnsafeUtility.Free(parentMap, Allocator.Temp);
                UnsafeUtility.Free(closed, Allocator.Temp);
                Found.Value = false;
                return;
            }

            for (int h = 0; h < numConfigs; ++h)
            {
                int startConfig = MeshGraph.InitialConfigByTheta[h];
                int startKey = EncodeKey(Start.x, Start.y, startConfig, gridW);
                if (gCosts[startKey] > 0f)
                {
                    gCosts[startKey] = 0f;
                    heap.TryInsertOrDecrease(new HeapNode(startKey, GridHeuristics.Octile(Start, Goal) * Weight));
                }
            }

            int explored = 0;

            while (heap.Count > 0)
            {
                if (!heap.TryPop(out var top)) break;
                int currentKey = top.Id;
                explored++;

                // Check closed bit
                int wordIdx = currentKey >> 5;
                int bitIdx = currentKey & 31;
                uint mask = 1u << bitIdx;
                if ((closed[wordIdx] & mask) != 0) continue;
                closed[wordIdx] |= mask;

                DecodeKey(currentKey, out int cx, out int cy, out int cConfig, gridW);

                if (cx == Goal.x && cy == Goal.y)
                {
                    int key = currentKey;
                    var reversePath = new NativeList<int2>(Allocator.Temp);
                    while (parentMap[key] != -1)
                    {
                        int px, py, pc;
                        DecodeKey(key, out px, out py, out pc, gridW);
                        reversePath.Add(new int2(px, py));
                        key = parentMap[key];
                    }
                    {
                        int sx, sy, sc;
                        DecodeKey(key, out sx, out sy, out sc, gridW);
                        reversePath.Add(new int2(sx, sy));
                    }

                    for (int i = reversePath.Length - 1; i >= 0; i--)
                    {
                        Path.Add(reversePath[i]);
                    }
                    reversePath.Dispose();

                    Found.Value = true;
                    PathCost.Value = gCosts[currentKey];
                    NodesExplored.Value = explored;

                    heap.Dispose();
                    UnsafeUtility.Free(gCosts, Allocator.Temp);
                    UnsafeUtility.Free(parentMap, Allocator.Temp);
                    UnsafeUtility.Free(closed, Allocator.Temp);
                    return;
                }

                float currentG = gCosts[currentKey];

                if (cConfig < 0 || cConfig >= MeshGraph.MaxConfigs) continue;
                int succOff = MeshGraph.SuccOffsets[cConfig];
                int succCnt = MeshGraph.SuccCounts[cConfig];
                if (succCnt == 0) continue;

                for (int si = 0; si < succCnt; si++)
                {
                    var succ = MeshGraph.SuccessorsFlat[succOff + si];
                    int nx = cx + succ.Di;
                    int ny = cy + succ.Dj;
                    int nConfig = succ.NextConfigId;

                    if (!Grid.InBounds(new int2(nx, ny))) continue;
                    if (!Grid.IsFree(new int2(nx, ny))) continue;

                    int nKey = EncodeKey(nx, ny, nConfig, gridW);

                    // Check closed bit
                    int nWord = nKey >> 5;
                    int nBit = nKey & 31;
                    if ((closed[nWord] & (1u << nBit)) != 0) continue;

                    float transCost = 0f;
                    if (succ.ConnectingPrimId >= 0)
                    {
                        transCost = PrimSet.Primitives[succ.ConnectingPrimId].ArcLength;
                    }

                    bool collisionFree = true;
                    if (succ.ConnectingPrimId >= 0)
                    {
                        var prim = PrimSet.Primitives[succ.ConnectingPrimId];
                        collisionFree = prim.IsCollisionFree(Grid, cx, cy, 0);
                    }
                    if (!collisionFree) continue;

                    float newG = currentG + transCost;

                    if (newG < gCosts[nKey])
                    {
                        gCosts[nKey] = newG;
                        parentMap[nKey] = currentKey;
                        float heuristic = GridHeuristics.Octile(new int2(nx, ny), Goal) * Weight;
                        heap.TryInsertOrDecrease(new HeapNode(nKey, newG + heuristic));
                    }
                }
            }

            Found.Value = false;
            PathCost.Value = -1f;
            NodesExplored.Value = explored;

            heap.Dispose();
            UnsafeUtility.Free(gCosts, Allocator.Temp);
            UnsafeUtility.Free(parentMap, Allocator.Temp);
            UnsafeUtility.Free(closed, Allocator.Temp);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int EncodeKey(int x, int y, int config, int gridW)
        {
            return (y * gridW + x) * MeshGraphBuilder.NumHeadings + config;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void DecodeKey(int key, out int x, out int y, out int config, int gridW)
        {
            config = key % MeshGraphBuilder.NumHeadings;
            int posIdx = key / MeshGraphBuilder.NumHeadings;
            y = posIdx / gridW;
            x = posIdx % gridW;
        }
    }

    [BurstCompile]
    public static class MeshAStar
    {
        public static PathResult FindPath(
            in NativeGrid2D grid,
            in PrimitiveSet primSet,
            in MeshGraphData meshGraph,
            int2 start,
            int2 goal,
            int startTheta = 0,
            float weight = 1.0f,
            Allocator allocator = Allocator.Temp)
        {
            TryFindPath(grid, primSet, meshGraph, start, goal, out var result, startTheta, weight, allocator);
            return result;
        }

        public static bool TryFindPath(
            in NativeGrid2D grid,
            in PrimitiveSet primSet,
            in MeshGraphData meshGraph,
            int2 start,
            int2 goal,
            out PathResult result,
            int startTheta = 0,
            float weight = 1.0f,
            Allocator allocator = Allocator.Temp)
        {
            result = new PathResult(allocator);
            var found = new NativeReference<bool>(allocator);
            var pathCost = new NativeReference<float>(allocator);
            var nodesExplored = new NativeReference<int>(allocator);

            var job = new MeshAStarJob
            {
                Grid = grid,
                MeshGraph = meshGraph,
                PrimSet = primSet,
                Start = start,
                Goal = goal,
                StartTheta = startTheta,
                Weight = weight,
                Path = result.Path,
                Found = found,
                PathCost = pathCost,
                NodesExplored = nodesExplored,
            };

            job.Execute();

            result.Found = found.Value;
            result.PathCost = pathCost.Value;
            result.NodesExplored = nodesExplored.Value;

            bool success = found.Value;

            found.Dispose();
            pathCost.Dispose();
            nodesExplored.Dispose();

            return success;
        }
    }
}
