using Unity.Collections;
using Unity.Mathematics;
using BovineLabs.Grid;

namespace BovineLabs.GraphCut
{
    public struct CutEdge
    {
        public int A;
        public int B;
        public int Capacity;
        public int Flow;
    }

    public struct GraphCutState
    {
        public Grid2D Grid;
        public NativeList<CutEdge> Edges;
        public NativeArray<int> Excess;
        public NativeArray<int> Height;
        public NativeArray<byte> SourceSide;
    }

    public static class GraphCutApi
    {
        public static GraphCutState Create(int width, int height, int maxEdges, Allocator a)
        {
            var g = Grid2D.Create(width, height);
            return new GraphCutState
            {
                Grid = g,
                Edges = new NativeList<CutEdge>(maxEdges, a),
                Excess = new NativeArray<int>(g.Length, a),
                Height = new NativeArray<int>(g.Length, a),
                SourceSide = new NativeArray<byte>(g.Length, a),
            };
        }

        public static void BuildBinaryEnergy(
            ref GraphCutState s,
            NativeArray<int> unary0,
            NativeArray<int> unary1,
            NativeArray<int> pairwise)
        {
            s.Edges.Clear();

            // Add source (label0) and sink (label1) edges as unary terms
            for (int i = 0; i < s.Grid.Length; i++)
            {
                if (unary0[i] > 0) AddEdge(ref s, i, -1, unary0[i]);   // source
                if (unary1[i] > 0) AddEdge(ref s, i, -2, unary1[i]);   // sink
            }

            // Add pairwise edges between 4-neighbors
            for (int i = 0; i < s.Grid.Length; i++)
            {
                int2 p = s.Grid.ToCoord(i);
                for (int d = 0; d < 2; d++) // only right and down to avoid duplicates
                {
                    int2 np = p + Grid2D.Directions4[d];
                    if (!s.Grid.InBounds(np)) continue;
                    int ni = s.Grid.ToIndex(np);
                    if (pairwise[i] > 0)
                    {
                        AddEdge(ref s, i, ni, pairwise[i]);
                        AddEdge(ref s, ni, i, pairwise[i]);
                    }
                }
            }
        }

        private static void AddEdge(ref GraphCutState s, int a, int b, int cap)
        {
            s.Edges.Add(new CutEdge { A = a, B = b, Capacity = cap, Flow = 0 });
        }

        public static bool MinCut(ref GraphCutState s)
        {
            // BFS-based min cut (Ford-Fulkerson with BFS = Edmonds-Karp)
            s.SourceSide.Fill((byte)0);
            int source = -1; // sentinel
            int sink = -2;

            // Build adjacency: for each edge with residual capacity, add to adjacency
            var parent = new NativeArray<int>(s.Grid.Length, Allocator.Temp);
            var queue = new NativeQueue<int>(Allocator.Temp);

            long maxFlow = 0;
            parent.Fill(-1);

            // BFS to find augmenting path
            while (true)
            {
                parent.Fill(-1);
                queue.Clear();
                // Find source-adjacent cells
                for (int i = 0; i < s.Edges.Length; i++)
                {
                    if (s.Edges[i].B == -1 && s.Edges[i].Flow < s.Edges[i].Capacity)
                    {
                        int cell = s.Edges[i].A;
                        parent[cell] = -3; // from source
                        queue.Enqueue(cell);
                    }
                }

                bool found = false;
                while (queue.TryDequeue(out int u))
                {
                    // Check if u connects to sink
                    for (int i = 0; i < s.Edges.Length; i++)
                    {
                        if (s.Edges[i].A == u && s.Edges[i].B == -2 && s.Edges[i].Flow < s.Edges[i].Capacity)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (found) { /* mark sink reached */ }

                    // Expand to neighbors with residual capacity
                    for (int i = 0; i < s.Edges.Length; i++)
                    {
                        if (s.Edges[i].A == u && s.Edges[i].B >= 0 && parent[s.Edges[i].B] == -1
                            && s.Edges[i].Flow < s.Edges[i].Capacity)
                        {
                            parent[s.Edges[i].B] = u;
                            queue.Enqueue(s.Edges[i].B);
                        }
                    }
                }

                if (!found) break;

                // Find bottleneck
                int bottleneck = int.MaxValue;
                for (int i = 0; i < s.Grid.Length; i++)
                    if (parent[i] >= 0) bottleneck = math.min(bottleneck, 1);

                if (bottleneck == int.MaxValue) break;
                maxFlow += bottleneck;
            }

            // Mark source side via BFS from source
            for (int i = 0; i < s.Edges.Length; i++)
            {
                if (s.Edges[i].B == -1 && s.Edges[i].Flow < s.Edges[i].Capacity)
                    s.SourceSide[s.Edges[i].A] = 1;
            }

            bool changed = true;
            while (changed)
            {
                changed = false;
                for (int i = 0; i < s.Edges.Length; i++)
                {
                    if (s.Edges[i].A >= 0 && s.Edges[i].B >= 0
                        && s.SourceSide[s.Edges[i].A] == 1 && s.SourceSide[s.Edges[i].B] == 0
                        && s.Edges[i].Flow < s.Edges[i].Capacity)
                    {
                        s.SourceSide[s.Edges[i].B] = 1;
                        changed = true;
                    }
                }
            }

            parent.Dispose();
            queue.Dispose();
            return true;
        }

        public static void ApplyCutLabels(ref GraphCutState s, NativeArray<int> labels, int label0, int label1)
        {
            for (int i = 0; i < s.Grid.Length; i++)
                labels[i] = s.SourceSide[i] == 1 ? label0 : label1;
        }

        public static void AlphaExpansion(ref GraphCutState s, NativeArray<int> labels, int alpha, NativeArray<int> unary, NativeArray<int> smooth)
        {
            // Build binary energy: keep current vs switch to alpha
            var unary0 = new NativeArray<int>(s.Grid.Length, Allocator.Temp);
            var unary1 = new NativeArray<int>(s.Grid.Length, Allocator.Temp);
            var pw = new NativeArray<int>(s.Grid.Length, Allocator.Temp);

            for (int i = 0; i < s.Grid.Length; i++)
            {
                unary0[i] = unary[i * 2 + labels[i]];
                unary1[i] = unary[i * 2 + alpha];
                pw[i] = smooth[i];
            }

            BuildBinaryEnergy(ref s, unary0, unary1, pw);
            MinCut(ref s);
            ApplyCutLabels(ref s, labels, alpha, labels[0]);

            unary0.Dispose(); unary1.Dispose(); pw.Dispose();
        }

        public static void Dispose(ref GraphCutState s)
        {
            if (s.Edges.IsCreated) s.Edges.Dispose();
            if (s.Excess.IsCreated) s.Excess.Dispose();
            if (s.Height.IsCreated) s.Height.Dispose();
            if (s.SourceSide.IsCreated) s.SourceSide.Dispose();
        }
    }
}
