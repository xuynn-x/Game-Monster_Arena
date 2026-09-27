using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

// One connection from MainMenu through Arena. Never reconnect into a different room on scene entry.
public sealed class BattleMatchmaking : MonoBehaviour
{
    public const string ArenaPath = "Assets/MonsterArena/Scenes/Arena.unity";
    public const string MenuPath = "Assets/MonsterArena/Scenes/MainMenu.unity";
    public enum Phase { Idle, Connecting, Searching, Loading, InArena, Stopping }
    public static BattleMatchmaking Instance { get; private set; }
    public static string MenuNotice { get; private set; }
    public NetworkRunner Runner { get; private set; }
    public Phase State { get; private set; }
    public string Message { get; private set; }
    public bool CanCancel => State == Phase.Connecting || State == Phase.Searching;
    public float Elapsed => Time.realtimeSinceStartup - startedAt;
    private CancellationTokenSource cancellation;
    private Task<StartGameResult> connecting;
    private Task stopping;
    private float startedAt;
    private float loadingAt;
    private bool sceneRequested;
    private NetworkSceneAsyncOp sceneOperation;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { Instance = null; MenuNotice = null; }

    public static string TakeMenuNotice()
    {
        string notice = MenuNotice;
        MenuNotice = null;
        return notice;
    }

    public static bool TryBegin(out string error)
    {
        if (Instance != null) { error = "Đang xử lý phiên trước. Vui lòng thử lại sau khi hoàn tất."; return false; }
        LocalAccountService.TryGetCurrentAccount(out var account);
        if (!BattleEntryRules.Validate(account, out error)) return false;
        if (SceneUtility.GetBuildIndexByScenePath(ArenaPath) < 0)
        { error = "Không tìm thấy Arena trong bản game này."; return false; }
        MenuNotice = null;
        var service = new GameObject("Battle Matchmaking").AddComponent<BattleMatchmaking>();
        Instance = service;
        DontDestroyOnLoad(service.gameObject);
        service.Begin();
        return true;
    }

    private async void Begin()
    {
        State = Phase.Connecting;
        Message = "Đang kết nối…";
        startedAt = Time.realtimeSinceStartup;
        cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var host = new GameObject("Match Runner");
        DontDestroyOnLoad(host);
        Runner = host.AddComponent<NetworkRunner>();
        var scenes = host.AddComponent<NetworkSceneManagerDefault>();
        var objects = host.AddComponent<NetworkObjectProviderDefault>();
        try
        {
            connecting = Runner.StartGame(new StartGameArgs
            {
                GameMode = GameMode.Shared,
                PlayerCount = 2,
                CustomLobbyName = "MonsterArena-ShadowFox-v1",
                SessionProperties = new Dictionary<string, SessionProperty> { ["battle"] = "shadowfox-v1" },
                IsOpen = true,
                IsVisible = true,
                SceneManager = scenes,
                ObjectProvider = objects,
                StartGameCancellationToken = cancellation.Token
            });
            var result = await connecting;
            if (this == null || State == Phase.Stopping) return;
            if (!result.Ok)
            {
                Debug.LogWarning("[Matchmaking] Connection failed: " + result.ShutdownReason);
                await StopAsync("Không thể kết nối tìm trận. Kiểm tra mạng rồi bấm BATTLE để thử lại.");
                return;
            }
            cancellation.CancelAfter(Timeout.Infinite);
            State = Phase.Searching;
            Message = "Đang tìm đối thủ…";
            Debug.Log("[Matchmaking] Waiting in " + Runner.SessionInfo.Name);
        }
        catch (Exception exception)
        {
            if (this == null || State == Phase.Stopping) return;
            Debug.LogWarning("[Matchmaking] " + exception.GetType().Name);
            await StopAsync("Kết nối bị gián đoạn hoặc quá thời gian. Hãy thử lại.");
        }
    }

    private void Update()
    {
        if (State == Phase.Idle || State == Phase.Connecting || State == Phase.Stopping) return;
        if (Runner == null || !Runner.IsRunning)
        {
            if (State != Phase.InArena) _ = StopAsync("Đã mất kết nối. Hãy bấm BATTLE để thử lại.");
            return;
        }
        if (State == Phase.Searching)
        {
            if (Elapsed > 120) { _ = StopAsync("Chưa tìm thấy đối thủ. Bạn có thể bấm BATTLE để tìm lại."); return; }
            if (Runner.ActivePlayers.Count() == 2)
            {
                State = Phase.Loading;
                loadingAt = Time.realtimeSinceStartup;
                Message = "Đã tìm thấy đối thủ. Đang vào đấu trường…";
            }
        }
        if (State != Phase.Loading) return;
        if (Runner.ActivePlayers.Count() < 2)
        { _ = StopAsync("Đối thủ đã rời trước khi bắt đầu. Hãy tìm trận lại."); return; }
        if (!sceneRequested && Runner.IsSharedModeMasterClient)
        {
            try
            {
                sceneRequested = true;
                Runner.SessionInfo.IsOpen = false;
                Runner.SessionInfo.IsVisible = false;
                sceneOperation = Runner.LoadScene(SceneRef.FromIndex(SceneUtility.GetBuildIndexByScenePath(ArenaPath)), LoadSceneMode.Single);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[Matchmaking] Scene load failed: " + exception.GetType().Name);
                _ = StopAsync("Không thể tải đấu trường. Hãy thử lại.");
                return;
            }
        }
        if (sceneRequested && sceneOperation.IsDone && sceneOperation.Error != null)
        { _ = StopAsync("Không thể tải đấu trường. Hãy thử lại."); return; }
        if (SceneManager.GetActiveScene().path == ArenaPath && !Runner.IsSceneManagerBusy)
        {
            var battle = FindFirstObjectByType<LocalBattleTest>();
            if (battle != null && battle.NetworkReady)
            {
                State = Phase.InArena;
                Debug.Log("[Matchmaking] Arena ready: " + Runner.SessionInfo.Name);
            }
        }
        if (Time.realtimeSinceStartup - loadingAt > 45)
            _ = StopAsync("Đối thủ chưa sẵn sàng hoặc tải trận thất bại. Hãy thử lại.");
    }

    public void Cancel()
    {
        if (CanCancel) _ = StopAsync("Đã hủy tìm trận.");
    }

    public Task StopAsync(string notice = null)
    {
        if (stopping != null) return stopping;
        State = Phase.Stopping;
        Message = "Đang rời phòng…";
        stopping = StopCore(notice);
        return stopping;
    }

    private async Task StopCore(string notice)
    {
        cancellation?.Cancel();
        try
        {
            if (connecting != null) { try { await connecting; } catch { /* Startup cancellation is expected. */ } }
            if (Runner != null) await Runner.Shutdown();
        }
        catch (Exception exception)
        {
            Debug.LogWarning("[Matchmaking] Shutdown: " + exception.GetType().Name);
        }
        finally
        {
            if (Runner != null) Destroy(Runner.gameObject);
            Runner = null;
            cancellation?.Dispose();
            cancellation = null;
            MenuNotice = notice;
            if (Instance == this) Instance = null;
            if (this != null)
            {
                if (SceneManager.GetActiveScene().path != MenuPath)
                    await SceneManager.LoadSceneAsync(MenuPath);
                Destroy(gameObject);
            }
        }
    }

    private void OnDestroy()
    {
        cancellation?.Cancel();
        cancellation?.Dispose();
        if (Instance == this) Instance = null;
    }
}
