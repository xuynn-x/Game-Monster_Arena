using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Explicit local requests only; never saves the user's scene or changes combat data on disk.
public static class ValidateBattleAudio
{
    const string Request = "Library/BattleAudio.request";
    const string Report = "Docs/Validation/BattleAudio/";
    const string Running = "MonsterArena.AudioValidation.Running";
    const string SavedScene = "MonsterArena.AudioValidation.Scene";
    const string VolumeKey = "MonsterArena.MasterVolume";
    static readonly Stack<IEnumerator> routines = new Stack<IEnumerator>();
    static double nextPoll;

    [InitializeOnLoadMethod]
    static void Initialize()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += OnPlayState;
    }

    static void Tick()
    {
        if (routines.Count > 0)
        {
            try
            {
                var routine = routines.Peek();
                if (!routine.MoveNext()) routines.Pop();
                else if (routine.Current is IEnumerator nested) routines.Push(nested);
            }
            catch (Exception exception)
            {
                File.AppendAllText(Report + (SessionState.GetBool(Running + ".Online", false) ? "OnlineTests.txt" : "PlayTests.txt"), "FAIL: " + exception + "\n");
                routines.Clear();
                EditorApplication.isPlaying = false;
            }
            return;
        }
        if (EditorApplication.timeSinceStartup < nextPoll || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        nextPoll = EditorApplication.timeSinceStartup + 1;
        if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
        string command = File.ReadAllText(Request).Trim();
        File.Delete(Request);
        try
        {
            if (command == "test") RunPlayTests();
            else if (command == "online") StartChecks(true);
            else if (command == "build") Build();
            else throw new InvalidOperationException("Unknown audio validation command: " + command);
        }
        catch (Exception exception)
        {
            Directory.CreateDirectory(Report);
            File.WriteAllText(Report + "RequestError.txt", exception.ToString());
            Debug.LogException(exception);
        }
    }

    static void RequireCleanScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Unsaved scene: no changes made by validation.");
    }

    static void ValidateAssets()
    {
        Directory.CreateDirectory(Report);
        var paths = Directory.GetFiles("Assets/MonsterArena/Resources/BattleAudio/ShadowFox", "*.wav");
        if (paths.Length != 11) throw new InvalidOperationException("Expected eleven approved audio assets.");
        var lines = new List<string>();
        foreach (string file in paths)
        {
            string path = file.Replace('\\', '/');
            var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            if (importer == null) throw new InvalidOperationException("Not imported: " + path);
            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.PCM;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
            importer.loadInBackground = false;
            importer.SaveAndReimport();
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null || clip.frequency != 48000 || clip.channels != 1 || clip.samples == 0)
                throw new InvalidOperationException("Invalid clip: " + path);
            var samples = new float[clip.samples];
            if (!clip.GetData(samples, 0)) throw new InvalidOperationException("Cannot decode: " + path);
            float peak = 0;
            foreach (float sample in samples) peak = Mathf.Max(peak, Mathf.Abs(sample));
            if (peak <= .01f || peak >= .99f) throw new InvalidOperationException("Silent/clipped clip: " + path);
            lines.Add(clip.name + " | " + clip.samples + " samples | peak=" + peak);
        }
        File.WriteAllLines(Report + "UnityAssets.txt", lines);
    }

    [MenuItem("Monster Arena/Audio/Run Playback Checks")]
    public static void RunPlayTests() => StartChecks(false);

    static void StartChecks(bool online)
    {
        RequireCleanScene();
        if (SceneManager.sceneCount != 1 || string.IsNullOrEmpty(SceneManager.GetActiveScene().path))
            throw new InvalidOperationException("Playback checks require a single saved scene.");
        ValidateAssets();
        string originalScene = SceneManager.GetActiveScene().path;
        SessionState.SetString(SavedScene, originalScene);
        SessionState.SetBool(Running, true);
        SessionState.SetBool(Running + ".Online", online);
        SessionState.SetBool(Running + ".HadVolume", PlayerPrefs.HasKey(VolumeKey));
        SessionState.SetFloat(Running + ".Volume", PlayerPrefs.GetFloat(VolumeKey, 1));
        SessionState.SetFloat(Running + ".Listener", AudioListener.volume);
        File.WriteAllText(Report + (online ? "OnlineTests.txt" : "PlayTests.txt"), "Started " + DateTime.UtcNow.ToString("O") + "\n");
        if (originalScene != "Assets/MonsterArena/Scenes/Arena.unity")
            EditorSceneManager.OpenScene("Assets/MonsterArena/Scenes/Arena.unity");
        var battle = Object.FindFirstObjectByType<LocalBattleTest>();
        if (battle == null) throw new InvalidOperationException("Arena controller missing.");
        // In-memory test configuration only. Restore the saved scene on leaving Play Mode.
        battle.onlineBattle = online;
        battle.autoOpponent = false;
        EditorApplication.isPlaying = true;
    }

    static void OnPlayState(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Running, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
            routines.Push(SessionState.GetBool(Running + ".Online", false) ? OnlineChecks() : PlayChecks());
        if (state != PlayModeStateChange.EnteredEditMode) return;
        routines.Clear();
        if (SessionState.GetBool(Running + ".HadVolume", false))
            PlayerPrefs.SetFloat(VolumeKey, SessionState.GetFloat(Running + ".Volume", 1));
        else PlayerPrefs.DeleteKey(VolumeKey);
        PlayerPrefs.Save();
        AudioListener.volume = SessionState.GetFloat(Running + ".Listener", 1);
        SessionState.SetBool(Running, false);
        EditorSceneManager.OpenScene(SessionState.GetString(SavedScene, "Assets/MonsterArena/Scenes/Arena.unity"));
        File.AppendAllText(Report + "PlayTests.txt", "Restored saved scene and original volume. No scene saved by tests.\n");
    }

    static IEnumerator Until(Func<bool> condition, string label, float timeout = 10)
    {
        float deadline = Time.realtimeSinceStartup + timeout;
        while (!condition())
        {
            if (Time.realtimeSinceStartup > deadline) throw new Exception("Timeout: " + label);
            yield return null;
        }
    }

    static IEnumerator OnlineChecks()
    {
        yield return null;
        Application.runInBackground = true;
        var battle = Object.FindFirstObjectByType<LocalBattleTest>();
        var audio = battle.GetComponent<ArenaBattleAudio>();
        audio.Diagnostics = true;
        var result = Source(audio, "Result");
        yield return Until(() => battle.NetworkReady, "second client connection", 90);
        int firstRound = battle.LocalMonster.Round;
        File.AppendAllText(Report + "OnlineTests.txt", "Connected two clients; editor blue=" + battle.LocalMonster.BlueTeam + "\n");
        for (int index = 0; index < 6; index++)
        {
            int skill = index % 3;
            yield return Until(() => battle.LocalMonster.RemainingCooldown(skill) <= 0 && !battle.IsBusy && !battle.LocalMonster.Recovering,
                "skill readiness", 15);
            if (!battle.TryCast(0, skill)) throw new Exception("Online cast rejected.");
            yield return Until(() => battle.IsBusy, "online cast started");
            yield return Until(() => !battle.IsBusy, "online cast completed");
            File.AppendAllText(Report + "OnlineTests.txt", "Editor cast " + skill + " opponentHP=" + battle.OpponentMonster.CurrentHealth + "\n");
        }
        yield return Until(() => result.isPlaying, "local victory audio");
        if (result.clip.name != "Victory") throw new Exception("Wrong local victory cue.");
        File.AppendAllText(Report + "OnlineTests.txt", "PASS editor Victory. WAIT Windows client R for rematch.\n");
        battle.Restart();
        yield return Until(() => battle.NetworkReady && battle.LocalMonster.Round > firstRound, "mutual rematch", 120);
        if (!Silent(audio)) throw new Exception("Audio survived new round.");
        File.AppendAllText(Report + "OnlineTests.txt", "PASS new round silent. WAIT Windows Q/W/E attacks until editor loses.\n");
        yield return Until(() => result.isPlaying && result.clip.name == "Defeat", "remote client victory / editor defeat", 150);
        File.AppendAllText(Report + "OnlineTests.txt", "PASS editor Defeat. Both local result perspectives exercised.\n");
        battle.LeaveMatch();
        if (!Silent(audio)) throw new Exception("Online leave did not stop sources.");
        yield return Until(() => SceneManager.GetActiveScene().name == "MainMenu", "online shutdown", 30);
        File.AppendAllText(Report + "OnlineTests.txt", "ALL ONLINE CHECKS PASSED " + DateTime.UtcNow.ToString("O") + "\n");
        EditorApplication.isPlaying = false;
    }

    static void Check(bool condition, string label)
    {
        if (!condition) throw new Exception(label);
        File.AppendAllText(Report + "PlayTests.txt", "PASS: " + label + "\n");
    }

    static AudioSource Source(ArenaBattleAudio audio, string name) => audio.transform.Find("Battle Audio " + name).GetComponent<AudioSource>();
    static bool Silent(ArenaBattleAudio audio)
    {
        foreach (var source in audio.GetComponentsInChildren<AudioSource>()) if (source.isPlaying) return false;
        return true;
    }

    static IEnumerator PlayChecks()
    {
        yield return null;
        var battle = Object.FindFirstObjectByType<LocalBattleTest>();
        var audio = battle.GetComponent<ArenaBattleAudio>();
        Check(audio != null && !battle.onlineBattle, "Runtime audio created; offline test session only");
        Check(audio.GetComponentsInChildren<AudioSource>().Length == 5, "Two cast, two impact, one result source");
        var result = Source(audio, "Result");
        var hp = (int[])typeof(LocalBattleTest).GetField("hp", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(battle);
        for (int team = 0; team < 2; team++)
        for (int skill = 0; skill < 3; skill++)
        {
            battle.Restart();
            string key = new[] { "Q", "W", "E" }[skill];
            var cast = Source(audio, "Cast " + team);
            var impact = Source(audio, "Impact " + team);
            Check(battle.TryCast(team, skill), "Cast accepted: team " + team + " " + key);
            Check(cast.clip.name == key + "_Charge" && cast.isPlaying, "Charge playback " + team + " " + key);
            yield return Until(() => cast.clip.name == key + "_Launch" && cast.isPlaying, "launch " + key);
            yield return Until(() => impact.isPlaying, "impact " + key);
            Check(hp[1 - team] == 100 - new[] { 12, 20, 32 }[skill], "Impact matches damage event " + team + " " + key);
            yield return Until(() => !battle.IsBusy, "cast finish");
            Check(!result.isPlaying, "No result sound before defeat");
        }

        audio.BeginCharge(0, 2, .25f);
        var late = Source(audio, "Cast 0");
        Check(late.time > 1.6f, "Late event seeks into charge instead of replaying from start");
        audio.Launch(0, 2);
        Check(late.clip.name == "E_Launch" && late.time < .1f, "Launch resets to exact launch event");
        audio.BeginCharge(0, 2, 0);
        Check(!late.isPlaying, "Expired windup does not replay charge");

        var slider = Object.FindFirstObjectByType<Canvas>().GetComponentInChildren<Slider>(true);
        if (slider == null) slider = Object.FindFirstObjectByType<Slider>(FindObjectsInactive.Include);
        slider.value = 0;
        Check(AudioListener.volume == 0, "Existing settings slider mutes master audio");
        slider.value = .37f;
        Check(Mathf.Abs(AudioListener.volume - .37f) < .001f && Mathf.Abs(PlayerPrefs.GetFloat(VolumeKey) - .37f) < .001f,
            "Existing slider updates and persists volume");

        foreach (bool won in new[] { true, false })
        {
            battle.Restart();
            hp[won ? 1 : 0] = 1;
            Check(battle.TryCast(won ? 0 : 1, 0), "Lethal cast accepted");
            yield return Until(() => result.isPlaying, "result cue");
            Check(result.clip.name == (won ? "Victory" : "Defeat"), "Correct local result: " + result.clip.name);
            float started = Time.realtimeSinceStartup;
            yield return Until(() => Time.realtimeSinceStartup - started > .4f, "result advance");
            Check(result.time > .3f, "Repeated HUD refresh does not restart result cue");
            audio.StopAll();
            yield return null;
            yield return null;
            Check(Silent(audio), "Stopped result is not replayed in same round");
        }
        battle.Restart();
        hp[0] = hp[1] = 0;
        yield return null;
        yield return null;
        Check(!result.isPlaying, "Draw does not announce victory or defeat");
        battle.Restart();
        Check(battle.TryCast(0, 2), "Cast before reset accepted");
        battle.Restart();
        Check(Silent(audio), "Rematch clears all audio");
        audio.BeginCharge(0, 2, 1.5f);
        battle.onlineBattle = true;
        yield return null;
        yield return null;
        Check(Silent(audio), "Lost network readiness stops audio even outside active cast");
        battle.onlineBattle = false;
        audio.Launch(0, 0);
        battle.enabled = false;
        Check(Silent(audio), "Disabling battle stops audio");
        battle.enabled = true;
        audio.Launch(0, 0);
        battle.LeaveMatch();
        Check(Silent(audio), "Leaving match immediately stops audio");
        yield return Until(() => SceneManager.GetActiveScene().name == "MainMenu", "leave match");
        Check(Object.FindFirstObjectByType<ArenaBattleAudio>() == null, "No audio host survives leaving arena");
        File.AppendAllText(Report + "PlayTests.txt", "ALL PLAYBACK CHECKS PASSED " + DateTime.UtcNow.ToString("O") + "\n");
        EditorApplication.isPlaying = false;
    }

    [MenuItem("Monster Arena/Audio/Build Windows Audio Client")]
    public static void Build()
    {
        RequireCleanScene();
        ValidateAssets();
        const string output = "Builds/MonsterArena-Audio/MonsterArena.exe";
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report = BuildPipeline.BuildPlayer(new[] { "Assets/MonsterArena/Scenes/Arena.unity",
            "Assets/MonsterArena/Scenes/MainMenu.unity", "Assets/MonsterArena/Scenes/Auth/Login.unity" },
            output, BuildTarget.StandaloneWindows64, BuildOptions.None);
        File.WriteAllText(Report + "Build.txt", report.summary.result + "\n" + DateTime.UtcNow.ToString("O") + "\n" + output
            + "\nErrors=" + report.summary.totalErrors + " Warnings=" + report.summary.totalWarnings);
        if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Audio client build failed.");
    }
}
