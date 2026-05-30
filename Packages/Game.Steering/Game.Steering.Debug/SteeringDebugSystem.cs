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

namespace Game.Steering.Debug
{
    [Configurable]
    public static class SteeringDebugConfig
    {
        [ConfigVar("steering.draw-enabled", false, "Master toggle for all steering debug drawing.")]
        public static readonly SharedStatic<bool> Enabled = SharedStatic<bool>.GetOrCreate<Tags.Enabled>();

        [ConfigVar("steering.draw-intent", true, "Draw preferred-velocity arrows on agents.")]
        public static readonly SharedStatic<bool> DrawIntent = SharedStatic<bool>.GetOrCreate<Tags.DrawIntent>();

        [ConfigVar("steering.draw-field", true, "Draw influence field arrows for all channels.")]
        public static readonly SharedStatic<bool> DrawField = SharedStatic<bool>.GetOrCreate<Tags.DrawField>();

        [ConfigVar("steering.draw-boundary", true, "Draw the field boundary rectangle.")]
        public static readonly SharedStatic<bool> DrawBoundary = SharedStatic<bool>.GetOrCreate<Tags.DrawBoundary>();

        [ConfigVar("steering.draw-objectives", true, "Draw nav objective markers.")]
        public static readonly SharedStatic<bool> DrawObjectives = SharedStatic<bool>.GetOrCreate<Tags.DrawObjectives>();

        [ConfigVar("steering.draw-threats", true, "Draw threat source markers.")]
        public static readonly SharedStatic<bool> DrawThreats = SharedStatic<bool>.GetOrCreate<Tags.DrawThreats>();

        [ConfigVar("steering.field-step", 4, "Cell sampling stride (higher = sparser arrows).")]
        public static readonly SharedStatic<int> FieldStep = SharedStatic<int>.GetOrCreate<Tags.FieldStep>();

        [ConfigVar("steering.field-scale", 1.5f, "Arrow length scale factor.")]
        public static readonly SharedStatic<float> FieldScale = SharedStatic<float>.GetOrCreate<Tags.FieldScale>();

        private struct Tags
        {
            public struct Enabled { }
            public struct DrawIntent { }
            public struct DrawField { }
            public struct DrawBoundary { }
            public struct DrawObjectives { }
            public struct DrawThreats { }
            public struct FieldStep { }
            public struct FieldScale { }
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

            var step = math.max(1, SteeringDebugConfig.FieldStep.Data);
            var scale = math.max(0.01f, SteeringDebugConfig.FieldScale.Data);

            if (SteeringDebugConfig.DrawIntent.Data)
                state.Dependency = new DrawIntentJob { Drawer = drawer }.ScheduleParallel(state.Dependency);

            if (SteeringDebugConfig.DrawField.Data)
                state.Dependency = new DrawFieldJob
                {
                    Drawer = drawer,
                    Step = step,
                    Scale = scale,
                }.ScheduleParallel(state.Dependency);

            if (SteeringDebugConfig.DrawBoundary.Data)
                state.Dependency = new DrawBoundaryJob { Drawer = drawer }.ScheduleParallel(state.Dependency);

            if (SteeringDebugConfig.DrawObjectives.Data)
                state.Dependency = new DrawObjectivesJob { Drawer = drawer }.ScheduleParallel(state.Dependency);

            if (SteeringDebugConfig.DrawThreats.Data)
                state.Dependency = new DrawThreatsJob { Drawer = drawer }.ScheduleParallel(state.Dependency);
        }

        // -----------------------------------------------------------------------
        // Per-channel color lookup
        // -----------------------------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UnityEngine.Color ChannelColor(int channel)
        {
            return channel switch
            {
                (int)Influence.Objective => SteeringDebugColors.Objective,
                (int)Influence.Threat => SteeringDebugColors.Threat,
                (int)Influence.AllyPressure => SteeringDebugColors.AllyPressure,
                (int)Influence.Hazard => SteeringDebugColors.Hazard,
                (int)Influence.Lure => SteeringDebugColors.Lure,
                _ => SteeringDebugColors.Field,
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float ChannelHeight(int channel)
        {
            // Stack each channel at a different height so they don't overlap.
            return 0.1f + (channel * 0.2f);
        }

        // -----------------------------------------------------------------------
        // Jobs
        // -----------------------------------------------------------------------

        [BurstCompile]
        private partial struct DrawIntentJob : IJobEntity
        {
            public Drawer Drawer;

            private void Execute(in LocalTransform transform, in SteeringIntent intent)
            {
                var pos = transform.Position + new float3(0f, 0.4f, 0f);
                var dir = new float3(intent.PreferredVelocity.x, 0f, intent.PreferredVelocity.y);
                if (math.lengthsq(dir) >0.01f)
                    Drawer.Arrow(pos, dir, SteeringDebugColors.Intent);
            }
        }

        [BurstCompile]
        private partial struct DrawFieldJob : IJobEntity
        {
            public Drawer Drawer;
            public int Step;
            public float Scale;

            private void Execute(in InfluenceField field, in DynamicBuffer<InfluenceValue> values)
            {
                var size = field.Size;

                for (var y = 0; y < size.y; y += Step)
                for (var x = 0; x < size.x; x += Step)
                {
                    var cell = new int2(x, y);

                    for (var channel = 0; channel < field.Channels; channel++)
                    {
                        var sample = Steering.Sample(in field, in values, channel, field.CellCenter(cell));
                        var lenSq = math.lengthsq(sample);
                        if (lenSq <= 1e-6f)
                            continue;

                        var worldPos = field.CellCenterWorld(cell);
                        worldPos += new float2(0f, ChannelHeight(channel));
                        var p3 = new float3(worldPos.x, 0f, worldPos.y);

                        var len = math.sqrt(lenSq);
                        var normalized = new float3(sample.x / len, 0f, sample.y / len);
                        var arrowLen = math.min(len, Scale);
                        var dir = normalized * arrowLen;

                        Drawer.Arrow(p3, dir, ChannelColor(channel));
                    }
                }
            }
        }

        [BurstCompile]
        private partial struct DrawBoundaryJob : IJobEntity
        {
            public Drawer Drawer;

            private void Execute(in InfluenceField field)
            {
                var half = (float2)(field.Size) * field.Step * 0.5f;
                var center = field.WorldOrigin + half;

                var corners = stackalloc float3[4];
                corners[0] = new float3(center.x - half.x, 0f, center.y - half.y);
                corners[1] = new float3(center.x + half.x, 0f, center.y - half.y);
                corners[2] = new float3(center.x + half.x, 0f, center.y + half.y);
                corners[3] = new float3(center.x - half.x, 0f, center.y + half.y);

                for (var i = 0; i < 4; i++)
                    Drawer.Line(corners[i], corners[(i + 1) % 4], SteeringDebugColors.Field);
            }
        }

        [BurstCompile]
        private partial struct DrawObjectivesJob : IJobEntity
        {
            public Drawer Drawer;

            private void Execute(in NavObjective obj)
            {
                var pos = new float3(obj.Position.x, 0f, obj.Position.y);
                Drawer.Sphere(pos, 0.5f, 16, SteeringDebugColors.Objective);
                Drawer.Text32(pos + new float3(0f, 1f, 0f), "Objective", SteeringDebugColors.Objective, 10f);
            }
        }

        [BurstCompile]
        private partial struct DrawThreatsJob : IJobEntity
        {
            public Drawer Drawer;

            private void Execute(in ThreatSource threat, in LocalTransform transform)
            {
                var pos = transform.Position;
                Drawer.Circle(pos, new float3(0f, threat.Radius, 0f), SteeringDebugColors.Threat);
                Drawer.Text32(pos + new float3(0f, 0.5f, 0f), "Threat", SteeringDebugColors.Threat, 10f);
            }
        }
    }
}
#endif
