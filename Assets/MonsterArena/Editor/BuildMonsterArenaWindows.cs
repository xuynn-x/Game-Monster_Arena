using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class BuildMonsterArenaWindows
{
    private const string ArenaScene =
        "Assets/MonsterArena/Scenes/Arena.unity";

    private const string OutputPath =
        "Builds/MonsterArena/MonsterArena.exe";

    [MenuItem("Monster Arena/Build Windows Client")]
    public static void BuildWindowsClient()
    {
        Directory.CreateDirectory(
            Path.GetDirectoryName(OutputPath)
        );

        BuildReport report = BuildPipeline.BuildPlayer(
            new[] { ArenaScene, "Assets/MonsterArena/Scenes/MainMenu.unity", "Assets/MonsterArena/Scenes/Auth/Login.unity" },
            OutputPath,
            BuildTarget.StandaloneWindows64,
            BuildOptions.None
        );

        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new System.InvalidOperationException(
                $"Monster Arena build failed: {report.summary.result}"
            );
        }

        UnityEngine.Debug.Log(
            $"Monster Arena client built at {OutputPath}"
        );
    }
}
