using Unity.Entities;
using Unity.Transforms;

[UpdateInGroup(typeof(PresentationSystemGroup))]
[Unity.Entities.WorldSystemFilter(Unity.Entities.WorldSystemFilterFlags.LocalSimulation | Unity.Entities.WorldSystemFilterFlags.ClientSimulation | Unity.Entities.WorldSystemFilterFlags.ServerSimulation)]
partial class MainCameraSystem : SystemBase
{
    protected override void OnUpdate()
    {
        if (MainGameObjectCamera.Instance != null && SystemAPI.HasSingleton<MainEntityCamera>())
        {
            Entity mainEntityCameraEntity = SystemAPI.GetSingletonEntity<MainEntityCamera>();
            LocalToWorld targetLocalToWorld = SystemAPI.GetComponent<LocalToWorld>(mainEntityCameraEntity);
            MainGameObjectCamera.Instance.transform.SetPositionAndRotation(targetLocalToWorld.Position, targetLocalToWorld.Rotation);
        }
    }
}
