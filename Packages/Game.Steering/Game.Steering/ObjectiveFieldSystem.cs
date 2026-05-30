using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Game.Steering
{
    public struct NavObjective : IComponentData
    {
        public float2 Position;
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(FieldBootstrapSystem))]
    [UpdateBefore(typeof(SteeringSystem))]
    public partial struct ObjectiveFieldSystem : ISystem
    {
        private const int MaxPasses = 512;
        private const float DiagonalCost = 1.41421356f;
        private const float Unreachable = 1e9f;

        private EntityQuery _objectiveQuery;
        private EntityQuery _fieldQuery;
        private BufferLookup<InfluenceValue> _valuesLookup;
        private BufferLookup<OccupancyValue> _occupancyLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _objectiveQuery = SystemAPI.QueryBuilder().WithAll<NavObjective>().Build();
            _fieldQuery = SystemAPI.QueryBuilder().WithAll<InfluenceField, OccupancyValue>().WithAllRW<InfluenceValue>().Build();
            
            _valuesLookup = state.GetBufferLookup<InfluenceValue>(false);
            _occupancyLookup = state.GetBufferLookup<OccupancyValue>(true);
            state.RequireForUpdate(_fieldQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _valuesLookup.Update(ref state);
            _occupancyLookup.Update(ref state);
            
            var fieldEntity = _fieldQuery.GetSingletonEntity();
            var field = SystemAPI.GetComponent<InfluenceField>(fieldEntity);

            var distance = new NativeArray<float>(field.Size.x * field.Size.y, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var goals = _objectiveQuery.ToComponentDataArrayAsync<NavObjective>(state.WorldUpdateAllocator, out var goalHandle);

            state.Dependency = JobHandle.CombineDependencies(state.Dependency, goalHandle);

            state.Dependency = new IntegrateJob
            {
                Field = field,
                FieldEntity = fieldEntity,
                OccupancyLookup = _occupancyLookup,
                ValuesLookup = _valuesLookup,
                Distance = distance,
                Channel = (int)Influence.Objective,
                Goals = goals,
            }.Schedule(state.Dependency);

            distance.Dispose(state.Dependency);
        }

        [BurstCompile]
        private struct IntegrateJob : IJob
        {
            public InfluenceField Field;
            public Entity FieldEntity;
            
            [ReadOnly] public BufferLookup<OccupancyValue> OccupancyLookup;
            public BufferLookup<InfluenceValue> ValuesLookup;
            public NativeArray<float> Distance;
            public int Channel;
            [ReadOnly] public NativeArray<NavObjective> Goals;

            public void Execute()
            {
                var size = Field.Size;
                var occupancy = OccupancyLookup[FieldEntity];

                for (var i = 0; i < Distance.Length; i++)
                {
                    Distance[i] = Unreachable;
                }

                for (int i = 0; i < Goals.Length; i++)
                {
                    var goal = (int2)math.floor(Goals[i].Position * Field.InvStep) - Field.Origin;
                    if (math.all(goal >= 0) && math.all(goal < size))
                    {
                        Distance[(goal.y * size.x) + goal.x] = 0f;
                    }
                }

                var maxPasses = math.min(MaxPasses, math.max(size.x, size.y) * 2);

                for (var pass = 0; pass < maxPasses; pass++)
                {
                    var changed = Sweep(size, occupancy, 0, size.x, 1, 0, size.y, 1);
                    changed |= Sweep(size, occupancy, size.x - 1, -1, -1, 0, size.y, 1);
                    changed |= Sweep(size, occupancy, 0, size.x, 1, size.y - 1, -1, -1);
                    changed |= Sweep(size, occupancy, size.x - 1, -1, -1, size.y - 1, -1, -1);

                    if (!changed) break;
                }

                WriteFlow(size, occupancy);
            }

            private bool Sweep(int2 size, DynamicBuffer<OccupancyValue> occupancy, int xStart, int xEnd, int xStep, int yStart, int yEnd, int yStep)
            {
                var changed = false;

                for (var y = yStart; y != yEnd; y += yStep)
                {
                    for (var x = xStart; x != xEnd; x += xStep)
                    {
                        var index = (y * size.x) + x;
                        if (occupancy[index].Blocked != 0) continue;

                        var best = Distance[index];

                        for (var ny = -1; ny <= 1; ny++)
                        {
                            for (var nx = -1; nx <= 1; nx++)
                            {
                                if ((nx == 0) & (ny == 0)) continue;

                                var neighbour = new int2(x + nx, y + ny);
                                if (math.any(neighbour < 0) || math.any(neighbour >= size)) continue;

                                var neighbourIndex = (neighbour.y * size.x) + neighbour.x;
                                if (occupancy[neighbourIndex].Blocked != 0) continue;

                                var step = ((nx == 0) | (ny == 0)) ? 1f : DiagonalCost;
                                best = math.min(best, Distance[neighbourIndex] + step);
                            }
                        }

                        if (best < Distance[index])
                        {
                            Distance[index] = best;
                            changed = true;
                        }
                    }
                }

                return changed;
            }

            private void WriteFlow(int2 size, DynamicBuffer<OccupancyValue> occupancy)
            {
                var values = ValuesLookup[FieldEntity];
                
                for (var y = 0; y < size.y; y++)
                {
                    for (var x = 0; x < size.x; x++)
                    {
                        var cell = new int2(x, y);
                        var index = (y * size.x) + x;
                        var valIndex = Field.IndexOf(Channel, cell);

                        if (occupancy[index].Blocked != 0 || Distance[index] >= Unreachable)
                        {
                            values[valIndex] = new InfluenceValue { Value = float2.zero };
                            continue;
                        }

                        var here = Distance[index];
                        var east = Neighbour(size, occupancy, new int2(x + 1, y), here);
                        var west = Neighbour(size, occupancy, new int2(x - 1, y), here);
                        var north = Neighbour(size, occupancy, new int2(x, y + 1), here);
                        var south = Neighbour(size, occupancy, new int2(x, y - 1), here);

                        var gradient = new float2(east - west, north - south) * 0.5f;
                        var dir = math.normalizesafe(-gradient);
                        var slowdown = math.saturate(here / 3f);
                        
                        values[valIndex] = new InfluenceValue { Value = dir * slowdown };
                    }
                }
            }

            private float Neighbour(int2 size, DynamicBuffer<OccupancyValue> occupancy, int2 cell, float fallback)
            {
                if (math.any(cell < 0) || math.any(cell >= size)) return fallback;

                var index = (cell.y * size.x) + cell.x;
                if (occupancy[index].Blocked != 0 || Distance[index] >= Unreachable) return fallback;

                return Distance[index];
            }
        }
    }
}