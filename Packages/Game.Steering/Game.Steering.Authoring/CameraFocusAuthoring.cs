using Game.Steering;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class CameraFocusAuthoring : MonoBehaviour
{
    private class Baker : Baker<CameraFocusAuthoring>
    {
        public override void Bake(CameraFocusAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            var p = authoring.transform.position;
            AddComponent(entity, new CameraFocus { Position = new float2(p.x, p.z) });
        }
    }
}