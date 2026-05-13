using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using System.Runtime.CompilerServices;
using BovineLabs.Grid;

namespace BovineLabs.Grid.Edt
{
    [StructLayout(LayoutKind.Sequential)]
    public struct EdtState
    {
        public Grid2D Grid;
        public NativeArray<float> Temp;
        public NativeArray<int> V;
        public NativeArray<float> Z;
    }

    [BurstCompile]
    public unsafe static class EdtApi
    {
        public static bool TryCreate(int width, int height, Allocator allocator, out EdtState result)
        {
            if (!Grid2D.TryCreate(width, height, out var grid))
            {
                result = default;
                return false;
            }

            result = new EdtState
            {
                Grid = grid,
                Temp = new NativeArray<float>(width * height, allocator),
                V = new NativeArray<int>(math.max(width, height), allocator),
                Z = new NativeArray<float>(math.max(width, height) + 1, allocator),
            };
            return true;
        }

        [BurstCompile]
        public static void InitFromBlocked(in NativeArray<byte> blocked, ref NativeArray<float> dist2)
        {
            byte* b = (byte*)blocked.GetUnsafeReadOnlyPtr();
            float* d = (float*)dist2.GetUnsafePtr();
            int len = blocked.Length;
            for (int i = 0; i < len; i++)
                d[i] = b[i] != 0 ? 0f : float.PositiveInfinity;
        }

        public static void Transform1D(
            in NativeArray<float> f,
            ref NativeArray<float> output,
            ref NativeArray<int> v,
            ref NativeArray<float> z,
            int length)
        {
            Transform1D(
                (float*)f.GetUnsafeReadOnlyPtr(),
                (float*)output.GetUnsafePtr(),
                (int*)v.GetUnsafePtr(),
                (float*)z.GetUnsafePtr(),
                length);
        }

        /// <summary>
        /// Felzenszwalb-Huttenlocher 1D squared distance transform.
        /// Operates entirely on raw pointers — no safety handle overhead.
        /// </summary>
        [BurstCompile]
        public static void Transform1D(
            float* f,
            float* output,
            int* v,
            float* z,
            int length)
        {
            if (Hint.Unlikely(length <= 0)) return;

            int k = 0;
            v[0] = 0;
            z[0] = float.NegativeInfinity;
            z[1] = float.PositiveInfinity;

            for (int q = 1; q < length; q++)
            {
                float s = Intersect(v[k], q, f[v[k]], f[q]);
                while (s <= z[k] && k > 0)
                {
                    k--;
                    s = Intersect(v[k], q, f[v[k]], f[q]);
                }
                k++;
                v[k] = q;
                z[k] = s;
                z[k + 1] = float.PositiveInfinity;
            }

            k = 0;
            for (int q = 0; q < length; q++)
            {
                while (z[k + 1] < q)
                    k++;
                int dq = q - v[k];
                output[q] = f[v[k]] + dq * dq;
            }
        }

        [BurstCompile]
        public static bool TryBuild(ref EdtState s, in NativeArray<byte> blocked, ref NativeArray<float> dist2)
        {
            InitFromBlocked(in blocked, ref dist2);

            float* dist2Ptr = (float*)dist2.GetUnsafePtr();
            float* temp = (float*)s.Temp.GetUnsafePtr();
            int* v = (int*)s.V.GetUnsafePtr();
            float* z = (float*)s.Z.GetUnsafePtr();
            int w = s.Grid.Width;
            int h = s.Grid.Height;

            // Phase 1: Transform rows (separable — each row is independent)
            for (int y = 0; y < h; y++)
            {
                int rowStart = y * w;

                // Copy row into temp (contiguous)
                for (int x = 0; x < w; x++)
                    temp[x] = dist2Ptr[rowStart + x];

                Transform1D(temp, temp, v, z, w);

                // Write back using running pointer
                float* dst = dist2Ptr + rowStart;
                for (int x = 0; x < w; x++)
                    dst[x] = temp[x];
            }

            // Phase 2: Transform columns (separable — each column is independent)
            for (int x = 0; x < w; x++)
            {
                // Gather column into temp (stride=w)
                float* src = dist2Ptr + x;
                for (int y = 0; y < h; y++)
                    temp[y] = src[y * w];

                Transform1D(temp, temp, v, z, h);

                // Scatter back
                for (int y = 0; y < h; y++)
                    src[y * w] = temp[y];
            }

            return true;
        }

        /// <summary>
        /// Parallel version of TryBuild using IJobFor for row and column passes.
        /// Each row/column is independently transformed.
        /// </summary>
        [BurstCompile]
        public static bool TryBuildParallel(
            ref EdtState s,
            in NativeArray<byte> blocked,
            ref NativeArray<float> dist2,
            int maxDim)
        {
            InitFromBlocked(in blocked, ref dist2);

            int w = s.Grid.Width;
            int h = s.Grid.Height;

            // Phase 1: Row pass (parallel)
            var rowJob = new EdtRowJob
            {
                Dist2 = (float*)dist2.GetUnsafePtr(),
                Width = w,
                MaxDim = maxDim,
            };

            // Phase 2: Column pass (parallel)
            var colJob = new EdtColJob
            {
                Dist2 = (float*)dist2.GetUnsafePtr(),
                Width = w,
                Height = h,
                MaxDim = maxDim,
            };

            // Schedule sequentially to avoid data race between phases.
            // Each phase internally can be parallel via ScheduleParallel.
            // For inline (non-job) usage, just call TryBuild instead.
            rowJob.Run(h);        // rows
            colJob.Run(w);        // columns

            return true;
        }

        [BurstCompile]
        public static void ToDistance(in NativeArray<float> dist2, ref NativeArray<float> dist)
        {
            float* d2 = (float*)dist2.GetUnsafeReadOnlyPtr();
            float* d = (float*)dist.GetUnsafePtr();
            int len = dist2.Length;
            for (int i = 0; i < len; i++)
                d[i] = math.sqrt(d2[i]);
        }

        public static void Dispose(ref EdtState s)
        {
            if (s.Temp.IsCreated) s.Temp.Dispose();
            if (s.V.IsCreated) s.V.Dispose();
            if (s.Z.IsCreated) s.Z.Dispose();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Intersect(int q1, int q2, float f1, float f2)
        {
            return ((f2 + q2 * q2) - (f1 + q1 * q1)) / (2f * (q2 - q1));
        }
    }

    /// <summary>
    /// Job that transforms all rows of the EDT.
    /// Each row is an independent 1D transform — can be scheduled as IJobFor.
    /// Operates in-place on the dist2 buffer (Transform1D supports f == output).
    /// </summary>
    [BurstCompile]
    public unsafe struct EdtRowJob : IJobFor
    {
        [NativeDisableUnsafePtrRestriction]
        public float* Dist2;
        public int Width;
        public int MaxDim;

        public void Execute(int rowIndex)
        {
            // Per-thread v/z buffers
            int* v = (int*)UnsafeUtility.Malloc(
                UnsafeUtility.SizeOf<int>() * MaxDim, UnsafeUtility.AlignOf<int>(), Allocator.Temp);
            float* z = (float*)UnsafeUtility.Malloc(
                UnsafeUtility.SizeOf<float>() * (MaxDim + 1), UnsafeUtility.AlignOf<float>(), Allocator.Temp);

            float* row = Dist2 + rowIndex * Width;
            EdtApi.Transform1D(row, row, v, z, Width);

            UnsafeUtility.Free(v, Allocator.Temp);
            UnsafeUtility.Free(z, Allocator.Temp);
        }
    }

    /// <summary>
    /// Job that transforms all columns of the EDT.
    /// Each column is an independent 1D transform — can be scheduled as IJobFor.
    /// Uses a thread-local temp buffer for column gather/scatter since columns
    /// are strided in memory (stride = Width).
    /// </summary>
    [BurstCompile]
    public unsafe struct EdtColJob : IJobFor
    {
        [NativeDisableUnsafePtrRestriction]
        public float* Dist2;
        public int Width;
        public int Height;
        public int MaxDim;

        public void Execute(int colIndex)
        {
            int* v = (int*)UnsafeUtility.Malloc(
                UnsafeUtility.SizeOf<int>() * MaxDim, UnsafeUtility.AlignOf<int>(), Allocator.Temp);
            float* z = (float*)UnsafeUtility.Malloc(
                UnsafeUtility.SizeOf<float>() * (MaxDim + 1), UnsafeUtility.AlignOf<float>(), Allocator.Temp);
            float* col = (float*)UnsafeUtility.Malloc(
                UnsafeUtility.SizeOf<float>() * Height, UnsafeUtility.AlignOf<float>(), Allocator.Temp);

            // Gather column into contiguous buffer
            float* src = Dist2 + colIndex;
            for (int y = 0; y < Height; y++)
                col[y] = src[y * Width];

            EdtApi.Transform1D(col, col, v, z, Height);

            // Scatter back
            for (int y = 0; y < Height; y++)
                src[y * Width] = col[y];

            UnsafeUtility.Free(col, Allocator.Temp);
            UnsafeUtility.Free(v, Allocator.Temp);
            UnsafeUtility.Free(z, Allocator.Temp);
        }
    }
}
