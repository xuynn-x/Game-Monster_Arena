using TMPro;
using UnityEngine;

public class BattleResultController : MonoBehaviour
{
    [Header("Monster Health")]
    [SerializeField] private MonsterHealth playerHealth;
    [SerializeField] private MonsterHealth enemyHealth;

    [Header("Result UI")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultText;

    [Header("Gameplay")]
    [SerializeField] private PlayerMonsterMovement playerMovement;
    [SerializeField] private MonsterSkillController playerSkills;
    [SerializeField] private MonsterSkillController enemySkills;
    [SerializeField] private BattleCameraController battleCamera;

    private bool resultPending;
    private bool battleEnded;
    private bool playerWon;

    private void Start()
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (battleEnded)
        {
            return;
        }

        CheckBattleResult();

        if (!resultPending)
        {
            return;
        }

        // Chờ camera skill kết thúc rồi mới hiện kết quả.
        if (battleCamera != null && battleCamera.IsBusy)
        {
            return;
        }

        ShowBattleResult();
    }

    private void CheckBattleResult()
    {
        if (resultPending ||
            playerHealth == null ||
            enemyHealth == null)
        {
            return;
        }

        // Khi đang thử Photon, hai quái cục bộ được tắt để nhường chỗ
        // cho quái mạng. Không được coi object chưa hoạt động là đã chết.
        if (!playerHealth.gameObject.activeInHierarchy ||
            !enemyHealth.gameObject.activeInHierarchy)
        {
            return;
        }

        if (enemyHealth.IsDead)
        {
            playerWon = true;
            resultPending = true;
        }
        else if (playerHealth.IsDead)
        {
            playerWon = false;
            resultPending = true;
        }
    }

    private void ShowBattleResult()
    {
        battleEnded = true;

        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        if (playerSkills != null)
        {
            playerSkills.enabled = false;
        }

        if (enemySkills != null)
        {
            enemySkills.enabled = false;
        }

        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
        }

        if (resultText != null)
        {
            resultText.text = playerWon
                ? "VICTORY"
                : "DEFEAT";

            resultText.color = playerWon
                ? new Color(0.2f, 1f, 0.4f)
                : new Color(1f, 0.25f, 0.25f);
        }
    }
}
