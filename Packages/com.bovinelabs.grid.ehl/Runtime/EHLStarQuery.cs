using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace BovineLabs.Grid.EHL
{


    [BurstCompile]
    public struct EHLStarQueryJob : IJob
    {
        public EHLIndex Index;
        public float2 Source;
        public float2 Target;


        public NativeList<float> ResultDistance;
        public NativeList<float2> ResultWaypoints;
        public NativeList<int> ResultPathFound;

        public void Execute()
        {
            float bestDist = float.MaxValue;
            int bestHubId = -1;
            int bestViaSource = -1;
            int bestViaTarget = -1;


            if (IsDirectlyVisible(Source, Target, Index.ObstacleEdges))
            {
                bestDist = math.distance(Source, Target);
                bestHubId = -1;
                bestViaSource = -1;
                bestViaTarget = -1;
            }
            else
            {

                int cs = Index.CellIndex(Source);
                int ct = Index.CellIndex(Target);

                var cellS = Index.Cells[cs];
                var cellT = Index.Cells[ct];


                var labelsS = new NativeSlice<ViaLabel>(Index.ViaLabels, cellS.LabelStart, cellS.LabelCount);
                var labelsT = new NativeSlice<ViaLabel>(Index.ViaLabels, cellT.LabelStart, cellT.LabelCount);


                int i = 0, j = 0;
                while (i < labelsS.Length && j < labelsT.Length)
                {
                    int hubS = labelsS[i].HubVertexId;
                    int hubT = labelsT[j].HubVertexId;

                    if (hubS == hubT)
                    {

                        var labelS = labelsS[i];
                        var labelT = labelsT[j];


                        float2 visVertS = Index.ConvexVertices[labelS.VisibleVertexId].Position;
                        float distSrcToVis = math.distance(Source, visVertS);
                        float cellDistS = math.distance(cellS.Center, visVertS);
                        float dVisToHubS = labelS.HubDistance - cellDistS;
                        float vdistS = distSrcToVis + math.max(0f, dVisToHubS);

                        float2 visVertT = Index.ConvexVertices[labelT.VisibleVertexId].Position;
                        float distTgtToVis = math.distance(Target, visVertT);
                        float cellDistT = math.distance(cellT.Center, visVertT);
                        float dVisToHubT = labelT.HubDistance - cellDistT;
                        float vdistT = distTgtToVis + math.max(0f, dVisToHubT);

                        float totalDist = vdistS + vdistT;

                        if (totalDist < bestDist)
                        {
                            bestDist = totalDist;
                            bestHubId = hubS;
                            bestViaSource = labelS.VisibleVertexId;
                            bestViaTarget = labelT.VisibleVertexId;
                        }

                        i++;
                        j++;
                    }
                    else if (hubS < hubT)
                    {
                        i++;
                    }
                    else
                    {
                        j++;
                    }
                }
            }


            ResultDistance.Add(bestDist);

            if (bestDist < float.MaxValue)
            {
                ResultPathFound.Add(1);


                var waypoints = new NativeList<float2>(Allocator.Temp);
                waypoints.Add(Source);

                if (bestViaSource >= 0)
                {

                    float2 viaSPos = Index.ConvexVertices[bestViaSource].Position;
                    if (math.lengthsq(viaSPos - Source) > 1e-6f)
                        waypoints.Add(viaSPos);


                    if (bestViaSource != bestHubId && bestHubId >= 0)
                    {
                        var path = new NativeList<int>(Allocator.Temp);
                        int current = bestViaSource;
                        long key = (long)current * Index.ConvexVertices.Length + bestHubId;

                        int safety = 0;
                        while (current != bestHubId && safety < 1000)
                        {
                            if (Index.SuccessorMap.TryGetValue(key, out int next))
                            {
                                if (next == current || next < 0)
                                    break;
                                path.Add(next);
                                current = next;
                                key = (long)current * Index.ConvexVertices.Length + bestHubId;
                            }
                            else
                            {
                                break;
                            }
                            safety++;
                        }

                        for (int p = 0; p < path.Length; p++)
                        {
                            float2 wp = Index.ConvexVertices[path[p]].Position;

                            if (math.lengthsq(wp - waypoints[waypoints.Length - 1]) > 1e-6f)
                                waypoints.Add(wp);
                        }

                        path.Dispose();
                    }


                    if (bestHubId >= 0)
                    {
                        float2 hubPos = Index.ConvexVertices[bestHubId].Position;
                        if (math.lengthsq(hubPos - waypoints[waypoints.Length - 1]) > 1e-6f)
                            waypoints.Add(hubPos);
                    }


                    if (bestHubId != bestViaTarget && bestHubId >= 0)
                    {
                        var path2 = new NativeList<int>(Allocator.Temp);
                        int current = bestHubId;
                        long key = (long)current * Index.ConvexVertices.Length + bestViaTarget;

                        int safety = 0;
                        while (current != bestViaTarget && safety < 1000)
                        {
                            if (Index.SuccessorMap.TryGetValue(key, out int next))
                            {
                                if (next == current || next < 0)
                                    break;
                                path2.Add(next);
                                current = next;
                                key = (long)current * Index.ConvexVertices.Length + bestViaTarget;
                            }
                            else
                            {
                                break;
                            }
                            safety++;
                        }

                        for (int p = 0; p < path2.Length; p++)
                        {
                            float2 wp = Index.ConvexVertices[path2[p]].Position;
                            if (math.lengthsq(wp - waypoints[waypoints.Length - 1]) > 1e-6f)
                                waypoints.Add(wp);
                        }

                        path2.Dispose();
                    }


                    float2 viaTPos = Index.ConvexVertices[bestViaTarget].Position;
                    if (math.lengthsq(viaTPos - waypoints[waypoints.Length - 1]) > 1e-6f)
                        waypoints.Add(viaTPos);
                }


                if (math.lengthsq(Target - waypoints[waypoints.Length - 1]) > 1e-6f)
                    waypoints.Add(Target);

                for (int w = 0; w < waypoints.Length; w++)
                    ResultWaypoints.Add(waypoints[w]);

                waypoints.Dispose();
            }
            else
            {
                ResultPathFound.Add(0);
            }
        }


        private bool IsDirectlyVisible(float2 a, float2 b, NativeArray<ObstacleEdge> edges)
        {
            float2 ab = b - a;
            float lenSq = math.lengthsq(ab);
            if (lenSq < 1e-10f) return true;

            for (int e = 0; e < edges.Length; e++)
            {
                float2 c = edges[e].A;
                float2 d = edges[e].B;

                float2 d1 = ab;
                float2 d2 = d - c;
                float cross = d1.x * d2.y - d1.y * d2.x;

                const float eps = 1e-10f;
                if (math.abs(cross) < eps) continue;

                float2 d3 = c - a;
                float t = (d3.x * d2.y - d3.y * d2.x) / cross;
                float u = (d3.x * d1.y - d3.y * d1.x) / cross;

                const float margin = 1e-5f;
                if (t > margin && t < 1.0f - margin && u > margin && u < 1.0f - margin)
                    return false;
            }

            return true;
        }
    }


    public static class EHLStarQuery
    {


        public static EHLQueryResult Query(ref EHLIndex index, float2 source, float2 target)
        {
            var resultDist = new NativeList<float>(Allocator.Temp);
            var resultWP = new NativeList<float2>(Allocator.Temp);
            var resultFound = new NativeList<int>(Allocator.Temp);

            var job = new EHLStarQueryJob
            {
                Index = index,
                Source = source,
                Target = target,
                ResultDistance = resultDist,
                ResultWaypoints = resultWP,
                ResultPathFound = resultFound,
            };


            job.Execute();

            var result = new EHLQueryResult(Allocator.Persistent);
            if (resultFound.Length > 0 && resultFound[0] == 1)
            {
                result.PathFound = true;
                result.Distance = resultDist[0];
                for (int i = 0; i < resultWP.Length; i++)
                    result.Waypoints.Add(resultWP[i]);
            }
            else
            {
                result.PathFound = false;
                result.Distance = float.MaxValue;
            }

            resultDist.Dispose();
            resultWP.Dispose();
            resultFound.Dispose();

            return result;
        }


        public static JobHandle ScheduleQuery(
            ref EHLIndex index,
            float2 source,
            float2 target,
            out NativeList<float> resultDist,
            out NativeList<float2> resultWP,
            out NativeList<int> resultFound,
            JobHandle dependency = default)
        {
            resultDist = new NativeList<float>(Allocator.TempJob);
            resultWP = new NativeList<float2>(Allocator.TempJob);
            resultFound = new NativeList<int>(Allocator.TempJob);

            var job = new EHLStarQueryJob
            {
                Index = index,
                Source = source,
                Target = target,
                ResultDistance = resultDist,
                ResultWaypoints = resultWP,
                ResultPathFound = resultFound,
            };

            return job.Schedule(dependency);
        }
    }
}
