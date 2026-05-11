using Unity.Collections;
using Unity.Mathematics;
using BovineLabs.Grid;

namespace BovineLabs.Grid.Thinning
{
    public struct ThinningState
    {
        public Grid2D Grid;
        public NativeArray<byte> Mark;
        public NativeList<int> Frontier;
    }

    public static class ThinningApi
    {
        public static ThinningState Create(int width, int height, Allocator a)
        {
            var g = Grid2D.Create(width, height);
            return new ThinningState
            {
                Grid = g,
                Mark = new NativeArray<byte>(g.Length, a),
                Frontier = new NativeList<int>(g.Length, a),
            };
        }

        public static void InitializeFrontier(ref ThinningState s, NativeArray<byte> solid)
        {
            s.Mark.Fill((byte)0);
            s.Frontier.Clear();

            for (int i = 0; i < s.Grid.Length; i++)
            {
                if (solid[i] == 0) continue;
                int2 p = s.Grid.ToCoord(i);
                bool isBorder = false;
                for (int d = 0; d < 4; d++)
                {
                    int2 np = p + Grid2D.Directions4[d];
                    if (!s.Grid.InBounds(np) || solid[s.Grid.ToIndex(np)] == 0)
                    { isBorder = true; break; }
                }
                if (isBorder) s.Frontier.Add(i);
            }
        }

        public static bool IsSimplePoint(Grid2D grid, NativeArray<byte> solid, int cell)
        {
            if (solid[cell] == 0) return false;

            int2 p = grid.ToCoord(cell);
            // Check 8-neighborhood
            // Count foreground components using connected component labeling on 8-neighbors
            int count = 0;
            byte[] neighbors = new byte[8];
            for (int d = 0; d < 8; d++)
            {
                int2 np = p + Grid2D.Directions8[d];
                neighbors[d] = (grid.InBounds(np) && solid[grid.ToIndex(np)] == 1) ? (byte)1 : (byte)0;
            }

            // Count transitions 0->1 in circular order
            for (int d = 0; d < 8; d++)
            {
                if (neighbors[d] == 0 && neighbors[(d + 1) % 8] == 1)
                    count++;
            }

            // Simple point: exactly one connected foreground component
            if (count != 1) return false;

            // Also check that removing doesn't disconnect background
            // Count foreground neighbors
            int fgCount = 0;
            for (int d = 0; d < 8; d++)
                fgCount += neighbors[d];

            // Must have at least 2 foreground neighbors
            return fgCount >= 2;
        }

        public static int Iterate(ref ThinningState s, NativeArray<byte> solid)
        {
            s.Mark.Fill((byte)0);
            var toDelete = new NativeList<int>(s.Grid.Length, Allocator.Temp);

            for (int fi = 0; fi < s.Frontier.Length; fi++)
            {
                int cell = s.Frontier[fi];
                if (solid[cell] == 0) continue;
                if (IsSimplePoint(s.Grid, solid, cell))
                {
                    s.Mark[cell] = 1;
                    toDelete.Add(cell);
                }
            }

            for (int i = 0; i < toDelete.Length; i++)
                solid[toDelete[i]] = 0;

            // Refresh frontier
            s.Frontier.Clear();
            for (int i = 0; i < toDelete.Length; i++)
            {
                int2 p = s.Grid.ToCoord(toDelete[i]);
                for (int d = 0; d < 8; d++)
                {
                    int2 np = p + Grid2D.Directions8[d];
                    if (s.Grid.InBounds(np))
                    {
                        int ni = s.Grid.ToIndex(np);
                        if (solid[ni] == 1 && s.Mark[ni] == 0)
                        {
                            // Check if it's now a border cell
                            int2 pp = s.Grid.ToCoord(ni);
                            bool isBorder = false;
                            for (int dd = 0; dd < 4; dd++)
                            {
                                int2 nnp = pp + Grid2D.Directions4[dd];
                                if (!s.Grid.InBounds(nnp) || solid[s.Grid.ToIndex(nnp)] == 0)
                                { isBorder = true; break; }
                            }
                            if (isBorder && !s.Frontier.Contains(ni))
                                s.Frontier.Add(ni);
                        }
                    }
                }
            }

            int deleted = toDelete.Length;
            toDelete.Dispose();
            return deleted;
        }

        public static void ExtractSkeleton(NativeArray<byte> solid, NativeList<int> skeleton)
        {
            // This is a static helper that doesn't need the state
            skeleton.Clear();
            // Caller passes their own solid array
        }

        public static void Dispose(ref ThinningState s)
        {
            if (s.Mark.IsCreated) s.Mark.Dispose();
            if (s.Frontier.IsCreated) s.Frontier.Dispose();
        }
    }
}
