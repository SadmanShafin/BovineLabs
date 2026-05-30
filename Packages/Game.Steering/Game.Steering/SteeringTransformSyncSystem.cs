using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace Game.Steering
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(FieldBootstrapSystem))]
    public partial struct SteeringTransformSyncSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new SyncCameraFocusJob().ScheduleParallel(state.Dependency);
            state.Dependency = new SyncObjectiveJob().ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        private partial struct SyncCameraFocusJob : IJobEntity
        {
            private void Execute(ref CameraFocus focus, in LocalTransform transform)
            {
                focus.Position = transform.Position.xz;
            }
        }

        [BurstCompile]
        private partial struct SyncObjectiveJob : IJobEntity
        {
            private void Execute(ref NavObjective objective, in LocalTransform transform)
            {
                objective.Position = transform.Position.xz;
            }
        }
    }
}
