#if UNITY_EDITOR
using Game.Steering;
using Game.Steering.Authoring;
using UnityEngine;
using UnityEditor.SceneManagement;

public static class ExampleCreator
{
    [UnityEditor.MenuItem("BovineLabs/Steering/Create Example Scene")]
    public static void CreateExampleScene()
    {
        var subScenePath = "Assets/Scenes/Physics.unity";
        var subScene = EditorSceneManager.OpenScene(subScenePath, OpenSceneMode.Additive);
        EditorSceneManager.SetActiveScene(subScene);

        var olds = new[] { "CameraFocus", "NavObjective", "ThreatSource", "Enemy" };
        foreach (var o in olds)
        {
            var go = GameObject.Find(o);
            if (go != null && go.scene.path == subScene.path)
                Object.DestroyImmediate(go);
        }

        // Camera focus — just an InfluenceAuthoring marker with IsCameraFocus.
        var focusGO = new GameObject("CameraFocus");
        var focusAuth = focusGO.AddComponent<InfluenceAuthoring>();
        focusAuth.Channel = Influence.Objective;
        focusAuth.Operation = InfluenceOp.Add;
        focusAuth.Shape = InfluenceShape.Point;
        focusAuth.IsCameraFocus = true;

        // Nav objective — point source on the Objective channel.
        var objGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        objGO.name = "NavObjective";
        objGO.transform.position = new Vector3(10f, 0f, 10f);
        var objAuth = objGO.AddComponent<InfluenceAuthoring>();
        objAuth.Channel = Influence.Objective;
        objAuth.Operation = InfluenceOp.Add;
        objAuth.Shape = InfluenceShape.Point;
        objAuth.Strength = 1f;

        // Threat source — sphere on the Threat channel.
        var threatGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        threatGO.name = "ThreatSource";
        threatGO.transform.position = new Vector3(-5f, 0f, 5f);
        var threatAuth = threatGO.AddComponent<InfluenceAuthoring>();
        threatAuth.Channel = Influence.Threat;
        threatAuth.Operation = InfluenceOp.Add;
        threatAuth.Shape = InfluenceShape.Sphere;
        threatAuth.Radius = 15f;
        threatAuth.Strength = 10f;

        // Enemy agent with behavior.
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
