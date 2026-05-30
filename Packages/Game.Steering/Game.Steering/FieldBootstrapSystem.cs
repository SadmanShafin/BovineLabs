using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Steering
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(InfluenceFieldSystem))]
    public partial struct FieldBootstrapSystem : ISystem
    {
        private EntityQuery _configQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _configQuery = SystemAPI.QueryBuilder().WithAll<InfluenceFieldConfig>().Build();
            state.RequireForUpdate(_configQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Create or resize the field entity if config changes.
            var config = _configQuery.GetSingleton<InfluenceFieldConfig>();

            var fieldEntity = SystemAPI.GetSingletonEntity<InfluenceField>();
            var field = SystemAPI.GetComponent<InfluenceField>(fieldEntity);

            var needsRebuild = field.Size.x != config.Size.x ||
                               field.Size.y != config.Size.y ||
                               field.Channels != config.Channels;

            if (needsRebuild)
            {
                field.Size = config.Size;
                field.Step = config.Step;
                field.InvStep = config.InvStep;
                field.Channels = config.Channels;
                field.Origin = int2.zero;
                field.WorldOrigin = float2.zero;

                var totalCells = field.Size.x * field.Size.y * field.Channels;
                var fieldBuffer = state.EntityManager.GetBuffer<InfluenceValue>(fieldEntity);
                fieldBuffer.ResizeUninitialized(totalCells);
                for (var i = 0; i < totalCells; i++) fieldBuffer[i] = new InfluenceValue { Value = float2.zero };

                var occBuffer = state.EntityManager.GetBuffer<OccupancyValue>(fieldEntity);
                var occTotal = field.Size.x * field.Size.y;
                occBuffer.ResizeUninitialized(occTotal);
                for (var i = 0; i < occTotal; i++) occBuffer[i] = new OccupancyValue { Blocked = 0 };

                SystemAPI.SetComponent(fieldEntity, field);
            }

            // Find camera focus from any InfluenceSource marked IsCameraFocus.
            float2 focusPosition = float2.zero;
            bool hasFocus = false;

            foreach (var (source, transform) in
                SystemAPI.Query<RefRO<InfluenceSource>, RefRO<LocalTransform>>())
            {
                if (source.ValueRO.IsCameraFocus != 0)
                {
                    focusPosition = transform.ValueRO.Position.xz;
                    hasFocus = true;
                    break;
                }
            }

            // Update origin so the grid is centered on the focus.
            if (hasFocus)
            {
                var focusCell = (int2)math.floor(focusPosition * field.InvStep);
                field.Origin = focusCell - (field.Size / 2);
                field.WorldOrigin = focusPosition - ((float2)(field.Size / 2)) * field.Step;
            }
            else
            {
                field.Origin = -(field.Size / 2);
                field.WorldOrigin = float2.zero;
            }

            SystemAPI.SetComponent(fieldEntity, field);
        }
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(InfluenceFieldSystem))]
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
