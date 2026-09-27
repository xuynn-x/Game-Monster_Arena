#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Opt-in development-player integration tests. Production builds exclude this entire class.
public sealed class MatchmakingValidationClient : MonoBehaviour
{
    private string scenario;
    private string report;
    private float deadline;
    private bool finished;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Boot()
    {
        var args = Environment.GetCommandLineArgs();
        int flag = Array.IndexOf(args, "-matchmaking-check");
        if (flag < 0 || flag + 2 >= args.Length) return;
        LocalAccountService.ValidationStorageSuffix = ".Validation." + Guid.NewGuid().ToString("N");
        var client = new GameObject("Matchmaking Validation").AddComponent<MatchmakingValidationClient>();
        DontDestroyOnLoad(client);
        client.scenario = args[flag + 1];
        client.report = Path.GetFullPath(args[flag + 2]);
        Directory.CreateDirectory(Path.GetDirectoryName(client.report));
        File.WriteAllText(client.report, "Started " + DateTime.UtcNow.ToString("O") + "\n");
        client.deadline = Time.realtimeSinceStartup + 110;
        Application.runInBackground = true;
    }

    private void Check(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException(label);
        File.AppendAllText(report, "PASS " + label + "\n");
    }

    private async Task Until(Func<bool> condition, float seconds = 40)
    {
        float until = Time.realtimeSinceStartup + seconds;
        while (!condition())
        {
            if (Time.realtimeSinceStartup > until) throw new TimeoutException("Timed out in " + scenario);
            await Task.Yield();
        }
    }

    private async Task Pause(float seconds)
    {
        float until = Time.realtimeSinceStartup + seconds;
        await Until(() => Time.realtimeSinceStartup >= until, seconds + 2);
    }

    private Button BattleButton() => GameObject.Find("BattleButton").GetComponent<Button>();

    private async void Start()
    {
        try
        {
            await Task.Yield();
            Check(LocalAccountService.Register("validation", "validation-local-only", "Validation", out _), "isolated account created");
            await SceneManager.LoadSceneAsync(BattleMatchmaking.MenuPath);
            await Task.Yield();
            Check(LocalAccountService.SaveTeam(Array.Empty<string>(), out _), "empty team saved");
            BattleButton().onClick.Invoke();
            Check(BattleMatchmaking.Instance == null, "BATTLE rejects empty team before connecting");
            Check(LocalAccountService.SaveTeam(new[] { MonsterCatalog.StarterShadowFoxId }, out _), "valid team saved");
            BattleButton().onClick.Invoke();
            Check(BattleMatchmaking.Instance != null, "BATTLE starts matchmaking");
            var original = BattleMatchmaking.Instance;
            BattleButton().onClick.Invoke();
            Check(BattleMatchmaking.Instance == original, "double click does not create another session");
            if (scenario == "cancel-connecting")
            {
                original.Cancel();
                await Until(() => BattleMatchmaking.Instance == null);
                Check(SceneManager.GetActiveScene().path == BattleMatchmaking.MenuPath, "cancel during startup stays in menu");
                Check(!NetworkRunner.Instances.Any(r => r.IsRunning), "no running runner after immediate cancel");
            }
            else
            {
                await Until(() => original == null || original.State != BattleMatchmaking.Phase.Connecting);
                Check(original != null && original.Runner != null && original.Runner.IsRunning, "Photon connected");
                if (scenario == "disconnect")
                {
                    await original.Runner.Shutdown();
                    await Until(() => BattleMatchmaking.Instance == null);
                    Check(SceneManager.GetActiveScene().path == BattleMatchmaking.MenuPath, "connection loss returns to menu");
                    BattleButton().onClick.Invoke();
                    Check(BattleMatchmaking.Instance != null, "can retry after connection loss");
                    await BattleMatchmaking.Instance.StopAsync();
                }
                else if (scenario == "cancel" || scenario == "solo" || scenario == "preview")
                {
                    await Pause(scenario == "preview" ? 35 : 3);
                    Check(original.State == BattleMatchmaking.Phase.Searching, "one player remains searching in menu");
                    Check(SceneManager.GetActiveScene().path == BattleMatchmaking.MenuPath, "Arena not entered early");
                    CaptureFrame();
                    await Pause(.5f);
                    GameObject.Find("Cancel Matchmaking").GetComponent<Button>().onClick.Invoke();
                    await Until(() => BattleMatchmaking.Instance == null);
                    await Task.Yield();
                    Check(!NetworkRunner.Instances.Any(r => r.IsRunning), "cancel button cleans running runner");
                    BattleButton().onClick.Invoke();
                    Check(BattleMatchmaking.Instance != null, "BATTLE can retry after cancellation");
                    await BattleMatchmaking.Instance.StopAsync();
                }
                else
                {
                    await Until(() => original == null || original.State == BattleMatchmaking.Phase.InArena, 65);
                    Check(original != null, "paired session survives scene change");
                    var battle = FindFirstObjectByType<LocalBattleTest>();
                    Check(battle != null && battle.NetworkReady, "two network monsters ready in Arena");
                    Check(NetworkRunner.Instances.Count(r => r.IsRunning) == 1, "exactly one running runner");
                    Check(original.Runner.ActivePlayers.Count() == 2, "exactly two players");
                    await Pause(1);
                    Check(!original.Runner.SessionInfo.IsOpen, "paired room closed to replacements");
                    File.AppendAllText(report, "ROOM " + original.Runner.SessionInfo.Name + "\nBLUE " + battle.LocalMonster.BlueTeam + "\n");
                    CaptureFrame();
                    await Pause(2);
                    Check(battle.TryCast(0, 0), "Q accepted after matchmaking handoff");
                    await Pause(3);
                    Check(battle.LocalMonster.CurrentHealth < 100 && battle.OpponentMonster.CurrentHealth < 100,
                        "both clients receive damage after handoff");
                    if (scenario == "pair-stay")
                    {
                        await Until(() => !battle.NetworkReady);
                        Check(battle.ConnectionMessage.Contains("rời"), "opponent departure shown without reopening matchmaking");
                        Check(!original.Runner.SessionInfo.IsOpen, "room remains closed after opponent leaves");
                    }
                    else await Pause(scenario == "pair-leave" ? 2 : 10);
                    battle.LeaveMatch();
                    await Until(() => SceneManager.GetActiveScene().path == BattleMatchmaking.MenuPath && BattleMatchmaking.Instance == null);
                    Check(!NetworkRunner.Instances.Any(r => r.IsRunning), "leave Arena cleans session and returns menu");
                    BattleButton().onClick.Invoke();
                    Check(BattleMatchmaking.Instance != null, "new search available after leaving Arena");
                    await BattleMatchmaking.Instance.StopAsync();
                }
            }
            Finish(null);
        }
        catch (Exception exception) { Finish(exception.ToString()); }
    }

    private void Update()
    {
        if (!finished && Time.realtimeSinceStartup > deadline) Finish("Global test timeout");
    }

    // Explicit rendering also works for hidden Windows players; normal screenshot capture
    // can return a black buffer when Windows suppresses presentation of an occluded window.
    private void CaptureFrame()
    {
        var camera = Camera.main;
        if (camera == null) throw new InvalidOperationException("Missing capture camera");
        var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None)
            .Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
        var oldCameras = canvases.Select(c => c.worldCamera).ToArray();
        var oldDistances = canvases.Select(c => c.planeDistance).ToArray();
        var previous = camera.targetTexture;
        var previousActive = RenderTexture.active;
        var target = new RenderTexture(1280, 720, 24);
        var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = target;
            foreach (var canvas in canvases)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = camera.nearClipPlane + .1f;
            }
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            pixels.Apply();
            File.WriteAllBytes(Path.ChangeExtension(report, ".png"), pixels.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previous;
            RenderTexture.active = previousActive;
            for (int i = 0; i < canvases.Length; i++)
            {
                canvases[i].renderMode = RenderMode.ScreenSpaceOverlay;
                canvases[i].worldCamera = oldCameras[i];
                canvases[i].planeDistance = oldDistances[i];
            }
            Destroy(target);
            Destroy(pixels);
        }
    }

    private void Finish(string error)
    {
        if (finished) return;
        finished = true;
        File.AppendAllText(report, error == null ? "ALL CHECKS PASSED\n" : "FAIL " + error + "\n");
        PlayerPrefs.DeleteKey("MonsterArena.LocalAccounts.v1" + LocalAccountService.ValidationStorageSuffix);
        PlayerPrefs.DeleteKey("MonsterArena.CurrentAccountId.v1" + LocalAccountService.ValidationStorageSuffix);
        PlayerPrefs.Save();
        Application.Quit(error == null ? 0 : 1);
    }
}
#endif
