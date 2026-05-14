using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace BovineLabs.Grid.Continuum
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ContinuumCrowdState
    {
        public Grid2D Grid;
        public NativeArray<float> Density;
        public NativeArray<float> Speed;
        public NativeArray<float> Potential;
        public NativeArray<float2> Flow;
        public NativeArray<float> Divergence;
    }

    [BurstCompile]
    public static unsafe class ContinuumCrowdApi
    {
        public static bool TryCreate(int width, int height, Allocator a, out ContinuumCrowdState result)
        {
            if (!Grid2D.TryCreate(width, height, out var g))
            {
                result = default;
                return false;
            }

            result = new ContinuumCrowdState
            {
                Grid = g,
                Density = new NativeArray<float>(g.Length, a),
                Speed = new NativeArray<float>(g.Length, a),
                Potential = new NativeArray<float>(g.Length, a),
                Flow = new NativeArray<float2>(g.Length, a),
                Divergence = new NativeArray<float>(g.Length, a)
            };
            return true;
        }

        [BurstCompile]
        public static void ClearDensity(ref ContinuumCrowdState s)
        {
            var density = (float*)s.Density.GetUnsafePtr();
            var div = (float*)s.Divergence.GetUnsafePtr();
            var len = s.Grid.Length;
            for (var i = 0; i < len; i++)
            {
                density[i] = 0f;
                div[i] = 0f;
            }
        }

        [BurstCompile]
        public static void SplatAgents(ref ContinuumCrowdState s, in NativeArray<float2> positions)
        {
            var density = (float*)s.Density.GetUnsafePtr();
            var pos = (float2*)positions.GetUnsafeReadOnlyPtr();
            var w = s.Grid.Width;
            var h = s.Grid.Height;
            for (var i = 0; i < positions.Length; i++)
            {
                var cx = (int)math.floor(pos[i].x);
                var cy = (int)math.floor(pos[i].y);
                if (cx >= 0 && cx < w && cy >= 0 && cy < h)
                    density[cy * w + cx] += 1f;
            }
        }

        [BurstCompile]
        public static bool TrySolvePotential(ref ContinuumCrowdState s, in NativeArray<byte> blocked, int goal,
            int iterations)
        {
            if (!s.Grid.InBounds(goal) || !blocked.IsCreated) return false;

            var w = s.Grid.Width;
            var h = s.Grid.Height;
            var len = s.Grid.Length;

            var pot = (float*)s.Potential.GetUnsafePtr();
            var spd = (float*)s.Speed.GetUnsafePtr();
            var dens = (float*)s.Density.GetUnsafePtr();
            var blk = (byte*)blocked.GetUnsafeReadOnlyPtr();

            for (var i = 0; i < len; i++)
                pot[i] = float.PositiveInfinity;

            for (var i = 0; i < len; i++)
            {
                if (blk[i] == 1)
                {
                    spd[i] = 0f;
                    continue;
                }

                spd[i] = 1f / (1f + dens[i] * 0.1f);
            }

            pot[goal] = 0f;

            for (var iter = 0; iter < iterations; iter++)
            {
                var idx = 0;
                for (var y = 0; y < h; y++)
                for (var x = 0; x < w; x++)
                {
                    RelaxCell(pot, blk, spd, x, y, idx, w, h);
                    idx++;
                }

                idx = len - 1;
                for (var y = h - 1; y >= 0; y--)
                for (var x = w - 1; x >= 0; x--)
                {
                    RelaxCell(pot, blk, spd, x, y, idx, w, h);
                    idx--;
                }
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void RelaxCell(float* pot, byte* blk, float* spd, int x, int y, int idx, int w, int h)
        {
            if (blk[idx] == 1) return;
            var sp = spd[idx];
            if (Hint.Unlikely(sp <= 0f)) return;

            var invSpeed = 1f / sp;

            var tx = float.PositiveInfinity;
            var ty = float.PositiveInfinity;

            if (x > 0)
            {
                var v = pot[idx - 1];
                if (v < tx) tx = v;
            }

            if (x < w - 1)
            {
                var v = pot[idx + 1];
                if (v < tx) tx = v;
            }

            if (y > 0)
            {
                var v = pot[idx - w];
                if (v < ty) ty = v;
            }

            if (y < h - 1)
            {
                var v = pot[idx + w];
                if (v < ty) ty = v;
            }

            float tNew;
            if (float.IsPositiveInfinity(tx))
            {
                tNew = ty + invSpeed;
            }
            else if (float.IsPositiveInfinity(ty))
            {
                tNew = tx + invSpeed;
            }
            else
            {
                var diff = math.abs(tx - ty);
                tNew = diff < invSpeed
                    ? (tx + ty + math.sqrt(2f * invSpeed * invSpeed - diff * diff)) * 0.5f
                    : math.min(tx, ty) + invSpeed;
            }

            if (tNew < pot[idx]) pot[idx] = tNew;
        }

        [BurstCompile]
        public static bool TryBuildFlow(ref ContinuumCrowdState s)
        {
            var w = s.Grid.Width;
            var h = s.Grid.Height;
            var len = s.Grid.Length;

            var pot = (float*)s.Potential.GetUnsafePtr();
            var flow = (float2*)s.Flow.GetUnsafePtr();

            var idx = 0;
            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var grad = float2.zero;

                if (x > 0 && x < w - 1)
                    grad.x = (pot[idx + 1] - pot[idx - 1]) * 0.5f;
                else if (x > 0)
                    grad.x = pot[idx] - pot[idx - 1];
                else if (x < w - 1)
                    grad.x = pot[idx + 1] - pot[idx];

                if (y > 0 && y < h - 1)
                    grad.y = (pot[idx + w] - pot[idx - w]) * 0.5f;
                else if (y > 0)
                    grad.y = pot[idx] - pot[idx - w];
                else if (y < h - 1)
                    grad.y = pot[idx + w] - pot[idx];

                var lenSq = math.lengthsq(grad);
                flow[idx] = lenSq > 0f ? -grad * math.rsqrt(lenSq) : float2.zero;

                idx++;
            }

            return true;
        }

        [BurstCompile]
        public static void AdvectAgents(ref ContinuumCrowdState s, ref NativeArray<float2> positions, float dt)
        {
            var flow = (float2*)s.Flow.GetUnsafePtr();
            var pos = (float2*)positions.GetUnsafePtr();
            var w = s.Grid.Width;
            var h = s.Grid.Height;
            for (var i = 0; i < positions.Length; i++)
            {
                var cx = (int)math.floor(pos[i].x);
                var cy = (int)math.floor(pos[i].y);
                if (cx < 0 || cx >= w || cy < 0 || cy >= h) continue;
                pos[i] += flow[cy * w + cx] * dt;
            }
        }

        public static void Dispose(ref ContinuumCrowdState s)
        {
            if (s.Density.IsCreated) s.Density.Dispose();
            if (s.Speed.IsCreated) s.Speed.Dispose();
            if (s.Potential.IsCreated) s.Potential.Dispose();
            if (s.Flow.IsCreated) s.Flow.Dispose();
            if (s.Divergence.IsCreated) s.Divergence.Dispose();
        }
    }
}