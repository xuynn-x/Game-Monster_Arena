using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

public static class ValidateArenaSizing
{
    const string Request = "Library/ArenaSizing.request";
    [InitializeOnLoadMethod]
    static void Schedule()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(Request);
            ValidateAndBuild();
        };
    }
    [MenuItem("Monster Arena/Environment/Validate Sized Arena and Build")]
    public static void ValidateAndBuild()
    {
        ApplyArenaEnvironment.Apply();
        var source = Camera.main;
        if (source == null) throw new InvalidOperationException("Missing battle camera");
        var obj = new GameObject("Temporary mirrored view validation") { hideFlags = HideFlags.HideAndDontSave };
        try
        {
            var camera = obj.AddComponent<Camera>();
            camera.CopyFrom(source);
            camera.enabled = false;
            obj.AddComponent<Skybox>().material = source.GetComponent<Skybox>().material;
            camera.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
            Capture(camera, "Blue");
            camera.transform.SetPositionAndRotation(new Vector3(-source.transform.position.x, source.transform.position.y, -source.transform.position.z),
                Quaternion.Euler(0, 180, 0) * source.transform.rotation);
            Capture(camera, "Red");
        }
        finally { UnityEngine.Object.DestroyImmediate(obj); }
        var output = "Builds/MonsterArena-Arena30/MonsterArena.exe";
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report = BuildPipeline.BuildPlayer(new[] { "Assets/MonsterArena/Scenes/Arena.unity",
            "Assets/MonsterArena/Scenes/MainMenu.unity", "Assets/MonsterArena/Scenes/Auth/Login.unity" },
            output, BuildTarget.StandaloneWindows64, BuildOptions.None);
        File.WriteAllText("Docs/Validation/ArenaSizing/Build.txt", report.summary.result + "\n" + DateTime.UtcNow.ToString("O") + "\n" + output);
        if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Arena build failed.");
        Debug.Log("[Arena Sizing] Both mirrored views rendered and Windows client built successfully.");
    }
    static void Capture(Camera camera, string side)
    {
        var rt = new RenderTexture(1920,1080,24);
        var old = RenderTexture.active;
        Texture2D capture = null;
        try
        {
            rt.Create();
            camera.aspect = 16f/9f;
            RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = rt });
            RenderTexture.active = rt;
            capture = new Texture2D(1920,1080,TextureFormat.RGB24,false);
            capture.ReadPixels(new Rect(0,0,1920,1080),0,0);
            capture.Apply();
            Directory.CreateDirectory("Docs/Validation/ArenaSizing");
            File.WriteAllBytes("Docs/Validation/ArenaSizing/" + side + ".png",capture.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = old;
            if(capture!=null) UnityEngine.Object.DestroyImmediate(capture);
            rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
        }
    }
}
