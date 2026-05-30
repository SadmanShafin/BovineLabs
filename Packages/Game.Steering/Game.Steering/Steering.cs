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
        public float MaxForce;
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

    public static class Steering
    {
        private const int Directions = 16;
        private const float Epsilon = 1e-4f;

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
                return previousVelocity;

            var stageIndex = math.clamp(stage, 0, behavior.Stages.Length - 1);
            ref var header = ref behavior.Stages[stageIndex];

            var maxSpeed = math.max(0f, header.MaxSpeed);
            if (maxSpeed <= 0f)
                return float2.zero;

            var requiredValueCount = field.Size.x * field.Size.y * field.Channels;
            if (values.Length < requiredValueCount)
                return Truncate(previousVelocity, maxSpeed);

            var weightBase = stageIndex * Influences.Count;
            var remainingWeights = behavior.Weights.Length - weightBase;
            if (remainingWeights <= 0)
                return Truncate(previousVelocity, maxSpeed);

            var channelCount = math.min(field.Channels, math.min(Influences.Count, remainingWeights));

            var slots = SlotDirections();
            var interest = Zeroed();
            var danger = Zeroed();

            for (var channel = 0; channel < channelCount; channel++)
            {
                var gain = behavior.Weights[weightBase + channel];
                if (gain <= Epsilon)
                    continue;

                var sample = Sample(in field, in values, channel, position);
                var strength = math.length(sample);
                if (strength <= Epsilon)
                    continue;

                var heading = sample / strength;
                var score = strength * gain;

                if (IsDanger(channel))
                    Accumulate(ref danger, in slots, heading, score);
                else
                    Accumulate(ref interest, in slots, heading, score);
            }

            var direction = Select(in interest, in danger, in slots, previousVelocity);
            var maxForce = header.MaxForce > 0f ? header.MaxForce : math.max(1f, maxSpeed * 8f);

            return Reynolds(previousVelocity, direction, maxSpeed, maxForce, deltaTime);
        }

        public static float2 Sample(
            in InfluenceField field,
            in DynamicBuffer<InfluenceValue> values,
            int channel,
            float2 position)
        {
            if ((uint)channel >= field.Channels || field.Step <= 0f)
                return float2.zero;

            var requiredValueCount = field.Size.x * field.Size.y * field.Channels;
            if (values.Length < requiredValueCount)
                return float2.zero;

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
                return float2.zero;

            return values[field.IndexOf(channel, cell)].Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsDanger(int channel)
        {
            return channel == (int)Influence.Threat ||
                   channel == (int)Influence.Hazard;
        }

        private static FixedList512Bytes<float2> SlotDirections()
        {
            var slots = new FixedList512Bytes<float2>();

            for (var i = 0; i < Directions; i++)
            {
                math.sincos((2f * math.PI * i) / Directions, out var sin, out var cos);
                slots.Add(new float2(cos, sin));
            }

            return slots;
        }

        private static FixedList128Bytes<float> Zeroed()
        {
            var scores = new FixedList128Bytes<float>();

            for (var i = 0; i < Directions; i++)
                scores.Add(0f);

            return scores;
        }

        private static void Accumulate(
            ref FixedList128Bytes<float> scores,
            in FixedList512Bytes<float2> slots,
            float2 heading,
            float magnitude)
        {
            for (var i = 0; i < slots.Length; i++)
                scores[i] += magnitude * math.max(0f, math.dot(slots[i], heading));
        }

        private static float2 Select(
            in FixedList128Bytes<float> interest,
            in FixedList128Bytes<float> danger,
            in FixedList512Bytes<float2> slots,
            float2 fallbackVelocity)
        {
            var lowestDanger = float.MaxValue;

            for (var i = 0; i < slots.Length; i++)
                lowestDanger = math.min(lowestDanger, danger[i]);

            var bestInterest = 0f;
            var chosen = float2.zero;

            for (var i = 0; i < slots.Length; i++)
            {
                if (danger[i] > lowestDanger + Epsilon)
                    continue;

                if (interest[i] > bestInterest)
                {
                    bestInterest = interest[i];
                    chosen = slots[i];
                }
            }

            if (bestInterest > Epsilon)
                return chosen;

            return math.normalizesafe(fallbackVelocity);
        }

        private static float2 Reynolds(
            float2 velocity,
            float2 direction,
            float maxSpeed,
            float maxForce,
            float deltaTime)
        {
            var desired = direction * maxSpeed;
            var steering = Truncate(desired - velocity, maxForce);
            return Truncate(velocity + steering * deltaTime, maxSpeed);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float2 Truncate(float2 value, float max)
        {
            if (max <= 0f)
                return float2.zero;

            var lengthSq = math.lengthsq(value);
            if (lengthSq <= max * max || lengthSq <= 1e-8f)
                return value;

            return value * (max * math.rsqrt(lengthSq));
        }
    }
}