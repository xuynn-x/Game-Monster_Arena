using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine.SceneManagement;

public static class ValidateMatchmaking
{
    private const string Request = "Library/Matchmaking.request";
    private const string Reports = "Docs/Validation/Matchmaking/";
    private static double nextPoll;

    [InitializeOnLoadMethod]
    private static void Initialize() => EditorApplication.update += Poll;

    private static void Poll()
    {
        if (EditorApplication.timeSinceStartup < nextPoll || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        nextPoll = EditorApplication.timeSinceStartup + 1;
        if (!File.Exists(Request)) return;
        string command = File.ReadAllText(Request).Trim();
        File.Delete(Request);
        Directory.CreateDirectory(Reports);
        try
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Unsaved scene; validation will not save or replace it.");
            if (command != "build" && command != "build-test") throw new InvalidOperationException("Unknown request.");
            Build(command == "build-test");
        }
        catch (Exception exception) { File.WriteAllText(Reports + "RequestError.txt", exception.ToString()); }
    }

    [MenuItem("Monster Arena/Matchmaking/Build Windows Client")]
    public static void BuildRelease() => Build(false);

    public static void Build(bool development)
    {
        Directory.CreateDirectory(Reports);
        string folder = development ? "Builds/MonsterArena-Matchmaking-Test" : "Builds/MonsterArena-Matchmaking";
        Directory.CreateDirectory(folder);
        var result = BuildPipeline.BuildPlayer(new[] {
            "Assets/MonsterArena/Scenes/Auth/Login.unity", BattleMatchmaking.MenuPath, BattleMatchmaking.ArenaPath
        }, folder + "/MonsterArena.exe", BuildTarget.StandaloneWindows64,
            development ? BuildOptions.Development : BuildOptions.None);
        File.WriteAllText(Reports + (development ? "TestBuild.txt" : "Build.txt"),
            result.summary.result + "\n" + DateTime.UtcNow.ToString("O") + "\n" + folder +
            "\nErrors=" + result.summary.totalErrors + " Warnings=" + result.summary.totalWarnings);
        if (result.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Build failed.");
    }
}
