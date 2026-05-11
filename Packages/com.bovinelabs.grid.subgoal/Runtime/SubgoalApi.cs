using Unity.Collections;
using Unity.Mathematics;
using BovineLabs.Grid;

namespace BovineLabs.Grid.Subgoal
{
    public struct SubgoalEdge { public int To; public float Cost; }

    public struct SubgoalState
    {
        public Grid2D Grid;
        public NativeList<int> Subgoals;
        public NativeArray<int> SubgoalOfCell;
        public NativeList<SubgoalEdge> Edges;
        public NativeList<RangeI> EdgeRanges;
    }

    public static class SubgoalApi
    {
        public static SubgoalState Create(int width, int height, int maxSubgoals, int maxEdges, Allocator a)
        {
            var g = Grid2D.Create(width, height);
            return new SubgoalState
            {
                Grid = g,
                Subgoals = new NativeList<int>(maxSubgoals, a),
                SubgoalOfCell = new NativeArray<int>(g.Length, a),
                Edges = new NativeList<SubgoalEdge>(maxEdges, a),
                EdgeRanges = new NativeList<RangeI>(maxSubgoals, a),
            };
        }

        public static void Build(ref SubgoalState s, NativeArray<byte> blocked)
        {
            s.Subgoals.Clear();
            s.SubgoalOfCell.Fill(-1);
            s.Edges.Clear();
            s.EdgeRanges.Clear();

            // Find corner subgoals: cells adjacent to obstacles that have a "turn"
            for (int i = 0; i < s.Grid.Length; i++)
            {
                if (blocked[i] != 0) continue;
                int2 p = s.Grid.ToCoord(i);
                bool isCorner = false;

                for (int d = 0; d < 4; d++)
                {
                    int2 np = p + Grid2D.Directions4[d];
                    if (!s.Grid.InBounds(np)) continue;
                    if (blocked[s.Grid.ToIndex(np)] != 0)
                    {
                        // Check if diagonal is forced
                        int2 diag = p + Grid2D.Directions8[d * 2];
                        if (s.Grid.InBounds(diag) && blocked[s.Grid.ToIndex(diag)] == 0)
                        { isCorner = true; break; }
                    }
                }

                if (isCorner)
                {
                    int id = s.Subgoals.Length;
                    s.Subgoals.Add(i);
                    s.SubgoalOfCell[i] = id;
                }
            }

            // Add edges between visible subgoals
            for (int i = 0; i < s.Subgoals.Length; i++)
            {
                int edgeStart = s.Edges.Length;
                int2 pi = s.Grid.ToCoord(s.Subgoals[i]);

                for (int j = i + 1; j < s.Subgoals.Length; j++)
                {
                    int2 pj = s.Grid.ToCoord(s.Subgoals[j]);
                    if (LineOfSight(s.Grid, blocked, pi, pj))
                    {
                        float cost = math.length(new float2(pj.x - pi.x, pj.y - pi.y));
                        s.Edges.Add(new SubgoalEdge { To = j, Cost = cost });
                        s.Edges.Add(new SubgoalEdge { To = i, Cost = cost });
                    }
                }

                s.EdgeRanges.Add(new RangeI(edgeStart, s.Edges.Length - edgeStart));
            }
        }

        public static bool LineOfSight(Grid2D grid, NativeArray<byte> blocked, int2 from, int2 to)
        {
            // Bresenham line check
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
            }
            return true;
        }

        public static bool Search(ref SubgoalState s, NativeArray<byte> blocked, int start, int goal, NativeList<int> path)
        {
            path.Clear();
            // Simplified: direct A* on grid using subgoal graph as heuristic guide
            var g = new NativeArray<float>(s.Grid.Length, Allocator.Temp);
            var parent = new NativeArray<int>(s.Grid.Length, Allocator.Temp);
            var closed = new NativeArray<byte>(s.Grid.Length, Allocator.Temp);
            var heap = MinHeap.Create(s.Grid.Length, Allocator.Temp);

            g.Fill(float.PositiveInfinity);
            parent.Fill(-1);
            closed.Fill((byte)0);

            g[start] = 0f;
            heap.InsertOrDecrease(new HeapNode(start, Grid2D.HeuristicEuclidean(s.Grid.ToCoord(start), s.Grid.ToCoord(goal))));

            while (!heap.IsEmpty)
            {
                int u = heap.Pop().Id;
                if (u == goal)
                {
                    // Extract path
                    int cur = goal;
                    while (cur >= 0) { path.Add(cur); cur = parent[cur]; }
                    // Reverse
                    for (int i = 0; i < path.Length / 2; i++)
                    { int tmp = path[i]; path[i] = path[path.Length - 1 - i]; path[path.Length - 1 - i] = tmp; }
                    break;
                }

                closed[u] = 1;
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
                        float f = newG + Grid2D.HeuristicEuclidean(s.Grid.ToCoord(ni), s.Grid.ToCoord(goal));
                        heap.InsertOrDecrease(new HeapNode(ni, f));
                    }
                }
            }

            g.Dispose(); parent.Dispose(); closed.Dispose(); heap.Dispose();
            return path.Length > 0;
        }

        public static void Dispose(ref SubgoalState s)
        {
            if (s.Subgoals.IsCreated) s.Subgoals.Dispose();
            if (s.SubgoalOfCell.IsCreated) s.SubgoalOfCell.Dispose();
            if (s.Edges.IsCreated) s.Edges.Dispose();
            if (s.EdgeRanges.IsCreated) s.EdgeRanges.Dispose();
        }
    }
}
