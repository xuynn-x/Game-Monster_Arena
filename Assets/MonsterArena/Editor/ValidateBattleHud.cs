using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ValidateBattleHud
{
    private const string Request = "Library/BattleHudPreview.request";

    [InitializeOnLoadMethod]
    private static void Schedule()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(Request);
            RenderPreview();
        };
    }

    [MenuItem("Monster Arena/Battle/Validate HUD Artwork")]
    public static void RenderPreview()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Scene previous = SceneManager.GetActiveScene();
        var arena = UnityEngine.Object.FindFirstObjectByType<LocalBattleTest>();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        RenderTexture target = null;
        Texture2D capture = null;
        object hud = null;
        var hudType = typeof(LocalBattleTest).Assembly.GetType("ArenaBattleHud");
        try
        {
            SceneManager.SetActiveScene(scene);
            var host = new GameObject("HUD Preview").AddComponent<LocalBattleTest>();
            host.onlineBattle = false;
            var camera = new GameObject("Preview Camera").AddComponent<Camera>();
            host.battleCamera = camera;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.025f, .025f, .055f);
            camera.nearClipPlane = .01f;
            camera.cullingMask = 1 << 31;
            if (arena != null && arena.battleCamera != null)
            {
                camera.CopyFrom(arena.battleCamera);
                camera.transform.SetPositionAndRotation(arena.battleCamera.transform.position, arena.battleCamera.transform.rotation);
                camera.cullingMask |= 1 << 31;
                host.blue = arena.blue;
                host.red = arena.red;
            }
            target = new RenderTexture(1280, 720, 24);
            camera.targetTexture = target;
            hud = Activator.CreateInstance(hudType, new object[] { host });
            foreach (var root in scene.GetRootGameObjects())
            {
                var canvas = root.GetComponent<Canvas>();
                if (canvas == null) continue;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1;
                foreach (var child in root.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 31;
            }
            var cooldowns = new float[2, 3];
            cooldowns[0, 0] = Time.time + 1.2f;
            // Regression: an arena pet must retain its overhead UI while matchmaking.
            host.onlineBattle = true;
            hudType.GetMethod("Refresh").Invoke(hud, new object[] { new[] { 87, 46 }, cooldowns, false, false });
            var enemyCard = (RectTransform)hudType.GetField("enemyCard", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(hud);
            if (host.OpponentVisual != null && !enemyCard.gameObject.activeSelf)
                throw new InvalidOperationException("Enemy overhead UI is hidden while waiting for an opponent.");
            foreach (var label in enemyCard.GetComponentsInChildren<UnityEngine.UI.Text>(true))
                if ((label.text.StartsWith("HP ") || label.text.StartsWith("EP ")) && !label.text.Contains("—"))
                    throw new InvalidOperationException("Waiting overhead UI shows unverified resource values.");
            host.onlineBattle = false;
            hudType.GetMethod("Refresh").Invoke(hud, new object[] { new[] { 87, 46 }, cooldowns, false, false });
            Canvas.ForceUpdateCanvases();
            camera.Render();
            var oldTarget = RenderTexture.active;
            try
            {
                RenderTexture.active = target;
                capture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                capture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                capture.Apply();
            }
            finally { RenderTexture.active = oldTarget; }
            Directory.CreateDirectory("Docs/Validation");
            File.WriteAllBytes("Docs/Validation/BattleHud.png", capture.EncodeToPNG());
            File.WriteAllText("Docs/Validation/BattleHud.txt", "HUD instantiated and rendered in Unity. Network session was not started.\n" + DateTime.UtcNow.ToString("O"));
            Debug.Log("[Battle HUD] Preview rendered to Docs/Validation/BattleHud.png");
        }
        catch (Exception exception) { Debug.LogException(exception); }
        finally
        {
            // Runtime HUD destruction uses Destroy; release its generated sprites explicitly in Edit Mode.
            if (hud != null)
            {
                var sprites = hudType.GetField("artwork", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(hud) as System.Collections.IEnumerable;
                if (sprites != null) foreach (UnityEngine.Object sprite in sprites)
                {
                    if (sprite is Sprite circle && circle.name == "HUD Circle") UnityEngine.Object.DestroyImmediate(circle.texture);
                    UnityEngine.Object.DestroyImmediate(sprite);
                }
            }
            if (previous.IsValid()) SceneManager.SetActiveScene(previous);
            EditorSceneManager.CloseScene(scene, true);
            if (target != null) UnityEngine.Object.DestroyImmediate(target);
            if (capture != null) UnityEngine.Object.DestroyImmediate(capture);
        }
    }
}
