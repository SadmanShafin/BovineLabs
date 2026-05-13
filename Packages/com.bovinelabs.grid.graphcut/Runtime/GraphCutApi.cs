using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using BovineLabs.Grid;

namespace BovineLabs.Grid.GraphCut
{
    public struct GraphCutState
    {
        public Grid2D Grid;
        
        public UnsafeList<int> EdgeTo;
        public UnsafeList<int> EdgeCap;
        public UnsafeList<int> EdgeFlow;
        public UnsafeList<int> EdgeRev;
        public UnsafeList<int> EdgeNext;
        
        public NativeArray<int> EdgeHead;
        public NativeArray<int> Excess;
        public NativeArray<int> Height;
        public NativeArray<byte> SourceSide;
        public UnsafeList<int> ActiveNodes;
        public NativeArray<byte> InActiveNodes;
    }

    [BurstCompile]
    public unsafe static class GraphCutApi
    {
        private const int SourceOffset = 0;
        private const int SinkOffset = 1;

        public static bool TryCreate(int width, int height, int maxEdges, Allocator a, out GraphCutState result)
        {
            if (!Grid2D.TryCreate(width, height, out var g))
            {
                result = default;
                return false;
            }

            int nodeCount = g.Length + 2;
            result = new GraphCutState
            {
                Grid = g,
                EdgeTo = new UnsafeList<int>(maxEdges, a),
                EdgeCap = new UnsafeList<int>(maxEdges, a),
                EdgeFlow = new UnsafeList<int>(maxEdges, a),
                EdgeRev = new UnsafeList<int>(maxEdges, a),
                EdgeNext = new UnsafeList<int>(maxEdges, a),
                EdgeHead = new NativeArray<int>(nodeCount, a),
                Excess = new NativeArray<int>(nodeCount, a),
                Height = new NativeArray<int>(nodeCount, a),
                SourceSide = new NativeArray<byte>(g.Length, a),
                ActiveNodes = new UnsafeList<int>(nodeCount, a),
                InActiveNodes = new NativeArray<byte>(nodeCount, a),
            };
            return true;
        }

        [BurstCompile]
        public static void BuildBinaryEnergy(
            ref GraphCutState s,
            in NativeArray<int> unary0,
            in NativeArray<int> unary1,
            in NativeArray<int> pairwise)
        {
            s.EdgeTo.Clear();
            s.EdgeCap.Clear();
            s.EdgeFlow.Clear();
            s.EdgeRev.Clear();
            s.EdgeNext.Clear();
            s.EdgeHead.Fill(-1);

            int cellCount = s.Grid.Length;
            int sourceIdx = cellCount;
            int sinkIdx = cellCount + 1;

            int* unary0Ptr = (int*)unary0.GetUnsafeReadOnlyPtr();
            int* unary1Ptr = (int*)unary1.GetUnsafeReadOnlyPtr();
            int* pairwisePtr = (int*)pairwise.GetUnsafeReadOnlyPtr();

            for (int i = 0; i < cellCount; i++)
            {
                if (unary0Ptr[i] > 0) AddEdgeInternal(ref s, sourceIdx, i, unary0Ptr[i]);
                if (unary1Ptr[i] > 0) AddEdgeInternal(ref s, i, sinkIdx, unary1Ptr[i]);
            }

            int width = s.Grid.Width;
            int height = s.Grid.Height;
            for (int i = 0; i < cellCount; i++)
            {
                int y = i / width;
                int x = i % width;
                int w = pairwisePtr[i];
                if (w <= 0) continue;

                for (int d = 0; d < 2; d++)
                {
                    int2 offset = Grid2D.Dir4(d);
                    int nx = x + offset.x;
                    int ny = y + offset.y;
                    if (Hint.Likely(nx >= 0 && ny >= 0 && nx < width && ny < height))
                    {
                        int neighbor = ny * width + nx;
                        AddEdgeInternal(ref s, i, neighbor, w);
                        AddEdgeInternal(ref s, neighbor, i, w);
                    }
                }
            }
        }

        public static void AddEdgeInternal(ref GraphCutState s, int u, int v, int cap)
        {
            int uvIdx = s.EdgeTo.Length;
            int vuIdx = uvIdx + 1;

            s.EdgeTo.Add(v); s.EdgeCap.Add(cap); s.EdgeFlow.Add(0); s.EdgeRev.Add(vuIdx); s.EdgeNext.Add(s.EdgeHead[u]);
            s.EdgeHead[u] = uvIdx;

            s.EdgeTo.Add(u); s.EdgeCap.Add(0); s.EdgeFlow.Add(0); s.EdgeRev.Add(uvIdx); s.EdgeNext.Add(s.EdgeHead[v]);
            s.EdgeHead[v] = vuIdx;
        }

        [BurstCompile]
        public static bool TrySolve(ref GraphCutState s, int sourceCell, int sinkCell)
        {
            var u0 = new NativeArray<int>(s.Grid.Length, Allocator.Temp);
            var u1 = new NativeArray<int>(s.Grid.Length, Allocator.Temp);
            var pw = new NativeArray<int>(s.Grid.Length, Allocator.Temp);
            if (sourceCell >= 0 && sourceCell < u0.Length) u0[sourceCell] = 1000000;
            if (sinkCell >= 0 && sinkCell < u1.Length) u1[sinkCell] = 1000000;
            for (int i = 0; i < pw.Length; i++) pw[i] = 1;
            BuildBinaryEnergy(ref s, u0, u1, pw);
            bool result = TryMinCut(ref s);
            u0.Dispose(); u1.Dispose(); pw.Dispose();
            return result;
        }

        [BurstCompile]
        public static bool TryMinCut(ref GraphCutState s)
        {
            int cellCount = s.Grid.Length;
            int nodeCount = cellCount + 2;
            int source = cellCount;
            int sink = cellCount + 1;

            s.Excess.Fill(0);
            s.Height.Fill(0);
            s.ActiveNodes.Clear();
            s.InActiveNodes.Fill((byte)0);

            int* excessPtr = (int*)s.Excess.GetUnsafePtr();
            int* heightPtr = (int*)s.Height.GetUnsafePtr();
            int* headPtr = (int*)s.EdgeHead.GetUnsafePtr();
            byte* inActivePtr = (byte*)s.InActiveNodes.GetUnsafePtr();
            int* toPtr = s.EdgeTo.Ptr;
            int* capPtr = s.EdgeCap.Ptr;
            int* flowPtr = s.EdgeFlow.Ptr;
            int* revPtr = s.EdgeRev.Ptr;
            int* nextPtr = s.EdgeNext.Ptr;

            heightPtr[source] = nodeCount;

            for (int e = headPtr[source]; e != -1; e = nextPtr[e])
            {
                int v = toPtr[e];
                int cap = capPtr[e];
                flowPtr[e] = cap;
                flowPtr[revPtr[e]] = -cap;
                excessPtr[v] = cap;
                excessPtr[source] -= cap;
                if (v != sink && v != source && inActivePtr[v] == 0)
                {
                    s.ActiveNodes.Add(v);
                    inActivePtr[v] = 1;
                }
            }

            while (s.ActiveNodes.Length > 0)
            {
                int u = s.ActiveNodes[s.ActiveNodes.Length - 1];
                s.ActiveNodes.RemoveAtSwapBack(s.ActiveNodes.Length - 1);
                inActivePtr[u] = 0;

                while (excessPtr[u] > 0)
                {
                    int minHeight = int.MaxValue;
                    bool pushed = false;

                    for (int e = headPtr[u]; e != -1; e = nextPtr[e])
                    {
                        int v = toPtr[e];
                        int resid = capPtr[e] - flowPtr[e];
                        if (resid > 0)
                        {
                            if (heightPtr[u] == heightPtr[v] + 1)
                            {
                                int push = math.min(excessPtr[u], resid);
                                flowPtr[e] += push;
                                flowPtr[revPtr[e]] -= push;
                                excessPtr[u] -= push;
                                excessPtr[v] += push;
                                if (v != source && v != sink && inActivePtr[v] == 0)
                                {
                                    s.ActiveNodes.Add(v);
                                    inActivePtr[v] = 1;
                                }
                                pushed = true;
                                if (excessPtr[u] == 0) break;
                            }
                            minHeight = math.min(minHeight, heightPtr[v]);
                        }
                    }

                    if (!pushed)
                    {
                        heightPtr[u] = (minHeight == int.MaxValue) ? heightPtr[u] : minHeight + 1;
                    }
                }
            }

            s.SourceSide.Fill((byte)0);
            byte* sidePtr = (byte*)s.SourceSide.GetUnsafePtr();
            var queue = new NativeQueue<int>(Allocator.Temp);
            queue.Enqueue(source);
            
            var visited = new NativeArray<byte>(nodeCount, Allocator.Temp);
            visited[source] = 1;

            while (queue.TryDequeue(out int u))
            {
                for (int e = headPtr[u]; e != -1; e = nextPtr[e])
                {
                    int v = toPtr[e];
                    if (visited[v] == 0 && capPtr[e] - flowPtr[e] > 0)
                    {
                        visited[v] = 1;
                        if (v < cellCount) sidePtr[v] = 1;
                        queue.Enqueue(v);
                    }
                }
            }
            
            visited.Dispose();
            queue.Dispose();
            return true;
        }

        [BurstCompile]
        public static bool TryExtractCutLabels(ref GraphCutState s, ref NativeArray<int> labels, int label0, int label1)
        {
            int* labelsPtr = (int*)labels.GetUnsafePtr();
            byte* sidePtr = (byte*)s.SourceSide.GetUnsafePtr();
            int len = s.Grid.Length;
            for (int i = 0; i < len; i++)
                labelsPtr[i] = sidePtr[i] == 1 ? label0 : label1;
            return true;
        }

        public static void Dispose(ref GraphCutState s)
        {
            s.EdgeTo.Dispose();
            s.EdgeCap.Dispose();
            s.EdgeFlow.Dispose();
            s.EdgeRev.Dispose();
            s.EdgeNext.Dispose();
            if (s.EdgeHead.IsCreated) s.EdgeHead.Dispose();
            if (s.Excess.IsCreated) s.Excess.Dispose();
            if (s.Height.IsCreated) s.Height.Dispose();
            if (s.SourceSide.IsCreated) s.SourceSide.Dispose();
            s.ActiveNodes.Dispose();
            if (s.InActiveNodes.IsCreated) s.InActiveNodes.Dispose();
        }
    }
}
