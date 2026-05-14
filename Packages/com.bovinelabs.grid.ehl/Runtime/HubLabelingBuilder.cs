using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace BovineLabs.Grid.EHL
{


    [BurstCompile]
    public struct HubLabelingBuilderJob : IJob
    {
        public NativeArray<ConvexVertex> ConvexVertices;
        public NativeArray<int> AdjOffsets;
        public NativeArray<int> AdjCounts;
        public NativeArray<AdjEdge> AdjEdges;


        public int VertexCount;


        public NativeList<VisibilityLabel> HubLabelsOut;


        public NativeList<int> HubOffsetsOut;
        public NativeList<int> HubCountsOut;


        public NativeList<long> SuccKeysOut;
        public NativeList<int> SuccValuesOut;


        public void Execute()
        {
            int n = VertexCount;
            if (n == 0) return;


            var dist = new NativeArray<float>(n * n, Allocator.Temp);
            var succ = new NativeArray<int>(n * n, Allocator.Temp);


            for (int i = 0; i < n * n; i++)
            {
                dist[i] = float.MaxValue;
                succ[i] = -1;
            }
            for (int i = 0; i < n; i++)
            {
                dist[i * n + i] = 0f;
                succ[i * n + i] = i;
            }


            for (int src = 0; src < n; src++)
            {
                RunDijkstra(src, n, ref dist, ref succ);
            }


            var covered = new NativeArray<bool>(n * n, Allocator.Temp);
            for (int i = 0; i < n * n; i++)
                covered[i] = false;


            var hubLabels = new NativeArray<NativeList<VisibilityLabel>>(n, Allocator.Temp);
            for (int i = 0; i < n; i++)
                hubLabels[i] = new NativeList<VisibilityLabel>(Allocator.Temp);


            int totalPairs = 0;
            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    if (dist[i * n + j] < float.MaxValue)
                        totalPairs++;
                }
            }

            int coveredCount = 0;
            int iterations = 0;

            while (coveredCount < totalPairs && iterations < n)
            {

                int bestHub = -1;
                int bestCoverage = 0;

                for (int h = 0; h < n; h++)
                {
                    int coverage = 0;
                    for (int i = 0; i < n; i++)
                    {
                        if (dist[i * n + h] >= float.MaxValue) continue;
                        for (int j = i + 1; j < n; j++)
                        {
                            if (covered[i * n + j]) continue;
                            if (dist[h * n + j] >= float.MaxValue) continue;


                            if (math.abs(dist[i * n + h] + dist[h * n + j] - dist[i * n + j]) < 1e-4f)
                            {
                                coverage++;
                            }
                        }
                    }

                    if (coverage > bestCoverage)
                    {
                        bestCoverage = coverage;
                        bestHub = h;
                    }
                }

                if (bestHub < 0 || bestCoverage == 0)
                    break;


                for (int v = 0; v < n; v++)
                {
                    if (dist[v * n + bestHub] < float.MaxValue)
                    {

                        bool alreadyPresent = false;
                        for (int k = 0; k < hubLabels[v].Length; k++)
                        {
                            if (hubLabels[v][k].HubVertexId == bestHub)
                            {
                                alreadyPresent = true;
                                break;
                            }
                        }

                        if (!alreadyPresent)
                        {
                            hubLabels[v].Add(new VisibilityLabel(
                                bestHub,
                                dist[v * n + bestHub],
                                succ[v * n + bestHub]
                            ));


                            SuccKeysOut.Add((long)v * n + bestHub);
                            SuccValuesOut.Add(succ[v * n + bestHub]);
                        }
                    }
                }


                for (int i = 0; i < n; i++)
                {
                    if (dist[i * n + bestHub] >= float.MaxValue) continue;
                    for (int j = i + 1; j < n; j++)
                    {
                        if (covered[i * n + j]) continue;
                        if (dist[bestHub * n + j] >= float.MaxValue) continue;

                        if (math.abs(dist[i * n + bestHub] + dist[bestHub * n + j] - dist[i * n + j]) < 1e-4f)
                        {
                            covered[i * n + j] = true;
                            covered[j * n + i] = true;
                            coveredCount++;
                        }
                    }
                }

                iterations++;
            }


            int offset = 0;
            for (int v = 0; v < n; v++)
            {
                hubLabels[v].Sort();
                HubOffsetsOut.Add(offset);
                HubCountsOut.Add(hubLabels[v].Length);
                offset += hubLabels[v].Length;

                for (int k = 0; k < hubLabels[v].Length; k++)
                {
                    HubLabelsOut.Add(hubLabels[v][k]);
                }

                hubLabels[v].Dispose();
            }

            hubLabels.Dispose();
            covered.Dispose();
            dist.Dispose();
            succ.Dispose();
        }


        private void RunDijkstra(int src, int n, ref NativeArray<float> dist, ref NativeArray<int> succ)
        {
            var visited = new NativeArray<bool>(n, Allocator.Temp);
            var queue = new NativeList<int>(Allocator.Temp);


            visited[src] = false;
            dist[src * n + src] = 0f;
            succ[src * n + src] = src;

            for (int iter = 0; iter < n; iter++)
            {

                float minDist = float.MaxValue;
                int u = -1;
                for (int i = 0; i < n; i++)
                {
                    if (!visited[i] && dist[src * n + i] < minDist)
                    {
                        minDist = dist[src * n + i];
                        u = i;
                    }
                }

                if (u < 0) break;
                visited[u] = true;


                int adjStart = AdjOffsets[u];
                int adjCount = AdjCounts[u];
                for (int e = 0; e < adjCount; e++)
                {
                    var edge = AdjEdges[adjStart + e];
                    int v = edge.TargetVertexId;
                    float newDist = dist[src * n + u] + edge.Distance;

                    if (newDist < dist[src * n + v])
                    {
                        dist[src * n + v] = newDist;


                        if (src == u)
                        {
                            succ[src * n + v] = v;
                        }
                        else
                        {
                            succ[src * n + v] = succ[src * n + u];
                        }
                    }
                }
            }

            visited.Dispose();
            queue.Dispose();
        }
    }


    public static class HubLabelingBuilder
    {
        public static JobHandle Build(
            NativeArray<ConvexVertex> convexVertices,
            NativeArray<int> adjOffsets,
            NativeArray<int> adjCounts,
            NativeArray<AdjEdge> adjEdges,
            out NativeList<VisibilityLabel> hubLabels,
            out NativeList<int> hubOffsets,
            out NativeList<int> hubCounts,
            out NativeList<long> succKeys,
            out NativeList<int> succValues,
            JobHandle dependency = default)
        {
            hubLabels = new NativeList<VisibilityLabel>(Allocator.Persistent);
            hubOffsets = new NativeList<int>(Allocator.Persistent);
            hubCounts = new NativeList<int>(Allocator.Persistent);
            succKeys = new NativeList<long>(Allocator.Persistent);
            succValues = new NativeList<int>(Allocator.Persistent);

            var job = new HubLabelingBuilderJob
            {
                ConvexVertices = convexVertices,
                AdjOffsets = adjOffsets,
                AdjCounts = adjCounts,
                AdjEdges = adjEdges,
                VertexCount = convexVertices.Length,
                HubLabelsOut = hubLabels,
                HubOffsetsOut = hubOffsets,
                HubCountsOut = hubCounts,
                SuccKeysOut = succKeys,
                SuccValuesOut = succValues,
            };

            return job.Schedule(dependency);
        }
    }
}
