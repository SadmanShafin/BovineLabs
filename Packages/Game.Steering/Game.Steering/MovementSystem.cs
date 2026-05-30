using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace Game.Steering
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(SteeringSystem))]
    public partial struct MovementSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new MoveJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        private partial struct MoveJob : IJobEntity
        {
            public float DeltaTime;

            private void Execute(in SteeringIntent intent, ref LocalTransform transform)
            {
                transform.Position.xz += intent.PreferredVelocity * DeltaTime;
            }
        }
    }
}