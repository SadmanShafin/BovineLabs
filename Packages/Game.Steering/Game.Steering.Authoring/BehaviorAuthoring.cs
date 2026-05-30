using System;
using Game.Steering;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class BehaviorAuthoring : MonoBehaviour
{
    [Serializable]
    public struct InfluenceWeight
    {
        public Influence Influence;
        public float Weight;
    }

    [Serializable]
    public struct StageDefinition
    {
        public string Name;
        public float MaxSpeed;

        // Acceleration / steering force limit.
        public float MaxForce;

        public InfluenceWeight[] Weights;
    }

    public StageDefinition[] Stages = Array.Empty<StageDefinition>();

    private class BehaviorBaker : Baker<BehaviorAuthoring>
    {
        public override void Bake(BehaviorAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            var blob = BuildBlob(authoring.Stages ?? Array.Empty<StageDefinition>());
            AddBlobAsset(ref blob, out _);

            AddComponent(entity, new BehaviorRef { Value = blob });
            AddComponent(entity, new Stage { Index = 0 });
            AddComponent(entity, new SteeringIntent());
        }

        private static BlobAssetReference<BehaviorBlob> BuildBlob(StageDefinition[] stages)
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<BehaviorBlob>();

            var headers = builder.Allocate(ref root.Stages, stages.Length);
            var weights = builder.Allocate(ref root.Weights, stages.Length * Influences.Count);

            for (var i = 0; i < weights.Length; i++)
                weights[i] = 0f;

            for (var s = 0; s < stages.Length; s++)
            {
                var maxSpeed = math.max(0f, stages[s].MaxSpeed);

                headers[s] = new StageHeader
                {
                    MaxSpeed = maxSpeed,

                    // Default: can reach max speed in about 0.125 seconds.
                    MaxForce = stages[s].MaxForce > 0f
                        ? stages[s].MaxForce
                        : math.max(1f, maxSpeed * 8f),
                };

                var stageWeights = stages[s].Weights;
                if (stageWeights == null)
                    continue;

                foreach (var entry in stageWeights)
                {
                    var channel = (int)entry.Influence;
                    if ((uint)channel < Influences.Count)
                        weights[(s * Influences.Count) + channel] = math.max(0f, entry.Weight);
                }
            }

            var result = builder.CreateBlobAssetReference<BehaviorBlob>(Allocator.Persistent);
            builder.Dispose();
            return result;
        }
    }
}