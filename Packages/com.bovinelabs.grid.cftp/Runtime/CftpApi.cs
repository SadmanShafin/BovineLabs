using Unity.Collections;
using Unity.Mathematics;
using BovineLabs.Grid;

namespace BovineLabs.Grid.Cftp
{
    public struct CftpUpdate
    {
        public int Cell;
        public uint RandomBits;
    }

    public struct CftpState
    {
        public Grid2D Grid;
        public NativeArray<byte> Low;
        public NativeArray<byte> High;
        public NativeList<CftpUpdate> Updates;
    }

    public static class CftpApi
    {
        public static CftpState Create(int width, int height, int maxUpdates, Allocator a)
        {
            var g = Grid2D.Create(width, height);
            return new CftpState
            {
                Grid = g,
                Low = new NativeArray<byte>(g.Length, a),
                High = new NativeArray<byte>(g.Length, a),
                Updates = new NativeList<CftpUpdate>(maxUpdates, a),
            };
        }

        public static void InitializeExtremes(ref CftpState s)
        {
            s.Low.Fill((byte)0);
            s.High.Fill((byte)1);
        }

        public static void GeneratePastUpdates(ref CftpState s, ref Unity.Mathematics.Random rng, int count)
        {
            s.Updates.Clear();
            for (int t = 0; t < count; t++)
            {
                for (int i = 0; i < s.Grid.Length; i++)
                {
                    s.Updates.Add(new CftpUpdate
                    {
                        Cell = i,
                        RandomBits = rng.NextUInt(),
                    });
                }
            }
        }

        public static void Replay(ref CftpState s)
        {
            InitializeExtremes(ref s);

            // Apply same updates to Low and High chains
            for (int i = 0; i < s.Updates.Length; i++)
            {
                var u = s.Updates[i];
                // Monotone transition: low chain uses min, high uses max
                byte lowBit = (byte)(u.RandomBits & 1);
                byte highBit = (byte)((u.RandomBits >> 1) & 1);
                s.Low[u.Cell] = lowBit;
                s.High[u.Cell] = highBit;
            }
        }

        public static bool Coalesced(ref CftpState s)
        {
            for (int i = 0; i < s.Grid.Length; i++)
            {
                if (s.Low[i] != s.High[i])
                    return false;
            }
            return true;
        }

        public static bool SampleExact(ref CftpState s, ref Unity.Mathematics.Random rng, NativeArray<byte> sample)
        {
            // Double past horizon until coalesced
            for (int attempt = 0; attempt < 20; attempt++)
            {
                GeneratePastUpdates(ref s, ref rng, 1 << attempt);
                Replay(ref s);
                if (Coalesced(ref s))
                {
                    NativeArray<byte>.Copy(s.Low, sample);
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
