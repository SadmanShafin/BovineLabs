using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

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

        public unsafe void Execute()
        {
            var gridW = Grid.Width;
            var gridH = Grid.Height;
            var numConfigs = MeshGraphBuilder.NumHeadings;
            var totalStates = gridW * gridH * numConfigs;

            var gCosts = (float*)UnsafeUtility.Malloc(
                UnsafeUtility.SizeOf<float>() * totalStates, UnsafeUtility.AlignOf<float>(), Allocator.Temp);
            var parentMap = (int*)UnsafeUtility.Malloc(
                UnsafeUtility.SizeOf<int>() * totalStates, UnsafeUtility.AlignOf<int>(), Allocator.Temp);
            var closedWords = (totalStates + 31) >> 5;
            var closed = (uint*)UnsafeUtility.Malloc(
                UnsafeUtility.SizeOf<uint>() * closedWords, UnsafeUtility.AlignOf<uint>(), Allocator.Temp);

            for (var i = 0; i < totalStates; i++) gCosts[i] = float.MaxValue;
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

            for (var h = 0; h < numConfigs; ++h)
            {
                var startConfig = MeshGraph.InitialConfigByTheta[h];
                var startKey = EncodeKey(Start.x, Start.y, startConfig, gridW);
                if (gCosts[startKey] > 0f)
                {
                    gCosts[startKey] = 0f;
                    heap.TryInsertOrDecrease(new HeapNode(startKey, GridHeuristics.Octile(Start, Goal) * Weight));
                }
            }

            var explored = 0;

            while (heap.Count > 0)
            {
                if (!heap.TryPop(out var top)) break;
                var currentKey = top.Id;
                explored++;

                var wordIdx = currentKey >> 5;
                var bitIdx = currentKey & 31;
                var mask = 1u << bitIdx;
                if ((closed[wordIdx] & mask) != 0) continue;
                closed[wordIdx] |= mask;

                DecodeKey(currentKey, out var cx, out var cy, out var cConfig, gridW);

                if (cx == Goal.x && cy == Goal.y)
                {
                    var key = currentKey;
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

                    for (var i = reversePath.Length - 1; i >= 0; i--) Path.Add(reversePath[i]);
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

                var currentG = gCosts[currentKey];

                if (cConfig < 0 || cConfig >= MeshGraph.MaxConfigs) continue;
                var succOff = MeshGraph.SuccOffsets[cConfig];
                var succCnt = MeshGraph.SuccCounts[cConfig];
                if (succCnt == 0) continue;

                for (var si = 0; si < succCnt; si++)
                {
                    var succ = MeshGraph.SuccessorsFlat[succOff + si];
                    var nx = cx + succ.Di;
                    var ny = cy + succ.Dj;
                    var nConfig = succ.NextConfigId;

                    if (!Grid.InBounds(new int2(nx, ny))) continue;
                    if (!Grid.IsFree(new int2(nx, ny))) continue;

                    var nKey = EncodeKey(nx, ny, nConfig, gridW);

                    var nWord = nKey >> 5;
                    var nBit = nKey & 31;
                    if ((closed[nWord] & (1u << nBit)) != 0) continue;

                    var transCost = 0f;
                    if (succ.ConnectingPrimId >= 0) transCost = PrimSet.Primitives[succ.ConnectingPrimId].ArcLength;

                    var collisionFree = true;
                    if (succ.ConnectingPrimId >= 0)
                    {
                        var prim = PrimSet.Primitives[succ.ConnectingPrimId];
                        collisionFree = prim.IsCollisionFree(Grid, cx, cy, 0);
                    }

                    if (!collisionFree) continue;

                    var newG = currentG + transCost;

                    if (newG < gCosts[nKey])
                    {
                        gCosts[nKey] = newG;
                        parentMap[nKey] = currentKey;
                        var heuristic = GridHeuristics.Octile(new int2(nx, ny), Goal) * Weight;
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
        private static int EncodeKey(int x, int y, int config, int gridW)
        {
            return (y * gridW + x) * MeshGraphBuilder.NumHeadings + config;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void DecodeKey(int key, out int x, out int y, out int config, int gridW)
        {
            config = key % MeshGraphBuilder.NumHeadings;
            var posIdx = key / MeshGraphBuilder.NumHeadings;
            y = posIdx / gridW;
            x = posIdx % gridW;
        }
    }
}