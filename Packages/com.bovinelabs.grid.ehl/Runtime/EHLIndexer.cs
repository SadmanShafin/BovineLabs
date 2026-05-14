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

        public unsafe void Execute()
        {
            int numCells = GridDims.x * GridDims.y;
            int numVertices = ConvexVertices.Length;

            // Maintain an array of pointers to ViaLabels UnsafeLists for merging
            var cellLabels = (UnsafeList<ViaLabel>**)UnsafeUtility.Malloc(sizeof(IntPtr) * numCells, UnsafeUtility.AlignOf<IntPtr>(), Allocator.Temp);
            
            var labelSet = new NativeHashMap<int, ViaLabel>(256, Allocator.Temp);
            var samplePoints = new NativeList<float2>(5, Allocator.Temp);
            var visibleVertices = new NativeList<int>(128, Allocator.Temp);

            for (int cy = 0; cy < GridDims.y; cy++)
            {
                for (int cx = 0; cx < GridDims.x; cx++)
                {
                    int cellIdx = cy * GridDims.x + cx;
                    var localLabels = new UnsafeList<ViaLabel>(64, Allocator.Temp);

                    labelSet.Clear();
                    samplePoints.Clear();
                    visibleVertices.Clear();

                    float2 cellMin = MapMin + new float2(cx * CellSize.x, cy * CellSize.y);
                    float2 cellMax = cellMin + CellSize;
                    float2 cellCenter = (cellMin + cellMax) * 0.5f;

                    samplePoints.Add(cellCenter);
                    samplePoints.Add(cellMin + CellSize * new float2(0.25f, 0.25f));
                    samplePoints.Add(cellMin + CellSize * new float2(0.75f, 0.25f));
                    samplePoints.Add(cellMin + CellSize * new float2(0.25f, 0.75f));
                    samplePoints.Add(cellMin + CellSize * new float2(0.75f, 0.75f));

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

                        if (canSee) visibleVertices.Add(v);
                    }

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
                                    labelSet[label.HubVertexId] = viaLabel;
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
                        localLabels.Add(values[i]);

                    var persistentList = (UnsafeList<ViaLabel>*)UnsafeUtility.Malloc(sizeof(UnsafeList<ViaLabel>), UnsafeUtility.AlignOf<UnsafeList<ViaLabel>>(), Allocator.Temp);
                    *persistentList = localLabels;
                    cellLabels[cellIdx] = persistentList;
                    
                    values.Dispose();
                }
            }

            labelSet.Dispose();
            samplePoints.Dispose();
            visibleVertices.Dispose();

            long currentMemory = 0;
            for (int i = 0; i < numCells; i++)
                currentMemory += cellLabels[i]->Length * UnsafeUtility.SizeOf<ViaLabel>();

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

                    if (bestA < 0 || bestOverlap <= 0f) break;

                    var merged = MergeLabels(cellLabels[bestA], cellLabels[bestB]);

                    currentMemory -= cellLabels[bestA]->Length * UnsafeUtility.SizeOf<ViaLabel>();
                    currentMemory -= cellLabels[bestB]->Length * UnsafeUtility.SizeOf<ViaLabel>();

                    cellLabels[bestA]->Dispose();
                    cellLabels[bestB]->Dispose();
                    
                    *cellLabels[bestA] = merged;
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
                        actualCell = cellMergedInto[actualCell];

                    float2 cellMin = MapMin + new float2(cx * CellSize.x, cy * CellSize.y);
                    float2 cellMax = cellMin + CellSize;

                    var labels = cellLabels[actualCell];
                    int count = labels->Length;

                    CellsOut.Add(new GridCell(cellMin, cellMax, labelOffset, count));
                    labelOffset += count;

                    for (int i = 0; i < count; i++)
                        ViaLabelsOut.Add(labels->Ptr[i]);
                }
            }

            for (int i = 0; i < numCells; i++)
            {
                if (activeCells[i])
                    cellLabels[i]->Dispose();
                UnsafeUtility.Free(cellLabels[i], Allocator.Temp);
            }

            UnsafeUtility.Free(cellLabels, Allocator.Temp);
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

        private unsafe float ComputeOverlap(UnsafeList<ViaLabel>* a, UnsafeList<ViaLabel>* b)
        {
            if (a->Length == 0 || b->Length == 0) return 0f;

            int maxHubId = math.max(a->Ptr[a->Length - 1].HubVertexId, b->Ptr[b->Length - 1].HubVertexId);
            if (maxHubId < 64)
            {
                ulong maskA = 0;
                for (int i = 0; i < a->Length; i++) maskA |= 1UL << a->Ptr[i].HubVertexId;

                ulong maskB = 0;
                for (int j = 0; j < b->Length; j++) maskB |= 1UL << b->Ptr[j].HubVertexId;

                ulong intersection = maskA & maskB;
                ulong union = maskA | maskB;

                int sharedBits = math.countbits(intersection);
                int unionBits = math.countbits(union);

                return unionBits > 0 ? (float)sharedBits / unionBits : 0f;
            }

            int numWords = (maxHubId >> 6) + 1;
            ulong* maskA2 = (ulong*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<ulong>() * numWords, UnsafeUtility.AlignOf<ulong>(), Allocator.Temp);
            ulong* maskB2 = (ulong*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<ulong>() * numWords, UnsafeUtility.AlignOf<ulong>(), Allocator.Temp);
            UnsafeUtility.MemSet(maskA2, 0, (long)numWords * UnsafeUtility.SizeOf<ulong>());
            UnsafeUtility.MemSet(maskB2, 0, (long)numWords * UnsafeUtility.SizeOf<ulong>());

            for (int i = 0; i < a->Length; i++)
            {
                int id = a->Ptr[i].HubVertexId;
                maskA2[id >> 6] |= 1UL << (id & 63);
            }

            for (int j = 0; j < b->Length; j++)
            {
                int id = b->Ptr[j].HubVertexId;
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

        private unsafe UnsafeList<ViaLabel> MergeLabels(UnsafeList<ViaLabel>* a, UnsafeList<ViaLabel>* b)
        {
            var result = new UnsafeList<ViaLabel>(a->Length + b->Length, Allocator.Temp);

            int i = 0, j = 0;
            while (i < a->Length && j < b->Length)
            {
                if (a->Ptr[i].HubVertexId == b->Ptr[j].HubVertexId)
                {
                    result.Add(a->Ptr[i].HubDistance <= b->Ptr[j].HubDistance ? a->Ptr[i] : b->Ptr[j]);
                    i++; j++;
                }
                else if (a->Ptr[i].HubVertexId < b->Ptr[j].HubVertexId)
                {
                    result.Add(a->Ptr[i]);
                    i++;
                }
                else
                {
                    result.Add(b->Ptr[j]);
                    j++;
                }
            }

            while (i < a->Length) { result.Add(a->Ptr[i]); i++; }
            while (j < b->Length) { result.Add(b->Ptr[j]); j++; }

            return result;
        }
    }
}