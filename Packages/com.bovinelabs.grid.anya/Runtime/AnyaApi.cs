using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using BovineLabs.Grid;

namespace BovineLabs.Grid.Anya
{
    public struct AnyaNode
    {
        public double L;
        public double R;
        public int y;
        public int dy;
        public double2 Root;
        public double RootG;
        public int Parent;
    }

    public struct AnyaState
    {
        public Grid2D Grid;
        public MinHeap Heap;
        public UnsafeList<AnyaNode> Pool;
        public NativeArray<double> RootGCost;
    }

    [BurstCompile]
    public unsafe static class AnyaApi
    {
        private const double EPS = 1e-7;

        public static bool TryCreate(int width, int height, int maxNodes, Allocator a, out AnyaState result)
        {
            if (!Grid2D.TryCreate(width, height, out var g) || !MinHeap.TryCreate(maxNodes, a, out var heap))
            {
                result = default;
                return false;
            }

            result = new AnyaState
            {
                Grid = g,
                Heap = heap,
                Pool = new UnsafeList<AnyaNode>(maxNodes, a),
                RootGCost = new NativeArray<double>((width + 1) * (height + 1), a),
            };
            return true;
        }

        [BurstCompile]
        public static bool TrySearch(
            ref AnyaState s,
            in NativeArray<byte> blocked,
            ref int2 start,
            ref int2 goal,
            ref NativeList<int2> path)
        {
            path.Clear();
            s.Heap.Clear();
            s.Pool.Clear();
            s.RootGCost.Fill(double.PositiveInfinity);

            if (Hint.Unlikely(!s.Grid.InBounds(start) || !s.Grid.InBounds(goal))) return false;

            int w = s.Grid.Width;
            int h = s.Grid.Height;
            byte* blk = (byte*)blocked.GetUnsafeReadOnlyPtr();

            if (blk[start.y * w + start.x] != 0 || blk[goal.y * w + goal.x] != 0) return false;

            if (start.Equals(goal))
            {
                path.Add(start);
                return true;
            }

            s.RootGCost[start.y * (w + 1) + start.x] = 0.0;

            if (LineOfSight(w, h, blk, start, goal))
            {
                path.Add(start);
                path.Add(goal);
                return true;
            }

            int lInt = start.x;
            while (lInt > 0 && IsEdgePassable(lInt - 1, start.y, w, h, blk)) lInt--;

            int rInt = start.x;
            while (rInt < w && IsEdgePassable(rInt, start.y, w, h, blk)) rInt++;

            PushNode(ref s, lInt, rInt, start.y, 0, new double2(start.x, start.y), 0.0, -1, goal);

            double bestCost = double.PositiveInfinity;
            int bestNode = -1;

            while (!s.Heap.IsEmpty)
            {
                if (!s.Heap.TryPop(out var top)) break;
                if (top.Key0 >= bestCost) break;

                int uIdx = top.Id;
                AnyaNode u = s.Pool[uIdx];

                for (int dir = -1; dir <= 1; dir += 2)
                {
                    int ny = u.y + dir;
                    if (ny < 0 || ny > h) continue;

                double pL = u.L;
                double pR = u.R;
                if (u.Root.y != u.y)
                {
                    double ratio = (double)(ny - u.Root.y) / (u.y - u.Root.y);
                    pL = u.Root.x + (u.L - u.Root.x) * ratio;
                    pR = u.Root.x + (u.R - u.Root.x) * ratio;
                }
                else
                {
                    if (u.Root.x <= u.L + EPS) pR = double.PositiveInfinity;
                    else if (u.Root.x >= u.R - EPS) pL = double.NegativeInfinity;
                    else
                    {
                        pL = double.NegativeInfinity;
                        pR = double.PositiveInfinity;
                    }
                }

                int cellY = math.min(u.y, ny);
                if (cellY < 0 || cellY >= h) continue;

                int startX = math.max(0, (int)math.floor(pL));
                int endX = math.min(w - 1, (int)math.ceil(pR));

                for (int x = startX; x <= endX; x++)
                {
                    if (blk[cellY * w + x] != 0) continue;

                    double oL = math.max(pL, x);
                    double oR = math.min(pR, x + 1.0);
                    if (oL > oR + EPS) continue;

                    if (ny == goal.y && goal.x >= oL - EPS && goal.x <= oR + EPS)
                    {
                        double2 goalD = new double2(goal.x, goal.y);
                        double cost = u.RootG + math.distance(u.Root, goalD);
                        if (cost < bestCost)
                        {
                            bestCost = cost;
                            bestNode = s.Pool.Length;
                            s.Pool.Add(new AnyaNode
                            {
                                L = goal.x,
                                R = goal.x,
                                y = ny,
                                dy = 0,
                                Root = u.Root,
                                RootG = u.RootG,
                                Parent = uIdx,
                            });
                        }
                    }
                    else
                    {
                        PushNode(ref s, oL, oR, ny, u.dy, u.Root, u.RootG, uIdx, goal);
                    }
                }

                if (u.L > EPS && u.L < w - EPS && math.abs(u.L - math.round(u.L)) < EPS)
                {
                    int lIdx = (int)math.round(u.L);
                    if (lIdx > 0 && blk[cellY * w + (lIdx - 1)] != 0 && blk[cellY * w + lIdx] == 0 && u.Root.x > u.L + EPS)
                    {
                        double2 newRoot = new double2(lIdx, u.y);
                        double newG = u.RootG + math.distance(u.Root, newRoot);
                        int rIdx = u.y * (w + 1) + lIdx;
                        if (newG < s.RootGCost[rIdx])
                        {
                            s.RootGCost[rIdx] = newG;
                            int fL = lIdx;
                            while (fL > 0 && IsEdgePassable(fL - 1, u.y, w, h, blk)) fL--;
                            PushNode(ref s, fL, lIdx, u.y, 1, newRoot, newG, uIdx, goal);
                            PushNode(ref s, fL, lIdx, u.y, -1, newRoot, newG, uIdx, goal);
                        }
                    }
                }

                if (u.R > EPS && u.R < w - EPS && math.abs(u.R - math.round(u.R)) < EPS)
                {
                    int rIdx = (int)math.round(u.R);
                    if (rIdx < w && blk[cellY * w + rIdx] != 0 && rIdx > 0 && blk[cellY * w + (rIdx - 1)] == 0 && u.Root.x < u.R - EPS)
                    {
                        double2 newRoot = new double2(rIdx, u.y);
                        double newG = u.RootG + math.distance(u.Root, newRoot);
                        int rootIdx = u.y * (w + 1) + rIdx;
                        if (newG < s.RootGCost[rootIdx])
                        {
                            s.RootGCost[rootIdx] = newG;
                            int fR = rIdx;
                            while (fR < w && IsEdgePassable(fR, u.y, w, h, blk)) fR++;
                            PushNode(ref s, rIdx, fR, u.y, 1, newRoot, newG, uIdx, goal);
                            PushNode(ref s, rIdx, fR, u.y, -1, newRoot, newG, uIdx, goal);
                        }
                    }
                }
                } // end for dir
            } // end while

            if (bestNode < 0) return false;

            ExtractPath(in s.Pool, bestNode, goal, ref path);
            return true;
        }

        private static void PushNode(ref AnyaState s, double L, double R, int y, int dy, double2 root, double rootG, int parent, int2 goal)
        {
            if (s.Pool.Length >= s.Pool.Capacity) return;

            double xInt = goal.x;
            if (goal.y != root.y)
            {
                double ratio = (y - root.y) / (goal.y - root.y);
                xInt = root.x + (goal.x - root.x) * ratio;
            }

            double xOpt = math.max(L, math.min(R, xInt));
            double f = rootG + math.distance(root, new double2(xOpt, y)) + math.distance(new double2(xOpt, y), new double2(goal.x, goal.y));

            int idx = s.Pool.Length;
            s.Pool.Add(new AnyaNode
            {
                L = L,
                R = R,
                y = y,
                dy = dy,
                Root = root,
                RootG = rootG,
                Parent = parent,
            });
            s.Heap.TryInsertOrDecrease(new HeapNode(idx, (float)f));
        }

        private static bool IsEdgePassable(int x, int y, int w, int h, byte* blk)
        {
            bool below = x >= 0 && x < w && y >= 0 && y < h && blk[y * w + x] == 0;
            bool above = x >= 0 && x < w && y - 1 >= 0 && y - 1 < h && blk[(y - 1) * w + x] == 0;
            return below || above;
        }

        [BurstCompile]
        public static bool LineOfSight(int w, int h, [NoAlias] byte* blk, int2 from, int2 to)
        {
            int dx = math.abs(to.x - from.x);
            int dy = math.abs(to.y - from.y);
            int sx = from.x < to.x ? 1 : -1;
            int sy = from.y < to.y ? 1 : -1;
            int err = dx - dy;
            int x = from.x, y = from.y;

            while (true)
            {
                if (x < 0 || y < 0 || x >= w || y >= h) return false;
                if (blk[y * w + x] != 0) return false;
                if (x == to.x && y == to.y) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x += sx; }
                if (e2 < dx) { err += dx; y += sy; }
            }
            return true;
        }

        private static void ExtractPath(in UnsafeList<AnyaNode> pool, int nodeIdx, int2 goal, ref NativeList<int2> path)
        {
            path.Add(goal);
            int cur = nodeIdx;
            double2 lastRoot = new double2(-1, -1);
            while (cur >= 0)
            {
                AnyaNode node = pool[cur];
                if (math.distance(node.Root, lastRoot) > EPS)
                {
                    int2 pt = new int2((int)math.round(node.Root.x), (int)math.round(node.Root.y));
                    if (path.Length == 0 || !path[path.Length - 1].Equals(pt)) path.Add(pt);
                    lastRoot = node.Root;
                }
                cur = node.Parent;
            }

            for (int i = 0, j = path.Length - 1; i < j; i++, j--)
            {
                var tmp = path[i];
                path[i] = path[j];
                path[j] = tmp;
            }
        }

        public static void Dispose(ref AnyaState s)
        {
            if (s.Heap.IsCreated) s.Heap.Dispose();
            if (s.Pool.IsCreated) s.Pool.Dispose();
            if (s.RootGCost.IsCreated) s.RootGCost.Dispose();
        }
    }
}
