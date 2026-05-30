using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Steering
{
    public struct CameraFocus : IComponentData
    {
        public float2 Position;
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(ThreatFieldSystem))]
    public partial struct FieldBootstrapSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            var size = new int2(128, 128);
            var step = 1f;
            var channels = Influences.Count;
            var totalCells = size.x * size.y * channels;

            var entity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(entity, new InfluenceField
            {
                Size = size,
                Step = step,
                InvStep = math.rcp(step),
                Channels = channels,
                Origin = int2.zero,
            });

            var fieldBuffer = state.EntityManager.AddBuffer<InfluenceValue>(entity);
            fieldBuffer.ResizeUninitialized(totalCells);
            for (var i = 0; i < totalCells; i++) fieldBuffer[i] = new InfluenceValue { Value = float2.zero };

            var occBuffer = state.EntityManager.AddBuffer<OccupancyValue>(entity);
            occBuffer.ResizeUninitialized(size.x * size.y);
            for (var i = 0; i < size.x * size.y; i++) occBuffer[i] = new OccupancyValue { Blocked = 0 };
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingletonRW<InfluenceField>(out var field)) return;

            if (SystemAPI.TryGetSingleton<CameraFocus>(out var focus))
            {
                var focusCell = (int2)math.floor(focus.Position * field.ValueRO.InvStep);
                field.ValueRW.Origin = focusCell - (field.ValueRO.Size / 2);
            }
            else
            {
                field.ValueRW.Origin = -(field.ValueRO.Size / 2);
            }
        }
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ThreatFieldSystem))]
    [UpdateAfter(typeof(ObjectiveFieldSystem))]
    public partial struct SteeringSystem : ISystem
    {
        private EntityQuery _fieldQuery;
        private BufferLookup<InfluenceValue> _valuesLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _fieldQuery = SystemAPI.QueryBuilder().WithAll<InfluenceField, InfluenceValue>().Build();
            _valuesLookup = state.GetBufferLookup<InfluenceValue>(true);
            state.RequireForUpdate(_fieldQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _valuesLookup.Update(ref state);
            
            var fieldEntity = _fieldQuery.GetSingletonEntity();
            var field = SystemAPI.GetComponent<InfluenceField>(fieldEntity);

            state.Dependency = new SteerJob
            {
                Field = field,
                FieldEntity = fieldEntity,
                ValuesLookup = _valuesLookup,
                DeltaTime = SystemAPI.Time.DeltaTime,
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        private partial struct SteerJob : IJobEntity
        {
            [ReadOnly] public InfluenceField Field;
            public Entity FieldEntity;
            [ReadOnly] public BufferLookup<InfluenceValue> ValuesLookup;
            public float DeltaTime;

            private void Execute(ref SteeringIntent intent, in BehaviorRef behavior, in Stage stage, in LocalTransform transform)
            {
                ref var blob = ref behavior.Value.Value;
                var resolvedStage = math.clamp((int)stage.Index, 0, blob.Stages.Length - 1);
                var position = transform.Position.xz;
                var values = ValuesLookup[FieldEntity];

                intent.MaxSpeed = blob.Stages[resolvedStage].MaxSpeed;
                intent.PreferredVelocity = Steering.Resolve(in Field, in values, ref blob, resolvedStage, position, intent.PreferredVelocity, DeltaTime);
            }
        }
    }
}