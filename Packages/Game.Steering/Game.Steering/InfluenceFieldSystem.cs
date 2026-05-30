using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Steering
{
    /// <summary>
    /// Unified influence field system.
    /// Iterates all <see cref="InfluenceSource"/> entities and stamps the field
    /// based on shape and operation. Replaces ThreatFieldSystem + ObjectiveFieldSystem.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(FieldBootstrapSystem))]
    [UpdateBefore(typeof(SteeringSystem))]
    public partial struct InfluenceFieldSystem : ISystem
    {
        private const float DiagonalCost = 1.41421356f;
        private const float Unreachable = 1e9f;
        private const int MaxPasses = 512;

        private EntityQuery _sourcesQuery;
        private EntityQuery _fieldQuery;
        private BufferLookup<InfluenceValue> _valuesLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _sourcesQuery = SystemAPI.QueryBuilder().WithAll<InfluenceSource, LocalTransform>().Build();
            _fieldQuery = SystemAPI.QueryBuilder().WithAll<InfluenceField, InfluenceValue>().WithAllRW<InfluenceValue>().Build();
            _valuesLookup = state.GetBufferLookup<InfluenceValue>(false);
            state.RequireForUpdate(_fieldQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _valuesLookup.Update(ref state);
            var fieldEntity = _fieldQuery.GetSingletonEntity();
            var field = SystemAPI.GetComponent<InfluenceField>(fieldEntity);

            var sources = _sourcesQuery.ToComponentDataArrayAsync<InfluenceSource>(state.WorldUpdateAllocator, out var jh1);
            var transforms = _sourcesQuery.ToComponentDataArrayAsync<LocalTransform>(state.WorldUpdateAllocator, out var jh2);

            state.Dependency = JobHandle.CombineDependencies(state.Dependency, jh1, jh2);

            // Clear all channels first.
            var clearJob = new ClearAllChannelsJob
            {
                Field = field,
                FieldEntity = fieldEntity,
                ValuesLookup = _valuesLookup,
            };

            // We need to clear first, then stamp. Use a two-phase approach.
            state.Dependency = clearJob.Schedule(field.Size.x * field.Size.y * field.Channels, 64, state.Dependency);

            // Stamp sphere and point sources in parallel.
            state.Dependency = new StampInfluenceJob
            {
                Field = field,
                Sources = sources,
                Transforms = transforms,
                FieldEntity = fieldEntity,
                ValuesLookup = _valuesLookup,
            }.Schedule(state.Dependency);

            // Propagate point sources via flow-field integration (sequential).
            state.Dependency = new IntegrateFlowFieldJob
            {
                Field = field,
                FieldEntity = fieldEntity,
                Sources = sources,
                Transforms = transforms,
                OccupancyLookup = state.GetBufferLookup<OccupancyValue>(true),
                ValuesLookup = _valuesLookup,
            }.Schedule(state.Dependency);
        }

        // -------------------------------------------------------------------
        // Jobs
        // -------------------------------------------------------------------

        [BurstCompile]
        private struct ClearAllChannelsJob : IJobParallelFor
        {
            public InfluenceField Field;
            public Entity FieldEntity;

            [NativeDisableParallelForRestriction]
            public BufferLookup<InfluenceValue> ValuesLookup;

            public void Execute(int index)
            {
                var values = ValuesLookup[FieldEntity];
                values[index] = new InfluenceValue { Value = float2.zero };
            }
        }

        [BurstCompile]
        private struct StampInfluenceJob : IJob
        {
            public InfluenceField Field;
            [ReadOnly] public NativeArray<InfluenceSource> Sources;
            [ReadOnly] public NativeArray<LocalTransform> Transforms;
            public Entity FieldEntity;

            [NativeDisableParallelForRestriction]
            public BufferLookup<InfluenceValue> ValuesLookup;

            public void Execute()
            {
                for (var s = 0; s < Sources.Length; s++)
                {
                    var source = Sources[s];
                    if (source.Shape != (byte)InfluenceShape.Point)
                        continue; // Point sources are handled by IntegrateFlowFieldJob

                    // Stamp spheres and skip point sources (they get flow-field integration).
                    if (source.Shape != (byte)InfluenceShape.Sphere)
                        continue;

                    var channel = source.Channel;
                    var sign = source.Operation == (byte)InfluenceOp.Subtract ? -1f : 1f;
                    var center = Transforms[s].Position.xz;
                    var radius = source.Radius;
                    var strength = source.Strength;

                    // Compute the affected cell range.
                    var minCell = (int2)math.floor((center - radius) * Field.InvStep) - Field.Origin;
                    var maxCell = (int2)math.ceil((center + radius) * Field.InvStep) - Field.Origin;
                    minCell = math.clamp(minCell, 0, Field.Size - 1);
                    maxCell = math.clamp(maxCell, 0, Field.Size - 1);

                    var values = ValuesLookup[FieldEntity];

                    for (var y = minCell.y; y <= maxCell.y; y++)
                    for (var x = minCell.x; x <= maxCell.x; x++)
                    {
                        var cell = new int2(x, y);
                        var cellWorld = Field.CellCenterWorld(cell);
                        var toCenter = center - cellWorld;
                        var dist = math.length(toCenter);

                        if (dist >= radius) continue;

                        var falloff = math.saturate(1f - dist * math.rcp(radius));
                        var dir = math.normalizesafe(toCenter);
                        var contribution = dir * (sign * strength * falloff * falloff);

                        var idx = Field.IndexOf(channel, cell);
                        values[idx] = new InfluenceValue
                        {
                            Value = values[idx].Value + contribution,
                        };
                    }
                }
            }
        }

        [BurstCompile]
        private struct IntegrateFlowFieldJob : IJob
        {
            public InfluenceField Field;
            public Entity FieldEntity;
            [ReadOnly] public NativeArray<InfluenceSource> Sources;
            [ReadOnly] public NativeArray<LocalTransform> Transforms;

            [ReadOnly] public BufferLookup<OccupancyValue> OccupancyLookup;

            [NativeDisableParallelForRestriction]
            public BufferLookup<InfluenceValue> ValuesLookup;

            public void Execute()
            {
                var size = Field.Size;
                var occupancy = OccupancyLookup[FieldEntity];

                // Collect point sources per channel.
                for (var ch = 0; ch < Field.Channels; ch++)
                {
                    // Find all point sources for this channel.
                    var goals = new NativeList<int2>(Allocator.Temp);
                    var isSubtract = false;

                    for (var s = 0; s < Sources.Length; s++)
                    {
                        if (Sources[s].Channel != ch) continue;
                        if (Sources[s].Shape != (byte)InfluenceShape.Point) continue;

                        var worldPos = Transforms[s].Position.xz;
                        var cell = (int2)math.floor(worldPos * Field.InvStep) - Field.Origin;

                        if (math.all(cell >= 0) && math.all(cell < size))
                        {
                            goals.Add(cell);
                            isSubtract = Sources[s].Operation == (byte)InfluenceOp.Subtract;
                        }
                    }

                    if (goals.Length == 0)
                    {
                        goals.Dispose();
                        continue;
                    }

                    // BFS-like distance integration.
                    var distance = new NativeArray<float>(size.x * size.y, Allocator.Temp);

                    for (var i = 0; i < distance.Length; i++)
                        distance[i] = Unreachable;

                    for (var g = 0; g < goals.Length; g++)
                        distance[(goals[g].y * size.x) + goals[g].x] = 0f;

                    var maxPasses = math.min(MaxPasses, math.max(size.x, size.y) * 2);

                    for (var pass = 0; pass < maxPasses; pass++)
                    {
                        var changed = Sweep(size, occupancy, distance, 0, size.x, 1, 0, size.y, 1);
                        changed |= Sweep(size, occupancy, distance, size.x - 1, -1, -1, 0, size.y, 1);
                        changed |= Sweep(size, occupancy, distance, 0, size.x, 1, size.y - 1, -1, -1);
                        changed |= Sweep(size, occupancy, distance, size.x - 1, -1, -1, size.y - 1, -1, -1);

                        if (!changed) break;
                    }

                    // Write flow vectors.
                    var values = ValuesLookup[FieldEntity];
                    var sign = isSubtract ? -1f : 1f;

                    for (var y = 0; y < size.y; y++)
                    for (var x = 0; x < size.x; x++)
                    {
                        var cell = new int2(x, y);
                        var index = (y * size.x) + x;
                        var valIndex = Field.IndexOf(ch, cell);

                        if (occupancy[index].Blocked != 0 || distance[index] >= Unreachable)
                            continue;

                        var here = distance[index];
                        var east = Neighbour(size, occupancy, distance, new int2(x + 1, y), here);
                        var west = Neighbour(size, occupancy, distance, new int2(x - 1, y), here);
                        var north = Neighbour(size, occupancy, distance, new int2(x, y + 1), here);
                        var south = Neighbour(size, occupancy, distance, new int2(x, y - 1), here);

                        var gradient = new float2(east - west, north - south) * 0.5f;
                        var dir = math.normalizesafe(-gradient);
                        var slowdown = math.saturate(here / 3f);

                        values[valIndex] = new InfluenceValue
                        {
                            Value = values[valIndex].Value + (dir * slowdown * sign),
                        };
                    }

                    distance.Dispose();
                    goals.Dispose();
                }
            }

            private static bool Sweep(
                int2 size, DynamicBuffer<OccupancyValue> occupancy, NativeArray<float> distance,
                int xStart, int xEnd, int xStep, int yStart, int yEnd, int yStep)
            {
                var changed = false;

                for (var y = yStart; y != yEnd; y += yStep)
                for (var x = xStart; x != xEnd; x += xStep)
                {
                    var index = (y * size.x) + x;
                    if (occupancy[index].Blocked != 0) continue;

                    var best = distance[index];

                    for (var ny = -1; ny <= 1; ny++)
                    for (var nx = -1; nx <= 1; nx++)
                    {
                        if ((nx == 0) & (ny == 0)) continue;

                        var neighbour = new int2(x + nx, y + ny);
                        if (math.any(neighbour < 0) || math.any(neighbour >= size)) continue;

                        var neighbourIndex = (neighbour.y * size.x) + neighbour.x;
                        if (occupancy[neighbourIndex].Blocked != 0) continue;

                        var step = ((nx == 0) | (ny == 0)) ? 1f : DiagonalCost;
                        best = math.min(best, distance[neighbourIndex] + step);
                    }

                    if (best < distance[index])
                    {
                        distance[index] = best;
                        changed = true;
                    }
                }

                return changed;
            }

            private static float Neighbour(
                int2 size, DynamicBuffer<OccupancyValue> occupancy, NativeArray<float> distance,
                int2 cell, float fallback)
            {
                if (math.any(cell < 0) || math.any(cell >= size)) return fallback;

                var index = (cell.y * size.x) + cell.x;
                if (occupancy[index].Blocked != 0 || distance[index] >= Unreachable) return fallback;

                return distance[index];
            }
        }
    }
}
