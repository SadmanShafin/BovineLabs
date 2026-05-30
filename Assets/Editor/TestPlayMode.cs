using UnityEditor;
using UnityEngine;
using Unity.Entities;

public static class TestPlayMode
{
    private static int frames = 0;

    [MenuItem("BovineLabs/Test/Run ECS Check")]
    public static void Run()
    {
        EditorApplication.update += OnUpdate;
        EditorApplication.isPlaying = true;
        frames = 0;
    }

    private static void OnUpdate()
    {
        if (!EditorApplication.isPlaying) return;

        frames++;
        if (frames == 100)
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world != null)
            {
                var em = world.EntityManager;
                var query = em.CreateEntityQuery(typeof(Game.Steering.SteeringIntent));
                var count = query.CalculateEntityCount();
                var intents = query.ToComponentDataArray<Game.Steering.SteeringIntent>(Unity.Collections.Allocator.Temp);
                
                Debug.Log($"[ECS CHECK] Entity count: {count}");
                foreach (var intent in intents)
                {
                    Debug.Log($"[ECS CHECK] Intent velocity: {intent.PreferredVelocity} (MaxSpeed: {intent.MaxSpeed})");
                }
                intents.Dispose();
            }
            else
            {
                Debug.Log("[ECS CHECK] World is null");
            }

            EditorApplication.isPlaying = false;
            EditorApplication.update -= OnUpdate;
        }
    }
}
