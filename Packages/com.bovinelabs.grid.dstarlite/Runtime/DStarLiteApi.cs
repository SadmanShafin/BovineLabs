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
            var key = CalculateKey(ref s, goal);
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
            return TryUpdateVertex(ref s, in blocked, in cost, cell);
        }

        [BurstCompile]
        public static bool TryRepair(ref DStarLiteState s, in NativeArray<byte> blocked, in NativeArray<float> cost, int maxPops)
        {
            int pops = 0;
            while (pops < maxPops)
            {
                while (!s.Open.IsEmpty)
                {
                    if (!s.Open.TryPeek(out var openTop)) return false;
                    if (s.G[openTop.Id] != s.RHS[openTop.Id]) break;
                    if (!s.Open.TryPop(out _)) return false;
                    s.InOpen[openTop.Id] = 0;
                }

                if (s.Open.IsEmpty) break;

                var topKey = CalculateKey(ref s, s.Start);
                if (!s.Open.TryPeek(out var openTop2)) return false;

                if (!LessOrEqual(openTop2.Key0, openTop2.Key1, topKey.x, topKey.y) &&
                    s.RHS[s.Start] == s.G[s.Start])
                    return true;

                if (!s.Open.TryPop(out var u)) return false;
                s.InOpen[u.Id] = 0;
                pops++;

                int uid = u.Id;
                var uKey = new float2(u.Key0, u.Key1);
                var trueKey = CalculateKey(ref s, uid);

                if (Less(uKey.x, uKey.y, trueKey.x, trueKey.y))
                {
                    if (!s.Open.TryInsertOrDecrease(new HeapNode(uid, trueKey.x, trueKey.y))) return false;
                    s.InOpen[uid] = 1;
                }
                else if (s.G[uid] > s.RHS[uid])
                {
                    s.G[uid] = s.RHS[uid];
                    if (!TryUpdateSuccessors(ref s, in blocked, in cost, uid)) return false;
                }
                else
                {
                    s.G[uid] = float.PositiveInfinity;
                    if (!TryUpdateVertex(ref s, in blocked, in cost, uid)) return false;
                    if (!TryUpdateSuccessors(ref s, in blocked, in cost, uid)) return false;
                }
            }

            return s.RHS[s.Start] < float.PositiveInfinity;
        }

        [BurstCompile]
        public static bool TryExtractPath(ref DStarLiteState s, in NativeArray<byte> blocked, in NativeArray<float> cost, ref NativeList<int> path)
        {
            path.Clear();
            if (s.RHS[s.Start] >= float.PositiveInfinity) return false;
            if (blocked[s.Start] != 0) return false;

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
                    if (blocked[ni] != 0) continue;

                    float edgeCost = GetEdgeCost(s.Grid.Width, in cost, current, ni, in blocked);
                    float total = edgeCost + s.G[ni];
                    if (total < bestCost || (total == bestCost && s.G[ni] < bestG))
                    {
                        bestCost = total;
                        bestG = s.G[ni];
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
            s.Open.Dispose();
            if (s.InOpen.IsCreated) s.InOpen.Dispose();
            if (s.Parent.IsCreated) s.Parent.Dispose();
        }

        private static float2 CalculateKey(ref DStarLiteState s, int cell)
        {
            float minGRhs = math.min(s.G[cell], s.RHS[cell]);
            float h = Grid2D.HeuristicOctile(s.Grid.ToCoord(s.Start), s.Grid.ToCoord(cell));
            return new float2(minGRhs + h + s.Km, minGRhs);
        }

        private static bool TryUpdateVertex(ref DStarLiteState s, in NativeArray<byte> blocked, in NativeArray<float> cost, int cell)
        {
            if (cell != s.Goal)
            {
                float minRhs = float.PositiveInfinity;
                int2 cp = s.Grid.ToCoord(cell);
                for (int d = 0; d < 8; d++)
                {
                    int2 np = cp + Grid2D.Dir8(d);
                    if (!s.Grid.InBounds(np)) continue;
                    int ni = s.Grid.ToIndex(np);
                    if (blocked[ni] != 0) continue;

                    float edgeCost = GetEdgeCost(s.Grid.Width, in cost, cell, ni, in blocked);
                    float candidate = edgeCost + s.G[ni];
                    if (candidate < minRhs)
                    {
                        minRhs = candidate;
                        s.Parent[cell] = ni;
                    }
                }
                s.RHS[cell] = minRhs;
            }

            if (s.G[cell] != s.RHS[cell])
            {
                var key = CalculateKey(ref s, cell);
                if (!s.Open.TryInsertOrDecrease(new HeapNode(cell, key.x, key.y))) return false;
                s.InOpen[cell] = 1;
            }
            else
            {
                s.Open.TryRemove(cell);
                s.InOpen[cell] = 0;
            }
            return true;
        }

        private static bool TryUpdateSuccessors(ref DStarLiteState s, in NativeArray<byte> blocked, in NativeArray<float> cost, int cell)
        {
            int2 cp = s.Grid.ToCoord(cell);
            for (int d = 0; d < 8; d++)
            {
                int2 np = cp + Grid2D.Dir8(d);
                if (!s.Grid.InBounds(np)) continue;
                int ni = s.Grid.ToIndex(np);
                if (blocked[ni] != 0) continue;
                if (!TryUpdateVertex(ref s, in blocked, in cost, ni)) return false;
            }
            return true;
        }

        private static float GetEdgeCost(int gridWidth, in NativeArray<float> cost, int from, int to, in NativeArray<byte> blocked)
        {
            if (blocked[to] != 0) return float.PositiveInfinity;
            if (cost.IsCreated && cost.Length > 0)
                return (cost[from] + cost[to]) * 0.5f;

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
