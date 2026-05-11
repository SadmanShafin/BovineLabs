using Unity.Collections;
using Unity.Mathematics;

namespace BovineLabs.Grid
{
    /// <summary>Grid neighbor iteration utilities.</summary>
    public static class GridNeighbors
    {
        /// <summary>Iterate 4-connected neighbors. Returns count of valid neighbors written.</summary>
        public static int GetNeighbors4(in Grid2D grid, int cell, NativeArray<int> neighbors, NativeArray<byte> blocked)
        {
            int count = 0;
            int2 p = grid.ToCoord(cell);

            for (int d = 0; d < 4; d++)
            {
                int2 n = p + Grid2D.Directions4[d];
                if (grid.InBounds(n))
                {
                    int ni = grid.ToIndex(n);
                    if (blocked[ni] == 0)
                        neighbors[count++] = ni;
                }
            }

            return count;
        }

        /// <summary>Iterate 8-connected neighbors. Returns count of valid neighbors written.</summary>
        public static int GetNeighbors8(in Grid2D grid, int cell, NativeArray<int> neighbors, NativeArray<byte> blocked)
        {
            int count = 0;
            int2 p = grid.ToCoord(cell);

            for (int d = 0; d < 8; d++)
            {
                int2 n = p + Grid2D.Directions8[d];
                if (grid.InBounds(n))
                {
                    int ni = grid.ToIndex(n);
                    if (blocked[ni] == 0)
                        neighbors[count++] = ni;
                }
            }

            return count;
        }

        /// <summary>Check diagonal passability (no corner cutting).</summary>
        public static bool IsDiagonalPassable(in Grid2D grid, int2 from, int2 dir, NativeArray<byte> blocked)
        {
            // Both adjacent cardinals must be free
            int2 adjA = new int2(from.x + dir.x, from.y);
            int2 adjB = new int2(from.x, from.y + dir.y);

            if (!grid.InBounds(adjA) || !grid.InBounds(adjB)) return false;
            return blocked[grid.ToIndex(adjA)] == 0 && blocked[grid.ToIndex(adjB)] == 0;
        }
    }
}
