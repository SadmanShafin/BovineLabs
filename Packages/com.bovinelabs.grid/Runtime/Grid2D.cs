using Unity.Mathematics;

namespace BovineLabs.Grid
{
    public struct Grid2D
    {
        public int Width;
        public int Height;
        public int Length;

        public void Setup(int width, int height)
        {
            Width = width;
            Height = height;
            Length = width * height;
        }

        public static bool TryCreate(int width, int height, out Grid2D result)
        {
            if (width < 0 || height < 0)
            {
                result = default;
                return false;
            }

            result = new Grid2D { Width = width, Height = height, Length = width * height };
            return true;
        }

        public static Grid2D Create(int width, int height)
        {
            TryCreate(width, height, out var result);
            return result;
        }

        public int ToIndex(int2 p) => p.y * Width + p.x;

        public int ToIndex(int x, int y) => y * Width + x;

        public int2 ToCoord(int index)
        {
            return new int2(index % Width, index / Width);
        }

        public bool InBounds(int2 p)
        {
            return (uint)p.x < (uint)Width && (uint)p.y < (uint)Height;
        }

        public bool InBounds(int index) => (uint)index < (uint)Length;

        public bool TryIndex(int2 p, out int index)
        {
            index = p.y * Width + p.x;
            return (uint)p.x < (uint)Width && (uint)p.y < (uint)Height;
        }

        public const int Dir4Count = 4;
        public const int Dir8Count = 8;

        public static int2 Dir4(int d)
        {
            return d switch
            {
                0 => new int2(1, 0),
                1 => new int2(0, 1),
                2 => new int2(-1, 0),
                _ => new int2(0, -1)
            };
        }

        public static int2 Dir8(int d)
        {
            return d switch
            {
                0 => new int2(1, 0),
                1 => new int2(1, 1),
                2 => new int2(0, 1),
                3 => new int2(-1, 1),
                4 => new int2(-1, 0),
                5 => new int2(-1, -1),
                6 => new int2(0, -1),
                _ => new int2(1, -1)
            };
        }

        public static float HeuristicManhattan(int2 a, int2 b) => math.abs(a.x - b.x) + math.abs(a.y - b.y);

        public static float HeuristicEuclidean(int2 a, int2 b) => math.length(new float2(a.x - b.x, a.y - b.y));

        public static float HeuristicOctile(int2 a, int2 b)
        {
            var dx = math.abs(a.x - b.x);
            var dy = math.abs(a.y - b.y);
            return dx + dy + (1.4142135f - 2f) * math.min(dx, dy);
        }
    }
}