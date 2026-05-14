using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace BovineLabs.Grid.Wavestar
{


    [BurstCompile]
    public struct MultiResThetaStarJob : IJob
    {


        public int3 startPos;


        public int3 goalPos;


        public float epsilon;


        public int maxHeight;


        public int minHeight;


        public int sizeX;
        public int sizeY;
        public int sizeZ;


        public NativeArray<int> obstacleGrid;


        public NativeParallelHashMap<int, SubvolumeData> costField;


        public NativeArray<bool> pathFound;


        public NativeArray<float> goalGCost;


        private NativeObstacleMap obstacleMap;

        public void Execute()
        {
            obstacleMap = new NativeObstacleMap(obstacleGrid, sizeX, sizeY, sizeZ);
            pathFound[0] = false;
            goalGCost[0] = float.PositiveInfinity;


            var open = new NativeMinPQ(Allocator.Temp);

            var closed = new NativeHashSet<int>(sizeX * sizeY, Allocator.Temp);

            var fScores = new NativeHashMap<int, float>(sizeX * sizeY, Allocator.Temp);


            OctreeIndex startSV = FindFinestSubvolume(startPos, maxHeight);
            OctreeIndex goalSV = FindFinestSubvolume(goalPos, maxHeight);


            float3 startCenter = startSV.Center;
            float startG = 0f;
            float startH = math.distance(startCenter, (float3)goalPos);
            float startF = startG + startH;

            var startData = new SubvolumeData(startPos.x, startPos.y, startPos.z, startG);
            costField.TryAdd(startSV.MortonCode, startData);
            open.Push(new OpenSetElement(startSV, startF));
            fScores[startSV.MortonCode] = startF;

            int iterations = 0;
            int maxIterations = sizeX * sizeY * sizeZ * 4;

            while (open.Count > 0 && iterations < maxIterations)
            {
                iterations++;
                var current = open.Pop();
                var currentIdx = current.index;


                if (closed.Contains(currentIdx.MortonCode))
                    continue;


                closed.Add(currentIdx.MortonCode);


                if (currentIdx.Contains(goalPos))
                {
                    pathFound[0] = true;
                    if (costField.TryGetValue(currentIdx.MortonCode, out var goalData))
                        goalGCost[0] = goalData.gCost;
                    open.Dispose();
                    closed.Dispose();
                    fScores.Dispose();
                    return;
                }


                if (!costField.TryGetValue(currentIdx.MortonCode, out var currentData))
                    continue;


                var neighbors = CollectNeighbors(currentIdx, closed);
                for (int i = 0; i < neighbors.Length; i++)
                {
                    var neighborIdx = neighbors[i];
                    UpdateSubvolume(ref open, ref fScores, ref closed, currentIdx, currentData, neighborIdx, goalSV);
                }
                neighbors.Dispose();
            }


            if (!pathFound[0] && costField.TryGetValue(goalSV.MortonCode, out var gData))
            {
                pathFound[0] = true;
                goalGCost[0] = gData.gCost;
            }

            open.Dispose();
            closed.Dispose();
            fScores.Dispose();
        }


        private OctreeIndex FindFinestSubvolume(int3 pos, int startHeight)
        {

            for (int h = startHeight; h >= 0; h--)
            {
                int s = 1 << h;
                int sx = pos.x >> h;
                int sy = pos.y >> h;
                int sz = pos.z >> h;
                var sv = new OctreeIndex(sx, sy, sz, h);
                if (obstacleMap.IsSubvolumeTraversable(sv))
                    return sv;
            }


            return new OctreeIndex(pos.x, pos.y, pos.z, 0);
        }


        private NativeList<OctreeIndex> CollectNeighbors(OctreeIndex idx, NativeHashSet<int> closed)
        {
            var neighbors = new NativeList<OctreeIndex>(Allocator.Temp);


            int s = idx.Size;


            for (int dz = -1; dz <= 1; dz++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0 && dz == 0)
                            continue;

                        int nx = idx.x + dx;
                        int ny = idx.y + dy;
                        int nz = idx.z + dz;


                        for (int nh = math.max(idx.height - 1, minHeight); nh <= math.min(idx.height + 1, maxHeight); nh++)
                        {

                            int deltaH = nh - idx.height;
                            int cnx, cny, cnz;
                            if (deltaH >= 0)
                            {

                                cnx = nx >> deltaH;
                                cny = ny >> deltaH;
                                cnz = nz >> deltaH;
                            }
                            else
                            {

                                cnx = nx << (-deltaH);
                                cny = ny << (-deltaH);
                                cnz = nz << (-deltaH);
                            }

                            var nIdx = new OctreeIndex(cnx, cny, cnz, nh);

                            if (nIdx.x < 0 || nIdx.y < 0 || nIdx.z < 0)
                                continue;
                            if ((nIdx.x + 1) * nIdx.Size > sizeX ||
                                (nIdx.y + 1) * nIdx.Size > sizeY ||
                                (nIdx.z + 1) * nIdx.Size > sizeZ)
                                continue;
                            if (closed.Contains(nIdx.MortonCode))
                                continue;
                            if (!obstacleMap.IsSubvolumeTraversable(nIdx))
                                continue;


                            bool alreadyAdded = false;
                            for (int j = 0; j < neighbors.Length; j++)
                            {
                                if (neighbors[j].MortonCode == nIdx.MortonCode)
                                {
                                    alreadyAdded = true;
                                    break;
                                }
                            }

                            if (!alreadyAdded)
                                neighbors.Add(nIdx);
                        }
                    }
                }
            }

            return neighbors;
        }


        private void UpdateSubvolume(
            ref NativeMinPQ open,
            ref NativeHashMap<int, float> fScores,
            ref NativeHashSet<int> closed,
            OctreeIndex currentIdx,
            SubvolumeData currentData,
            OctreeIndex neighborIdx,
            OctreeIndex goalSV)
        {
            float3 currentCenter = currentIdx.Center;
            float3 neighborCenter = neighborIdx.Center;


            float existingG = float.PositiveInfinity;
            if (costField.TryGetValue(neighborIdx.MortonCode, out var existingData))
            {
                existingG = existingData.gCost;
            }


            float directG = currentData.gCost + math.distance(currentCenter, neighborCenter);


            float losG = float.PositiveInfinity;
            int3 losPred = currentData.Predecessor;
            float3 predCenter = currentData.PredecessorCenter;

            if (HasLineOfSight(predCenter, neighborCenter))
            {
                losG = currentData.gCost - math.distance(currentCenter, predCenter)
                       + math.distance(predCenter, neighborCenter);


                losG = currentData.gCost
                       - math.distance(predCenter, currentCenter)
                       + math.distance(predCenter, neighborCenter);
            }


            float candidateG = math.min(directG, losG);
            int3 candidatePred;
            if (candidateG == losG && losG < directG)
            {
                candidatePred = losPred;
            }
            else
            {
                candidatePred = new int3((int)currentCenter.x, (int)currentCenter.y, (int)currentCenter.z);
            }


            var cmp = CompareCosts(existingG, candidateG);

            switch (cmp)
            {
                case ComparisonResult.StrictlyBetter:

                    var newData = new SubvolumeData(candidatePred.x, candidatePred.y, candidatePred.z, candidateG);
                    costField[neighborIdx.MortonCode] = newData;


                    float h = math.distance(neighborCenter, (float3)goalPos);
                    float f = candidateG + h;

                    fScores[neighborIdx.MortonCode] = f;
                    open.Push(new OpenSetElement(neighborIdx, f));
                    break;

                case ComparisonResult.Ambiguous:

                    if (neighborIdx.height > minHeight)
                {
                    SubdivideAndRepropagate(
                        ref open, ref fScores, ref closed,
                        currentIdx, currentData, neighborIdx, goalSV);
                }
                else
                {

                    goto case ComparisonResult.StrictlyBetter;
                }
                break;

                case ComparisonResult.NotBetter:
                default:

                    break;
            }
        }


        private ComparisonResult CompareCosts(float existing, float candidate)
        {
            if (float.IsInfinity(existing))
                return ComparisonResult.StrictlyBetter;

            float threshold = epsilon * math.max(math.abs(existing), math.abs(candidate));
            threshold = math.max(threshold, 1e-6f);

            float diff = existing - candidate;

            if (diff > threshold)
                return ComparisonResult.StrictlyBetter;
            else if (diff > -threshold)
                return ComparisonResult.Ambiguous;
            else
                return ComparisonResult.NotBetter;
        }


        private void SubdivideAndRepropagate(
            ref NativeMinPQ open,
            ref NativeHashMap<int, float> fScores,
            ref NativeHashSet<int> closed,
            OctreeIndex currentIdx,
            SubvolumeData currentData,
            OctreeIndex neighborIdx,
            OctreeIndex goalSV)
        {
            int childCount = (sizeY > 1) ? 8 : 4;
            for (int c = 0; c < childCount; c++)
            {
                var child = neighborIdx.Child(c);


                if (child.x < 0 || child.y < 0 || child.z < 0)
                    continue;
                if ((child.x + 1) * child.Size > sizeX ||
                    (child.y + 1) * child.Size > sizeY ||
                    (child.z + 1) * child.Size > sizeZ)
                    continue;

                if (!obstacleMap.IsSubvolumeTraversable(child))
                    continue;
                if (closed.Contains(child.MortonCode))
                    continue;


                UpdateSubvolume(ref open, ref fScores, ref closed, currentIdx, currentData, child, goalSV);
            }
        }


        private bool HasLineOfSight(float3 from, float3 to)
        {

            int x0 = (int)math.floor(from.x);
            int y0 = (int)math.floor(from.y);
            int z0 = (int)math.floor(from.z);
            int x1 = (int)math.floor(to.x);
            int y1 = (int)math.floor(to.y);
            int z1 = (int)math.floor(to.z);

            int dx = math.abs(x1 - x0);
            int dy = math.abs(y1 - y0);
            int dz = math.abs(z1 - z0);

            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int sz = z0 < z1 ? 1 : -1;


            if (dx >= dy && dx >= dz)
            {
                int errY = 2 * dy - dx;
                int errZ = 2 * dz - dx;
                for (int i = 0; i <= dx; i++)
                {
                    if (!obstacleMap.IsTraversable(x0, y0, z0))
                        return false;
                    if (errY > 0) { y0 += sy; errY -= 2 * dx; }
                    if (errZ > 0) { z0 += sz; errZ -= 2 * dx; }
                    errY += 2 * dy;
                    errZ += 2 * dz;
                    x0 += sx;
                }
            }
            else if (dy >= dx && dy >= dz)
            {
                int errX = 2 * dx - dy;
                int errZ = 2 * dz - dy;
                for (int i = 0; i <= dy; i++)
                {
                    if (!obstacleMap.IsTraversable(x0, y0, z0))
                        return false;
                    if (errX > 0) { x0 += sx; errX -= 2 * dy; }
                    if (errZ > 0) { z0 += sz; errZ -= 2 * dy; }
                    errX += 2 * dx;
                    errZ += 2 * dz;
                    y0 += sy;
                }
            }
            else
            {
                int errX = 2 * dx - dz;
                int errY = 2 * dy - dz;
                for (int i = 0; i <= dz; i++)
                {
                    if (!obstacleMap.IsTraversable(x0, y0, z0))
                        return false;
                    if (errX > 0) { x0 += sx; errX -= 2 * dz; }
                    if (errY > 0) { y0 += sy; errY -= 2 * dz; }
                    errX += 2 * dx;
                    errY += 2 * dy;
                    z0 += sz;
                }
            }

            return true;
        }
    }
}
