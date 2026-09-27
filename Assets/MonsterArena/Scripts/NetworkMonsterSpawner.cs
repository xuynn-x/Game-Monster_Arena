using Fusion;
using UnityEngine;

public class NetworkMonsterSpawner : SimulationBehaviour, IPlayerJoined
{
    private static NetworkRunner activeSpawnerRunner;

    [Header("Network Prefab")]
    [SerializeField] private NetworkObject blueMonsterPrefab;
    [SerializeField] private NetworkObject redMonsterPrefab;

    [Header("Arena Spawn Positions")]
    [SerializeField] private Vector3 firstPlayerPosition = new(-3f, 1f, -4f);
    [SerializeField] private Vector3 secondPlayerPosition = new(3f, 1f, 4f);

    public void AttachToMatchRunner(NetworkRunner runner)
    {
        var spawner = runner.GetComponent<NetworkMonsterSpawner>();
        if (spawner == null)
        {
            spawner = runner.gameObject.AddComponent<NetworkMonsterSpawner>();
            spawner.blueMonsterPrefab = blueMonsterPrefab;
            spawner.redMonsterPrefab = redMonsterPrefab;
            spawner.firstPlayerPosition = firstPlayerPosition;
            spawner.secondPlayerPosition = secondPlayerPosition;
            runner.AddGlobal(spawner);
        }
        spawner.PlayerJoined(runner.LocalPlayer);
    }

    public void PlayerJoined(PlayerRef player)
    {
        // Arena được Fusion nạp lại như network scene, vì vậy có thể tồn tại
        // nhiều NetworkMonsterSpawner. Chỉ runner đang chạy đầu tiên được
        // quyền tạo player object.
        if (activeSpawnerRunner == null ||
            !activeSpawnerRunner.IsRunning)
        {
            activeSpawnerRunner = Runner;
        }

        if (Runner != activeSpawnerRunner)
        {
            return;
        }

        // Mỗi máy chỉ sinh quái thuộc quyền sở hữu của chính máy đó.
        if (player != Runner.LocalPlayer)
        {
            return;
        }

        // Một PlayerRef chỉ được có đúng một NetworkMonster.
        if (Runner.TryGetPlayerObject(player, out NetworkObject existing) &&
            existing != null)
        {
            return;
        }

        // Arena 1v1 hiện tại: Player 1 luôn là phe xanh, Player 2 là phe đỏ.
        bool isFirstPlayer = player.PlayerId == 1;
        NetworkObject monsterPrefab = isFirstPlayer
            ? blueMonsterPrefab
            : redMonsterPrefab;

        if (monsterPrefab == null)
        {
            Debug.LogError(
                "[Fusion] Chưa gán BlueNetworkMonster hoặc " +
                "RedNetworkMonster vào NetworkBootstrap."
            );
            return;
        }

        Vector3 spawnPosition = isFirstPlayer
            ? firstPlayerPosition
            : secondPlayerPosition;

        Quaternion spawnRotation = isFirstPlayer
            ? Quaternion.Euler(0f, 37f, 0f)
            : Quaternion.Euler(0f, 217f, 0f);

        NetworkObject monster = Runner.Spawn(
            monsterPrefab,
            spawnPosition,
            spawnRotation,
            player,
            (runner, networkObject) =>
            {
                // NetworkTransform lấy trạng thái đầu tiên ngay khi object được
                // đăng ký. Đặt vị trí ở đây để không bị transform gốc của prefab
                // (-3, 1, -4) ghi đè lên quái phe đỏ.
                networkObject.transform.SetPositionAndRotation(
                    spawnPosition,
                    spawnRotation
                );
            }
        );

        Runner.SetPlayerObject(player, monster);

        Debug.Log($"[Fusion] Đã sinh quái mạng cho {player}.");
    }
}
