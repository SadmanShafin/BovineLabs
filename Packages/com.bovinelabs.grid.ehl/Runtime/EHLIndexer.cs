using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace BovineLabs.Grid.EHL
{
    [BurstCompile]
    public struct EHLIndexerJob : IJob
    {
        public NativeArray<ConvexVertex> ConvexVertices;
        public NativeArray<ObstacleEdge> ObstacleEdges;

        public NativeArray<int> HubOffsets;
        public NativeArray<int> HubCounts;
        public NativeArray<VisibilityLabel> HubLabels;

        public NativeArray<int> AdjOffsets;
        public NativeArray<int> AdjCounts;
        public NativeArray<AdjEdge> AdjEdges;

        public float2 MapMin;
        public float2 MapMax;
        public int2 GridDims;
        public float2 CellSize;

        public long MemoryBudgetBytes;

        public NativeList<GridCell> CellsOut;

        public NativeList<ViaLabel> ViaLabelsOut;

        public void Execute()
        {
            int numCells = GridDims.x * GridDims.y;
            int numVertices = ConvexVertices.Length;

            var cellLabels = new NativeArray<NativeList<ViaLabel>>(numCells, Allocator.Temp);

            for (int cy = 0; cy < GridDims.y; cy++)
            {
                for (int cx = 0; cx < GridDims.x; cx++)
                {
                    int cellIdx = cy * GridDims.x + cx;
                    cellLabels[cellIdx] = new NativeList<ViaLabel>(Allocator.Temp);

                    float2 cellMin = MapMin + new float2(cx * CellSize.x, cy * CellSize.y);
                    float2 cellMax = cellMin + CellSize;
                    float2 cellCenter = (cellMin + cellMax) * 0.5f;

                    var samplePoints = new NativeList<float2>(Allocator.Temp);
                    samplePoints.Add(cellCenter);
                    samplePoints.Add(cellMin + CellSize * new float2(0.25f, 0.25f));
                    samplePoints.Add(cellMin + CellSize * new float2(0.75f, 0.25f));
                    samplePoints.Add(cellMin + CellSize * new float2(0.25f, 0.75f));
                    samplePoints.Add(cellMin + CellSize * new float2(0.75f, 0.75f));

                    var visibleVertices = new NativeList<int>(Allocator.Temp);

                    for (int v = 0; v < numVertices; v++)
                    {
                        float2 vPos = ConvexVertices[v].Position;
                        bool canSee = false;

                        for (int s = 0; s < samplePoints.Length; s++)
                        {
                            if (IsVisible(vPos, samplePoints[s], ObstacleEdges))
                            {
                                canSee = true;
                                break;
                            }
                        }

                        if (canSee)
                        {
                            visibleVertices.Add(v);
                        }
                    }

                    var labelSet = new NativeHashMap<int, ViaLabel>(64, Allocator.Temp);

                    for (int vi = 0; vi < visibleVertices.Length; vi++)
                    {
                        int v = visibleVertices[vi];
                        float2 vPos = ConvexVertices[v].Position;
                        float distToCell = math.distance(cellCenter, vPos);

                        int hubStart = HubOffsets[v];
                        int hubCount = HubCounts[v];

                        for (int h = 0; h < hubCount; h++)
                        {
                            var label = HubLabels[hubStart + h];
                            float totalDist = distToCell + label.Distance;

                            var viaLabel = new ViaLabel(
                                label.HubVertexId,
                                totalDist,
                                label.ViaVertexId,
                                v
                            );

                            if (labelSet.TryGetValue(label.HubVertexId, out var existing))
                            {
                                if (totalDist < existing.HubDistance)
                                {
                                    labelSet[label.HubVertexId] = viaLabel;
                                }
                            }
                            else
                            {
                                labelSet.TryAdd(label.HubVertexId, viaLabel);
                            }
                        }
                    }

                    var values = labelSet.GetValueArray(Allocator.Temp);
                    values.Sort();

                    for (int i = 0; i < values.Length; i++)
                    {
                        cellLabels[cellIdx].Add(values[i]);
                    }

                    values.Dispose();
                    labelSet.Dispose();
                    visibleVertices.Dispose();
                    samplePoints.Dispose();
                }
            }

            long currentMemory = 0;
            for (int i = 0; i < numCells; i++)
            {
                currentMemory += cellLabels[i].Length * UnsafeUtility.SizeOf<ViaLabel>();
            }

            var activeCells = new NativeArray<bool>(numCells, Allocator.Temp);
            var cellMergedInto = new NativeArray<int>(numCells, Allocator.Temp);
            for (int i = 0; i < numCells; i++)
            {
                activeCells[i] = true;
                cellMergedInto[i] = i;
            }

            if (currentMemory > MemoryBudgetBytes)
            {
                int maxIterations = numCells;
                int iter = 0;

                while (currentMemory > MemoryBudgetBytes && iter < maxIterations)
                {
                    int bestA = -1, bestB = -1;
                    float bestOverlap = -1f;

                    for (int cy = 0; cy < GridDims.y; cy++)
                    {
                        for (int cx = 0; cx < GridDims.x; cx++)
                        {
                            int idx = cy * GridDims.x + cx;
                            if (!activeCells[idx]) continue;

                            if (cx + 1 < GridDims.x)
                            {
                                int right = cy * GridDims.x + (cx + 1);
                                if (activeCells[right])
                                {
                                    float overlap = ComputeOverlap(cellLabels[idx], cellLabels[right]);
                                    if (overlap > bestOverlap)
                                    {
                                        bestOverlap = overlap;
                                        bestA = idx;
                                        bestB = right;
                                    }
                                }
                            }

                            if (cy + 1 < GridDims.y)
                            {
                                int top = (cy + 1) * GridDims.x + cx;
                                if (activeCells[top])
                                {
                                    float overlap = ComputeOverlap(cellLabels[idx], cellLabels[top]);
                                    if (overlap > bestOverlap)
                                    {
                                        bestOverlap = overlap;
                                        bestA = idx;
                                        bestB = top;
                                    }
                                }
                            }
                        }
                    }

                    if (bestA < 0 || bestOverlap <= 0f)
                        break;

                    var merged = MergeLabels(cellLabels[bestA], cellLabels[bestB]);

                    currentMemory -= cellLabels[bestA].Length * UnsafeUtility.SizeOf<ViaLabel>();
                    currentMemory -= cellLabels[bestB].Length * UnsafeUtility.SizeOf<ViaLabel>();

                    cellLabels[bestA].Dispose();
                    cellLabels[bestB].Dispose();
                    cellLabels[bestA] = merged;

                    currentMemory += merged.Length * UnsafeUtility.SizeOf<ViaLabel>();

                    activeCells[bestB] = false;
                    cellMergedInto[bestB] = bestA;

                    iter++;
                }
            }

            int labelOffset = 0;
            for (int cy = 0; cy < GridDims.y; cy++)
            {
                for (int cx = 0; cx < GridDims.x; cx++)
                {
                    int cellIdx = cy * GridDims.x + cx;

                    int actualCell = cellIdx;
                    while (!activeCells[actualCell])
                    {
                        actualCell = cellMergedInto[actualCell];
                    }

                    float2 cellMin = MapMin + new float2(cx * CellSize.x, cy * CellSize.y);
                    float2 cellMax = cellMin + CellSize;

                    var labels = cellLabels[actualCell];
                    int count = labels.Length;

                    CellsOut.Add(new GridCell(cellMin, cellMax, labelOffset, count));
                    labelOffset += count;

                    for (int i = 0; i < count; i++)
                    {
                        ViaLabelsOut.Add(labels[i]);
                    }
                }
            }

            for (int i = 0; i < numCells; i++)
            {
                if (cellLabels[i].IsCreated)
                    cellLabels[i].Dispose();
            }

            cellLabels.Dispose();
            activeCells.Dispose();
            cellMergedInto.Dispose();
        }

        private bool IsVisible(float2 a, float2 b, NativeArray<ObstacleEdge> edges)
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

        /// <summary>
        /// Computes Jaccard overlap coefficient using SIMD bitmask operations.
        /// Packs hub vertex IDs into ulong bitmasks (up to 64 distinct hubs per cell)
        /// and uses math.countbits() for fast intersection size computation.
        /// Falls back to sorted merge join when hub IDs exceed 64.
        /// </summary>
        private float ComputeOverlap(NativeList<ViaLabel> a, NativeList<ViaLabel> b)
        {
            if (a.Length == 0 || b.Length == 0) return 0f;

            // Check if all hub IDs fit in a single ulong bitmask (IDs 0..63)
            // The labels are sorted by HubVertexId, so max ID is the last element
            int maxHubId = math.max(a[a.Length - 1].HubVertexId, b[b.Length - 1].HubVertexId);
            if (maxHubId < 64)
            {
                // Fast path: single-word bitmask
                ulong maskA = 0;
                for (int i = 0; i < a.Length; i++)
                    maskA |= 1UL << a[i].HubVertexId;

                ulong maskB = 0;
                for (int j = 0; j < b.Length; j++)
                    maskB |= 1UL << b[j].HubVertexId;

                ulong intersection = maskA & maskB;
                ulong union = maskA | maskB;

                int sharedBits = math.countbits(intersection);
                int unionBits = math.countbits(union);

                return unionBits > 0 ? (float)sharedBits / unionBits : 0f;
            }

            // Fallback: multi-word bitmask path for large hub ID ranges
            int numWords = (maxHubId >> 6) + 1;
            ulong* maskA2 = (ulong*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<ulong>() * numWords, UnsafeUtility.AlignOf<ulong>(), Allocator.Temp);
            ulong* maskB2 = (ulong*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<ulong>() * numWords, UnsafeUtility.AlignOf<ulong>(), Allocator.Temp);
            UnsafeUtility.MemSet(maskA2, 0, (long)numWords * UnsafeUtility.SizeOf<ulong>());
            UnsafeUtility.MemSet(maskB2, 0, (long)numWords * UnsafeUtility.SizeOf<ulong>());

            for (int i = 0; i < a.Length; i++)
            {
                int id = a[i].HubVertexId;
                maskA2[id >> 6] |= 1UL << (id & 63);
            }

            for (int j = 0; j < b.Length; j++)
            {
                int id = b[j].HubVertexId;
                maskB2[id >> 6] |= 1UL << (id & 63);
            }

            int shared = 0;
            int totalUnion = 0;
            for (int w = 0; w < numWords; w++)
            {
                shared += math.countbits(maskA2[w] & maskB2[w]);
                totalUnion += math.countbits(maskA2[w] | maskB2[w]);
            }

            UnsafeUtility.Free(maskA2, Allocator.Temp);
            UnsafeUtility.Free(maskB2, Allocator.Temp);

            return totalUnion > 0 ? (float)shared / totalUnion : 0f;
        }

        private NativeList<ViaLabel> MergeLabels(NativeList<ViaLabel> a, NativeList<ViaLabel> b)
        {
            var result = new NativeList<ViaLabel>(Allocator.Temp);

            int i = 0, j = 0;
            while (i < a.Length && j < b.Length)
            {
                if (a[i].HubVertexId == b[j].HubVertexId)
                {
                    result.Add(a[i].HubDistance <= b[j].HubDistance ? a[i] : b[j]);
                    i++;
                    j++;
                }
                else if (a[i].HubVertexId < b[j].HubVertexId)
                {
                    result.Add(a[i]);
                    i++;
                }
                else
                {
                    result.Add(b[j]);
                    j++;
                }
            }

            while (i < a.Length)
            {
                result.Add(a[i]);
                i++;
            }

            while (j < b.Length)
            {
                result.Add(b[j]);
                j++;
            }

            return result;
        }
    }

    public static class EHLIndexer
    {
        public static bool TryBuild(
            NativeArray<ConvexVertex> convexVertices,
            NativeArray<ObstacleEdge> obstacleEdges,
            NativeArray<int> hubOffsets,
            NativeArray<int> hubCounts,
            NativeArray<VisibilityLabel> hubLabels,
            NativeArray<int> adjOffsets,
            NativeArray<int> adjCounts,
            NativeArray<AdjEdge> adjEdges,
            float2 mapMin,
            float2 mapMax,
            int2 gridDims,
            long memoryBudgetBytes,
            out NativeList<GridCell> cells,
            out NativeList<ViaLabel> viaLabels,
            out JobHandle result,
            JobHandle dependency = default)
        {
            cells = new NativeList<GridCell>(Allocator.Persistent);
            viaLabels = new NativeList<ViaLabel>(Allocator.Persistent);

            float2 cellSize = (mapMax - mapMin) / new float2(gridDims.x, gridDims.y);

            var job = new EHLIndexerJob
            {
                ConvexVertices = convexVertices,
                ObstacleEdges = obstacleEdges,
                HubOffsets = hubOffsets,
                HubCounts = hubCounts,
                HubLabels = hubLabels,
                AdjOffsets = adjOffsets,
                AdjCounts = adjCounts,
                AdjEdges = adjEdges,
                MapMin = mapMin,
                MapMax = mapMax,
                GridDims = gridDims,
                CellSize = cellSize,
                MemoryBudgetBytes = memoryBudgetBytes,
                CellsOut = cells,
                ViaLabelsOut = viaLabels,
            };

            result = job.Schedule(dependency);
            return true;
        }

        public static bool TryAssembleIndex(
            float2 mapMin,
            float2 mapMax,
            int2 gridDims,
            NativeList<GridCell> cells,
            NativeList<ViaLabel> viaLabels,
            NativeArray<ConvexVertex> convexVertices,
            NativeArray<ObstacleEdge> obstacleEdges,
            NativeList<int> adjOffsets,
            NativeList<int> adjCounts,
            NativeList<AdjEdge> adjEdges,
            NativeList<int> hubOffsets,
            NativeList<int> hubCounts,
            NativeList<VisibilityLabel> hubLabels,
            NativeList<long> succKeys,
            NativeList<int> succValues,
            out EHLIndex result)
        {
            float2 cellSize = (mapMax - mapMin) / new float2(gridDims.x, gridDims.y);

            var successorMap = new NativeHashMap<long, int>(succKeys.Length, Allocator.Persistent);
            for (int i = 0; i < succKeys.Length; i++)
            {
                successorMap.TryAdd(succKeys[i], succValues[i]);
            }

            result = new EHLIndex
            {
                MapMin = mapMin,
                MapMax = mapMax,
                GridDims = gridDims,
                CellSize = cellSize,
                Cells = cells.ToNativeArray(Allocator.Persistent),
                ViaLabels = viaLabels.ToNativeArray(Allocator.Persistent),
                ConvexVertices = convexVertices,
                ObstacleEdges = obstacleEdges,
                AdjOffsets = adjOffsets.ToNativeArray(Allocator.Persistent),
                AdjCounts = adjCounts.ToNativeArray(Allocator.Persistent),
                AdjEdges = adjEdges.ToNativeArray(Allocator.Persistent),
                HubOffsets = hubOffsets.ToNativeArray(Allocator.Persistent),
                HubCounts = hubCounts.ToNativeArray(Allocator.Persistent),
                HubLabels = hubLabels.ToNativeArray(Allocator.Persistent),
                SuccessorMap = successorMap,
            };
            return true;
        }
    }

    internal static class NativeListExtensions
    {
        public static NativeArray<T> ToNativeArray<T>(this NativeList<T> list, Allocator allocator)
            where T : unmanaged
        {
            var arr = new NativeArray<T>(list.Length, allocator);
            for (int i = 0; i < list.Length; i++)
                arr[i] = list[i];
            return arr;
        }
    }
}
