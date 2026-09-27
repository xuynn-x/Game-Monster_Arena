using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUIController : MonoBehaviour
{
    private const string LoginSceneName = "Login";
    [Header("Profile")]
    [SerializeField] private TMP_Text displayNameText;
    [SerializeField] private TMP_Text usernameText;
    [SerializeField] private TMP_Text playerLevelText;
    [SerializeField] private TMP_Text experienceText;
    [SerializeField] private Image experienceFill;
    [SerializeField] private GameObject profilePanel;
    [SerializeField] private TMP_Text profileDetailsText;
    [SerializeField] private Button closeProfileButton;
    [SerializeField] private GameObject rewardsPanel;
    [SerializeField] private Button closeRewardsButton;
    [SerializeField] private GameObject missionsPanel;
    [SerializeField] private Button closeMissionsButton;
    [SerializeField] private Button dailyMissionsButton;
    [SerializeField] private Button weeklyMissionsButton;
    [SerializeField] private TMP_Text missionsPeriodText;
    [SerializeField] private TMP_Text[] missionLabels;
    [SerializeField] private Image[] missionProgressBars;
    [Header("Ranking")]
    [SerializeField] private GameObject rankingPanel;
    [SerializeField] private Button closeRankingButton;
    [SerializeField] private TMP_Text rankingPlayerNameText;
    [SerializeField] private TMP_Text rankingStatisticsText;
    [Header("Shop")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private Button closeShopButton;
    [SerializeField] private Button[] shopCategoryButtons;
    [SerializeField] private TMP_Text shopCategoryTitle;
    [SerializeField] private TMP_Text shopStatusText;
    [Header("Inventory")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private Button closeInventoryButton;
    [SerializeField] private Button inventoryShopButton;
    [SerializeField] private Button[] inventoryCategoryButtons;
    [SerializeField] private TMP_Text inventoryCategoryText;
    [SerializeField] private TMP_Text inventoryEmptyText;

    [Header("Active Monster")]
    [SerializeField] private TMP_Text selectedMonsterNameText;
    [SerializeField] private TMP_Text selectedMonsterStatusText;

    [Header("Statistics")]
    [SerializeField] private TMP_Text ownedMonsterCountText;
    [SerializeField] private TMP_Text totalMatchesText;
    [SerializeField] private TMP_Text winRateText;

    [Header("Actions")]
    [SerializeField] private Button homeButton;
    [SerializeField] private Button monsterNavButton;
    [SerializeField] private Button teamButton;
    [SerializeField] private Button inventoryButton;
    [SerializeField] private Button profileButton;
    [SerializeField] private Button rankingButton;
    [SerializeField] private Button missionsButton;
    [SerializeField] private Button rewardsButton;
    [UnityEngine.Serialization.FormerlySerializedAs("guideButton")]
    [SerializeField] private Button shopButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button historyNavButton;
    [SerializeField] private Button findMatchButton;
    [SerializeField] private Button chooseMonsterButton;
    [SerializeField] private Button historyButton;
    [SerializeField] private Button logoutButton;
    [SerializeField] private TMP_Text actionMessageText;

    private void Awake()
    {
        actionMessageText.text = string.Empty;
        Bind(monsterNavButton, () => actionMessageText.text = "Xem bộ sưu tập quái của bạn.");
        Bind(teamButton, () => actionMessageText.text = "Sắp xếp đội quái trước khi tìm trận.");
        Bind(homeButton, ShowHomeMessage);
        Bind(inventoryButton, ShowInventoryMessage);
        Bind(profileButton, ShowProfileMessage);
        Bind(rankingButton, ShowRankingMessage);
        Bind(missionsButton, ShowMissionsMessage);
        Bind(rewardsButton, ShowRewardsMessage);
        Bind(shopButton, ShowShop);
        Bind(settingsButton, ShowSettingsMessage);
        Bind(historyNavButton, ShowHistoryMessage);
        Bind(findMatchButton, ShowFindMatchMessage);
        Bind(chooseMonsterButton, ShowChooseMonsterMessage);
        Bind(historyButton, ShowHistoryMessage);
        Bind(logoutButton, Logout);
        Bind(closeProfileButton, () => profilePanel.SetActive(false));
        Bind(closeRewardsButton, () => rewardsPanel.SetActive(false));
        Bind(closeMissionsButton, () => missionsPanel.SetActive(false));
        Bind(dailyMissionsButton, () => RefreshMissions(false));
        Bind(weeklyMissionsButton, () => RefreshMissions(true));
        Bind(closeRankingButton, () => rankingPanel.SetActive(false));
        Bind(closeShopButton, () => shopPanel.SetActive(false));
        Bind(closeInventoryButton, () => inventoryPanel.SetActive(false));
        Bind(inventoryShopButton, () => { inventoryPanel.SetActive(false); ShowShop(); });
        if (inventoryCategoryButtons != null)
            for (int i = 0; i < inventoryCategoryButtons.Length; i++)
            {
                int category = i;
                Bind(inventoryCategoryButtons[i], () => SelectInventoryCategory(category));
            }
        if (shopCategoryButtons != null)
            for (int i = 0; i < shopCategoryButtons.Length; i++)
            {
                int category = i;
                Bind(shopCategoryButtons[i], () => SelectShopCategory(category));
            }
    }

    private static void Bind(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
        {
            button.onClick.AddListener(action);
        }
    }

    private void Start()
    {
        if (!LocalAccountService.TryGetCurrentAccount(out LocalAccountData account))
        {
            ReturnToLogin();
            return;
        }

        if (account.ownedMonsterIds.Count == 0)
        {
            if (!LocalAccountService.AddOwnedMonster(
                MonsterCatalog.StarterShadowFoxId,
                out string errorMessage
            ) || !LocalAccountService.TryGetCurrentAccount(out account))
            {
                Debug.LogError($"Không cấp được quái khởi đầu: {errorMessage}");
                ReturnToLogin();
                return;
            }
        }

        Refresh(account);
    }

    private void Refresh(LocalAccountData account)
    {
        displayNameText.text = account.displayName;
        usernameText.text = $"@{account.username}";
        int level = Mathf.Max(1, account.level);
        int experience = Mathf.Max(0, account.experience);
        int requiredExperience = level * 100;
        if (playerLevelText != null) playerLevelText.text = $"LV. {level:00}";
        if (experienceText != null) experienceText.text = $"{experience} / {requiredExperience} EXP";
        if (experienceFill != null) experienceFill.fillAmount = Mathf.Clamp01((float)experience / requiredExperience);

        selectedMonsterNameText.text = string.IsNullOrEmpty(account.selectedMonsterId)
            ? "CHƯA CHỌN QUÁI"
            : account.selectedMonsterId == MonsterCatalog.StarterShadowFoxId
                ? MonsterCatalog.StarterShadowFoxDisplayName
                : account.selectedMonsterId;

        selectedMonsterStatusText.text = string.IsNullOrEmpty(
            account.selectedMonsterId
        )
            ? "Hãy chọn một quái trước khi tìm trận"
            : account.selectedMonsterId == MonsterCatalog.StarterShadowFoxId
                ? $"HỆ {MonsterCatalog.StarterShadowFoxElement} • Sẵn sàng chiến đấu"
                : "Sẵn sàng chiến đấu";

        int totalMatches = account.matchHistory.Count;
        int wins = account.matchHistory.Count(match => match.playerWon);
        int winRate = totalMatches == 0
            ? 0
            : Mathf.RoundToInt(wins * 100f / totalMatches);

        ownedMonsterCountText.text = account.ownedMonsterIds.Count.ToString();
        totalMatchesText.text = totalMatches.ToString();
        winRateText.text = $"{winRate}%";
        GetComponent<MonsterTeamUIController>()?.RefreshHomeArt();
    }

    private void ShowFindMatchMessage()
    {
        if (!BattleMatchmaking.TryBegin(out string error))
        {
            actionMessageText.text = error;
            return;
        }
        actionMessageText.text = "Đang tìm đối thủ…";
        MatchmakingPanel.Show(findMatchButton, actionMessageText);
    }

    private void Update()
    {
        string notice = BattleMatchmaking.TakeMenuNotice();
        if (!string.IsNullOrEmpty(notice)) actionMessageText.text = notice;
    }

    private void ShowHomeMessage()
    {
        actionMessageText.text = "Bạn đang ở Trang chủ.";
    }

    private void ShowChooseMonsterMessage()
    {
        actionMessageText.text = "Xem bộ sưu tập quái của bạn.";
        GetComponent<MonsterTeamUIController>()?.OpenMonsters();
    }

    private void ShowTeamMessage()
    {
        actionMessageText.text = "Sắp xếp đội quái trước khi tìm trận.";
        GetComponent<MonsterTeamUIController>()?.OpenTeam();
    }

    public void RefreshCurrentAccount()
    {
        if (LocalAccountService.TryGetCurrentAccount(out LocalAccountData account)) Refresh(account);
    }

    private void ShowInventoryMessage()
    {
        actionMessageText.text = "Xem vật phẩm trong túi đồ.";
        if (inventoryPanel == null) return;
        SelectInventoryCategory(0);
        inventoryPanel.SetActive(true);
    }

    private void SelectInventoryCategory(int category)
    {
        string[] names = { "TẤT CẢ", "TIÊU HAO", "NGUYÊN LIỆU", "ĐẶC BIỆT" };
        if (category < 0 || category >= names.Length) return;
        inventoryCategoryText.text = names[category] + "  •  0 VẬT PHẨM";
        inventoryEmptyText.text = category == 0 ? "BALÔ ĐANG TRỐNG\nVật phẩm nhận được sẽ xuất hiện tại đây." :
            "CHƯA CÓ VẬT PHẨM\nKhông có vật phẩm trong danh mục này.";
        for (int i = 0; i < inventoryCategoryButtons.Length; i++)
            inventoryCategoryButtons[i].interactable = i != category;
    }

    private void ShowProfileMessage()
    {
        actionMessageText.text = "Xem hồ sơ và thống kê của bạn.";
        if (profilePanel == null || !LocalAccountService.TryGetCurrentAccount(out LocalAccountData account)) return;
        int wins = account.matchHistory.Count(match => match.playerWon);
        string history = account.matchHistory.Count == 0 ? "Chưa có trận đấu." :
            string.Join("\n", account.matchHistory.AsEnumerable().Reverse().Take(5)
                .Select(match => $"{(match.playerWon ? "THẮNG" : "THUA")}  •  {match.opponentName}"));
        profileDetailsText.text = $"{account.displayName}\n@{account.username}\n\nLV. {Mathf.Max(1, account.level):00}   •   {Mathf.Max(0, account.experience)} EXP\n" +
            $"Quái sở hữu: {account.ownedMonsterIds.Count}   •   Thắng: {wins}/{account.matchHistory.Count}\n\nLỊCH SỬ GẦN ĐÂY\n{history}";
        profileDetailsText.richText = false;
        profilePanel.SetActive(true);
    }

    private void ShowRankingMessage()
    {
        actionMessageText.text = "Xem thống kê thi đấu của bạn.";
        if (rankingPanel == null || !LocalAccountService.TryGetCurrentAccount(out LocalAccountData account)) return;
        int total = account.matchHistory.Count;
        int wins = account.matchHistory.Count(match => match.playerWon);
        int rate = total == 0 ? 0 : Mathf.RoundToInt(100f * wins / total);
        rankingPlayerNameText.richText = false;
        rankingPlayerNameText.text = account.displayName;
        rankingStatisticsText.text = $"TRẬN ĐÃ ĐẤU    {total}\nCHIẾN THẮNG    {wins}\nTỶ LỆ THẮNG    {rate}%";
        rankingPanel.SetActive(true);
    }

    private void ShowMissionsMessage()
    {
        actionMessageText.text = "Theo dõi nhiệm vụ ngày và tuần.";
        if (missionsPanel == null) return;
        RefreshMissions(false);
        missionsPanel.SetActive(true);
    }

    private void RefreshMissions(bool weekly)
    {
        if (!LocalAccountService.TryGetCurrentAccount(out LocalAccountData account)) return;
        System.DateTime start = System.DateTime.UtcNow.Date;
        if (weekly) start = start.AddDays(-((int)start.DayOfWeek + 6) % 7);
        System.DateTime end = start.AddDays(weekly ? 7 : 1);
        var matches = account.matchHistory.Where(match => System.DateTime.TryParse(match.playedAtUtc,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal,
            out var date) && date >= start && date < end).ToList();
        int[] targets = weekly ? new[] { 10, 5, 20 } : new[] { 1, 1, 3 };
        int[] values = { matches.Count, matches.Count(match => match.playerWon), matches.Count };
        string[] names = { "BƯỚC VÀO ĐẤU TRƯỜNG", "GIÀNH CHIẾN THẮNG", "CHĂM CHỈ TRANH TÀI" };
        for (int i = 0; i < 3; i++)
        {
            int value = Mathf.Min(values[i], targets[i]);
            missionLabels[i].text = $"{names[i]}\n<size=19>{(i == 1 ? "Thắng" : "Hoàn thành")} {targets[i]} trận  •  {value}/{targets[i]}" +
                (value >= targets[i] ? "  •  HOÀN THÀNH</size>" : "  •  ĐANG THỰC HIỆN</size>");
            missionProgressBars[i].fillAmount = (float)value / targets[i];
        }
        missionsPeriodText.text = $"{(weekly ? "NHIỆM VỤ TUẦN" : "NHIỆM VỤ NGÀY")}  •  Làm mới {end:dd/MM HH:mm} UTC";
        dailyMissionsButton.interactable = weekly;
        weeklyMissionsButton.interactable = !weekly;
    }

    private void ShowRewardsMessage()
    {
        actionMessageText.text = "Xem phần thưởng.";
        if (rewardsPanel != null) rewardsPanel.SetActive(true);
    }

    private void ShowShop()
    {
        actionMessageText.text = "Xem các danh mục cửa hàng.";
        if (shopPanel == null) return;
        SelectShopCategory(0);
        shopPanel.SetActive(true);
    }

    private void SelectShopCategory(int category)
    {
        string[] titles = { "QUÁI ĐỒNG HÀNH", "VẬT PHẨM & NGUYÊN LIỆU", "COIN & RUBY" };
        string[] descriptions = {
            "Chưa có quái được mở bán.\nGhé lại khi cửa hàng cập nhật bộ sưu tập mới.",
            "Chưa có vật phẩm được mở bán.\nVật phẩm và nguyên liệu sẽ được cập nhật tại đây.",
            "Chưa có gói tiền tệ được mở bán.\nCác gói Coin và Ruby hiện chưa khả dụng."
        };
        if (category < 0 || category >= titles.Length) return;
        shopCategoryTitle.text = titles[category];
        shopStatusText.text = descriptions[category];
        for (int i = 0; i < shopCategoryButtons.Length; i++)
        {
            Button tab = shopCategoryButtons[i];
            tab.interactable = i != category;
        }
    }

    private void ShowSettingsMessage()
    {
        actionMessageText.text = "SETTINGS → Graphics, Audio, Controls, Language, Exit.";
    }

    private void ShowHistoryMessage()
    {
        if (!LocalAccountService.TryGetCurrentAccount(out LocalAccountData account))
        {
            ReturnToLogin();
            return;
        }

        actionMessageText.text = account.matchHistory.Count == 0
            ? "Bạn chưa có trận đấu nào."
            : $"Bạn đã hoàn thành {account.matchHistory.Count} trận.";
    }

    private void Logout()
    {
        LocalAccountService.Logout();
        ReturnToLogin();
    }

    private static void ReturnToLogin()
    {
        if (Application.CanStreamedLevelBeLoaded(LoginSceneName))
        {
            SceneManager.LoadScene(LoginSceneName);
        }
    }
}
