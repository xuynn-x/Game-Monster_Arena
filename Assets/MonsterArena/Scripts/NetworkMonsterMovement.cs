using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class NetworkMonsterMovement : NetworkBehaviour
{
    private static readonly int BaseColorProperty =
        Shader.PropertyToID("_BaseColor");

    private static readonly int ColorProperty =
        Shader.PropertyToID("_Color");

    private static readonly Color LocalPlayerColor =
        new(0f, 0.739f, 1f, 1f);

    private static readonly Color OpponentColor =
        new(1f, 0.41f, 0.47f, 1f);

    [Networked] private Vector3 NetworkPosition { get; set; }
    [Networked] private Quaternion NetworkRotation { get; set; }
    [Networked] private NetworkBool HasNetworkTransformState { get; set; }
    [Networked] public int CurrentHealth { get; private set; }
    [Networked] public NetworkBool IsDefeated { get; private set; }
    [Networked] public NetworkBool ArenaCombat { get; private set; }
    [Networked] public NetworkBool BlueTeam { get; private set; }
    [Networked] public NetworkString<_32> DisplayName { get; private set; }
    [Networked] public NetworkString<_64> AvatarId { get; private set; }
    [Networked] public int PlayerLevel { get; private set; }
    [Networked] public int CurrentEnergy { get; private set; }
    public const int MaxEnergy = 60;
    [Networked] public int Round { get; private set; }
    [Networked] public int RematchRound { get; private set; }
    [Networked] public int AttackSequence { get; private set; }
    [Networked] public int AttackSkill { get; private set; }
    [Networked] public TickTimer ImpactTimer { get; private set; }
    [Networked] public TickTimer RecoveryTimer { get; private set; }
    [Networked] private TickTimer CooldownQ { get; set; }
    [Networked] private TickTimer CooldownW { get; set; }
    [Networked] private TickTimer CooldownE { get; set; }
    [Networked] private int ReceivedSequence { get; set; }
    private int queuedArenaSkill = -1;
    public bool Recovering => !RecoveryTimer.ExpiredOrNotRunning(Runner);
    public float RemainingCooldown(int skill) =>
        (skill == 0 ? CooldownQ : skill == 1 ? CooldownW : CooldownE).RemainingTime(Runner) ?? 0;

    public bool QueueArenaSkill(int skill)
    {
        if (skill < 0 || skill > 2 || Object == null || !Object.IsValid || !Object.HasStateAuthority ||
            !ArenaCombat || IsDefeated || Recovering || RemainingCooldown(skill) > 0) return false;
        var opponent = FindArenaOpponent();
        if (opponent == null || opponent.Round != Round || opponent.IsDefeated) return false;
        queuedArenaSkill = skill;
        return true;
    }

    public NetworkMonsterMovement FindArenaOpponent()
    {
        foreach (var monster in FindObjectsByType<NetworkMonsterMovement>(FindObjectsSortMode.None))
            if (monster != this && monster.Object != null && monster.Object.IsValid &&
                monster.Runner == Runner && monster.ArenaCombat) return monster;
        return null;
    }

    public void RequestRematch()
    {
        if (Object == null || !Object.IsValid || !Object.HasStateAuthority) return;
        var opponent = FindArenaOpponent();
        if (opponent != null && (IsDefeated || opponent.IsDefeated)) RematchRound = Round + 1;
    }

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float rotationSpeed = 720f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int testSkillDamage = 20;
    [SerializeField] private float testSkillCooldown = 1f;

    private CharacterController characterController;
    private Renderer monsterRenderer;
    private Material localPlayerMaterial;
    private Material opponentMaterial;
    private Transform cameraTransform;
    private float verticalSpeed;
    private bool controlEnabled = true;
    private float nextSkillUseTime;
    private bool skillQueued;

    public int MaxHealth => maxHealth;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        monsterRenderer = GetComponent<Renderer>();

        CreateTeamMaterials();

        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    public override void Spawned()
    {
        var arena = FindFirstObjectByType<LocalBattleTest>();
        if (arena != null && arena.onlineBattle)
        {
            foreach (var oldSkill in GetComponents<MonsterSkillController>()) oldSkill.enabled = false;
        }
        // Trong Shared Mode, chỉ máy sở hữu quái mới được điều khiển nó.
        characterController.enabled = Object.HasStateAuthority;

        // Prefab là trung tính. Mọi bản sao, gồm proxy của đối thủ, đều chọn
        // đúng điểm phe ngay khi được Fusion tạo.
        SetInitialTeamTransform();

        if (Object.HasStateAuthority)
        {
            var battle = FindFirstObjectByType<LocalBattleTest>();
            ArenaCombat = battle != null && battle.onlineBattle;
            BlueTeam = Runner.IsSharedModeMasterClient;
            Round = 1;
            string playerName = LocalAccountService.TryGetCurrentAccount(out var account)
                ? account.displayName : "Người chơi " + Object.StateAuthority.PlayerId;
            if (string.IsNullOrWhiteSpace(playerName)) playerName = "Người chơi";
            DisplayName = playerName.Length > 32 ? playerName.Substring(0, 32) : playerName;
            string avatar = account != null ? account.avatarId : "avatar_pink_trainer_portrait";
            AvatarId = string.IsNullOrEmpty(avatar) ? "avatar_pink_trainer_portrait" : avatar.Substring(0, Mathf.Min(64, avatar.Length));
            PlayerLevel = account != null ? Mathf.Max(1, account.level) : 1;
            CurrentEnergy = MaxEnergy;
            WriteTransformState();
            CurrentHealth = maxHealth;
            IsDefeated = false;
        }

        ApplyTeamColor();

        if (Object.HasStateAuthority)
        {
            BattleCameraController battleCamera =
                FindFirstObjectByType<BattleCameraController>();

            if (battleCamera != null)
            {
                battleCamera.ConfigureForLocalPlayer(
                    Object.StateAuthority.PlayerId
                );
            }
        }

        Debug.Log(
            $"[Fusion] Nhìn thấy {name}: chủ {Object.StateAuthority}, " +
            $"StateAuthority={Object.HasStateAuthority}, vị trí " +
            $"{transform.position}."
        );

    }

    public override void FixedUpdateNetwork()
    {
        if (ArenaCombat)
        {
            if (Object.HasStateAuthority) UpdateArenaCombat();
            return;
        }
        if (!Object.HasStateAuthority || !controlEnabled || IsDefeated)
        {
            return;
        }

        TryUseTestSkill();

        ApplyGravity();

        Vector2 input = ReadMovementInput();
        Vector3 direction = CalculateCameraDirection(input);

        RotateTowards(direction);
        Move(direction);
        WriteTransformState();
    }

    private void Update()
    {
        if (Object != null && Object.IsValid && ArenaCombat) return;
        if (Object == null || !Object.HasStateAuthority ||
            Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            skillQueued = true;
        }
    }

    public override void Render()
    {
        // NetworkTransform của prefab cũ giữ transform gốc khi tạo proxy.
        // Proxy dùng state do chủ quái gửi để luôn hiện đúng vị trí và góc xoay.
        if (!Object.HasStateAuthority)
        {
            if (HasNetworkTransformState)
            {
                transform.SetPositionAndRotation(
                    Vector3.Lerp(
                        transform.position,
                        NetworkPosition,
                        Time.deltaTime * 15f
                    ),
                    Quaternion.Slerp(
                        transform.rotation,
                        NetworkRotation,
                        Time.deltaTime * 15f
                    )
                );
            }
            else
            {
                // Khi proxy vừa tạo, state mạng có thể đến muộn một vài frame.
                // Không cho transform gốc của prefab kéo quái đỏ về điểm xanh.
                SetInitialTeamTransform();
            }
        }
    }

    private Vector2 ReadMovementInput()
    {
        if (Keyboard.current == null)
        {
            return Vector2.zero;
        }

        Vector2 input = Vector2.zero;

        if (Keyboard.current.wKey.isPressed) input.y += 1f;
        if (Keyboard.current.sKey.isPressed) input.y -= 1f;
        if (Keyboard.current.aKey.isPressed) input.x -= 1f;
        if (Keyboard.current.dKey.isPressed) input.x += 1f;

        return Vector2.ClampMagnitude(input, 1f);
    }

    private Vector3 CalculateCameraDirection(Vector2 input)
    {
        if (cameraTransform == null)
        {
            return new Vector3(input.x, 0f, input.y);
        }

        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        return (cameraForward * input.y + cameraRight * input.x).normalized;
    }

    private void RotateTowards(Vector3 direction)
    {
        if (direction.sqrMagnitude <= 0f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotationSpeed * Runner.DeltaTime
        );
    }

    private void ApplyGravity()
    {
        if (characterController.isGrounded && verticalSpeed < 0f)
        {
            verticalSpeed = -2f;
        }

        verticalSpeed += gravity * Runner.DeltaTime;
    }

    private void Move(Vector3 direction)
    {
        Vector3 velocity = direction * moveSpeed;
        velocity.y = verticalSpeed;

        characterController.Move(velocity * Runner.DeltaTime);
    }

    private void TryUseTestSkill()
    {
        if (!skillQueued || Runner.SimulationTime < nextSkillUseTime)
        {
            return;
        }

        skillQueued = false;
        nextSkillUseTime = Runner.SimulationTime + testSkillCooldown;

        NetworkMonsterMovement target = FindOpponent();
        if (target == null)
        {
            Debug.Log("[Fusion] Chưa có quái đối thủ để dùng Q.");
            return;
        }

        target.RPC_ReceiveDamage(testSkillDamage);
        Debug.Log($"[Fusion] {Object.StateAuthority} dùng Q vào {target.Object.StateAuthority}.");
    }

    private void UpdateArenaCombat()
    {
        var opponent = FindArenaOpponent();
        if (opponent == null) { queuedArenaSkill = -1; return; }
        int nextRound = Round + 1;
        if (RematchRound == nextRound &&
            ((opponent.Round == Round && opponent.RematchRound == nextRound) || opponent.Round == nextRound))
        {
            Round = nextRound;
            CurrentEnergy = MaxEnergy;
            CurrentHealth = maxHealth;
            IsDefeated = false;
            AttackSequence = ReceivedSequence = 0;
            ImpactTimer = RecoveryTimer = CooldownQ = CooldownW = CooldownE = TickTimer.None;
            queuedArenaSkill = -1;
            return;
        }
        if (opponent.Round != Round) return;
        // The target's authority applies each published attack exactly once.
        if (opponent.AttackSequence > ReceivedSequence && opponent.ImpactTimer.Expired(Runner))
        {
            ReceivedSequence = opponent.AttackSequence;
            int skill = opponent.AttackSkill;
            if (!IsDefeated && skill >= 0 && skill < 3)
            {
                CurrentHealth = Mathf.Max(0, CurrentHealth - new[] { 12, 20, 32 }[skill]);
                IsDefeated = CurrentHealth == 0;
            }
        }
        int queued = queuedArenaSkill;
        queuedArenaSkill = -1;
        if (queued < 0 || IsDefeated || opponent.IsDefeated || Recovering || RemainingCooldown(queued) > 0) return;
        AttackSkill = queued;
        AttackSequence++;
        float duration = new[] { .58f, .79f, 3.5f }[queued];
        float flight = (new Vector3(2.65f, 0, 5.5f).magnitude - 1.1f) / new[] { 13f, 19f, 9f }[queued];
        ImpactTimer = TickTimer.CreateFromSeconds(Runner, duration * .55f + flight);
        RecoveryTimer = TickTimer.CreateFromSeconds(Runner, duration * .55f + flight + Mathf.Max(.85f, duration * .45f));
        var timer = TickTimer.CreateFromSeconds(Runner, new[] { 2f, 5f, 9f }[queued]);
        if (queued == 0) CooldownQ = timer;
        else if (queued == 1) CooldownW = timer;
        else CooldownE = timer;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_ReceiveDamage(int damage)
    {
        if (ArenaCombat) return;
        if (IsDefeated || damage <= 0)
        {
            return;
        }

        CurrentHealth = Mathf.Max(CurrentHealth - damage, 0);
        IsDefeated = CurrentHealth == 0;

        Debug.Log(
            $"[Fusion] {Object.StateAuthority} nhận {damage} sát thương. " +
            $"HP: {CurrentHealth}/{maxHealth}."
        );
    }

    private NetworkMonsterMovement FindOpponent()
    {
        NetworkMonsterMovement[] monsters =
            FindObjectsByType<NetworkMonsterMovement>(
                FindObjectsSortMode.None
            );

        foreach (NetworkMonsterMovement monster in monsters)
        {
            if (monster != this && monster.Object != null &&
                !monster.IsDefeated)
            {
                return monster;
            }
        }

        return null;
    }

    public void SetControlEnabled(bool enabled)
    {
        controlEnabled = enabled;
    }

    private void ApplyTeamColor()
    {
        if (monsterRenderer == null || !Object.StateAuthority.IsValid)
        {
            return;
        }

        // Màu thuộc về phe trong đấu trường, không phụ thuộc máy đang nhìn.
        bool isBlueTeam = Object.StateAuthority.PlayerId == 1;
        monsterRenderer.SetPropertyBlock(null);
        monsterRenderer.sharedMaterial =
            isBlueTeam ? localPlayerMaterial : opponentMaterial;
    }

    private void SetInitialTeamTransform()
    {
        bool isBlueTeam = Object.StateAuthority.PlayerId == 1;
        transform.SetPositionAndRotation(
            isBlueTeam ? new Vector3(-3f, 1f, -4f) : new Vector3(3f, 1f, 4f),
            Quaternion.Euler(0f, isBlueTeam ? 37f : 217f, 0f)
        );
    }

    private void WriteTransformState()
    {
        NetworkPosition = transform.position;
        NetworkRotation = transform.rotation;
        HasNetworkTransformState = true;
    }

    private void CreateTeamMaterials()
    {
        if (monsterRenderer == null ||
            monsterRenderer.sharedMaterial == null)
        {
            return;
        }

        localPlayerMaterial = new Material(
            monsterRenderer.sharedMaterial
        );

        opponentMaterial = new Material(
            monsterRenderer.sharedMaterial
        );

        SetMaterialColor(localPlayerMaterial, LocalPlayerColor);
        SetMaterialColor(opponentMaterial, OpponentColor);

    }

    private static void SetMaterialColor(
        Material material,
        Color color
    )
    {
        material.SetColor(BaseColorProperty, color);
        material.SetColor(ColorProperty, color);
    }
}
