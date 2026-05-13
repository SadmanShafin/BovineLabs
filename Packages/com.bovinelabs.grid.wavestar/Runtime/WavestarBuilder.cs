using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace BovineLabs.Grid.Wavestar
{
    [BurstCompile]
    public struct WavestarBuilderJob : IJob
    {
        public NativeArray<int> obstacleGrid;
        public int sizeX;
        public int sizeY;
        public int sizeZ;
        public int maxHeight;
        public int refinementRadius;
        public NativeHashSet<int> traversableSubvolumes;
        public NativeArray<int> distanceToObstacle;

        public void Execute()
        {
            var obstacleMap = new NativeObstacleMap(obstacleGrid, sizeX, sizeY, sizeZ);
            ComputeDistanceField();
            BuildMultiResDecomposition(obstacleMap);
        }

        private void ComputeDistanceField()
        {
            int totalCells = sizeX * sizeY * sizeZ;
            for (int i = 0; i < totalCells; i++)
            {
                distanceToObstacle[i] = obstacleGrid[i] != 0 ? 0 : totalCells;
            }
            var queue = new NativeList<int3>(Allocator.Temp);
            for (int z = 0; z < sizeZ; z++)
            {
                for (int y = 0; y < sizeY; y++)
                {
                    for (int x = 0; x < sizeX; x++)
                    {
                        if (obstacleGrid[x + y * sizeX + z * sizeX * sizeY] != 0)
                        {
                            queue.Add(new int3(x, y, z));
                        }
                    }
                }
            }
            int head = 0;
            while (head < queue.Length)
            {
                int3 pos = queue[head];
                head++;
                int currentDist = distanceToObstacle[pos.x + pos.y * sizeX + pos.z * sizeX * sizeY];
                TryPropagate(queue, pos.x + 1, pos.y, pos.z, currentDist);
                TryPropagate(queue, pos.x - 1, pos.y, pos.z, currentDist);
                if (sizeY > 1)
                {
                    TryPropagate(queue, pos.x, pos.y + 1, pos.z, currentDist);
                    TryPropagate(queue, pos.x, pos.y - 1, pos.z, currentDist);
                }
                if (sizeZ > 1)
                {
                    TryPropagate(queue, pos.x, pos.y, pos.z + 1, currentDist);
                    TryPropagate(queue, pos.x, pos.y, pos.z - 1, currentDist);
                }
            }
            queue.Dispose();
        }

        private void TryPropagate(NativeList<int3> queue, int x, int y, int z, int parentDist)
        {
            if (x < 0 || x >= sizeX || y < 0 || y >= sizeY || z < 0 || z >= sizeZ)
                return;
            int idx = x + y * sizeX + z * sizeX * sizeY;
            if (distanceToObstacle[idx] > parentDist + 1)
            {
                distanceToObstacle[idx] = parentDist + 1;
                queue.Add(new int3(x, y, z));
            }
        }

        private void BuildMultiResDecomposition(NativeObstacleMap obstacleMap)
        {
            int rootSize = 1 << maxHeight;
            int rootsX = (sizeX + rootSize - 1) / rootSize;
            int rootsY = (sizeY + rootSize - 1) / rootSize;
            int rootsZ = (sizeZ + rootSize - 1) / rootSize;
            for (int rz = 0; rz < rootsZ; rz++)
            {
                for (int ry = 0; ry < rootsY; ry++)
                {
                    for (int rx = 0; rx < rootsX; rx++)
                    {
                        var rootIdx = new OctreeIndex(rx, ry, rz, maxHeight);
                        DecomposeRecursive(rootIdx, obstacleMap);
                    }
                }
            }
        }

        private void DecomposeRecursive(OctreeIndex idx, NativeObstacleMap obstacleMap)
        {
            bool allBlocked = true;
            bool allFree = true;
            int s = idx.Size;
            int minX = idx.x * s;
            int minY = idx.y * s;
            int minZ = idx.z * s;
            int maxX = math.min(minX + s, sizeX);
            int maxY = math.min(minY + s, sizeY);
            int maxZ = math.min(minZ + s, sizeZ);
            int step = math.max(1, s / 4);
            for (int zz = minZ; zz < maxZ; zz += step)
            {
                for (int yy = minY; yy < maxY; yy += step)
                {
                    for (int xx = minX; xx < maxX; xx += step)
                    {
                        int cellIdx = xx + yy * sizeX + zz * sizeX * sizeY;
                        bool blocked = obstacleGrid[cellIdx] != 0;
                        if (blocked) allFree = false;
                        else allBlocked = false;
                    }
                }
            }
            if (allFree || allBlocked)
            {
                if (s <= 4)
                {
                    allBlocked = true;
                    allFree = true;
                    for (int zz = minZ; zz < maxZ; zz++)
                    {
                        for (int yy = minY; yy < maxY; yy++)
                        {
                            for (int xx = minX; xx < maxX; xx++)
                            {
                                int cellIdx = xx + yy * sizeX + zz * sizeX * sizeY;
                                bool blocked = obstacleGrid[cellIdx] != 0;
                                if (blocked) allFree = false;
                                else allBlocked = false;
                            }
                        }
                    }
                }
            }
            if (allBlocked)
                return;
            if (allFree)
            {
                int minDist = int.MaxValue;
                for (int zz = minZ; zz < maxZ && minDist > refinementRadius; zz++)
                {
                    for (int yy = minY; yy < maxY && minDist > refinementRadius; yy++)
                    {
                        for (int xx = minX; xx < maxX && minDist > refinementRadius; xx++)
                        {
                            int d = distanceToObstacle[xx + yy * sizeX + zz * sizeX * sizeY];
                            minDist = math.min(minDist, d);
                        }
                    }
                }
                if (minDist > refinementRadius)
                {
                    traversableSubvolumes.Add(idx.MortonCode);
                    return;
                }
            }
            if (idx.height > 0)
            {
                int childCount = (sizeY > 1) ? 8 : 4;
                for (int c = 0; c < childCount; c++)
                {
                    var child = idx.Child(c);
                    int cs = child.Size;
                    if (child.x * cs >= sizeX || child.y * cs >= sizeY || child.z * cs >= sizeZ)
                        continue;
                    DecomposeRecursive(child, obstacleMap);
                }
            }
            else
            {
                for (int zz = minZ; zz < maxZ; zz++)
                {
                    for (int yy = minY; yy < maxY; yy++)
                    {
                        for (int xx = minX; xx < maxX; xx++)
                        {
                            int cellIdx = xx + yy * sizeX + zz * sizeX * sizeY;
                            if (obstacleGrid[cellIdx] == 0)
                            {
                                var leaf = new OctreeIndex(xx, yy, zz, 0);
                                traversableSubvolumes.Add(leaf.MortonCode);
                            }
                        }
                    }
                }
            }
        }
    }

    public static class WavestarBuilder
    {
        public static int ComputeMaxHeight(int sizeX, int sizeY, int sizeZ)
        {
            int maxDim = math.max(math.max(sizeX, sizeY), sizeZ);
            int h = 0;
            while ((1 << h) < maxDim)
                h++;
            return h;
        }

        public static bool TryBuild(
            NativeArray<int> obstacleGrid,
            int sizeX, int sizeY, int sizeZ,
            int refinementRadius,
            out NativeHashSet<int> traversable,
            out NativeArray<int> distanceToObstacle)
        {
            int maxHeight = ComputeMaxHeight(sizeX, sizeY, sizeZ);
            int totalCells = sizeX * sizeY * sizeZ;
            distanceToObstacle = new NativeArray<int>(totalCells, Allocator.Persistent);
            traversable = new NativeHashSet<int>(totalCells / 4, Allocator.Persistent);
            var job = new WavestarBuilderJob
            {
                obstacleGrid = obstacleGrid,
                sizeX = sizeX,
                sizeY = sizeY,
                sizeZ = sizeZ,
                maxHeight = maxHeight,
                refinementRadius = refinementRadius,
                traversableSubvolumes = traversable,
                distanceToObstacle = distanceToObstacle,
            };
            var handle = job.Schedule();
            handle.Complete();
            return true;
        }
    }
}
