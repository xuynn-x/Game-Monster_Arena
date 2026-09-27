using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ClearDefaultMenuMessage
{
    [InitializeOnLoadMethod]
    private static void Initialize() => EditorApplication.delayCall += ApplyRequested;

    private static void ApplyRequested()
    {
        const string request = "Library/ClearDefaultMenuMessage.request";
        if (!File.Exists(request)) return;
        File.Delete(request);
        Clear();
    }

    [MenuItem("Monster Arena/UI/Clear Default Menu Message")]
    public static void Clear()
    {
        const string report = "Library/ClearDefaultMenuMessage-result.txt";
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != "Assets/MonsterArena/Scenes/MainMenu.unity")
        { File.WriteAllText(report, "Not changed: requires MainMenu in Edit Mode."); return; }
        var label = GameObject.Find("MainMenuCanvas/ActionMessageText")?.GetComponent<TMP_Text>();
        if (label == null) { File.WriteAllText(report, "Not changed: message label missing."); return; }
        Undo.RecordObject(label, "Clear default menu message");
        label.text = string.Empty;
        label.ForceMeshUpdate();
        EditorUtility.SetDirty(label);
        EditorSceneManager.MarkSceneDirty(scene);
        File.WriteAllText(report, "Saved=" + EditorSceneManager.SaveScene(scene) + "\nDefault message is empty; transform/style preserved.");
    }
}
