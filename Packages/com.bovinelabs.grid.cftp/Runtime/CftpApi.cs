using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using BovineLabs.Grid;

namespace BovineLabs.Grid.Cftp
{
    [StructLayout(LayoutKind.Sequential)]
    public struct CftpUpdate
    {
        public int Cell;
        public uint RandomBits;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CftpState
    {
        public Grid2D Grid;
        public NativeArray<byte> Low;
        public NativeArray<byte> High;
        public UnsafeList<CftpUpdate> Updates;
    }

    [BurstCompile]
    public unsafe static class CftpApi
    {
        public static bool TryCreate(int width, int height, int maxUpdates, Allocator a, out CftpState result)
        {
            if (!Grid2D.TryCreate(width, height, out var g))
            {
                result = default;
                return false;
            }

            result = new CftpState
            {
                Grid = g,
                Low = new NativeArray<byte>(g.Length, a),
                High = new NativeArray<byte>(g.Length, a),
                Updates = new UnsafeList<CftpUpdate>(maxUpdates, a),
            };
            return true;
        }

        [BurstCompile]
        public static void InitializeExtremes(ref CftpState s)
        {
            byte* low = (byte*)s.Low.GetUnsafePtr();
            byte* high = (byte*)s.High.GetUnsafePtr();
            int len = s.Grid.Length;
            for (int i = 0; i < len; i++)
            {
                low[i] = 0;
                high[i] = 1;
            }
        }

        [BurstCompile]
        public static bool TryGeneratePastUpdates(ref CftpState s, ref Unity.Mathematics.Random rng, int count)
        {
            s.Updates.Clear();
            int len = s.Grid.Length;
            int total = count * len;
            if (s.Updates.Capacity < total)
                s.Updates.SetCapacity(total);

            for (int t = 0; t < count; t++)
            {
                for (int i = 0; i < len; i++)
                {
                    s.Updates.Add(new CftpUpdate
                    {
                        Cell = i,
                        RandomBits = rng.NextUInt(),
                    });
                }
            }
            return true;
        }

        [BurstCompile]
        public static void Replay(ref CftpState s)
        {
            InitializeExtremes(ref s);

            byte* low = (byte*)s.Low.GetUnsafePtr();
            byte* high = (byte*)s.High.GetUnsafePtr();
            int w = s.Grid.Width;
            int h = s.Grid.Height;
            int len = s.Grid.Length;

            for (int i = 0; i < s.Updates.Length; i++)
            {
                var u = s.Updates[i];
                byte bit = (byte)(u.RandomBits & 1);
                int cell = u.Cell;
                int cx = cell % w;
                int cy = cell / w;

                int lowN = 0, highN = 0;
                if (cx + 1 < w) { lowN += low[cell + 1]; highN += high[cell + 1]; }
                if (cy + 1 < h) { lowN += low[cell + w]; highN += high[cell + w]; }
                if (cx > 0) { lowN += low[cell - 1]; highN += high[cell - 1]; }
                if (cy > 0) { lowN += low[cell - w]; highN += high[cell - w]; }

                low[cell] = (byte)((bit & math.select(0, 1, lowN >= 2)) & 1);
                high[cell] = (byte)((bit & math.select(0, 1, highN >= 2)) & 1);
            }
        }

        [BurstCompile]
        public static bool Coalesced(ref CftpState s)
        {
            byte* low = (byte*)s.Low.GetUnsafePtr();
            byte* high = (byte*)s.High.GetUnsafePtr();
            int len = s.Grid.Length;
            for (int i = 0; i < len; i++)
                if (low[i] != high[i]) return false;
            return true;
        }

        [BurstCompile]
        public static bool TrySampleExact(ref CftpState s, ref Unity.Mathematics.Random rng, ref NativeArray<byte> sample)
        {
            if (!sample.IsCreated || sample.Length < s.Grid.Length) return false;

            for (int attempt = 0; attempt < 20; attempt++)
            {
                TryGeneratePastUpdates(ref s, ref rng, 1 << attempt);
                Replay(ref s);
                if (Coalesced(ref s))
                {
                    UnsafeUtility.MemCpy(sample.GetUnsafePtr(), s.Low.GetUnsafeReadOnlyPtr(), s.Grid.Length);
                    return true;
                }
            }
            return false;
        }

        public static void Dispose(ref CftpState s)
        {
            if (s.Low.IsCreated) s.Low.Dispose();
            if (s.High.IsCreated) s.High.Dispose();
            if (s.Updates.IsCreated) s.Updates.Dispose();
        }
    }
}
