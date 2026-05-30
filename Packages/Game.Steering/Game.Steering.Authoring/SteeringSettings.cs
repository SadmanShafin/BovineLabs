using BovineLabs.Core.Authoring.Settings;
using BovineLabs.Core.Settings;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Steering.Authoring
{
    [SettingsGroup("Steering")]
    public class SteeringSettings : SettingsBase
    {
        [Header("Grid")]
        [Tooltip("Number of cells in (width, height).")]
        public int2 GridSize = new(128, 128);

        [Tooltip("World-space size of each cell.")]
        public float CellStep = 1f;

        [Tooltip("Number of influence channels (must match Influences.Count).")]
        public int Channels = Influences.Count;

        [Header("Field")]
        [Tooltip("Radius of the camera focus's influence area.")]
        public float FocusRadius = 64f;

        public override void Bake(Baker<SettingsAuthoring> baker)
        {
            var entity = baker.GetEntity(TransformUsageFlags.None);

            baker.AddComponent(entity, new InfluenceFieldConfig
            {
                Size = math.max(new int2(1, 1), GridSize),
                Step = math.max(0.01f, CellStep),
                InvStep = math.rcp(math.max(0.01f, CellStep)),
                Channels = math.max(1, Channels),
                FocusRadius = math.max(0f, FocusRadius),
            });
        }
    }
}
