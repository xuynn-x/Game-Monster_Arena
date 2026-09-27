using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class InstallCelestialFrostArena
{
    private const string ScenePath = "Assets/MonsterArena/Scenes/Arena.unity";
    private const string ArtFolder = "Assets/MonsterArena/Art/CelestialFrostArena";
    private const string ModelPath = ArtFolder + "/Meshy_AI_Celestial_Frost_Arena_0919170027_texture.fbx";
    private const string BaseMapPath = ArtFolder + "/Meshy_AI_Celestial_Frost_Arena_0919170027_texture.png";
    private const string NormalMapPath = ArtFolder + "/Meshy_AI_Celestial_Frost_Arena_0919170027_texture_normal.png";
    private const string MetallicPath = ArtFolder + "/Meshy_AI_Celestial_Frost_Arena_0919170027_texture_metallic.png";
    private const string RoughnessPath = ArtFolder + "/Meshy_AI_Celestial_Frost_Arena_0919170027_texture_roughness.png";
    private const string PackedMapPath = ArtFolder + "/CelestialFrostArena_MetallicSmoothness.png";
    private const string MaterialPath = ArtFolder + "/CelestialFrostArena.mat";
    private const string PrefabPath = ArtFolder + "/CelestialFrostArena.prefab";
    private const string InstanceName = "CelestialFrostArena";
    private const float TargetDiameter = 22f;

    [InitializeOnLoadMethod]
    private static void ScheduleInstall()
    {
        EditorApplication.delayCall += InstallIfNeeded;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
            EditorApplication.delayCall += InstallIfNeeded;
    }

    private static void InstallIfNeeded()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) == null)
            return;

        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool openedHere = !scene.IsValid() || !scene.isLoaded;
        if (openedHere)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

        try
        {
            if (FindRoot(scene, InstanceName) != null &&
                AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
                return;

            Install(scene);
        }
        finally
        {
            if (openedHere && scene.IsValid() && scene.isLoaded)
                EditorSceneManager.CloseScene(scene, true);
        }
    }

    [MenuItem("Monster Arena/Environment/Install Celestial Frost Arena")]
    public static void InstallFromMenu()
    {
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool openedHere = !scene.IsValid() || !scene.isLoaded;
        if (openedHere)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

        try
        {
            Install(scene);
        }
        finally
        {
            if (openedHere && scene.IsValid() && scene.isLoaded)
                EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static void Install(Scene scene)
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (model == null)
            throw new InvalidOperationException("Celestial Frost Arena FBX has not been imported.");

        ConfigureTexture(NormalMapPath, TextureImporterType.NormalMap, false);
        ConfigureTexture(MetallicPath, TextureImporterType.Default, true);
        ConfigureTexture(RoughnessPath, TextureImporterType.Default, true);

        CreatePackedMetallicSmoothness();
        Material material = CreateOrUpdateMaterial();
        GameObject visualPrefab = CreateOrUpdatePrefab(model, material);

        GameObject previous = FindRoot(scene, InstanceName);
        if (previous != null)
            UnityEngine.Object.DestroyImmediate(previous);

        GameObject floor = FindRoot(scene, "ArenaFloor");
        if (floor != null)
        {
            Renderer oldRenderer = floor.GetComponent<Renderer>();
            if (oldRenderer != null)
                oldRenderer.enabled = false;
            floor.transform.SetPositionAndRotation(new Vector3(0f, -0.03f, 0f), Quaternion.identity);
            // Unity's built-in plane is ten units wide, not a unit cube.
            floor.transform.localScale = new Vector3(TargetDiameter / 10f, 1f, TargetDiameter / 10f);
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(visualPrefab, scene);
        instance.name = InstanceName;
        instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        instance.transform.localScale = Vector3.one;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("[Arena] Celestial Frost Arena fitted to a 22 m diameter with its battle surface at Y=0.");
    }

    private static void ConfigureTexture(string path, TextureImporterType type, bool linear)
    {
        if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
            return;

        bool changed = importer.textureType != type || importer.sRGBTexture == linear;
        importer.textureType = type;
        importer.sRGBTexture = !linear;
        if (changed)
            importer.SaveAndReimport();
    }

    private static void CreatePackedMetallicSmoothness()
    {
        TextureImporter metallicImporter = AssetImporter.GetAtPath(MetallicPath) as TextureImporter;
        TextureImporter roughnessImporter = AssetImporter.GetAtPath(RoughnessPath) as TextureImporter;
        if (metallicImporter == null || roughnessImporter == null)
            return;

        bool metallicReadable = metallicImporter.isReadable;
        bool roughnessReadable = roughnessImporter.isReadable;
        metallicImporter.isReadable = true;
        roughnessImporter.isReadable = true;
        metallicImporter.SaveAndReimport();
        roughnessImporter.SaveAndReimport();

        Texture2D metallic = AssetDatabase.LoadAssetAtPath<Texture2D>(MetallicPath);
        Texture2D roughness = AssetDatabase.LoadAssetAtPath<Texture2D>(RoughnessPath);
        if (metallic == null || roughness == null ||
            metallic.width != roughness.width || metallic.height != roughness.height)
            throw new InvalidOperationException("Metallic and roughness textures must have matching dimensions.");

        Color32[] metalPixels = metallic.GetPixels32();
        Color32[] roughPixels = roughness.GetPixels32();
        var packedPixels = new Color32[metalPixels.Length];
        for (int i = 0; i < packedPixels.Length; i++)
            packedPixels[i] = new Color32(metalPixels[i].r, 0, 0, (byte)(255 - roughPixels[i].r));

        var packed = new Texture2D(metallic.width, metallic.height, TextureFormat.RGBA32, false, true);
        packed.SetPixels32(packedPixels);
        packed.Apply(false, false);
        File.WriteAllBytes(PackedMapPath, packed.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(packed);

        metallicImporter.isReadable = metallicReadable;
        roughnessImporter.isReadable = roughnessReadable;
        metallicImporter.SaveAndReimport();
        roughnessImporter.SaveAndReimport();
        AssetDatabase.ImportAsset(PackedMapPath, ImportAssetOptions.ForceSynchronousImport);
        ConfigureTexture(PackedMapPath, TextureImporterType.Default, true);
    }

    private static Material CreateOrUpdateMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            throw new InvalidOperationException("URP/Lit shader is unavailable.");

        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(shader) { name = "CelestialFrostArena" };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        else
        {
            material.shader = shader;
        }

        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(BaseMapPath));
        material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(NormalMapPath));
        material.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(PackedMapPath));
        material.SetFloat("_Metallic", 1f);
        material.SetFloat("_Smoothness", 0.42f);
        material.SetFloat("_BumpScale", 0.65f);
        material.EnableKeyword("_NORMALMAP");
        material.EnableKeyword("_METALLICSPECGLOSSMAP");
        EditorUtility.SetDirty(material);
        return material;
    }

    private static GameObject CreateOrUpdatePrefab(GameObject model, Material material)
    {
        var root = new GameObject(InstanceName);
        try
        {
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(model);
            visual.name = "Visual";
            visual.transform.SetParent(root.transform, false);

            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                    materials[i] = material;
                renderer.sharedMaterials = materials;
            }

            Bounds bounds = CalculateBounds(visual);
            float diameter = Mathf.Max(bounds.size.x, bounds.size.z);
            if (diameter <= Mathf.Epsilon)
                throw new InvalidOperationException("Imported arena has no usable renderer bounds.");

            // Preserve the FBX unit conversion already present on its root.
            visual.transform.localScale *= TargetDiameter / diameter;
            bounds = CalculateBounds(visual);
            visual.transform.position += new Vector3(-bounds.center.x, 0f, -bounds.center.z);
            // Align the centre walking surface rather than the bottom of the pedestal.
            float surfaceY = bounds.max.y;
            bool foundSurface = false;
            foreach (var filter in visual.GetComponentsInChildren<MeshFilter>(true))
            {
                var probe = filter.gameObject.AddComponent<MeshCollider>();
                probe.sharedMesh = filter.sharedMesh;
                Physics.SyncTransforms();
                if (probe.Raycast(new Ray(new Vector3(0, bounds.max.y + 1f, 0), Vector3.down), out var hit, bounds.size.y + 2f))
                {
                    surfaceY = foundSurface ? Mathf.Max(surfaceY, hit.point.y) : hit.point.y;
                    foundSurface = true;
                }
                UnityEngine.Object.DestroyImmediate(probe);
            }
            if (!foundSurface) throw new InvalidOperationException("Cannot locate arena centre surface.");
            visual.transform.position -= Vector3.up * surfaceY;

            return PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static Bounds CalculateBounds(GameObject root)
    {
        // Renderer bounds can still reflect the previous transform in an editor import.
        Bounds bounds = new Bounds();
        bool initialized = false;
        foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null) continue;
            Bounds local = filter.sharedMesh.bounds;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 p = filter.transform.TransformPoint(local.center + Vector3.Scale(local.extents,
                    new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1)));
                if (!initialized) { bounds = new Bounds(p, Vector3.zero); initialized = true; }
                else bounds.Encapsulate(p);
            }
        }
        return bounds;
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.name == name)
                return root;
        return null;
    }
}
