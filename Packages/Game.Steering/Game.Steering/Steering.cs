using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Steering
{
    public struct StageHeader
    {
        public float MaxSpeed;

        // Units: velocity units / second.
        // This prevents instant velocity snapping.
        public float MaxAcceleration;

        // Higher = faster heading response.
        // Used with frame-rate independent exponential smoothing.
        public float TurnResponse;

        // Ignore tiny noisy field values.
        public float DeadZone;
    }

    public struct BehaviorBlob
    {
        public BlobArray<StageHeader> Stages;
        public BlobArray<float> Weights;
    }

    public struct BehaviorRef : IComponentData
    {
        public BlobAssetReference<BehaviorBlob> Value;
    }

    public struct Stage : IComponentData
    {
        public byte Index;
    }

    public struct SteeringIntent : IComponentData
    {
        public float2 PreferredVelocity;
        public float MaxSpeed;
    }

    /// <summary>
    /// Weighted steering over sampled influence fields.
    ///
    /// Backing:
    /// - Reynolds 1999: weighted steering behaviors.
    /// - Treuille, Cooper, Popović 2006: dynamic potential/flow fields for crowds.
    ///
    /// This only outputs preferred velocity. Final collision-free velocity should still be handled
    /// by ORCA/RVO or your existing avoidance system.
    /// </summary>
    public static class Steering
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 Resolve(
            in InfluenceField field,
            in DynamicBuffer<InfluenceValue> values,
            ref BehaviorBlob behavior,
            int stage,
            float2 position,
            float2 previousVelocity,
            float deltaTime)
        {
            if (deltaTime <= 0f || behavior.Stages.Length == 0 || field.Channels <= 0)
            {
                return previousVelocity;
            }

            var stageIndex = math.clamp(stage, 0, behavior.Stages.Length - 1);
            ref var header = ref behavior.Stages[stageIndex];

            var maxSpeed = math.max(0f, header.MaxSpeed);
            if (maxSpeed <= 0f)
            {
                return float2.zero;
            }

            var requiredValueCount = field.Size.x * field.Size.y * field.Channels;
            if (values.Length < requiredValueCount)
            {
                return ClampMagnitude(previousVelocity, maxSpeed);
            }

            var weightBase = stageIndex * field.Channels;
            var accumulation = float2.zero;

            for (var channel = 0; channel < field.Channels; channel++)
            {
                var weight = behavior.Weights[weightBase + channel];
                if (math.abs(weight) <= 1e-5f)
                {
                    continue;
                }

                accumulation += weight * Sample(in field, in values, channel, position);
            }

            var magnitude = math.length(accumulation);
            var deadZone = math.max(0f, header.DeadZone);

            var desiredVelocity = float2.zero;
            if (magnitude > deadZone)
            {
                // Field magnitude controls speed, direction controls heading.
                // Objective fields near the goal can intentionally slow the agent down.
                var desiredSpeed = maxSpeed * math.saturate(magnitude);
                desiredVelocity = (accumulation / magnitude) * desiredSpeed;
            }

            // Frame-rate independent response:
            // alpha = 1 - exp(-response * dt)
            var response = header.TurnResponse <= 0f
                ? 1f
                : 1f - math.exp(-header.TurnResponse * deltaTime);

            var steered = math.lerp(previousVelocity, desiredVelocity, response);

            // Acceleration clamp prevents sudden snapping.
            var maxAcceleration = header.MaxAcceleration;
            if (maxAcceleration > 0f)
            {
                var maxDelta = maxAcceleration * deltaTime;
                var delta = steered - previousVelocity;
                var deltaSq = math.lengthsq(delta);

                if (deltaSq > maxDelta * maxDelta)
                {
                    steered = previousVelocity + (delta * math.rsqrt(deltaSq)) * maxDelta;
                }
            }

            return ClampMagnitude(steered, maxSpeed);
        }

        public static float2 Sample(
            in InfluenceField field,
            in DynamicBuffer<InfluenceValue> values,
            int channel,
            float2 position)
        {
            if ((uint)channel >= field.Channels || field.Step <= 0f)
            {
                return float2.zero;
            }

            // Convert world position to cell-center space.
            // Cell center maps exactly to integer cell coordinate.
            var local = (position * field.InvStep) - (float2)field.Origin - 0.5f;
            var baseCell = (int2)math.floor(local);
            var fraction = local - baseCell;

            var bottomLeft = At(in field, in values, channel, baseCell);
            var bottomRight = At(in field, in values, channel, baseCell + new int2(1, 0));
            var topLeft = At(in field, in values, channel, baseCell + new int2(0, 1));
            var topRight = At(in field, in values, channel, baseCell + new int2(1, 1));

            var bottom = math.lerp(bottomLeft, bottomRight, fraction.x);
            var top = math.lerp(topLeft, topRight, fraction.x);

            return math.lerp(bottom, top, fraction.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float2 At(
            in InfluenceField field,
            in DynamicBuffer<InfluenceValue> values,
            int channel,
            int2 cell)
        {
            if (math.any(cell < 0) || math.any(cell >= field.Size))
            {
                return float2.zero;
            }

            return values[field.IndexOf(channel, cell)].Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float2 ClampMagnitude(float2 value, float maxLength)
        {
            var lengthSq = math.lengthsq(value);
            if (lengthSq <= maxLength * maxLength || lengthSq <= 1e-8f)
            {
                return value;
            }

            return value * (maxLength * math.rsqrt(lengthSq));
        }
    }
}