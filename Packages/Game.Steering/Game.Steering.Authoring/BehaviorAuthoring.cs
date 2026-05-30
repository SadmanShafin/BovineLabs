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
        public float MaxAcceleration;
        public float TurnResponse;
        public float DeadZone;
        public InfluenceWeight[] Weights;
    }

    public StageDefinition[] Stages = Array.Empty<StageDefinition>();

    private class BehaviorBaker : Baker<BehaviorAuthoring>
    {
        public override void Bake(BehaviorAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            var blob = BuildBlob(authoring.Stages);
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
                headers[s] = new StageHeader
                {
                    MaxSpeed = math.max(0f, stages[s].MaxSpeed),

                    // Good default: can reach max speed in about 0.125 seconds.
                    MaxAcceleration = stages[s].MaxAcceleration > 0f
                        ? stages[s].MaxAcceleration
                        : math.max(1f, stages[s].MaxSpeed * 8f),

                    TurnResponse = stages[s].TurnResponse > 0f
                        ? stages[s].TurnResponse
                        : 12f,

                    DeadZone = stages[s].DeadZone > 0f
                        ? stages[s].DeadZone
                        : 0.001f,
                };

                foreach (var entry in stages[s].Weights)
                {
                    var channel = (int)entry.Influence;
                    if ((uint)channel < Influences.Count)
                        weights[(s * Influences.Count) + channel] = entry.Weight;
                }
            }

            var result = builder.CreateBlobAssetReference<BehaviorBlob>(Allocator.Persistent);
            builder.Dispose();
            return result;
        }
    }
}