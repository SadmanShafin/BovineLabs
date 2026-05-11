using Unity.Collections;
using Unity.Mathematics;
using BovineLabs.Grid;

namespace BovineLabs.Grid.Sipp
{
    public struct SafeInterval { public int Cell; public float Start; public float End; }

    public struct SippNode { public int Cell; public int Interval; public float Time; public float F; public int Parent; }

    public struct SippState
    {
        public Grid2D Grid;
        public NativeList<SafeInterval> Intervals;
        public NativeArray<RangeI> CellIntervals;
        public NativeList<SippNode> Nodes;
        public MinHeap Heap;
        public NativeArray<float> BestTime;
    }

    public static class SippApi
    {
        public static SippState Create(int width, int height, int maxIntervals, int maxNodes, Allocator a)
        {
            var g = Grid2D.Create(width, height);
            return new SippState
            {
                Grid = g,
                Intervals = new NativeList<SafeInterval>(maxIntervals, a),
                CellIntervals = new NativeArray<RangeI>(g.Length, a),
                Nodes = new NativeList<SippNode>(maxNodes, a),
                Heap = MinHeap.Create(maxNodes, a),
                BestTime = new NativeArray<float>(g.Length, a),
            };
        }

        public static void BuildSafeIntervals(ref SippState s)
        {
            s.Intervals.Clear();
            for (int i = 0; i < s.Grid.Length; i++)
            {
                s.CellIntervals[i] = new RangeI(s.Intervals.Length, 1);
                s.Intervals.Add(new SafeInterval { Cell = i, Start = 0f, End = float.PositiveInfinity });
            }
        }

        public static bool Search(
            ref SippState s,
            NativeArray<byte> blocked,
            int start, int goal,
            float startTime,
            NativeList<int> path)
        {
            s.Nodes.Clear();
            s.Heap.Clear();
            path.Clear();
            BuildSafeIntervals(ref s);

            for (int i = 0; i < s.BestTime.Length; i++) s.BestTime[i] = float.PositiveInfinity;

            int startNode = 0;
            s.Nodes.Add(new SippNode { Cell = start, Interval = 0, Time = startTime, F = startTime, Parent = -1 });
            s.Heap.InsertOrDecrease(new HeapNode(startNode, startTime));
            s.BestTime[start] = startTime;

            while (!s.Heap.IsEmpty)
            {
                int nodeId = s.Heap.Pop().Id;
                if (nodeId >= s.Nodes.Length) continue;
                var node = s.Nodes[nodeId];

                if (node.Time > s.BestTime[node.Cell]) continue;

                if (node.Cell == goal)
                {
                    int cur = nodeId;
                    while (cur >= 0) { path.Add(s.Nodes[cur].Cell); cur = s.Nodes[cur].Parent; }
                    for (int i = 0; i < path.Length / 2; i++)
                    { int tmp = path[i]; path[i] = path[path.Length - 1 - i]; path[path.Length - 1 - i] = tmp; }
                    return true;
                }

                int2 p = s.Grid.ToCoord(node.Cell);
                for (int d = 0; d < 4; d++)
                {
                    int2 np = p + Grid2D.Directions4[d];
                    if (!s.Grid.InBounds(np)) continue;
                    int ni = s.Grid.ToIndex(np);
                    if (blocked[ni] != 0) continue;

                    float arrivalTime = node.Time + 1f;
                    if (arrivalTime >= s.BestTime[ni]) continue;
                    s.BestTime[ni] = arrivalTime;

                    float h = Grid2D.HeuristicManhattan(s.Grid.ToCoord(ni), s.Grid.ToCoord(goal));
                    int newNodeId = s.Nodes.Length;
                    s.Nodes.Add(new SippNode { Cell = ni, Interval = 0, Time = arrivalTime, F = arrivalTime + h, Parent = nodeId });
                    s.Heap.InsertOrDecrease(new HeapNode(newNodeId, arrivalTime + h));
                }
            }

            return false;
        }

        public static void Dispose(ref SippState s)
        {
            if (s.Intervals.IsCreated) s.Intervals.Dispose();
            if (s.CellIntervals.IsCreated) s.CellIntervals.Dispose();
            if (s.Nodes.IsCreated) s.Nodes.Dispose();
            if (s.Heap.IsCreated) s.Heap.Dispose();
            if (s.BestTime.IsCreated) s.BestTime.Dispose();
        }
    }
}
