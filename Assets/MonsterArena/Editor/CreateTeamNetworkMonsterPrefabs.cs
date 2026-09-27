using Fusion;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CreateTeamNetworkMonsterPrefabs
{
    private const string SourcePath =
        "Assets/MonsterArena/Prefabs/NetworkMonster.prefab";

    private const string BluePath =
        "Assets/MonsterArena/Prefabs/BlueNetworkMonster.prefab";

    private const string RedPath =
        "Assets/MonsterArena/Prefabs/RedNetworkMonster.prefab";

    [MenuItem("Monster Arena/Create Team Network Prefabs")]
    public static void Create()
    {
        NetworkObject blue = CreateOrUpdate(
            BluePath,
            new Vector3(-3f, 1f, -4f),
            Quaternion.Euler(0f, 37f, 0f)
        );

        NetworkObject red = CreateOrUpdate(
            RedPath,
            new Vector3(3f, 1f, 4f),
            Quaternion.Euler(0f, 217f, 0f)
        );

        NetworkMonsterSpawner[] spawners =
            Object.FindObjectsByType<NetworkMonsterSpawner>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (NetworkMonsterSpawner spawner in spawners)
        {
            SerializedObject serializedSpawner = new(spawner);
            serializedSpawner.FindProperty("blueMonsterPrefab")
                .objectReferenceValue = blue;
            serializedSpawner.FindProperty("redMonsterPrefab")
                .objectReferenceValue = red;
            serializedSpawner.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);
        }

        AssetDatabase.SaveAssets();
        Debug.Log(
            "[Monster Arena] Đã tạo BlueNetworkMonster và " +
            "RedNetworkMonster, đồng thời gán vào NetworkBootstrap."
        );
    }

    private static NetworkObject CreateOrUpdate(
        string targetPath,
        Vector3 position,
        Quaternion rotation
    )
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(targetPath) == null &&
            !AssetDatabase.CopyAsset(SourcePath, targetPath))
        {
            throw new System.InvalidOperationException(
                $"Không thể tạo prefab: {targetPath}"
            );
        }

        GameObject root = PrefabUtility.LoadPrefabContents(targetPath);
        root.transform.SetPositionAndRotation(position, rotation);
        PrefabUtility.SaveAsPrefabAsset(root, targetPath);
        PrefabUtility.UnloadPrefabContents(root);

        return AssetDatabase.LoadAssetAtPath<NetworkObject>(targetPath);
    }
}
