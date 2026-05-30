using Game.Steering;
using Unity.Entities;
using UnityEngine;

public class ThreatSourceAuthoring : MonoBehaviour
{
    public float Radius = 10f;
    public float Strength = 5f;

    private class Baker : Baker<ThreatSourceAuthoring>
    {
        public override void Bake(ThreatSourceAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new ThreatSource
            {
                Radius = authoring.Radius,
                Strength = authoring.Strength
            });
        }
    }
}