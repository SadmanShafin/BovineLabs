using Game.Steering;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Steering.Authoring
{
    /// <summary>
    /// Unified authoring for any influence source.
    /// Replaces separate CameraFocusAuthoring, NavObjectiveAuthoring, ThreatSourceAuthoring.
    /// </summary>
    public class InfluenceAuthoring : MonoBehaviour
    {
        [Tooltip("Which influence channel this source writes to.")]
        public Influence Channel = Influence.Objective;

        [Tooltip("How this source contributes to the channel.")]
        public InfluenceOp Operation = InfluenceOp.Add;

        [Tooltip("Shape of the influence area.")]
        public InfluenceShape Shape = InfluenceShape.Point;

        [Tooltip("Radius for Sphere shape. Ignored for Point.")]
        public float Radius = 10f;

        [Tooltip("Strength multiplier.")]
        public float Strength = 1f;

        [Tooltip("If true, this entity is the camera follow target and centers the field grid.")]
        public bool IsCameraFocus;

        private class Baker : Baker<InfluenceAuthoring>
        {
            public override void Bake(InfluenceAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new InfluenceSource
                {
                    Channel = (byte)authoring.Channel,
                    Operation = (byte)authoring.Operation,
                    Shape = (byte)authoring.Shape,
                    Radius = math.max(0f, authoring.Radius),
                    Strength = authoring.Strength,
                    IsCameraFocus = authoring.IsCameraFocus ? (byte)1 : (byte)0,
                });
            }
        }
    }
}
