using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace BovineLabs.Grid.FieldDStar
{
    public struct FieldDStarState
    {
        public Grid2D Grid;
        public int Goal;
        public NativeArray<float> G;
        public NativeArray<float> RHS;
        public NativeArray<float2> Flow;
        public MinHeap Heap;
    }

    [BurstCompile]
    public static unsafe class FieldDStarApi
    {
        public static bool TryCreate(int width, int height, Allocator a, out FieldDStarState result)
        {
            if (!Grid2D.TryCreate(width, height, out var g))
            {
                result = default;
                return false;
            }

            if (!MinHeap.TryCreate(g.Length, a, out var heap))
            {
                result = default;
                return false;
            }

            result = new FieldDStarState
            {
                Grid = g,
                G = new NativeArray<float>(g.Length, a),
                RHS = new NativeArray<float>(g.Length, a),
                Flow = new NativeArray<float2>(g.Length, a),
                Heap = heap
            };
            return true;
        }

        public static bool TryReset(ref FieldDStarState s)
        {
            s.Heap.Clear();
            s.G.Fill(float.PositiveInfinity);
            s.RHS.Fill(float.PositiveInfinity);
            s.Flow.Fill(float2.zero);
            return true;
        }

        public static bool TrySetGoal(ref FieldDStarState s, int goal)
        {
            s.Goal = goal;
            s.RHS[goal] = 0f;
            return s.Heap.TryInsertOrDecrease(new HeapNode(goal, 0f));
        }

        [BurstCompile]
        public static bool TryStep(ref FieldDStarState s, in NativeArray<float> cost)
        {
            while (!s.Heap.IsEmpty)
            {
                if (!s.Heap.TryPeek(out var top)) return false;
                if (s.G[top.Id] != s.RHS[top.Id]) break;
                if (!s.Heap.TryPop(out _)) return false;
            }

            if (s.Heap.IsEmpty) return false;

            if (!s.Heap.TryPop(out var node)) return false;
            var u = node.Id;
            var gPtr = (float*)s.G.GetUnsafePtr();
            var rhsPtr = (float*)s.RHS.GetUnsafePtr();

            if (gPtr[u] > rhsPtr[u])
            {
                gPtr[u] = rhsPtr[u];
            }
            else
            {
                gPtr[u] = float.PositiveInfinity;
                UpdateRHS(ref s, in cost, u);
            }

            var p = s.Grid.ToCoord(u);
            var width = s.Grid.Width;
            var height = s.Grid.Height;

            for (var d = 0; d < 8; d++)
            {
                var np = p + Grid2D.Dir8(d);
                if (Hint.Likely(np.x >= 0 && np.y >= 0 && np.x < width && np.y < height))
                    UpdateRHS(ref s, in cost, np.y * width + np.x);
            }

            return !s.Heap.IsEmpty;
        }

        [BurstCompile]
        private static void UpdateRHS(ref FieldDStarState s, in NativeArray<float> cost, int cell)
        {
            if (Hint.Unlikely(cell == s.Goal)) return;

            var bestRHS = float.PositiveInfinity;
            var p = s.Grid.ToCoord(cell);
            var width = s.Grid.Width;
            var height = s.Grid.Height;
            var gPtr = (float*)s.G.GetUnsafePtr();
            var rhsPtr = (float*)s.RHS.GetUnsafePtr();
            var costPtr = (float*)cost.GetUnsafeReadOnlyPtr();

            for (var i = 0; i < 8; i++)
            {
                var n1 = p + Grid2D.Dir8(i);
                var n2 = p + Grid2D.Dir8((i + 1) % 8);

                if (Hint.Likely(n1.x >= 0 && n1.y >= 0 && n1.x < width && n1.y < height &&
                                n2.x >= 0 && n2.y >= 0 && n2.x < width && n2.y < height))
                {
                    var c = costPtr[cell];
                    var v1 = gPtr[n1.y * width + n1.x];
                    var v2 = gPtr[n2.y * width + n2.x];

                    var val = ComputeCost(v1, v2, c);
                    if (val < bestRHS) bestRHS = val;
                }
            }

            rhsPtr[cell] = bestRHS;

            if (gPtr[cell] != rhsPtr[cell])
            {
                var key = math.min(gPtr[cell], rhsPtr[cell]);
                s.Heap.TryInsertOrDecrease(new HeapNode(cell, key));
            }
            else
            {
                s.Heap.TryRemove(cell);
            }
        }

        private static float ComputeCost(float v1, float v2, float c)
        {
            if (float.IsPositiveInfinity(v1) && float.IsPositiveInfinity(v2)) return float.PositiveInfinity;

            if (v1 <= v2)
            {
                if (v2 - v1 <= c)
                {
                    var d = v2 - v1;
                    if (Hint.Likely(c > d + 1e-6f))
                    {
                        var x = d / math.sqrt(c * c - d * d);
                        if (Hint.Likely(x < 1.0f)) return c * math.sqrt(x * x + 1.0f) + v1 * (1.0f - x) + v2 * x;
                    }

                    return c * 1.4142135f + v1;
                }

                return c + v1;
            }

            if (v1 - v2 <= c)
            {
                var d = v1 - v2;
                if (Hint.Likely(c > d + 1e-6f))
                {
                    var x = d / math.sqrt(c * c - d * d);
                    if (Hint.Likely(x < 1.0f)) return c * math.sqrt(x * x + 1.0f) + v2 * (1.0f - x) + v1 * x;
                }

                return c * 1.4142135f + v2;
            }

            return c + v2;
        }

        [BurstCompile]
        public static bool TryExtractFlow(ref FieldDStarState s, in NativeArray<float> cost)
        {
            var cellCount = s.Grid.Length;
            var width = s.Grid.Width;
            var height = s.Grid.Height;
            var gPtr = (float*)s.G.GetUnsafeReadOnlyPtr();
            var flowPtr = (float2*)s.Flow.GetUnsafePtr();

            for (var i = 0; i < cellCount; i++)
            {
                var y = i / width;
                var x = i % width;
                var grad = float2.zero;

                for (var d = 0; d < 8; d++)
                {
                    var offset = Grid2D.Dir8(d);
                    var nx = x + offset.x;
                    var ny = y + offset.y;

                    if (Hint.Likely(nx >= 0 && ny >= 0 && nx < width && ny < height))
                    {
                        var ni = ny * width + nx;
                        var dir = math.normalize(new float2(offset.x, offset.y));
                        var diff = gPtr[i] - gPtr[ni];
                        grad += dir * diff;
                    }
                }

                var len = math.length(grad);
                flowPtr[i] = len > 0f ? grad / len : float2.zero;
            }

            return true;
        }

        public static void Dispose(ref FieldDStarState s)
        {
            if (s.G.IsCreated) s.G.Dispose();
            if (s.RHS.IsCreated) s.RHS.Dispose();
            if (s.Flow.IsCreated) s.Flow.Dispose();
            if (s.Heap.IsCreated) s.Heap.Dispose();
        }
    }
}