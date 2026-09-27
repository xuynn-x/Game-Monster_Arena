using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Creates isolated staging assets; never replaces the existing Arena or network prefabs.
public static class PrepareBattleModels
{
    private const string Root = "Assets/MonsterArena/Models/BattleReady";
    private const string Stamp = "Library/BattleModelsPrepared.v2";

    [InitializeOnLoadMethod]
    private static void Schedule()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(Stamp) && !EditorApplication.isPlayingOrWillChangePlaymode &&
                File.Exists(Root + "/Trainer/Trainer.fbx")) Prepare();
        };
    }

    [MenuItem("Monster Arena/Battle/Step 1 - Prepare Model Assets")]
    public static void Prepare()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Stop Play Mode before preparing model assets.");
            return;
        }
        try
        {
            AssetDatabase.Refresh();
            var report = new StringBuilder("# Battle model import verification\n\n");
            var fox = PrepareOne("ShadowFox", "Monster_Idle", report);
            var trainer = PrepareOne("Trainer", "idle", report);
            BuildPreview(fox, trainer, report);
            Directory.CreateDirectory("Docs");
            File.WriteAllText("Docs/Battle_Model_Import_Report.md", report.ToString());
            AssetDatabase.SaveAssets();
            File.WriteAllText(Stamp, DateTime.UtcNow.ToString("O"));
            Debug.Log("[Battle Models] Prepared two textured, animated visual prefabs and ModelPreview scene. Existing Arena unchanged.");
        }
        catch (Exception exception) { Debug.LogException(exception); }
    }

    private static GameObject PrepareOne(string name, string idleName, StringBuilder report)
    {
        string folder = Root + "/" + name;
        string modelPath = folder + "/" + name + ".fbx";
        AssetDatabase.ImportAsset(modelPath, ImportAssetOptions.ForceSynchronousImport);
        var importer = (ModelImporter)AssetImporter.GetAtPath(modelPath);
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = true;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.SaveAndReimport();
        var takes = importer.defaultClipAnimations;
        foreach (var clip in takes)
        {
            clip.loopTime = clip.name.EndsWith(idleName, StringComparison.OrdinalIgnoreCase);
            clip.loopPose = clip.loopTime;
        }
        importer.clipAnimations = takes;
        importer.SaveAndReimport();

        var albedo = ImportTexture(folder + "/Textures/Image_0.jpg", false, true);
        var normal = ImportTexture(folder + "/Textures/Image_2.jpg", true, false);
        var packed = ImportTexture(folder + "/Textures/Image_1.jpg", false, false, true);
        // Blender: metallic = B, roughness = G. URP: metallic = R, smoothness = A.
        var pixels = packed.GetPixels();
        for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color(pixels[i].b, 0, 0, 1 - pixels[i].g);
        var mask = new Texture2D(packed.width, packed.height, TextureFormat.RGBA32, false, true);
        mask.SetPixels(pixels);
        mask.Apply();
        string maskPath = folder + "/Textures/MetallicSmoothness.png";
        File.WriteAllBytes(maskPath, mask.EncodeToPNG());
        Object.DestroyImmediate(mask);
        var metallic = ImportTexture(maskPath, false, false);
        ImportTexture(folder + "/Textures/Image_1.jpg", false, false);

        string materialPath = folder + "/" + name + "_URP.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP Lit shader missing.");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, materialPath);
        }
        material.SetColor("_BaseColor", Color.white);
        material.SetTexture("_BaseMap", albedo);
        material.SetTexture("_BumpMap", normal);
        material.SetTexture("_MetallicGlossMap", metallic);
        material.SetFloat("_BumpScale", 1);
        material.SetFloat("_Smoothness", 1);
        material.EnableKeyword("_NORMALMAP");
        material.EnableKeyword("_METALLICSPECGLOSSMAP");
        EditorUtility.SetDirty(material);

        var clips = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<AnimationClip>()
            .Where(c => !c.name.StartsWith("__preview__")).ToArray();
        var idle = clips.FirstOrDefault(c => c.name.EndsWith(idleName, StringComparison.OrdinalIgnoreCase));
        if (idle == null) throw new InvalidOperationException(name + " has no idle clip.");
        string controllerPath = folder + "/" + name + ".controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        var machine = controller.layers[0].stateMachine;
        foreach (var clip in clips)
        {
            string stateName = clip == idle ? "Idle" : clip.name.Split('|').Last();
            var state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == stateName) ?? machine.AddState(stateName);
            state.motion = clip;
            if (clip == idle) machine.defaultState = state;
        }
        EditorUtility.SetDirty(controller);

        var preview = EditorSceneManager.NewPreviewScene();
        GameObject root = null;
        try
        {
            root = new GameObject(name + "_Visual");
            SceneManager.MoveGameObjectToScene(root, preview);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(modelPath), preview);
            model.name = "Model";
            model.transform.SetParent(root.transform, false);
            var animator = model.GetComponent<Animator>() ?? model.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            var renderers = model.GetComponentsInChildren<SkinnedMeshRenderer>();
            if (renderers.Length == 0) throw new InvalidOperationException(name + " has no skinned mesh.");
            foreach (var renderer in model.GetComponentsInChildren<Renderer>())
                renderer.sharedMaterials = renderer.sharedMaterials.Select(_ => material).ToArray();
            idle.SampleAnimation(model, 0);
            Bounds bounds = new Bounds();
            bool firstVertex = true;
            foreach (var renderer in renderers)
            {
                var baked = new Mesh();
                renderer.BakeMesh(baked);
                Debug.Log($"[Battle Model Bounds] {name}: baked={baked.bounds}, renderer={renderer.bounds}, lossyScale={renderer.transform.lossyScale}, rootScale={model.transform.lossyScale}");
                foreach (var vertex in baked.vertices)
                {
                    // BakeMesh returns scaled skin vertices; apply position and rotation only.
                    Vector3 world = renderer.transform.position + renderer.transform.rotation * vertex;
                    if (firstVertex) { bounds = new Bounds(world, Vector3.zero); firstVertex = false; }
                    else bounds.Encapsulate(world);
                }
                Object.DestroyImmediate(baked);
            }
            model.transform.position -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            if (bounds.size.y < .1f || bounds.size.y > 10f) throw new InvalidOperationException(name + " has unexpected scale: " + bounds.size);
            string prefabPath = folder + "/" + name + "_Visual.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            int vertices = renderers.Sum(r => r.sharedMesh.vertexCount);
            int triangles = renderers.Sum(r => r.sharedMesh.triangles.Length / 3);
            report.AppendLine($"## {name}\n\n- Skinned meshes: {renderers.Length}; vertices (Unity import): {vertices}; triangles: {triangles}.");
            report.AppendLine($"- Bones referenced: {renderers.Sum(r => r.bones.Length)}; avatar valid: {animator.avatar != null && animator.avatar.isValid}.");
            report.AppendLine($"- Idle bounds before grounding: {bounds.size}; grounded using sampled idle pose.");
            report.AppendLine($"- Textures: {albedo.width} x {albedo.height}; albedo, normal and converted metallic/smoothness assigned.");
            report.AppendLine("- Clips: " + string.Join(", ", clips.Select(c => $"{c.name} ({c.length:F2}s)")) + ".");
            report.AppendLine($"- Prefab: `{prefabPath}`. Generic rig, Idle default, root motion disabled.\n");
            return prefab;
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
    }

    private static Texture2D ImportTexture(string path, bool normal, bool srgb, bool readable = false)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        importer.sRGBTexture = srgb;
        importer.isReadable = readable;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static void BuildPreview(GameObject fox, GameObject trainer, StringBuilder report)
    {
        if (File.Exists("Assets/MonsterArena/Scenes/ModelPreview.unity"))
        {
            report.AppendLine("## Preview\n\nExisting `Assets/MonsterArena/Scenes/ModelPreview.unity` preserved; prefab references update in place. Existing Arena untouched.");
            return;
        }
        Scene previous = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        try
        {
            SceneManager.SetActiveScene(scene);
            var a = (GameObject)PrefabUtility.InstantiatePrefab(fox, scene);
            var b = (GameObject)PrefabUtility.InstantiatePrefab(trainer, scene);
            a.transform.position = new Vector3(-1.2f, 0, 0);
            b.transform.position = new Vector3(1.2f, 0, 0);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "PreviewFloor";
            floor.transform.position = new Vector3(0, -.1f, 0);
            floor.transform.localScale = new Vector3(8, .2f, 6);
            var light = new GameObject("KeyLight").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 2;
            light.transform.rotation = Quaternion.Euler(40, -30, 0);
            var fill = new GameObject("FillLight").AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(.66f, .7f, 1);
            fill.intensity = 1;
            fill.transform.rotation = Quaternion.Euler(20, 150, 0);
            var camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0, 2.3f, 6);
            camera.transform.LookAt(new Vector3(0, .95f, 0));
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.04f, .035f, .075f);
            camera.fieldOfView = 35;
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 40;
            EditorSceneManager.SaveScene(scene, "Assets/MonsterArena/Scenes/ModelPreview.unity");
            report.AppendLine("## Preview\n\n`Assets/MonsterArena/Scenes/ModelPreview.unity`: separate model review scene; not added to build settings. Existing Arena untouched.");
        }
        finally
        {
            SceneManager.SetActiveScene(previous);
            EditorSceneManager.CloseScene(scene, true);
        }
    }
}
