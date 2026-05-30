#if UNITY_EDITOR || BL_DEBUG
using BovineLabs.Core;
using BovineLabs.Core.ConfigVars;
using BovineLabs.Quill;
using BovineLabs.Timeline.Core.Debug;
using Game.Steering;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[Configurable]
public static class SteeringDebugConfig
{
    [ConfigVar("steering.draw-enabled", false, "Enable the steering debug drawer.")]
    public static readonly SharedStatic<bool> Enabled = SharedStatic<bool>.GetOrCreate<Tags.Enabled>();

    [ConfigVar("steering.draw-intent", true, "Enable debug drawing for steering intent.")]
    public static readonly SharedStatic<bool> DrawIntent = SharedStatic<bool>.GetOrCreate<Tags.DrawIntent>();

    [ConfigVar("steering.draw-field", true, "Enable debug drawing for influence fields.")]
    public static readonly SharedStatic<bool> DrawField = SharedStatic<bool>.GetOrCreate<Tags.DrawField>();

    [ConfigVar("steering.field-channel", 0, "Channel to draw for the field (0=Objective, 1=Threat, etc.).")]
    public static readonly SharedStatic<int> FieldChannel = SharedStatic<int>.GetOrCreate<Tags.FieldChannel>();

    private struct Tags
    {
        public struct Enabled { }
        public struct DrawIntent { }
        public struct DrawField { }
        public struct FieldChannel { }
    }
}

[WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ServerSimulation |
                   WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.Editor)]
[UpdateInGroup(typeof(DebugSystemGroup))]
[UpdateAfter(typeof(SteeringSystem))]
public partial struct SteeringDebugSystem : ISystem
{
    [BurstCompile] public void OnCreate(ref SystemState state) => state.RequireForUpdate<DrawSystem.Singleton>();

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!TimelineDebugUtility.TryGetDrawer<SteeringDebugSystem>(ref state, SteeringDebugConfig.Enabled.Data, out var drawer))
            return;

        if (SteeringDebugConfig.DrawIntent.Data)
            state.Dependency = new DrawIntentJob { Drawer = drawer }.ScheduleParallel(state.Dependency);

        if (SteeringDebugConfig.DrawField.Data)
            state.Dependency = new DrawFieldJob
            {
                Drawer = drawer,
                Channel = math.clamp(SteeringDebugConfig.FieldChannel.Data, 0, Influences.Count - 1)
            }.ScheduleParallel(state.Dependency);

        state.Dependency = new DrawThreatJob { Drawer = drawer }.ScheduleParallel(state.Dependency);
        state.Dependency = new DrawObjectiveJob { Drawer = drawer }.ScheduleParallel(state.Dependency);
    }

    [BurstCompile]
    private partial struct DrawIntentJob : IJobEntity
    {
        public Drawer Drawer;
        private void Execute(in LocalTransform transform, in SteeringIntent intent)
        {
            var pos = transform.Position + new float3(0, 0.3f, 0);
            var dir = new float3(intent.PreferredVelocity.x, 0f, intent.PreferredVelocity.y);
            if (math.lengthsq(dir) > 0.01f)
                Drawer.Arrow(pos, dir, SteeringDebugColors.Intent);
        }
    }

    [BurstCompile]
    private partial struct DrawFieldJob : IJobEntity
    {
        public Drawer Drawer;
        public int Channel;

        private void Execute(in InfluenceField field, in DynamicBuffer<InfluenceValue> values)
        {
            var size = field.Size;
            const int step = 4;

            for (int y = 0; y < size.y; y += step)
            for (int x = 0; x < size.x; x += step)
            {
                var center = field.CellCenter(new int2(x, y));
                // FIX: pass the DynamicBuffer directly, not AsNativeArray()
                var val = Steering.Sample(in field, in values, Channel, center);
                if (math.lengthsq(val) > 0.01f)
                {
                    var p3 = new float3(center.x, 0.2f, center.y);
                    var dir = new float3(val.x, 0f, val.y);
                    var len = math.length(dir);
                    Drawer.Arrow(p3, (dir / len) * math.min(len, 1.5f), SteeringDebugColors.Field);
                }
            }
        }
    }

    [BurstCompile] private partial struct DrawThreatJob : IJobEntity
    {
        public Drawer Drawer;
        private void Execute(in ThreatSource threat, in LocalTransform transform)
        {
            var pos = transform.Position;
            Drawer.Circle(pos, new float3(0f, threat.Radius, 0f), SteeringDebugColors.Threat);
            Drawer.Text32(pos + new float3(0, 0.5f, 0), "Threat", SteeringDebugColors.Threat, 10f);
        }
    }

    [BurstCompile] private partial struct DrawObjectiveJob : IJobEntity
    {
        public Drawer Drawer;
        private void Execute(in NavObjective obj)
        {
            var pos = new float3(obj.Position.x, 0f, obj.Position.y);
            Drawer.Sphere(pos, 0.5f, 16, SteeringDebugColors.Objective);
            Drawer.Text32(pos + new float3(0, 0.8f, 0), "Objective", SteeringDebugColors.Objective, 10f);
        }
    }
}
#endif