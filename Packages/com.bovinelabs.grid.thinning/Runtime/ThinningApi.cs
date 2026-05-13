using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using BovineLabs.Grid;

namespace BovineLabs.Grid.Thinning
{
    public struct ThinningState
    {
        public Grid2D Grid;
        public NativeArray<byte> Mark;
        public UnsafeList<int> Frontier;
    }

    [BurstCompile]
    public unsafe static class ThinningApi
    {
        public static bool TryCreate(int width, int height, Allocator a, out ThinningState s)
        {
            s = default;
            if (!Grid2D.TryCreate(width, height, out var g)) return false;
            s = new ThinningState
            {
                Grid = g,
                Mark = new NativeArray<byte>(g.Length, a),
                Frontier = new UnsafeList<int>(g.Length, a),
            };
            return true;
        }

        [BurstCompile]
        public static bool TryIterate(ref ThinningState s, ref NativeArray<byte> solid, out int changed)
        {
            changed = 0;
            if (!TrySubIterate(ref s, ref solid, 0, out int c1)) return false;
            if (!TrySubIterate(ref s, ref solid, 1, out int c2)) return false;
            changed = c1 + c2;
            return true;
        }

        private static bool TrySubIterate(ref ThinningState s, ref NativeArray<byte> solid, int step, out int changed)
        {
            changed = 0;
            int width = s.Grid.Width;
            int height = s.Grid.Height;
            byte* solidPtr = (byte*)solid.GetUnsafePtr();
            byte* markPtr = (byte*)s.Mark.GetUnsafePtr();
            s.Mark.Fill((byte)0);

            var toDelete = new UnsafeList<int>(s.Grid.Length, Allocator.Temp);

            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    int i = y * width + x;
                    if (solidPtr[i] == 0) continue;

                    byte p2 = solidPtr[(y - 1) * width + x];
                    byte p3 = solidPtr[(y - 1) * width + (x + 1)];
                    byte p4 = solidPtr[y * width + (x + 1)];
                    byte p5 = solidPtr[(y + 1) * width + (x + 1)];
                    byte p6 = solidPtr[(y + 1) * width + x];
                    byte p7 = solidPtr[(y + 1) * width + (x - 1)];
                    byte p8 = solidPtr[y * width + (x - 1)];
                    byte p9 = solidPtr[(y - 1) * width + (x - 1)];

                    int b = p2 + p3 + p4 + p5 + p6 + p7 + p8 + p9;
                    if (b < 2 || b > 6) continue;

                    int a = 0;
                    if (p2 == 0 && p3 == 1) a++;
                    if (p3 == 0 && p4 == 1) a++;
                    if (p4 == 0 && p5 == 1) a++;
                    if (p5 == 0 && p6 == 1) a++;
                    if (p6 == 0 && p7 == 1) a++;
                    if (p7 == 0 && p8 == 1) a++;
                    if (p8 == 0 && p9 == 1) a++;
                    if (p9 == 0 && p2 == 1) a++;

                    if (a != 1) continue;

                    if (step == 0)
                    {
                        if (p2 * p4 * p6 != 0) continue;
                        if (p4 * p6 * p8 != 0) continue;
                    }
                    else
                    {
                        if (p2 * p4 * p8 != 0) continue;
                        if (p2 * p6 * p8 != 0) continue;
                    }

                    markPtr[i] = 1;
                    toDelete.Add(i);
                }
            }

            for (int i = 0; i < toDelete.Length; i++)
            {
                solidPtr[toDelete[i]] = 0;
            }

            changed = toDelete.Length;
            toDelete.Dispose();
            return true;
        }

        public static void Dispose(ref ThinningState s)
        {
            if (s.Mark.IsCreated) s.Mark.Dispose();
            s.Frontier.Dispose();
        }
    }
}
