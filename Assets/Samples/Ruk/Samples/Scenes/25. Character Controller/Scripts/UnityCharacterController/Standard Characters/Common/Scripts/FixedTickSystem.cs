using Unity.Burst;
using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup), OrderLast = true)]
[BurstCompile]
[Unity.Entities.WorldSystemFilter(Unity.Entities.WorldSystemFilterFlags.LocalSimulation | Unity.Entities.WorldSystemFilterFlags.ClientSimulation | Unity.Entities.WorldSystemFilterFlags.ServerSimulation)]
partial struct FixedTickSystem : ISystem
{
    public struct Singleton : IComponentData
    {
        public uint Tick;
    }

    public void OnCreate(ref SystemState state)
    {
        if (!SystemAPI.HasSingleton<Singleton>())
        {
            Entity singletonEntity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(singletonEntity, new Singleton());
        }
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        ref Singleton singleton = ref SystemAPI.GetSingletonRW<Singleton>().ValueRW;
        singleton.Tick++;
    }
}
