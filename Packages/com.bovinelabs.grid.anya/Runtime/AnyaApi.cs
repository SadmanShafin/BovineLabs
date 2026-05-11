using Unity.Collections;
using Unity.Mathematics;
using BovineLabs.Grid;

namespace BovineLabs.Grid.Anya
{
    public struct AnyaNode
    {
        public int2 Root;
        public int Row;
        public int XMin;
        public int XMax;
        public float G;
        public float F;
        public int Parent;
    }

    public struct AnyaState
    {
        public Grid2D Grid;
        public NativeList<AnyaNode> Nodes;
        public MinHeap Heap;
    }

    public static class AnyaApi
    {
        public static AnyaState Create(int width, int height, int maxNodes, Allocator a)
        {
            var g = Grid2D.Create(width, height);
            return new AnyaState
            {
                Grid = g,
                Nodes = new NativeList<AnyaNode>(maxNodes, a),
                Heap = MinHeap.Create(maxNodes, a),
            };
        }

        public static void Clear(ref AnyaState s)
        {
            s.Nodes.Clear();
            s.Heap.Clear();
        }

        public static bool Search(
            ref AnyaState s,
            NativeArray<byte> blocked,
            int2 start,
            int2 goal,
            NativeList<int2> path)
        {
            Clear(ref s);
            path.Clear();

            if (blocked[s.Grid.ToIndex(start)] != 0 || blocked[s.Grid.ToIndex(goal)] != 0) return false;

            // Simplified any-angle: direct line check first
            if (LineOfSight(s.Grid, blocked, start, goal))
            {
                path.Add(start);
                path.Add(goal);
                return true;
            }

            // A* on grid cells with any-angle line-of-sight optimization
            var g = new NativeArray<float>(s.Grid.Length, Allocator.Temp);
            var parent = new NativeArray<int>(s.Grid.Length, Allocator.Temp);
            var closed = new NativeArray<byte>(s.Grid.Length, Allocator.Temp);
            g.Fill(float.PositiveInfinity);
            parent.Fill(-1);
            closed.Fill((byte)0);

            int si = s.Grid.ToIndex(start);
            int gi = s.Grid.ToIndex(goal);
            g[si] = 0f;
            s.Heap.InsertOrDecrease(new HeapNode(si, Grid2D.HeuristicEuclidean(start, goal)));

            while (!s.Heap.IsEmpty)
            {
                int u = s.Heap.Pop().Id;
                if (u == gi)
                {
                    // Extract path
                    int cur = gi;
                    while (cur >= 0) { path.Add(s.Grid.ToCoord(cur)); cur = parent[cur]; }
                    // Reverse
                    for (int i = 0; i < path.Length / 2; i++)
                    {
                        var tmp = path[i]; path[i] = path[path.Length - 1 - i]; path[path.Length - 1 - i] = tmp;
                    }
                    break;
                }

                closed[u] = 1;

                // Try line-of-sight from parent's parent (any-angle optimization)
                if (parent[u] >= 0)
                {
                    int grandparent = parent[parent[u]];
                    if (grandparent >= 0)
                    {
                        int2 gpCoord = s.Grid.ToCoord(grandparent);
                        int2 uCoord = s.Grid.ToCoord(u);
                        if (LineOfSight(s.Grid, blocked, gpCoord, uCoord))
                        {
                            float newG = g[grandparent] + math.length(new float2(uCoord.x - gpCoord.x, uCoord.y - gpCoord.y));
                            if (newG < g[u])
                            {
                                g[u] = newG;
                                parent[u] = grandparent;
                            }
                        }
                    }
                }

                int2 up = s.Grid.ToCoord(u);
                for (int d = 0; d < 8; d++)
                {
                    int2 np = up + Grid2D.Directions8[d];
                    if (!s.Grid.InBounds(np)) continue;
                    int ni = s.Grid.ToIndex(np);
                    if (blocked[ni] != 0 || closed[ni] != 0) continue;

                    float cost = (d < 4) ? 1f : 1.414f;
                    float newG = g[u] + cost;
                    if (newG < g[ni])
                    {
                        g[ni] = newG;
                        parent[ni] = u;
                        float f = newG + Grid2D.HeuristicEuclidean(np, goal);
                        s.Heap.InsertOrDecrease(new HeapNode(ni, f));
                    }
                }
            }

            g.Dispose(); parent.Dispose(); closed.Dispose();
            return path.Length > 0;
        }

        public static bool LineOfSight(Grid2D grid, NativeArray<byte> blocked, int2 from, int2 to)
        {
            int dx = math.abs(to.x - from.x);
            int dy = math.abs(to.y - from.y);
            int sx = from.x < to.x ? 1 : -1;
            int sy = from.y < to.y ? 1 : -1;
            int err = dx - dy;
            int x = from.x, y = from.y;

            while (true)
            {
                if (blocked[grid.ToIndex(x, y)] != 0) return false;
                if (x == to.x && y == to.y) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x += sx; }
                if (e2 < dx) { err += dx; y += sy; }
                // Check we don't go OOB
                if (x < 0 || y < 0 || x >= grid.Width || y >= grid.Height) return false;
            }
            return true;
        }

        public static void Dispose(ref AnyaState s)
        {
            if (s.Nodes.IsCreated) s.Nodes.Dispose();
            if (s.Heap.IsCreated) s.Heap.Dispose();
        }
    }
}
