using Unity.Collections;
using Unity.Mathematics;
using BovineLabs.Grid;

namespace BovineLabs.Grid.Kasteleyn
{
    public struct KasteleynState
    {
        public Grid2D Grid;
        public NativeArray<byte> Region;
        public NativeList<int2> Edges;
        public NativeArray<double> Matrix;
        public int VertexCount;
    }

    public static class KasteleynApi
    {
        public static KasteleynState Create(int width, int height, int maxEdges, Allocator a)
        {
            var g = Grid2D.Create(width, height);
            return new KasteleynState
            {
                Grid = g,
                Region = new NativeArray<byte>(g.Length, a),
                Edges = new NativeList<int2>(maxEdges, a),
                Matrix = new NativeArray<double>(g.Length * g.Length, a),
                VertexCount = 0,
            };
        }

        public static void SetRegion(ref KasteleynState s, NativeArray<byte> region)
        {
            NativeArray<byte>.Copy(region, s.Region);
        }

        public static void BuildPlanarGraph(ref KasteleynState s)
        {
            s.Edges.Clear();

            // Map cells to compact vertex ids
            var vertexId = new NativeArray<int>(s.Grid.Length, Allocator.Temp);
            vertexId.Fill(-1);
            int count = 0;
            for (int i = 0; i < s.Grid.Length; i++)
            {
                if (s.Region[i] != 0)
                    vertexId[i] = count++;
            }
            s.VertexCount = count;

            // Add edges (right and down only, to avoid duplicates)
            for (int i = 0; i < s.Grid.Length; i++)
            {
                if (vertexId[i] < 0) continue;
                int2 p = s.Grid.ToCoord(i);
                for (int d = 0; d < 2; d++)
                {
                    int2 np = p + Grid2D.Directions4[d];
                    if (!s.Grid.InBounds(np)) continue;
                    int ni = s.Grid.ToIndex(np);
                    if (vertexId[ni] < 0) continue;
                    s.Edges.Add(new int2(vertexId[i], vertexId[ni]));
                }
            }
            vertexId.Dispose();
        }

        public static void OrientKasteleyn(ref KasteleynState s)
        {
            s.Matrix.Fill(0.0);
            // Kasteleyn orientation: assign +1 or -1 to edges
            // Simple rule for grid: checkerboard sign
            for (int i = 0; i < s.Edges.Length; i++)
            {
                int2 e = s.Edges[i];
                int sign = ((e.x + e.y) % 2 == 0) ? 1 : -1;
                s.Matrix[e.x * s.VertexCount + e.y] = sign;
                s.Matrix[e.y * s.VertexCount + e.x] = -sign;
            }
        }

        public static bool CountPerfectMatchings(ref KasteleynState s, out double count)
        {
            count = 0.0;
            if (s.VertexCount == 0) return false;

            // Compute determinant of skew-symmetric matrix using cofactor expansion (slow but correct)
            // For small matrices only
            if (s.VertexCount > 20) return false;

            // Simple determinant via Gaussian elimination on copy
            int n = s.VertexCount;
            var mat = new NativeArray<double>(n * n, Allocator.Temp);
            NativeArray<double>.Copy(s.Matrix, mat);

            double det = 1.0;
            for (int col = 0; col < n; col++)
            {
                // Find pivot
                int pivot = -1;
                for (int row = col; row < n; row++)
                {
                    if (math.abs(mat[row * n + col]) > 1e-10) { pivot = row; break; }
                }
                if (pivot < 0) { det = 0; break; }

                // Swap rows
                if (pivot != col)
                {
                    det = -det;
                    for (int j = 0; j < n; j++)
                    {
                        double tmp = mat[col * n + j];
                        mat[col * n + j] = mat[pivot * n + j];
                        mat[pivot * n + j] = tmp;
                    }
                }

                det *= mat[col * n + col];

                // Eliminate
                for (int row = col + 1; row < n; row++)
                {
                    double factor = mat[row * n + col] / mat[col * n + col];
                    for (int j = col; j < n; j++)
                        mat[row * n + j] -= factor * mat[col * n + j];
                }
            }

            count = math.sqrt(math.abs(det));
            mat.Dispose();
            return true;
        }

        public static void Dispose(ref KasteleynState s)
        {
            if (s.Region.IsCreated) s.Region.Dispose();
            if (s.Edges.IsCreated) s.Edges.Dispose();
            if (s.Matrix.IsCreated) s.Matrix.Dispose();
        }
    }
}
