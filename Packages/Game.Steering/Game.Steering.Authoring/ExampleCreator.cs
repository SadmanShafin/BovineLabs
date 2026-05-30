#if UNITY_EDITOR
using UnityEngine;
using UnityEditor.SceneManagement;

public static class ExampleCreator
{
    [UnityEditor.MenuItem("BovineLabs/Steering/Create Example Scene")]
    public static void CreateExampleScene()
    {
        var subScenePath = "Assets/Scenes/Physics.unity"; // fixed typo
        var subScene = EditorSceneManager.OpenScene(subScenePath, OpenSceneMode.Additive);
        EditorSceneManager.SetActiveScene(subScene);

        var olds = new[] { "CameraFocus", "WorldOccupancy", "NavObjective", "ThreatSource", "Enemy" };
        foreach (var o in olds)
        {
            var go = GameObject.Find(o);
            if (go!= null && go.scene.path == subScene.path)
                Object.DestroyImmediate(go);
        }

        var focusGO = new GameObject("CameraFocus");
        focusGO.AddComponent<CameraFocusAuthoring>();

        var objGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        objGO.name = "NavObjective";
        objGO.transform.position = new Vector3(10f, 0f, 10f);
        objGO.AddComponent<NavObjectiveAuthoring>();

        var threatGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        threatGO.name = "ThreatSource";
        threatGO.transform.position = new Vector3(-5f, 0f, 5f);
        var tAuth = threatGO.AddComponent<ThreatSourceAuthoring>();
        tAuth.Radius = 15f;
        tAuth.Strength = 10f;

        var enemyGO = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        enemyGO.name = "Enemy";
        enemyGO.transform.position = new Vector3(0f, 1f, 0f);

        var behavior = enemyGO.AddComponent<BehaviorAuthoring>();
        behavior.Stages = new BehaviorAuthoring.StageDefinition[1];
            behavior.Stages[0] = new BehaviorAuthoring.StageDefinition
        {
            Name = "Default",
            MaxSpeed = 5f,
            MaxForce = 40f,
            Weights = new[]
            {
                new BehaviorAuthoring.InfluenceWeight
                {
                    Influence = Game.Steering.Influence.Objective,
                    Weight = 1f
                },
                new BehaviorAuthoring.InfluenceWeight
                {
                    Influence = Game.Steering.Influence.Threat,
                    Weight = 1f
                }
            }
        };

        EditorSceneManager.SaveScene(subScene);
        EditorSceneManager.CloseScene(subScene, false);
        Debug.Log("Created example in subscene.");
    }
}
#endif