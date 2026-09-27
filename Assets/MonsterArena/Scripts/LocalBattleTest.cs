using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using Fusion;
using Object = UnityEngine.Object;

// Arena presentation; Fusion owns combat state in online mode.
public sealed class LocalBattleTest : MonoBehaviour
{
    public Animator blue;
    public Animator red;
    public Camera battleCamera;
    public bool autoOpponent = true;
    public bool onlineBattle = true;
    private NetworkRunner matchRunner;
    private System.Threading.Tasks.Task<StartGameResult> connectionTask;
    public NetworkMonsterMovement LocalMonster { get; private set; }
    public NetworkMonsterMovement OpponentMonster { get; private set; }
    public string ConnectionMessage { get; private set; } = "Đang kết nối Photon…";
    public bool Leaving { get; private set; }
    public bool NetworkReady => LocalMonster != null && OpponentMonster != null &&
        LocalMonster.Object != null && LocalMonster.Object.IsValid &&
        OpponentMonster.Object != null && OpponentMonster.Object.IsValid &&
        LocalMonster.Round == OpponentMonster.Round;
    private readonly int[] displayedAttacks = new int[2];
    private int displayedRound;
    private bool cameraAssigned;
    private readonly int[] hp = { 100, 100 };
    private readonly float[,] readyAt = new float[2, 3];
    private static readonly int[] Damage = { 12, 20, 32 };
    private static readonly float[] Cooldown = { 2, 5, 9 };
    private static readonly float[] Duration = { .58f, .79f, 3.5f };
    private bool busy;
    private int activeCasts;
    private float nextOpponent;
    private Vector3 cameraHome;
    private Quaternion cameraRotation;
    private Transform recoilingRoot;
    private Vector3 recoilOrigin;
    private GameObject effectsRoot;
    private Material energyMaterial;
    private Material coreMaterial;
    private ArenaBattleHud hud;
    private ArenaBattleAudio battleAudio;
    private NetworkMonsterMovement audioOpponent;
    private string message = "Blue: Q / W / E. Red: U / I / O. R: restart.";

    public int BlueHP => hp[0];
    public int RedHP => hp[1];
    public bool IsBusy => busy;
    public Transform OpponentVisual => onlineBattle && NetworkReady && !LocalMonster.BlueTeam
        ? blue != null ? blue.transform : null : red != null ? red.transform : null;

    private void Start()
    {
        cameraHome = battleCamera.transform.position;
        cameraRotation = battleCamera.transform.rotation;
        nextOpponent = Time.time + 4;
        // Reuse the model's URP shader so it is also available in player builds.
        Shader shader = blue.GetComponentInChildren<Renderer>().sharedMaterial.shader;
        energyMaterial = CreateEnergyMaterial(shader, new Color(.38f, .025f, .8f), 2.5f);
        coreMaterial = CreateEnergyMaterial(shader, new Color(.85f, .5f, 1), 3.5f);
        hud = new ArenaBattleHud(this);
        battleAudio = gameObject.AddComponent<ArenaBattleAudio>();
        if (onlineBattle) ConnectOnline();
    }

    private async void ConnectOnline()
    {
        autoOpponent = false;
        try
        {
            NetworkMonsterSpawner spawner = FindFirstObjectByType<NetworkMonsterSpawner>(FindObjectsInactive.Include);
            if (spawner == null) { ConnectionMessage = "Thiếu cấu hình phòng Photon."; return; }
            var matchmaking = BattleMatchmaking.Instance;
            if (matchmaking != null)
            {
                matchRunner = matchmaking.Runner;
                if (matchRunner == null || !matchRunner.IsRunning)
                { ConnectionMessage = "Phiên ghép trận đã kết thúc. Hãy rời trận và thử lại."; return; }
                while (matchRunner != null && matchRunner.IsRunning && matchRunner.IsSceneManagerBusy)
                    await System.Threading.Tasks.Task.Yield();
                if (this == null || Leaving || matchRunner == null || !matchRunner.IsRunning) return;
                spawner.AttachToMatchRunner(matchRunner);
                ConnectionMessage = "Đang chờ đối thủ sẵn sàng…";
                return;
            }
            var bootstrap = spawner.GetComponent<FusionBootstrap>();
            if (bootstrap != null) { bootstrap.StartMode = FusionBootstrap.StartModes.Manual; bootstrap.enabled = false; }
            var debugUi = spawner.GetComponent<FusionBootstrapDebugGUI>();
            if (debugUi != null) debugUi.enabled = false;
            spawner.gameObject.SetActive(true);
            matchRunner = spawner.GetComponent<NetworkRunner>();
            if (matchRunner == null) matchRunner = spawner.gameObject.AddComponent<NetworkRunner>();
            var sceneManager = matchRunner.GetComponent<NetworkSceneManagerDefault>();
            if (sceneManager == null) sceneManager = matchRunner.gameObject.AddComponent<NetworkSceneManagerDefault>();
            var objectProvider = matchRunner.GetComponent<NetworkObjectProviderDefault>();
            if (objectProvider == null) objectProvider = matchRunner.gameObject.AddComponent<NetworkObjectProviderDefault>();
            connectionTask = matchRunner.StartGame(new StartGameArgs
            {
                GameMode = GameMode.Shared,
                SessionName = "MonsterArena-1v1",
                PlayerCount = 2,
                SceneManager = sceneManager,
                ObjectProvider = objectProvider,
                OnGameStarted = runner =>
                {
                    if (spawner.Runner != runner) runner.AddGlobal(spawner);
                    spawner.PlayerJoined(runner.LocalPlayer);
                }
            });
            var result = await connectionTask;
            if (this == null || Leaving) return;
            ConnectionMessage = result.Ok ? "Đang chờ đối thủ…" : "Không thể kết nối: " + result.ShutdownReason;
        }
        catch (System.Exception exception)
        {
            if (this != null) ConnectionMessage = "Kết nối thất bại. Hãy rời trận và thử lại.";
            Debug.LogException(exception);
        }
    }

    public async void LeaveMatch()
    {
        if (Leaving) return;
        Leaving = true;
        battleAudio?.StopAll();
        ConnectionMessage = "Đang rời phòng…";
        StopAllCoroutines();
        RestoreRecoil();
        ClearEffects();
        try
        {
            if (connectionTask != null && !connectionTask.IsCompleted) await connectionTask;
            if (BattleMatchmaking.Instance != null)
                await BattleMatchmaking.Instance.StopAsync();
            else
            {
                if (matchRunner != null) await matchRunner.Shutdown();
                if (this != null) SceneManager.LoadScene("Assets/MonsterArena/Scenes/MainMenu.unity");
            }
        }
        catch (System.Exception exception)
        {
            Leaving = false;
            ConnectionMessage = "Chưa thể rời trận. Hãy thử lại.";
            Debug.LogException(exception);
        }
    }

    private void SyncOnline()
    {
        LocalMonster = OpponentMonster = null;
        foreach (var monster in FindObjectsByType<NetworkMonsterMovement>(FindObjectsSortMode.None))
        {
            if (monster.Object == null || !monster.Object.IsValid || monster.Runner != matchRunner || !monster.ArenaCombat) continue;
            if (monster.Object.HasStateAuthority) LocalMonster = monster;
            else OpponentMonster = monster;
            foreach (Renderer renderer in monster.GetComponentsInChildren<Renderer>()) renderer.enabled = false;
            var collider = monster.GetComponent<CharacterController>();
            if (collider != null) collider.enabled = false;
        }
        if (!NetworkReady)
        {
            battleAudio?.StopAll();
            if (busy)
            {
                StopAllCoroutines();
                RestoreRecoil();
                ClearEffects();
                busy = false;
                battleCamera.transform.SetPositionAndRotation(cameraHome, cameraRotation);
            }
            if (matchRunner != null && matchRunner.IsRunning) ConnectionMessage = displayedRound > 0 && BattleMatchmaking.Instance != null
                ? "Đối thủ đã rời trận. Hãy rời trận để tìm đối thủ mới." : "Đang chờ đối thủ…";
            else if (displayedRound > 0) ConnectionMessage = "Đã mất kết nối. Hãy rời trận và thử lại.";
            return;
        }
        if (audioOpponent != OpponentMonster)
        {
            // A replacement opponent can start at the same round number.
            battleAudio?.ResetRound();
            audioOpponent = OpponentMonster;
        }
        if (!cameraAssigned)
        {
            if (!LocalMonster.BlueTeam)
            {
                // Mirror the approved blue-side camera exactly. The old hard-coded
                // red position was closer and offset, so Windows clients framed
                // characters much larger than the Unity/blue-side view.
                cameraHome = new Vector3(-cameraHome.x, cameraHome.y, -cameraHome.z);
                cameraRotation = Quaternion.Euler(0, 180, 0) * cameraRotation;
                battleCamera.transform.SetPositionAndRotation(cameraHome, cameraRotation);
            }
            cameraAssigned = true;
        }
        if (displayedRound != LocalMonster.Round)
        {
            ResetPresentation();
            displayedRound = LocalMonster.Round;
            System.Array.Clear(displayedAttacks, 0, displayedAttacks.Length);
        }
        var blueState = LocalMonster.BlueTeam ? LocalMonster : OpponentMonster;
        var redState = LocalMonster.BlueTeam ? OpponentMonster : LocalMonster;
        var states = new[] { blueState, redState };
        for (int team = 0; team < 2; team++)
        {
            var state = states[team];
            Animator actor = team == 0 ? blue : red;
            if (state.CurrentHealth < hp[team])
            {
                hud.ShowDamage(actor.transform.parent.position + Vector3.up * 2.5f, hp[team] - state.CurrentHealth);
                if (state.IsDefeated) actor.CrossFadeInFixedTime("Chet", .06f, 0, 0);
                else if (recoilingRoot == null) StartCoroutine(Recoil(actor.transform.parent, (team == 0 ? red : blue).transform.parent));
            }
            hp[team] = state.CurrentHealth;
            if (state.AttackSequence > displayedAttacks[team])
            {
                displayedAttacks[team] = state.AttackSequence;
                if (!state.IsDefeated && state.Recovering)
                {
                    busy = true;
                    StartCoroutine(Cast(team, state.AttackSkill, state));
                }
            }
        }
        ConnectionMessage = "Photon · Trận " + LocalMonster.Round;
    }

    private void Update()
    {
        if (Leaving) return;
        if (onlineBattle) SyncOnline();
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.escapeKey.wasPressedThisFrame) hud?.ToggleSettings();
            if (hud != null && hud.SettingsOpen) return;
            if (keyboard.rKey.wasPressedThisFrame) Restart();
            if (keyboard.qKey.wasPressedThisFrame) TryCast(0, 0);
            if (keyboard.wKey.wasPressedThisFrame) TryCast(0, 1);
            if (keyboard.eKey.wasPressedThisFrame) TryCast(0, 2);
            if (!onlineBattle && keyboard.uKey.wasPressedThisFrame) TryCast(1, 0);
            if (!onlineBattle && keyboard.iKey.wasPressedThisFrame) TryCast(1, 1);
            if (!onlineBattle && keyboard.oKey.wasPressedThisFrame) TryCast(1, 2);
        }
        if (!onlineBattle && autoOpponent && !busy && hp[0] > 0 && hp[1] > 0 && Time.time >= nextOpponent)
        {
            for (int skill = 2; skill >= 0; skill--)
                if (TryCast(1, skill)) break;
            nextOpponent = Time.time + 3;
        }
    }

    private void LateUpdate()
    {
        hud?.Refresh(hp, readyAt, busy, autoOpponent);
    }

    public void PlayResultAudio(int localHealth, int opponentHealth)
    {
        if (!Leaving) battleAudio?.PlayResult(localHealth, opponentHealth);
    }

    public bool TryCast(int team, int skill)
    {
        if (hud != null && hud.SettingsOpen) return false;
        if (onlineBattle) return !Leaving && team == 0 && NetworkReady && LocalMonster.QueueArenaSkill(skill);
        if (team < 0 || team > 1 || skill < 0 || skill > 2 || busy ||
            hp[0] <= 0 || hp[1] <= 0 || Time.time < readyAt[team, skill]) return false;
        busy = true;
        readyAt[team, skill] = Time.time + Cooldown[skill];
        StartCoroutine(Cast(team, skill));
        return true;
    }

    private IEnumerator Cast(int team, int skill, NetworkMonsterMovement networkState = null)
    {
        activeCasts++;
        Animator attacker = team == 0 ? blue : red;
        Animator target = team == 0 ? red : blue;
        message = (team == 0 ? "Blue" : "Red") + " casts skill " + (skill + 1);
        attacker.CrossFadeInFixedTime("Tungchieu" + (skill + 1), .08f, 0, 0);
        // A small push-in preserves the selected over-the-shoulder composition.
        battleCamera.transform.SetPositionAndRotation(
            cameraHome + cameraRotation * Vector3.forward * .25f, cameraRotation);
        Transform attackerRoot = attacker.transform.parent;
        Transform targetRoot = target.transform.parent;
        Vector3 direction = (targetRoot.position - attackerRoot.position).normalized;
        Vector3 launch = attackerRoot.position + Vector3.up * 1.5f + direction * 1.1f;
        if (effectsRoot == null)
        {
            effectsRoot = new GameObject("Shadow Fox Skill Effects");
            effectsRoot.transform.SetParent(transform, false);
        }
        var castEffects = new GameObject("Cast " + team);
        castEffects.transform.SetParent(effectsRoot.transform, false);
        GameObject projectile = CreateProjectile(skill, launch, direction, castEffects.transform);
        float speed = skill == 1 ? 19 : skill == 2 ? 9 : 13;
        float flightTime = Vector3.Distance(launch, targetRoot.position + Vector3.up * 1.5f) / speed;
        float windup = networkState != null
            ? Mathf.Max(0, (networkState.ImpactTimer.RemainingTime(networkState.Runner) ?? 0) - flightTime)
            : Duration[skill] * .55f;
        battleAudio?.BeginCharge(team, skill, windup);
        if (skill == 2)
        {
            float chargeTime = windup;
            for (float elapsed = 0; elapsed < chargeTime; elapsed += Time.deltaTime)
            {
                projectile.transform.localScale = Vector3.one * Mathf.Lerp(.15f, 1, elapsed / chargeTime);
                projectile.transform.Rotate(direction, 160 * Time.deltaTime, Space.World);
                yield return null;
            }
            projectile.transform.localScale = Vector3.one;
        }
        else
        {
            projectile.SetActive(false);
            yield return new WaitForSeconds(windup);
            projectile.SetActive(true);
        }

        battleAudio?.Launch(team, skill);
        // MoveTowards clamps to the target even at low frame rates. Damage is applied
        // once, only after arrival, instead of after an estimated flight delay.
        while (targetRoot != null)
        {
            Vector3 impactPoint = targetRoot.position + Vector3.up * 1.5f;
            float currentSpeed = speed;
            if (networkState != null && networkState.Object != null && networkState.Object.IsValid)
            {
                float remaining = networkState.ImpactTimer.RemainingTime(networkState.Runner) ?? 0;
                currentSpeed = Vector3.Distance(projectile.transform.position, impactPoint) / Mathf.Max(Time.deltaTime, remaining);
            }
            projectile.transform.position = Vector3.MoveTowards(projectile.transform.position, impactPoint, currentSpeed * Time.deltaTime);
            if ((projectile.transform.position - impactPoint).sqrMagnitude <= .0025f) break;
            yield return null;
        }
        if (targetRoot == null)
        {
            Destroy(castEffects);
            attacker.CrossFadeInFixedTime("Idle", .12f, 0, 0);
            battleCamera.transform.SetPositionAndRotation(cameraHome, cameraRotation);
            busy = --activeCasts > 0;
            yield break;
        }
        Vector3 hitPosition = projectile.transform.position;
        projectile.SetActive(false);
        battleAudio?.Impact(team, skill);
        StartCoroutine(ImpactEffect(hitPosition, skill, castEffects.transform));
        if (!onlineBattle)
        {
        hp[1 - team] = Mathf.Max(0, hp[1 - team] - Damage[skill]);
        hud?.ShowDamage(hitPosition + Vector3.up, Damage[skill]);
        if (hp[1 - team] == 0)
            target.CrossFadeInFixedTime("Chet", .06f, 0, 0);
        else
            StartCoroutine(Recoil(target.transform.parent, attacker.transform.parent));
        }
        message += " — " + Damage[skill] + " damage";
        yield return new WaitForSeconds(Mathf.Max(.85f, Duration[skill] * .45f));
        if (hp[team] > 0) attacker.CrossFadeInFixedTime("Idle", .12f, 0, 0);
        Destroy(castEffects);
        busy = --activeCasts > 0;
        if (!busy) battleCamera.transform.SetPositionAndRotation(cameraHome, cameraRotation);
        if (hp[1 - team] == 0) message = (team == 0 ? "BLUE" : "RED") + " WINS — R to restart";
    }

    private IEnumerator Recoil(Transform targetRoot, Transform attackerRoot)
    {
        // Move the visual prefab root so the Animator cannot overwrite the recoil.
        recoilingRoot = targetRoot;
        recoilOrigin = targetRoot.position;
        Vector3 direction = targetRoot.position - attackerRoot.position;
        direction.y = 0;
        Vector3 offset = direction.normalized * .35f;
        const float pushTime = .12f;
        const float returnTime = .28f;
        float elapsed = 0;
        while (elapsed < pushTime + returnTime)
        {
            elapsed += Time.deltaTime;
            float amount = elapsed < pushTime
                ? Mathf.SmoothStep(0, 1, elapsed / pushTime)
                : 1 - Mathf.SmoothStep(0, 1, (elapsed - pushTime) / returnTime);
            targetRoot.position = recoilOrigin + offset * amount;
            yield return null;
        }
        RestoreRecoil();
    }

    private void RestoreRecoil()
    {
        if (recoilingRoot != null) recoilingRoot.position = recoilOrigin;
        recoilingRoot = null;
    }

    private void OnDisable()
    {
        battleAudio?.StopAll();
        hud?.SetVisible(false);
        StopAllCoroutines();
        RestoreRecoil();
        ClearEffects();
        if (battleCamera != null && cameraRotation != default)
            battleCamera.transform.SetPositionAndRotation(cameraHome, cameraRotation);
        busy = false;
    }

    private void OnEnable() => hud?.SetVisible(true);

    public void Restart()
    {
        if (onlineBattle)
        {
            if (NetworkReady && !Leaving)
            {
                battleAudio?.StopAll();
                LocalMonster.RequestRematch();
            }
            return;
        }
        ResetPresentation();
    }

    private void ResetPresentation()
    {
        battleAudio?.ResetRound();
        StopAllCoroutines();
        RestoreRecoil();
        ClearEffects();
        hud?.Reset();
        busy = false;
        hp[0] = hp[1] = 100;
        System.Array.Clear(readyAt, 0, readyAt.Length);
        blue.Play("Idle", 0, 0);
        red.Play("Idle", 0, 0);
        battleCamera.transform.SetPositionAndRotation(cameraHome, cameraRotation);
        nextOpponent = Time.time + 4;
        message = "New match — Blue: Q / W / E. Red: U / I / O.";
    }

    private static Material CreateEnergyMaterial(Shader shader, Color color, float intensity)
    {
        var material = new Material(shader);
        material.SetColor("_BaseColor", color);
        material.SetColor("_EmissionColor", color * intensity);
        material.EnableKeyword("_EMISSION");
        return material;
    }

    private GameObject Orb(Transform parent, float size, Material material)
    {
        var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orb.name = "Energy";
        orb.transform.SetParent(parent, false);
        orb.transform.localScale = Vector3.one * size;
        var collider = orb.GetComponent<Collider>();
        collider.enabled = false;
        Destroy(collider);
        var renderer = orb.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return orb;
    }

    private LineRenderer Arc(Transform parent, float radius, float degrees, float width)
    {
        var line = new GameObject("Energy Arc").AddComponent<LineRenderer>();
        line.transform.SetParent(parent, false);
        line.useWorldSpace = false;
        line.sharedMaterial = coreMaterial;
        line.widthMultiplier = width;
        line.numCapVertices = 4;
        line.positionCount = 40;
        for (int i = 0; i < line.positionCount; i++)
        {
            float angle = (-degrees * .5f + degrees * i / (line.positionCount - 1)) * Mathf.Deg2Rad;
            line.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius);
        }
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return line;
    }

    private GameObject CreateProjectile(int skill, Vector3 position, Vector3 direction, Transform effectsParent)
    {
        var projectile = new GameObject(skill == 1 ? "Crescent Slash" : "Shadow Orb");
        projectile.transform.SetParent(effectsParent, false);
        projectile.transform.SetPositionAndRotation(position, Quaternion.LookRotation(direction));
        if (skill == 1)
        {
            var slash = Arc(projectile.transform, .85f, 150, .18f);
            slash.widthCurve = new AnimationCurve(new Keyframe(0, .08f), new Keyframe(.5f, 1), new Keyframe(1, .08f));
        }
        else
        {
            float size = skill == 2 ? .9f : .3f;
            Orb(projectile.transform, size, energyMaterial);
            var core = Orb(projectile.transform, size * .5f, coreMaterial);
            core.transform.localPosition = Vector3.back * size * .4f;
            if (skill == 2)
            {
                Arc(projectile.transform, .65f, 360, .045f);
                var ring = Arc(projectile.transform, .65f, 360, .045f);
                ring.transform.localRotation = Quaternion.Euler(65, 25, 0);
            }
        }
        var trail = projectile.AddComponent<TrailRenderer>();
        trail.sharedMaterial = energyMaterial;
        trail.time = skill == 1 ? .13f : .25f;
        trail.startWidth = skill == 2 ? .4f : .14f;
        trail.endWidth = 0;
        trail.minVertexDistance = .05f;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return projectile;
    }

    private IEnumerator ImpactEffect(Vector3 position, int skill, Transform effectsParent)
    {
        var impact = new GameObject("Shadow Impact");
        impact.transform.SetParent(effectsParent, false);
        impact.transform.position = position;
        impact.transform.rotation = battleCamera.transform.rotation;
        var ring = Arc(impact.transform, 1, 360, .08f);
        var flash = Orb(impact.transform, .3f, coreMaterial);
        var sparks = new Transform[skill == 2 ? 14 : 8];
        for (int i = 0; i < sparks.Length; i++)
            sparks[i] = Orb(impact.transform, .075f, coreMaterial).transform;
        const float lifetime = .5f;
        for (float elapsed = 0; elapsed < lifetime; elapsed += Time.deltaTime)
        {
            float t = elapsed / lifetime;
            float radius = Mathf.Lerp(.1f, skill == 2 ? 1.8f : .8f, t);
            ring.transform.localScale = Vector3.one * radius;
            ring.widthMultiplier = .1f * (1 - t);
            flash.transform.localScale = Vector3.one * (1 - t) * (skill == 2 ? .8f : .35f);
            for (int i = 0; i < sparks.Length; i++)
            {
                float angle = i * Mathf.PI * 2 / sparks.Length;
                sparks[i].localPosition = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;
                sparks[i].localScale = Vector3.one * .075f * (1 - t);
            }
            yield return null;
        }
        Destroy(impact);
    }

    private void ClearEffects()
    {
        activeCasts = 0;
        if (effectsRoot != null)
        {
            effectsRoot.SetActive(false);
            Destroy(effectsRoot);
            effectsRoot = null;
        }
    }

    private void OnDestroy()
    {
        hud?.Dispose();
        if (energyMaterial != null) Destroy(energyMaterial);
        if (coreMaterial != null) Destroy(coreMaterial);
    }

}
