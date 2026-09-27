using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BuildLocalBattleTest
{
    public const string ScenePath = "Assets/MonsterArena/Scenes/BattleTest.unity";
    public const string ArenaPath = "Assets/MonsterArena/Scenes/Arena.unity";
    private const string ArenaRootName = "Offline Battle Test";
    private const string Stamp = "Library/OfflineBattleIntegrated.v4";

    [InitializeOnLoadMethod]
    private static void Schedule()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(Stamp) && !EditorApplication.isPlayingOrWillChangePlaymode)
                IntegrateIntoArena();
        };
    }

    [MenuItem("Monster Arena/Battle/Step 2 - Integrate Offline Test Into Arena")]
    public static void IntegrateIntoArena()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var fox = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MonsterArena/Models/BattleReady/ShadowFox/ShadowFox_Visual.prefab");
        var trainer = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MonsterArena/Models/BattleReady/Trainer/Trainer_Visual.prefab");
        if (fox == null || trainer == null) throw new InvalidOperationException("Step 1 visual prefabs missing.");

        Scene previous = SceneManager.GetActiveScene();
        Scene scene = SceneManager.GetSceneByPath(ArenaPath);
        bool openedHere = !scene.IsValid() || !scene.isLoaded;
        if (openedHere) scene = EditorSceneManager.OpenScene(ArenaPath, OpenSceneMode.Additive);
        try
        {
            SceneManager.SetActiveScene(scene);
            GameObject oldRoot = FindRoot(scene, ArenaRootName);
            if (oldRoot != null) UnityEngine.Object.DestroyImmediate(oldRoot);

            foreach (string oldName in new[] { "NetworkBootstrap", "PlayerMonster", "EnemyMonster",
                         "PlayerTrainer", "EnemyTrainer", "BattleHUD", "BattleManager" })
            {
                GameObject old = FindRoot(scene, oldName);
                if (old != null) old.SetActive(false);
            }

            Camera camera = FindRoot(scene, "Main Camera")?.GetComponent<Camera>();
            if (camera == null) throw new InvalidOperationException("Arena Main Camera missing.");
            foreach (MonoBehaviour behaviour in camera.GetComponents<MonoBehaviour>())
                if (behaviour != null && behaviour.GetType().Name == "BattleCameraController")
                    behaviour.enabled = false;
            // Approved diagonal staging: both trainers remain fully visible behind
            // their monsters, with a clear lane through the centre for skill VFX.
            camera.transform.position = new Vector3(0, 6.1f, -13.5f);
            camera.transform.LookAt(new Vector3(0, 1.65f, 0));
            camera.fieldOfView = 46;
            camera.nearClipPlane = .1f;

            var root = new GameObject(ArenaRootName);
            var match = root.AddComponent<LocalBattleTest>();
            match.onlineBattle = true;
            match.autoOpponent = false;
            GameObject blueFox = Spawn(fox, "Blue Shadow Fox", new Vector3(-1.9f, 0, -2.2f), 31f, root.transform);
            GameObject redFox = Spawn(fox, "Red Shadow Fox", new Vector3(1.9f, 0, 2.2f), 211f, root.transform);
            GameObject blueTrainer = Spawn(trainer, "Blue Trainer", new Vector3(-4.6f, 0, -4f), 31f, root.transform);
            GameObject redTrainer = Spawn(trainer, "Red Trainer", new Vector3(4.6f, 0, 4f), 211f, root.transform);
            blueFox.transform.localScale = redFox.transform.localScale = Vector3.one * 1.8f;
            blueTrainer.transform.localScale = redTrainer.transform.localScale = Vector3.one * 1.6f;
            Tint(blueFox, new Color(.72f, .86f, 1));
            Tint(blueTrainer, new Color(.72f, .86f, 1));
            Tint(redFox, new Color(1, .72f, .72f));
            Tint(redTrainer, new Color(1, .72f, .72f));
            match.blue = blueFox.GetComponentInChildren<Animator>();
            match.red = redFox.GetComponentInChildren<Animator>();
            match.battleCamera = camera;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ArenaPath);
            AssetDatabase.SaveAssets();
            File.WriteAllText(Stamp, DateTime.UtcNow.ToString("O"));
            Debug.Log("[Battle Test] Integrated two teams, camera, HP and three skills into Arena. Fusion objects retained but disabled.");
        }
        finally
        {
            if (previous.IsValid()) SceneManager.SetActiveScene(previous);
            if (openedHere) EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.name == name) return root;
        return null;
    }

    private static GameObject Spawn(GameObject prefab, string name, Vector3 position, float yaw, Transform parent)
    {
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = name;
        instance.transform.SetParent(parent, false);
        instance.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
        return instance;
    }

    private static void Tint(GameObject actor, Color color)
    {
        var block = new MaterialPropertyBlock();
        block.SetColor("_BaseColor", color);
        foreach (Renderer renderer in actor.GetComponentsInChildren<Renderer>())
            renderer.SetPropertyBlock(block);
    }
}
