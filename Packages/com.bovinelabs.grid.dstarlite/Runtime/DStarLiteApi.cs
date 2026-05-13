using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using BovineLabs.Grid;

namespace BovineLabs.Grid.DStarLite
{
    [StructLayout(LayoutKind.Sequential)]
    public struct DStarLiteState
    {
        public Grid2D Grid;
        public int Start;
        public int Goal;
        public float Km;
        public NativeArray<float> G;
        public NativeArray<float> RHS;
        public MinHeap Open;
        public NativeArray<byte> InOpen;
        public NativeArray<int> Parent;
    }

    [BurstCompile]
    public unsafe static class DStarLiteApi
    {
        public static bool TryCreate(int width, int height, Allocator allocator, out DStarLiteState result)
        {
            if (!Grid2D.TryCreate(width, height, out var g) || !MinHeap.TryCreate(g.Length, allocator, out var heap))
            {
                result = default;
                return false;
            }

            result = new DStarLiteState
            {
                Grid = g,
                G = new NativeArray<float>(g.Length, allocator),
                RHS = new NativeArray<float>(g.Length, allocator),
                Open = heap,
                InOpen = new NativeArray<byte>(g.Length, allocator),
                Parent = new NativeArray<int>(g.Length, allocator),
            };
            return true;
        }

        [BurstCompile]
        public static bool TryInitialize(ref DStarLiteState s, int start, int goal, in NativeArray<byte> blocked)
        {
            if (!s.Grid.InBounds(start) || !s.Grid.InBounds(goal) || !blocked.IsCreated) return false;

            s.Start = start;
            s.Goal = goal;
            s.Km = 0f;
            s.Open.Clear();

            float* gPtr = (float*)s.G.GetUnsafePtr();
            float* rhsPtr = (float*)s.RHS.GetUnsafePtr();
            byte* inOpen = (byte*)s.InOpen.GetUnsafePtr();
            int* parent = (int*)s.Parent.GetUnsafePtr();
            int len = s.Grid.Length;
            for (int i = 0; i < len; i++) { gPtr[i] = float.PositiveInfinity; rhsPtr[i] = float.PositiveInfinity; inOpen[i] = 0; parent[i] = -1; }

            byte* blk = (byte*)blocked.GetUnsafeReadOnlyPtr();
            if (blk[goal] != 0) return false;

            rhsPtr[goal] = 0f;
            var key = CalculateKey(gPtr, rhsPtr, s.Start, s.Goal, s.Grid, s.Km, goal);
            if (!s.Open.TryInsertOrDecrease(new HeapNode(goal, key.x, key.y))) return false;
            inOpen[goal] = 1;
            return true;
        }

        [BurstCompile]
        public static void NotifyMoved(ref DStarLiteState s, int newStart)
        {
            s.Km += Grid2D.HeuristicOctile(s.Grid.ToCoord(s.Start), s.Grid.ToCoord(newStart));
            s.Start = newStart;
        }

        [BurstCompile]
        public static bool TryUpdateCell(ref DStarLiteState s, in NativeArray<byte> blocked, in NativeArray<float> cost, int cell)
        {
            if (!s.Grid.InBounds(cell)) return false;
            float* gPtr = (float*)s.G.GetUnsafePtr();
            float* rhsPtr = (float*)s.RHS.GetUnsafePtr();
            byte* inOpen = (byte*)s.InOpen.GetUnsafePtr();
            int* parentPtr = (int*)s.Parent.GetUnsafePtr();
            byte* blk = (byte*)blocked.GetUnsafeReadOnlyPtr();
            float* costPtr = cost.IsCreated ? (float*)cost.GetUnsafeReadOnlyPtr() : null;
            return TryUpdateVertex(gPtr, rhsPtr, inOpen, parentPtr, blk, costPtr, ref s.Open, s.Grid, s.Goal, s.Start, s.Km, cell);
        }

        [BurstCompile]
        public static bool TryRepair(ref DStarLiteState s, in NativeArray<byte> blocked, in NativeArray<float> cost, int maxPops)
        {
            float* gPtr = (float*)s.G.GetUnsafePtr();
            float* rhsPtr = (float*)s.RHS.GetUnsafePtr();
            byte* inOpen = (byte*)s.InOpen.GetUnsafePtr();
            int* parentPtr = (int*)s.Parent.GetUnsafePtr();
            byte* blk = (byte*)blocked.GetUnsafeReadOnlyPtr();
            float* costPtr = cost.IsCreated ? (float*)cost.GetUnsafeReadOnlyPtr() : null;
            int w = s.Grid.Width;

            int pops = 0;
            while (pops < maxPops)
            {
                // Drain consistent nodes from top of heap
                while (!s.Open.IsEmpty)
                {
                    if (!s.Open.TryPeek(out var openTop)) return false;
                    if (gPtr[openTop.Id] != rhsPtr[openTop.Id]) break;
                    if (!s.Open.TryPop(out _)) return false;
                    inOpen[openTop.Id] = 0;
                }

                if (s.Open.IsEmpty) break;

                var topKey = CalculateKey(gPtr, rhsPtr, s.Start, s.Goal, s.Grid, s.Km, s.Start);
                if (!s.Open.TryPeek(out var openTop2)) return false;

                if (!LessOrEqual(openTop2.Key0, openTop2.Key1, topKey.x, topKey.y) &&
                    rhsPtr[s.Start] == gPtr[s.Start])
                    return true;

                if (!s.Open.TryPop(out var u)) return false;
                inOpen[u.Id] = 0;
                pops++;

                int uid = u.Id;
                var uKey = new float2(u.Key0, u.Key1);
                var trueKey = CalculateKey(gPtr, rhsPtr, s.Start, s.Goal, s.Grid, s.Km, uid);

                if (Less(uKey.x, uKey.y, trueKey.x, trueKey.y))
                {
                    if (!s.Open.TryInsertOrDecrease(new HeapNode(uid, trueKey.x, trueKey.y))) return false;
                    inOpen[uid] = 1;
                }
                else if (gPtr[uid] > rhsPtr[uid])
                {
                    gPtr[uid] = rhsPtr[uid];
                    if (!TryUpdateSuccessors(gPtr, rhsPtr, inOpen, parentPtr, blk, costPtr, ref s.Open, s.Grid, s.Goal, s.Start, s.Km, w, uid)) return false;
                }
                else
                {
                    gPtr[uid] = float.PositiveInfinity;
                    if (!TryUpdateVertex(gPtr, rhsPtr, inOpen, parentPtr, blk, costPtr, ref s.Open, s.Grid, s.Goal, s.Start, s.Km, uid)) return false;
                    if (!TryUpdateSuccessors(gPtr, rhsPtr, inOpen, parentPtr, blk, costPtr, ref s.Open, s.Grid, s.Goal, s.Start, s.Km, w, uid)) return false;
                }
            }

            return rhsPtr[s.Start] < float.PositiveInfinity;
        }

        [BurstCompile]
        public static bool TryExtractPath(ref DStarLiteState s, in NativeArray<byte> blocked, in NativeArray<float> cost, ref NativeList<int> path)
        {
            path.Clear();
            float* gPtr = (float*)s.G.GetUnsafePtr();
            byte* blk = (byte*)blocked.GetUnsafeReadOnlyPtr();
            float* costPtr = cost.IsCreated ? (float*)cost.GetUnsafeReadOnlyPtr() : null;
            int w = s.Grid.Width;

            float rhsStart = ((float*)s.RHS.GetUnsafePtr())[s.Start];
            if (rhsStart >= float.PositiveInfinity) return false;
            if (blk[s.Start] != 0) return false;

            int current = s.Start;
            path.Add(current);
            int maxSteps = s.Grid.Length * 2;

            while (current != s.Goal && maxSteps-- > 0)
            {
                int best = -1;
                float bestCost = float.PositiveInfinity;
                float bestG = float.PositiveInfinity;

                int2 cp = s.Grid.ToCoord(current);
                for (int d = 0; d < 8; d++)
                {
                    int2 np = cp + Grid2D.Dir8(d);
                    if (!s.Grid.InBounds(np)) continue;
                    int ni = s.Grid.ToIndex(np);
                    if (blk[ni] != 0) continue;

                    float edgeCost = GetEdgeCost(w, costPtr, current, ni, blk);
                    float total = edgeCost + gPtr[ni];
                    if (total < bestCost || (total == bestCost && gPtr[ni] < bestG))
                    {
                        bestCost = total;
                        bestG = gPtr[ni];
                        best = ni;
                    }
                }

                if (best < 0) break;
                current = best;
                path.Add(current);
            }
            return path.Length > 0 && path[path.Length - 1] == s.Goal;
        }

        public static void Dispose(ref DStarLiteState s)
        {
            if (s.G.IsCreated) s.G.Dispose();
            if (s.RHS.IsCreated) s.RHS.Dispose();
            if (s.Open.IsCreated) s.Open.Dispose();
            if (s.InOpen.IsCreated) s.InOpen.Dispose();
            if (s.Parent.IsCreated) s.Parent.Dispose();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float2 CalculateKey(float* gPtr, float* rhsPtr, int start, int goal, Grid2D grid, float km, int cell)
        {
            float minGRhs = math.min(gPtr[cell], rhsPtr[cell]);
            float h = Grid2D.HeuristicOctile(grid.ToCoord(start), grid.ToCoord(cell));
            return new float2(minGRhs + h + km, minGRhs);
        }

        private static bool TryUpdateVertex(
            float* gPtr, float* rhsPtr, byte* inOpen, int* parentPtr,
            byte* blk, float* costPtr,
            ref MinHeap open, Grid2D grid, int goal, int start, float km, int cell)
        {
            if (cell != goal)
            {
                float minRhs = float.PositiveInfinity;
                int2 cp = grid.ToCoord(cell);
                for (int d = 0; d < 8; d++)
                {
                    int2 np = cp + Grid2D.Dir8(d);
                    if (!grid.InBounds(np)) continue;
                    int ni = grid.ToIndex(np);
                    if (blk[ni] != 0) continue;

                    float edgeCost = GetEdgeCost(grid.Width, costPtr, cell, ni, blk);
                    float candidate = edgeCost + gPtr[ni];
                    if (candidate < minRhs)
                    {
                        minRhs = candidate;
                        parentPtr[cell] = ni;
                    }
                }
                rhsPtr[cell] = minRhs;
            }

            if (gPtr[cell] != rhsPtr[cell])
            {
                var key = CalculateKey(gPtr, rhsPtr, start, goal, grid, km, cell);
                if (!open.TryInsertOrDecrease(new HeapNode(cell, key.x, key.y))) return false;
                inOpen[cell] = 1;
            }
            else
            {
                open.TryRemove(cell);
                inOpen[cell] = 0;
            }
            return true;
        }

        private static bool TryUpdateSuccessors(
            float* gPtr, float* rhsPtr, byte* inOpen, int* parentPtr,
            byte* blk, float* costPtr,
            ref MinHeap open, Grid2D grid, int goal, int start, float km, int w, int cell)
        {
            int2 cp = grid.ToCoord(cell);
            for (int d = 0; d < 8; d++)
            {
                int2 np = cp + Grid2D.Dir8(d);
                if (!grid.InBounds(np)) continue;
                int ni = grid.ToIndex(np);
                if (blk[ni] != 0) continue;
                if (!TryUpdateVertex(gPtr, rhsPtr, inOpen, parentPtr, blk, costPtr, ref open, grid, goal, start, km, ni)) return false;
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float GetEdgeCost(int gridWidth, float* costPtr, int from, int to, byte* blk)
        {
            if (blk[to] != 0) return float.PositiveInfinity;
            if (costPtr != null)
                return (costPtr[from] + costPtr[to]) * 0.5f;

            int diff = math.abs(from - to);
            return math.select(1.414f, 1f, diff == 1 || diff == gridWidth);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool LessOrEqual(float k0a, float k1a, float k0b, float k1b)
        {
            return k0a != k0b ? k0a <= k0b : k1a <= k1b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool Less(float k0a, float k1a, float k0b, float k1b)
        {
            return k0a != k0b ? k0a < k0b : k1a < k1b;
        }
    }
}
