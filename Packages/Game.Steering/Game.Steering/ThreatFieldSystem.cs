using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Steering
{
    public struct ThreatSource : IComponentData
    {
        public float Radius;
        public float Strength;
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(FieldBootstrapSystem))]
    [UpdateBefore(typeof(SteeringSystem))]
    public partial struct ThreatFieldSystem : ISystem
    {
        private EntityQuery _sourcesQuery;
        private EntityQuery _fieldQuery;
        private BufferLookup<InfluenceValue> _valuesLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _sourcesQuery = SystemAPI.QueryBuilder().WithAll<ThreatSource, LocalTransform>().Build();
            _fieldQuery = SystemAPI.QueryBuilder().WithAll<InfluenceField>().WithAllRW<InfluenceValue>().Build();
            _valuesLookup = state.GetBufferLookup<InfluenceValue>(false);
            state.RequireForUpdate(_fieldQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _valuesLookup.Update(ref state);
            var fieldEntity = _fieldQuery.GetSingletonEntity();
            var field = SystemAPI.GetComponent<InfluenceField>(fieldEntity);

            var threats = _sourcesQuery.ToComponentDataArrayAsync<ThreatSource>(state.WorldUpdateAllocator, out var jh1);
            var transforms = _sourcesQuery.ToComponentDataArrayAsync<LocalTransform>(state.WorldUpdateAllocator, out var jh2);

            state.Dependency = JobHandle.CombineDependencies(state.Dependency, jh1, jh2);

            // First, clear the Threat channel optimally in parallel
            state.Dependency = new ClearChannelJob
            {
                Field = field,
                Channel = (int)Influence.Threat,
                FieldEntity = fieldEntity,
                ValuesLookup = _valuesLookup
            }.Schedule(field.Size.x * field.Size.y, 64, state.Dependency);

            // Stamp threats directly using worker threads preventing main-thread allocations
            state.Dependency = new StampThreatJob
            {
                Field = field,
                Channel = (int)Influence.Threat,
                Threats = threats,
                Transforms = transforms,
                FieldEntity = fieldEntity,
                ValuesLookup = _valuesLookup
            }.Schedule(field.Size.x * field.Size.y, 64, state.Dependency);
        }

        [BurstCompile]
        private struct ClearChannelJob : IJobParallelFor
        {
            public InfluenceField Field;
            public int Channel;
            public Entity FieldEntity;
            
            [NativeDisableParallelForRestriction]
            public BufferLookup<InfluenceValue> ValuesLookup;

            public void Execute(int index)
            {
                var values = ValuesLookup[FieldEntity];
                int x = index % Field.Size.x;
                int y = index / Field.Size.x;
                int valIndex = Field.IndexOf(Channel, new int2(x, y));
                values[valIndex] = new InfluenceValue { Value = float2.zero };
            }
        }

        [BurstCompile]
        private struct StampThreatJob : IJobParallelFor
        {
            public InfluenceField Field;
            public int Channel;
            [ReadOnly] public NativeArray<ThreatSource> Threats;
            [ReadOnly] public NativeArray<LocalTransform> Transforms;
            public Entity FieldEntity;

            [NativeDisableParallelForRestriction]
            public BufferLookup<InfluenceValue> ValuesLookup;

            public void Execute(int index)
            {
                var values = ValuesLookup[FieldEntity];
                int x = index % Field.Size.x;
                int y = index / Field.Size.x;
                var cell = new int2(x, y);
                var center = Field.CellCenter(cell);
                var accumulation = float2.zero;

                for (var i = 0; i < Threats.Length; i++)
                {
                    var away = center - Transforms[i].Position.xz;
                    var distance = math.length(away);
                    if (distance >= Threats[i].Radius) continue;

                    var falloff = math.saturate(1f - (distance * math.rcp(Threats[i].Radius)));
                    accumulation += math.normalizesafe(away) * (Threats[i].Strength * falloff * falloff);
                }

                values[Field.IndexOf(Channel, cell)] = new InfluenceValue { Value = accumulation };
            }
        }
    }
}