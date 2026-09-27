using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class ApplyArenaEnvironment
{
    const string Folder = "Assets/MonsterArena/Art/Environment/CelestialSanctuary";
    const string Request = "Library/ApplyArenaEnvironment.request";
    [InitializeOnLoadMethod]
    static void Schedule()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            Apply();
            File.Delete(Request);
        };
    }

    [MenuItem("Monster Arena/Environment/Apply Celestial Sanctuary")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var scene = SceneManager.GetSceneByPath("Assets/MonsterArena/Scenes/Arena.unity");
        if (!scene.IsValid() || !scene.isLoaded)
            throw new InvalidOperationException("Open Arena in Edit Mode first.");
        var existingCamera = Array.Find(scene.GetRootGameObjects(), root => root.name == "Main Camera")?.GetComponent<Camera>();
        bool alreadyApplied = existingCamera != null && existingCamera.GetComponent<Skybox>()?.material ==
            AssetDatabase.LoadAssetAtPath<Material>(Folder + "/SanctuarySkybox.mat") && existingCamera.GetComponent<Skybox>() != null;
        if (scene.isDirty)
            throw new InvalidOperationException("Save your existing Arena edits before applying the environment.");
        AssetDatabase.ImportAsset(Folder + "/SanctuaryPanorama.png", ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(Folder + "/SanctuaryPanorama.png");
        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = true;
        importer.wrapModeU = TextureWrapMode.Repeat;
        importer.wrapModeV = TextureWrapMode.Clamp;
        importer.maxTextureSize = 4096;
        importer.mipmapEnabled = true;
        importer.SaveAndReimport();
        var material = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/SanctuarySkybox.mat");
        if (material == null)
        {
            material = new Material(Shader.Find("MonsterArena/Sanctuary Sky"));
            AssetDatabase.CreateAsset(material, Folder + "/SanctuarySkybox.mat");
        }
        material.shader = Shader.Find("MonsterArena/Sanctuary Sky");
        material.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/SanctuaryPanorama.png"));
        material.SetFloat("_HorizonOffset", 0.26f);
        material.SetFloat("_Exposure", 1);
        material.SetFloat("_Rotation", 90);
        EditorUtility.SetDirty(material);
        Camera camera = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == "CelestialFrostArena")
            {
                Undo.RecordObject(root.transform, "Position arena scenery");
                // Centre scenery on the same origin used by the two mirrored team cameras.
                root.transform.position = Vector3.zero;
                // The imported prefab has a 22 m footprint. Widen only the ground plane.
                root.transform.localScale = new Vector3(30f / 22f, 1f, 30f / 22f);
                PrefabUtility.RecordPrefabInstancePropertyModifications(root.transform);
            }
            if (root.name == "ArenaFloor")
            {
                Undo.RecordObject(root.transform, "Align arena collision floor");
                var position = root.transform.position;
                position.z = 0f;
                root.transform.position = position;
                root.transform.localScale = new Vector3(3f, 1f, 3f);
            }
            if (root.name == "Main Camera") camera = root.GetComponent<Camera>();
        }
        if (camera == null) throw new InvalidOperationException("Arena camera missing.");
        // A camera-specific skybox preserves the existing scene lighting and other scenes.
        var sky = camera.GetComponent<Skybox>();
        if (sky == null) sky = Undo.AddComponent<Skybox>(camera.gameObject);
        Undo.RecordObject(sky, "Assign sanctuary panorama");
        sky.material = material;
        EditorUtility.SetDirty(sky);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Capture(camera);
        Debug.Log("[Arena Environment] Applied scenery only. Camera transform, HUD, gameplay and lighting unchanged.");
    }

    static void Capture(Camera camera)
    {
        var rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
        var old = RenderTexture.active;
        Texture2D image = null;
        try
        {
            rt.Create();
            RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = rt });
            RenderTexture.active = rt;
            image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            image.Apply();
            Directory.CreateDirectory("Docs/Validation/CelestialSanctuary");
            File.WriteAllBytes("Docs/Validation/CelestialSanctuary/Arena-applied.png", image.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = old;
            if (image != null) UnityEngine.Object.DestroyImmediate(image);
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);
        }
    }
}
