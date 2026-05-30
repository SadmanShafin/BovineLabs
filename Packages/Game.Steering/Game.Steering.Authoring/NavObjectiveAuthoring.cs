using Game.Steering;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class NavObjectiveAuthoring : MonoBehaviour
{
    private class Baker : Baker<NavObjectiveAuthoring>
    {
        public override void Bake(NavObjectiveAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            var pos = authoring.transform.position;
            AddComponent(entity, new NavObjective { Position = new float2(pos.x, pos.z) });
        }
    }
}