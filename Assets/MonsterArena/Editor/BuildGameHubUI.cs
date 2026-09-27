using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class BuildGameHubUI
{
    private const string AutoBuildKey = "MonsterArena.GameHub.AutoBuild.20260916.v18";
    private const string ScenePath = "Assets/MonsterArena/Scenes/MainMenu.unity";
    private const string LoginScenePath = "Assets/MonsterArena/Scenes/Auth/Login.unity";
    private const string ArenaScenePath = "Assets/MonsterArena/Scenes/Arena.unity";
    private const string BackgroundPath = "Assets/MonsterArena/Art/UI/DesignSystem/ui_home_arena_v2.png";
    private const string AvatarPath = "Assets/MonsterArena/Art/UI/Avatars/avatar_pink_trainer_portrait.png";
    private const string MonsterPath = "Assets/MonsterArena/Art/Monsters/ShadowFox/shadow_fox_art_side.png";
    private const string MonsterCutoutPath = "Assets/MonsterArena/Art/Monsters/ShadowFox/shadow_fox_art_side_cutout.png";
    private const string UiKitPath = "Assets/MonsterArena/Art/UI/DesignSystem/";
    private const string TopHudFramePath = UiKitPath + "ui_top_hud_frame_trimmed.png";
    private const string MonsterShowcaseFramePath = UiKitPath + "ui_monster_showcase_frame_trimmed.png";
    private const string BottomNavigationPath = UiKitPath + "ui_bottom_navigation_trimmed.png";
    private const string MenuButtonPath = UiKitPath + "btn_menu_normal_trimmed.png";
    private const string SideButtonPath = UiKitPath + "btn_side_action_normal_trimmed.png";
    private const string BattleButtonPath = UiKitPath + "btn_battle_normal_trimmed.png";
    private const string TrainerSummaryPanelPath = UiKitPath + "ui_trainer_summary_panel_v1.png";
    private const string IconPath = UiKitPath + "Icons/";
    private const string MonsterIconPath = IconPath + "icon_monsters_v1.png";
    private const string TeamIconPath = IconPath + "icon_team_v1.png";
    private const string InventoryIconPath = IconPath + "icon_inventory_v1.png";
    private const string ProfileIconPath = IconPath + "icon_profile_v1.png";
    private const string BattleIconPath = IconPath + "icon_battle_v1.png";

    private static readonly Color Ink = Hex("090817");
    private static readonly Color Glass = Hex("151126", 0.92f);
    private static readonly Color GlassLight = Hex("24183A", 0.94f);
    private static readonly Color Violet = Hex("8C3DFF");
    private static readonly Color VioletBright = Hex("B76DFF");
    private static readonly Color Pink = Hex("FF6FAE");
    private static readonly Color Cyan = Hex("53E2FF");
    private static readonly Color White = Hex("FFF9FF");
    private static readonly Color Muted = Hex("B8AEC7");
    private static readonly Color Green = Hex("69F0B4");

    [InitializeOnLoadMethod]
    private static void ScheduleAutomaticBuild()
    {
        EditorApplication.delayCall += TryAutomaticBuild;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.delayCall += TryAutomaticBuild;
        }
    }

    private static void TryAutomaticBuild()
    {
        if (SessionState.GetBool(AutoBuildKey, false))
        {
            return;
        }

        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
            return;
        }

        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        SessionState.SetBool(AutoBuildKey, true);
        Build();
    }

    [MenuItem("Monster Arena/Rebuild Home Game Hub")]
    public static void Build()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("Hãy dừng Play Mode trước khi dựng lại Home Menu.");
            return;
        }

        TMP_FontAsset font = TMP_Settings.defaultFontAsset;
        TMP_FontAsset titleFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Bangers SDF.asset"
        ) ?? font;
        Sprite panel = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        Sprite circle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        Sprite backgroundSprite = LoadSprite(BackgroundPath, 2048);
        Sprite avatarSprite = LoadSprite(AvatarPath, 1024);
        Sprite monsterSprite = LoadSprite("Assets/MonsterArena/Art/Monsters/ShadowFox/shadow_fox_cutout_v2.png", 2048);
        EnsureTrimmedVariant(UiKitPath + "ui_top_hud_frame.png", TopHudFramePath);
        EnsureTrimmedVariant(UiKitPath + "ui_monster_showcase_frame.png", MonsterShowcaseFramePath);
        EnsureTrimmedVariant(UiKitPath + "ui_bottom_navigation.png", BottomNavigationPath);
        EnsureTrimmedVariant(UiKitPath + "btn_menu_normal.png", MenuButtonPath);
        EnsureTrimmedVariant(UiKitPath + "btn_side_action_normal.png", SideButtonPath);
        EnsureTrimmedVariant(UiKitPath + "btn_battle_normal.png", BattleButtonPath);
        Sprite topHudSprite = LoadSprite(TopHudFramePath, 4096);
        Sprite monsterFrameSprite = LoadSprite(MonsterShowcaseFramePath, 2048);
        Sprite bottomNavigationSprite = LoadSprite(BottomNavigationPath, 4096);
        Sprite menuButtonSprite = LoadSprite(MenuButtonPath, 4096);
        Sprite sideButtonSprite = LoadSprite(SideButtonPath, 4096);
        Sprite battleButtonSprite = LoadSprite(BattleButtonPath, 4096);
        Sprite trainerSummarySprite = LoadSprite(TrainerSummaryPanelPath, 2048);
        Sprite monsterIconSprite = LoadSprite(MonsterIconPath, 1024);
        Sprite teamIconSprite = LoadSprite(TeamIconPath, 1024);
        Sprite inventoryIconSprite = LoadSprite(InventoryIconPath, 1024);
        Sprite profileIconSprite = LoadSprite(ProfileIconPath, 1024);
        Sprite battleIconSprite = LoadSprite(BattleIconPath, 1024);

        Scene scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null
            ? EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single)
            : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Object.DestroyImmediate(root);
        }

        CreateCamera();
        GameObject canvasObject = UI("MainMenuCanvas", null);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();
        MainMenuUIController controller = canvasObject.AddComponent<MainMenuUIController>();

        GameObject background = Image("ArenaBackground", canvasObject.transform, Color.white, backgroundSprite);
        Stretch(background.GetComponent<RectTransform>());
        background.GetComponent<Image>().type = UnityEngine.UI.Image.Type.Simple;

        GameObject veil = Image("CinematicVeil", canvasObject.transform, Hex("080716", 0.12f), null);
        Stretch(veil.GetComponent<RectTransform>());

        GameObject topShade = Image("TopShade", canvasObject.transform, Color.clear, null);
        Anchors(topShade, new Vector2(0f, 0.84f), Vector2.one, Vector2.zero, Vector2.zero);

        GameObject bottomShade = Image("BottomShade", canvasObject.transform, Color.clear, null);
        Anchors(bottomShade, Vector2.zero, new Vector2(1f, 0.23f), Vector2.zero, Vector2.zero);

        GameObject topBar = UI("TopBar", canvasObject.transform);
        Anchors(topBar, new Vector2(0.035f, 0.82f), new Vector2(0.965f, 0.965f), Vector2.zero, Vector2.zero);

        GameObject avatar = Image("AvatarImage", topBar.transform, Color.white, avatarSprite);
        Rect(avatar, new Vector2(0.05f, 0.5f), new Vector2(92f, 92f));
        avatar.GetComponent<Image>().type = UnityEngine.UI.Image.Type.Simple;
        avatar.GetComponent<Image>().preserveAspect = true;
        Outline(avatar, Pink, new Vector2(2f, -2f));

        TMP_Text displayName = Text("DisplayNameText", topBar.transform, "Người chơi", font, 23f, White, TextAlignmentOptions.Left);
        Rect(displayName.gameObject, new Vector2(0.145f, 0.65f), new Vector2(245f, 34f));
        displayName.fontStyle = FontStyles.Bold;
        TMP_Text username = Text("UsernameText", topBar.transform, "@username", font, 13f, Muted, TextAlignmentOptions.Left);
        Rect(username.gameObject, new Vector2(0.145f, 0.31f), new Vector2(245f, 25f));

        TMP_Text level = Text("PlayerLevel", topBar.transform, "LV. 01", font, 17f, Cyan, TextAlignmentOptions.Center);
        Rect(level.gameObject, new Vector2(0.25f, 0.62f), new Vector2(110f, 30f));
        level.fontStyle = FontStyles.Bold;
        TMP_Text rank = Text("PlayerRank", topBar.transform, "HẠNG TÂN BINH", font, 12f, Muted, TextAlignmentOptions.Center);
        Rect(rank.gameObject, new Vector2(0.25f, 0.30f), new Vector2(150f, 24f));

        TMP_Text logo = Text("GameLogo", topBar.transform, "MONSTER\nARENA", titleFont, 62f, White, TextAlignmentOptions.Center);
        Rect(logo.gameObject, new Vector2(0.5f, 0.55f), new Vector2(500f, 140f));
        logo.lineSpacing = -12f;
        logo.enableVertexGradient = true;
        logo.colorGradient = new VertexGradient(White, White, Cyan, VioletBright);
        logo.outlineColor = Hex("311259");
        logo.outlineWidth = 0.18f;
        TMP_Text season = Text("SeasonLabel", topBar.transform, "SEASON 01  •  SHADOW AWAKENING", font, 11f, Cyan, TextAlignmentOptions.Center);
        Rect(season.gameObject, new Vector2(0.5f, -0.02f), new Vector2(440f, 22f));
        season.characterSpacing = 2f;

        CreateCurrency(topBar.transform, "Gold", "COIN", "0", Hex("FFD36A"), new Vector2(0.73f, 0.5f), font, panel);
        CreateCurrency(topBar.transform, "Gems", "RUBY", "0", Hex("FF7185"), new Vector2(0.865f, 0.5f), font, panel);
        Button settings = ButtonUI("SettingsButton", topBar.transform, "SETTINGS", font, Hex("332346", 0.95f), VioletBright, 14f);
        Rect(settings.gameObject, new Vector2(0.955f, 0.65f), new Vector2(115f, 38f));
        Button logout = ButtonUI("LogoutButton", topBar.transform, "ĐĂNG XUẤT", font, Hex("44172E", 0.9f), Pink, 11f);
        Rect(logout.gameObject, new Vector2(0.955f, 0.25f), new Vector2(115f, 28f));

        GameObject heroAura = Image("HeroAura", canvasObject.transform, Color.clear, circle);
        Rect(heroAura, new Vector2(0.56f, 0.52f), new Vector2(650f, 650f));

        GameObject monsterFrame = UI("MonsterShowcase", canvasObject.transform);
        Rect(monsterFrame, new Vector2(0.48f, 0.51f), new Vector2(500f, 610f));

        GameObject monster = Image("MonsterImage", monsterFrame.transform, Color.white, monsterSprite);
        Rect(monster, new Vector2(0.5f, 0.5f), new Vector2(500f, 610f));
        monster.GetComponent<Image>().type = UnityEngine.UI.Image.Type.Simple;
        monster.GetComponent<Image>().preserveAspect = true;

        TMP_Text monsterName = Text("SelectedMonsterNameText", monsterFrame.transform, "SHADOW FOX", font, 30f, White, TextAlignmentOptions.Center);
        Rect(monsterName.gameObject, new Vector2(1.20f, 0.57f), new Vector2(320f, 46f));
        monsterName.fontStyle = FontStyles.Bold;
        TMP_Text monsterStatus = Text("SelectedMonsterStatusText", monsterFrame.transform, "HỆ BÓNG TỐI • SẴN SÀNG", font, 13f, Green, TextAlignmentOptions.Center);
        Rect(monsterStatus.gameObject, new Vector2(1.20f, 0.50f), new Vector2(320f, 32f));

        GameObject statusCard = Image("BattleStatusCard", canvasObject.transform, new Color(1f, 1f, 1f, 0.48f), trainerSummarySprite);
        Rect(statusCard, new Vector2(0.18f, 0.32f), new Vector2(390f, 170f));
        statusCard.GetComponent<Image>().type = UnityEngine.UI.Image.Type.Simple;
        statusCard.GetComponent<Image>().preserveAspect = true;
        TMP_Text statusTitle = Text("StatusTitle", statusCard.transform, "HUẤN LUYỆN VIÊN", font, 14f, Pink, TextAlignmentOptions.Center);
        Rect(statusTitle.gameObject, new Vector2(0.5f, 0.87f), new Vector2(330f, 28f));
        statusTitle.fontStyle = FontStyles.Bold;
        TMP_Text tagline = Text("StatusTagline", statusCard.transform, "Sẵn sàng bước vào đấu trường?", font, 18f, White, TextAlignmentOptions.Center);
        Rect(tagline.gameObject, new Vector2(0.5f, 0.70f), new Vector2(340f, 38f));
        CreateStat(statusCard.transform, "OwnedMonsterCountText", "0", "QUÁI SỞ HỮU", new Vector2(0.18f, 0.42f), VioletBright, font);
        CreateStat(statusCard.transform, "TotalMatchesText", "0", "TRẬN ĐÃ ĐẤU", new Vector2(0.5f, 0.42f), Pink, font);
        CreateStat(statusCard.transform, "WinRateText", "0%", "TỶ LỆ THẮNG", new Vector2(0.82f, 0.42f), Cyan, font);
        TMP_Text teamHint = Text("TeamHint", statusCard.transform, "TEAM  1 / 5   •   BATTLE POWER  --", font, 12f, Muted, TextAlignmentOptions.Center);
        Rect(teamHint.gameObject, new Vector2(0.5f, 0.13f), new Vector2(340f, 28f));
        statusTitle.gameObject.SetActive(false);
        tagline.gameObject.SetActive(false);
        teamHint.gameObject.SetActive(false);

        Button missions = SideButton("MissionsButton", canvasObject.transform, "MISSIONS", "Nhiệm vụ ngày / tuần", new Vector2(0.105f, 0.61f), font, sideButtonSprite);
        Button ranking = SideButton("RankingButton", canvasObject.transform, "RANKING", "Bảng xếp hạng PvP", new Vector2(0.105f, 0.43f), font, sideButtonSprite);
        Button rewards = SideButton("RewardsButton", canvasObject.transform, "REWARDS", "Quà và phần thưởng", new Vector2(0.895f, 0.61f), font, sideButtonSprite);
        Button guide = SideButton("ShopButton", canvasObject.transform, "SHOP", "Quái • Vật phẩm", new Vector2(0.895f, 0.43f), font, sideButtonSprite);

        TMP_Text actionMessage = Text("ActionMessageText", canvasObject.transform, string.Empty, font, 15f, White, TextAlignmentOptions.Center);
        Rect(actionMessage.gameObject, new Vector2(0.5f, 0.225f), new Vector2(920f, 34f));

        GameObject dock = UI("MainActionDock", canvasObject.transform);
        Anchors(dock, new Vector2(0.035f, 0.035f), new Vector2(0.965f, 0.205f), Vector2.zero, Vector2.zero);

        Button monsters = MainButton("MonstersButton", dock.transform, "MONSTERS", "QUÁI SỞ HỮU", new Vector2(0.105f, 0.5f), menuButtonSprite, monsterIconSprite, font);
        Button team = MainButton("TeamButton", dock.transform, "TEAM", "ĐỘI HÌNH 5 QUÁI", new Vector2(0.30f, 0.5f), menuButtonSprite, teamIconSprite, font);
        Button inventory = MainButton("InventoryButton", dock.transform, "INVENTORY", "VẬT PHẨM", new Vector2(0.495f, 0.5f), menuButtonSprite, inventoryIconSprite, font);
        Button profile = MainButton("ProfileButton", dock.transform, "PROFILE", "THÔNG TIN", new Vector2(0.69f, 0.5f), menuButtonSprite, profileIconSprite, font);
        Button battle = ButtonUI("BattleButton", dock.transform, "Start\n<size=22><color=#FFE2F5>RANK</color></size>", font, Color.white, Hex("FFD4F0"), 25f, battleButtonSprite);
        Rect(battle.gameObject, new Vector2(0.875f, 0.5f), new Vector2(440f, 160f));
        AddButtonIcon(battle, battleIconSprite, 72f, 0.17f);
        // Preserve the Battle icon placement approved in MainMenu on 27/09/2026.
        var battleIcon = battle.transform.Find("ActionIcon") as RectTransform;
        if (battleIcon != null) battleIcon.anchoredPosition = new Vector2(30.6f, -11.5f);
        TMP_Text battleLabel = battle.GetComponentInChildren<TMP_Text>();
        Stretch(battleLabel.rectTransform);
        battleLabel.alignment = TextAlignmentOptions.Center;

        Assign(controller, displayName, username, monsterName, monsterStatus,
            FindText(canvasObject, "OwnedMonsterCountText"), FindText(canvasObject, "TotalMatchesText"), FindText(canvasObject, "WinRateText"),
            monsters, team, inventory, profile, ranking, missions, rewards, guide, settings, battle, logout, actionMessage);

        RefineHome(canvasObject, controller, font, circle, panel, avatarSprite, settings,
            profile, logout, monsters, team, inventory, battle, missions, ranking, rewards, guide);
        CreateEventSystem();
        EnsureBuildSettings();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = canvasObject;
        Debug.Log("[Monster Arena] Đã dựng Home Game Hub mới và nối toàn bộ nút.");
    }

    private static void RefineHome(GameObject root, MainMenuUIController controller, TMP_FontAsset font,
        Sprite circle, Sprite panel, Sprite portrait, Button settings, Button oldProfile, Button logout,
        Button monsters, Button team, Button inventory, Button battle, Button missions, Button ranking, Button rewards, Button guide)
    {
        Object.DestroyImmediate(oldProfile.gameObject);
        Object.DestroyImmediate(logout.gameObject);
        TMP_Text logo = FindText(root, "GameLogo");
        logo.fontSize = 82f;
        Rect(logo.gameObject, new Vector2(0.5f, 0.48f), new Vector2(540f, 182f));
        FindText(root, "SeasonLabel").gameObject.SetActive(false);

        GameObject hud = Image("PlayerHud", root.transform, Hex("090C18", 0.88f), panel);
        Rect(hud, new Vector2(0.175f, 0.895f), new Vector2(560f, 150f));
        Outline(hud, Hex("A998C2", 0.65f), new Vector2(1f, -1f));
        GameObject oldAvatar = root.transform.Find("TopBar/AvatarImage").gameObject;
        Object.DestroyImmediate(oldAvatar);
        GameObject avatarMask = Image("AvatarProfileButton", hud.transform, Color.white, circle);
        Rect(avatarMask, new Vector2(0.125f, 0.5f), new Vector2(136f, 136f));
        avatarMask.GetComponent<Image>().type = UnityEngine.UI.Image.Type.Simple;
        avatarMask.GetComponent<Image>().raycastTarget = true;
        avatarMask.AddComponent<Mask>().showMaskGraphic = false;
        GameObject avatar = Image("Portrait", avatarMask.transform, Color.white, portrait);
        Stretch(avatar.GetComponent<RectTransform>());
        avatar.GetComponent<Image>().type = UnityEngine.UI.Image.Type.Simple;
        avatar.GetComponent<Image>().preserveAspect = true;
        Button profile = avatarMask.AddComponent<Button>();
        profile.targetGraphic = avatarMask.GetComponent<Image>();
        GameObject ring = Image("AvatarRing", hud.transform, Color.white, LoadSprite(UiKitPath + "ui_avatar_ring_v1.png", 512));
        Rect(ring, new Vector2(0.125f, 0.5f), new Vector2(172f, 172f));
        ring.GetComponent<Image>().type = UnityEngine.UI.Image.Type.Simple;
        ring.GetComponent<Image>().preserveAspect = true;
        TMP_Text name = FindText(root, "DisplayNameText");
        name.transform.SetParent(hud.transform, false);
        Rect(name.gameObject, new Vector2(0.625f, 0.77f), new Vector2(370f, 40f));
        name.text = "Tên hiển thị";
        name.fontSize = 30f;
        name.richText = false;
        name.enableAutoSizing = true;
        name.fontSizeMin = 18f;
        name.fontSizeMax = 30f;
        name.textWrappingMode = TextWrappingModes.NoWrap;
        name.overflowMode = TextOverflowModes.Ellipsis;
        FindText(root, "UsernameText").gameObject.SetActive(false);
        TMP_Text level = FindText(root, "PlayerLevel");
        level.transform.SetParent(hud.transform, false);
        Rect(level.gameObject, new Vector2(0.37f, 0.49f), new Vector2(86f, 30f));
        level.fontSize = 22f;
        TMP_Text rank = FindText(root, "PlayerRank");
        rank.transform.SetParent(hud.transform, false);
        Rect(rank.gameObject, new Vector2(0.62f, 0.14f), new Vector2(350f, 26f));
        rank.fontSize = 17f;
        GameObject expTrack = Image("ExperienceTrack", hud.transform, Hex("030611"), panel);
        Rect(expTrack, new Vector2(0.72f, 0.49f), new Vector2(245f, 14f));
        GameObject expBar = Image("ExperienceFill", expTrack.transform, VioletBright, panel);
        Stretch(expBar.GetComponent<RectTransform>());
        Image fill = expBar.GetComponent<Image>();
        fill.type = UnityEngine.UI.Image.Type.Filled;
        fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
        fill.fillAmount = 0f;
        TMP_Text exp = Text("ExperienceText", hud.transform, "0 / 100 EXP", font, 13f, Muted, TextAlignmentOptions.Center);
        Rect(exp.gameObject, new Vector2(0.72f, 0.32f), new Vector2(245f, 22f));

        settings.GetComponent<Image>().sprite = LoadSprite(IconPath + "icon_settings_v1.png", 256);
        settings.GetComponent<Image>().color = Color.white;
        settings.GetComponent<Image>().preserveAspect = true;
        ColorBlock settingsColors = settings.colors;
        settingsColors.normalColor = Color.white;
        settings.colors = settingsColors;
        settings.GetComponentInChildren<TMP_Text>().text = "";
        Rect(settings.gameObject, new Vector2(0.967f, 0.52f), new Vector2(92f, 92f));
        Button[] utility = { missions, ranking, rewards, guide };
        for (int i = 0; i < utility.Length; i++)
        {
            Rect(utility[i].gameObject, new Vector2(0.87f, 0.71f - i * 0.135f), new Vector2(370f, 112f));
            TMP_Text label = utility[i].GetComponentInChildren<TMP_Text>();
            Anchors(label.gameObject, new Vector2(0.28f, 0f), new Vector2(0.91f, 1f), Vector2.zero, Vector2.zero);
            label.fontSize = 22f;
        }
        AddButtonIcon(rewards, LoadSprite(IconPath + "icon_rewards_v1.png", 512), 78f, 0.16f);
        AddButtonIcon(missions, LoadSprite(IconPath + "icon_missions_v1.png", 512), 78f, 0.16f);
        Sprite rankingIcon = LoadSprite(IconPath + "icon_ranking_v1.png", 512);
        AddButtonIcon(ranking, rankingIcon, 78f, 0.16f);
        Sprite shopIcon = LoadSprite(IconPath + "icon_shop_v1.png", 512);
        AddButtonIcon(guide, shopIcon, 78f, 0.16f);
        Rect(root.transform.Find("MonsterShowcase").gameObject, new Vector2(0.47f, 0.50f), new Vector2(500f, 610f));
        Rect(FindText(root, "SelectedMonsterNameText").gameObject, new Vector2(0.5f, 0.025f), new Vector2(360f, 42f));
        Rect(FindText(root, "SelectedMonsterStatusText").gameObject, new Vector2(0.5f, -0.025f), new Vector2(440f, 30f));
        GameObject dock = root.transform.Find("MainActionDock").gameObject;
        Anchors(dock, new Vector2(0.03f, 0f), new Vector2(0.97f, 0.18f), Vector2.zero, Vector2.zero);
        Button[] main = { monsters, team, inventory };
        for (int i = 0; i < main.Length; i++)
        {
            Rect(main[i].gameObject, new Vector2(0.105f + i * 0.225f, 0.5f), new Vector2(390f, 150f));
            main[i].GetComponentInChildren<TMP_Text>().fontSize = 26f;
            Rect(main[i].transform.Find("ActionIcon").gameObject, new Vector2(0.17f, 0.5f), new Vector2(78f, 78f));
        }
        Rect(battle.gameObject, new Vector2(0.865f, 0.68f), new Vector2(450f, 250f));
        battle.GetComponent<Image>().preserveAspect = false;
        TMP_Text startLabel = battle.GetComponentInChildren<TMP_Text>();
        startLabel.fontSize = 48f;
        startLabel.lineSpacing = 4f;
        startLabel.textWrappingMode = TextWrappingModes.NoWrap;
        Shadow(startLabel.gameObject, Hex("370D30", 0.9f), new Vector2(2f, -2f));
        Rect(FindText(root, "ActionMessageText").gameObject, new Vector2(0.5f, 0.197f), new Vector2(1100f, 30f));

        GameObject profileModal = CreateModal(root.transform, "ProfilePanel", "HỒ SƠ HUẤN LUYỆN VIÊN", font, panel, out Button closeProfile);
        TMP_Text details = Text("ProfileDetails", profileModal.transform.Find("Card"), "", font, 22f, White, TextAlignmentOptions.TopLeft);
        Anchors(details.gameObject, new Vector2(0.08f, 0.28f), new Vector2(0.92f, 0.78f), Vector2.zero, Vector2.zero);
        GameObject statistics = root.transform.Find("BattleStatusCard").gameObject;
        statistics.transform.SetParent(profileModal.transform.Find("Card"), false);
        Rect(statistics, new Vector2(0.5f, 0.14f), new Vector2(740f, 140f));
        statistics.GetComponent<Image>().sprite = panel;
        statistics.GetComponent<Image>().type = UnityEngine.UI.Image.Type.Sliced;
        statistics.GetComponent<Image>().preserveAspect = false;
        statistics.GetComponent<Image>().color = Hex("211630", 0.8f);
        GameObject rewardsModal = CreateModal(root.transform, "RewardsPanel", "QUÀ & PHẦN THƯỞNG", font, panel, out Button closeRewards);
        GameObject gift = Image("GiftIllustration", rewardsModal.transform.Find("Card"), Color.white, LoadSprite(IconPath + "icon_rewards_v1.png", 512));
        Rect(gift, new Vector2(0.5f, 0.56f), new Vector2(230f, 230f));
        gift.GetComponent<Image>().type = UnityEngine.UI.Image.Type.Simple;
        gift.GetComponent<Image>().preserveAspect = true;
        TMP_Text empty = Text("RewardStatus", rewardsModal.transform.Find("Card"), "Chưa có phần thưởng để nhận.\nQuà đăng nhập và phần thưởng sự kiện sẽ hiển thị tại đây.", font, 23f, White, TextAlignmentOptions.Center);
        Rect(empty.gameObject, new Vector2(0.5f, 0.22f), new Vector2(720f, 100f));
        SerializedObject so = new(controller);
        Set(so, "profileButton", profile);
        Set(so, "logoutButton", null);
        Set(so, "playerLevelText", level);
        Set(so, "experienceText", exp);
        Set(so, "experienceFill", fill);
        Set(so, "profilePanel", profileModal);
        Set(so, "profileDetailsText", details);
        Set(so, "closeProfileButton", closeProfile);
        Set(so, "rewardsPanel", rewardsModal);
        Set(so, "closeRewardsButton", closeRewards);
        GameObject missionsModal = CreateModal(root.transform, "MissionsPanel", "MISSIONS • NHIỆM VỤ", font, panel, out Button closeMissions);
        Transform missionCard = missionsModal.transform.Find("Card");
        Rect(missionCard.gameObject, new Vector2(0.5f, 0.5f), new Vector2(1040f, 780f));
        Button daily = ButtonUI("DailyTab", missionCard, "HÀNG NGÀY", font, Violet, White, 23f);
        Rect(daily.gameObject, new Vector2(0.30f, 0.79f), new Vector2(340f, 58f));
        Button weekly = ButtonUI("WeeklyTab", missionCard, "HÀNG TUẦN", font, Violet, White, 23f);
        Rect(weekly.gameObject, new Vector2(0.70f, 0.79f), new Vector2(340f, 58f));
        TMP_Text period = Text("Period", missionCard, "", font, 17f, Muted, TextAlignmentOptions.Center);
        Rect(period.gameObject, new Vector2(0.5f, 0.715f), new Vector2(900f, 32f));
        SerializedProperty labels = so.FindProperty("missionLabels");
        SerializedProperty bars = so.FindProperty("missionProgressBars");
        labels.arraySize = bars.arraySize = 3;
        for (int i = 0; i < 3; i++)
        {
            GameObject row = Image("MissionRow" + i, missionCard, Hex("241B36"), panel);
            Rect(row, new Vector2(0.5f, 0.60f - i * 0.18f), new Vector2(900f, 122f));
            Outline(row, Hex("78558F", 0.6f), new Vector2(1f, -1f));
            GameObject icon = Image("QuestIcon", row.transform, Color.white, LoadSprite(IconPath + "icon_missions_v1.png", 512));
            Rect(icon, new Vector2(0.065f, 0.5f), new Vector2(76f, 76f));
            icon.GetComponent<Image>().type = UnityEngine.UI.Image.Type.Simple;
            icon.GetComponent<Image>().preserveAspect = true;
            TMP_Text label = Text("MissionText", row.transform, "", font, 24f, White, TextAlignmentOptions.Left);
            Rect(label.gameObject, new Vector2(0.56f, 0.61f), new Vector2(740f, 70f));
            GameObject track = Image("ProgressTrack", row.transform, Ink, panel);
            Rect(track, new Vector2(0.56f, 0.19f), new Vector2(740f, 12f));
            GameObject progress = Image("ProgressFill", track.transform, VioletBright, panel);
            Stretch(progress.GetComponent<RectTransform>());
            Image fillImage = progress.GetComponent<Image>();
            fillImage.type = UnityEngine.UI.Image.Type.Filled;
            fillImage.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            fillImage.fillAmount = 0f;
            labels.GetArrayElementAtIndex(i).objectReferenceValue = label;
            bars.GetArrayElementAtIndex(i).objectReferenceValue = fillImage;
        }
        TMP_Text note = Text("RewardNote", missionCard, "Tiến độ từ lịch sử trận • Chưa có phần thưởng được cấu hình", font, 18f, Muted, TextAlignmentOptions.Center);
        Rect(note.gameObject, new Vector2(0.5f, 0.08f), new Vector2(900f, 36f));
        Set(so, "missionsPanel", missionsModal);
        Set(so, "closeMissionsButton", closeMissions);
        Set(so, "dailyMissionsButton", daily);
        Set(so, "weeklyMissionsButton", weekly);
        Set(so, "missionsPeriodText", period);
        GameObject rankingModal = CreateModal(root.transform, "RankingPanel", "RANKING • XẾP HẠNG PVP", font, panel, out Button closeRanking);
        Transform rankingCard = rankingModal.transform.Find("Card");
        Rect(rankingCard.gameObject, new Vector2(0.5f, 0.5f), new Vector2(1160f, 760f));
        GameObject personalCard = Image("PersonalRankCard", rankingCard, Hex("21152F"), panel);
        Rect(personalCard, new Vector2(0.215f, 0.47f), new Vector2(360f, 550f));
        Outline(personalCard, Hex("8D689E", 0.65f), new Vector2(1f, -1f));
        GameObject trophy = Image("Trophy", personalCard.transform, Color.white, rankingIcon);
        Rect(trophy, new Vector2(0.5f, 0.79f), new Vector2(150f, 164f));
        trophy.GetComponent<Image>().type = UnityEngine.UI.Image.Type.Simple;
        trophy.GetComponent<Image>().preserveAspect = true;
        TMP_Text playerName = Text("RankingPlayerName", personalCard.transform, "Tên hiển thị", font, 29f, White, TextAlignmentOptions.Center);
        Rect(playerName.gameObject, new Vector2(0.5f, 0.59f), new Vector2(310f, 48f));
        playerName.enableAutoSizing = true;
        playerName.fontSizeMin = 18f;
        playerName.fontSizeMax = 29f;
        playerName.textWrappingMode = TextWrappingModes.NoWrap;
        playerName.overflowMode = TextOverflowModes.Ellipsis;
        TMP_Text rankStatus = Text("RankStatus", personalCard.transform, "CHƯA XẾP HẠNG", font, 20f, VioletBright, TextAlignmentOptions.Center);
        Rect(rankStatus.gameObject, new Vector2(0.5f, 0.49f), new Vector2(310f, 35f));
        TMP_Text rankingStats = Text("RankingStatistics", personalCard.transform, "", font, 23f, White, TextAlignmentOptions.Left);
        Rect(rankingStats.gameObject, new Vector2(0.5f, 0.28f), new Vector2(290f, 135f));
        rankingStats.lineSpacing = 12f;
        TMP_Text localNote = Text("StatisticsCaption", personalCard.transform, "Thống kê trận trên tài khoản này", font, 15f, Muted, TextAlignmentOptions.Center);
        Rect(localNote.gameObject, new Vector2(0.5f, 0.095f), new Vector2(320f, 28f));

        GameObject board = Image("Leaderboard", rankingCard, Hex("0A0D1B"), panel);
        Rect(board, new Vector2(0.675f, 0.47f), new Vector2(630f, 550f));
        TMP_Text boardTitle = Text("LeaderboardTitle", board.transform, "BẢNG XẾP HẠNG", font, 26f, White, TextAlignmentOptions.Left);
        Rect(boardTitle.gameObject, new Vector2(0.5f, 0.91f), new Vector2(554f, 44f));
        GameObject header = Image("ColumnHeader", board.transform, Hex("292139"), panel);
        Rect(header, new Vector2(0.5f, 0.79f), new Vector2(560f, 48f));
        string[] columns = { "HẠNG", "NGƯỜI CHƠI", "ĐIỂM" };
        float[] centers = { 0.12f, 0.49f, 0.88f };
        for (int i = 0; i < columns.Length; i++)
        {
            TMP_Text column = Text("Column" + i, header.transform, columns[i], font, 17f, Muted, TextAlignmentOptions.Center);
            Rect(column.gameObject, new Vector2(centers[i], 0.5f), new Vector2(i == 1 ? 230f : 100f, 36f));
        }
        TMP_Text emptyRanking = Text("EmptyLeaderboard", board.transform,
            "CHƯA CÓ BẢNG XẾP HẠNG\n\n<size=20>Xếp hạng PvP chưa khả dụng.\nDanh sách người chơi và điểm hạng\nsẽ xuất hiện khi mùa đấu mở.</size>", font, 25f, White, TextAlignmentOptions.Center);
        Rect(emptyRanking.gameObject, new Vector2(0.5f, 0.44f), new Vector2(540f, 230f));
        TMP_Text rankFooter = Text("RankingFooter", board.transform, "Thứ hạng của bạn: —    •    Điểm hạng: —", font, 19f, VioletBright, TextAlignmentOptions.Center);
        Rect(rankFooter.gameObject, new Vector2(0.5f, 0.12f), new Vector2(560f, 45f));
        Set(so, "rankingPanel", rankingModal);
        Set(so, "closeRankingButton", closeRanking);
        Set(so, "rankingPlayerNameText", playerName);
        Set(so, "rankingStatisticsText", rankingStats);
        GameObject shopModal = CreateModal(root.transform, "ShopPanel", "SHOP • CỬA HÀNG", font, panel, out Button closeShop);
        Transform shopCard = shopModal.transform.Find("Card");
        Rect(shopCard.gameObject, new Vector2(0.5f, 0.5f), new Vector2(1160f, 760f));
        GameObject categories = Image("Categories", shopCard, Hex("21152F"), panel);
        Rect(categories, new Vector2(0.19f, 0.47f), new Vector2(290f, 550f));
        TMP_Text categoryCaption = Text("CategoryCaption", categories.transform, "DANH MỤC", font, 23f, White, TextAlignmentOptions.Center);
        Rect(categoryCaption.gameObject, new Vector2(0.5f, 0.90f), new Vector2(250f, 42f));
        string[] shopCategories = { "QUÁI", "VẬT PHẨM", "TIỀN TỆ" };
        SerializedProperty shopTabs = so.FindProperty("shopCategoryButtons");
        shopTabs.arraySize = shopCategories.Length;
        for (int i = 0; i < shopCategories.Length; i++)
        {
            Button tab = ButtonUI("Category" + i, categories.transform, shopCategories[i], font, GlassLight, White, 23f);
            Rect(tab.gameObject, new Vector2(0.5f, 0.72f - i * 0.20f), new Vector2(250f, 82f));
            ColorBlock colors = tab.colors;
            colors.disabledColor = Hex("C28BFF");
            tab.colors = colors;
            shopTabs.GetArrayElementAtIndex(i).objectReferenceValue = tab;
        }
        GameObject catalog = Image("Catalog", shopCard, Hex("0A0D1B"), panel);
        Rect(catalog, new Vector2(0.655f, 0.47f), new Vector2(710f, 550f));
        TMP_Text shopTitle = Text("CategoryTitle", catalog.transform, "QUÁI ĐỒNG HÀNH", font, 28f, White, TextAlignmentOptions.Center);
        Rect(shopTitle.gameObject, new Vector2(0.5f, 0.9f), new Vector2(650f, 54f));
        GameObject storeArt = Image("ShopIllustration", catalog.transform, Color.white, shopIcon);
        Rect(storeArt, new Vector2(0.5f, 0.57f), new Vector2(245f, 245f));
        storeArt.GetComponent<Image>().type = UnityEngine.UI.Image.Type.Simple;
        storeArt.GetComponent<Image>().preserveAspect = true;
        TMP_Text shopStatus = Text("ShopStatus", catalog.transform, "", font, 23f, Muted, TextAlignmentOptions.Center);
        Rect(shopStatus.gameObject, new Vector2(0.5f, 0.22f), new Vector2(650f, 120f));
        Set(so, "shopPanel", shopModal);
        Set(so, "closeShopButton", closeShop);
        Set(so, "shopCategoryTitle", shopTitle);
        Set(so, "shopStatusText", shopStatus);
        BuildInventoryPanel(root.transform, so, font, panel);
        BuildMonsterTeamPanels(root, font, panel);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BuildMonsterTeamPanels(GameObject root, TMP_FontAsset font, Sprite panel)
    {
        MonsterTeamUIController ui = root.AddComponent<MonsterTeamUIController>();
        ui.openMonsters = root.transform.Find("MainActionDock/MonstersButton").GetComponent<Button>();
        ui.openTeam = root.transform.Find("MainActionDock/TeamButton").GetComponent<Button>();
        ui.shadowFoxArt = LoadSprite("Assets/MonsterArena/Art/Monsters/ShadowFox/shadow_fox_cutout_v2.png", 1024);
        ui.unknownMonsterArt = LoadSprite(MonsterIconPath, 1024);
        Sprite frame = LoadSprite(MenuButtonPath, 1024);
        ui.monstersPanel = CreateModal(root.transform, "MonstersPanel", "MONSTERS • BỘ SƯU TẬP", font, panel, out ui.closeMonsters);
        Transform card = ui.monstersPanel.transform.Find("Card");
        Rect(card.gameObject, new Vector2(.5f, .5f), new Vector2(1420, 860));
        CollectionArt(card, "Icon", ui.unknownMonsterArt, new Vector2(.065f, .91f), new Vector2(84, 84));
        ui.collectionSummary = CollectionText(card, "CollectionSummary", "", font, 23, Cyan, new Vector2(.19f, .81f), new Vector2(420, 44));
        ui.monsterRows = new Button[5];
        ui.monsterRowLabels = new TMP_Text[5];
        ui.monsterRowArt = new Image[5];
        for (int i = 0; i < 5; i++)
        {
            Button row = CollectionButton(card, "MonsterRow" + i, "", font, new Vector2(.19f, .71f - i * .116f), new Vector2(430, 88));
            ui.monsterRows[i] = row;
            ui.monsterRowArt[i] = CollectionArt(row.transform, "Portrait", ui.shadowFoxArt, new Vector2(.12f, .5f), new Vector2(80, 80));
            TMP_Text label = row.GetComponentInChildren<TMP_Text>();
            Anchors(label.gameObject, new Vector2(.24f, .04f), new Vector2(.96f, .96f), Vector2.zero, Vector2.zero);
            label.fontSize = 23;
            label.alignment = TextAlignmentOptions.Left;
            label.overflowMode = TextOverflowModes.Truncate;
            ui.monsterRowLabels[i] = label;
        }
        ui.previousPage = CollectionButton(card, "PreviousPage", "<", font, new Vector2(.095f, .125f), new Vector2(72, 52));
        ui.nextPage = CollectionButton(card, "NextPage", ">", font, new Vector2(.285f, .125f), new Vector2(72, 52));
        ui.pageLabel = CollectionText(card, "Page", "1 / 1", font, 23, Muted, new Vector2(.19f, .125f), new Vector2(130, 48));
        GameObject well = Image("MonsterShowcase", card, Hex("0A0D1B", .88f), panel);
        Rect(well, new Vector2(.50f, .50f), new Vector2(400, 548));
        Outline(well, Hex("654D81", .55f), new Vector2(1, -1));
        ui.monsterArt = CollectionArt(well.transform, "MonsterArt", ui.shadowFoxArt, new Vector2(.5f, .52f), new Vector2(374, 475));
        CollectionText(well.transform, "Ownership", "QUÁI ĐÃ SỞ HỮU", font, 20, VioletBright, new Vector2(.5f, .055f), new Vector2(350, 36));
        ui.monsterName = CollectionText(card, "MonsterName", "", font, 30, White, new Vector2(.81f, .785f), new Vector2(450, 56));
        ui.monsterName.overflowMode = TextOverflowModes.Truncate;
        ui.monsterDetails = CollectionText(card, "MonsterDetails", "", font, 23, Muted, new Vector2(.815f, .54f), new Vector2(420, 340));
        ui.monsterDetails.alignment = TextAlignmentOptions.TopLeft;
        ui.addToTeam = CollectionButton(card, "AddToTeam", "THÊM VÀO TEAM", font, new Vector2(.81f, .27f), new Vector2(430, 80), frame);
        ui.selectCompanion = CollectionButton(card, "SelectCompanion", "HIỂN THỊ Ở HOME", font, new Vector2(.81f, .17f), new Vector2(430, 64));
        ui.viewTeam = CollectionButton(card, "ViewTeam", "XEM TEAM", font, new Vector2(.5f, .125f), new Vector2(320, 62));
        ui.monsterMessage = CollectionText(card, "Message", "", font, 22, Green, new Vector2(.5f, .048f), new Vector2(1280, 40));

        ui.teamPanel = CreateModal(root.transform, "TeamPanel", "TEAM • ĐỘI HÌNH", font, panel, out ui.closeTeam);
        card = ui.teamPanel.transform.Find("Card");
        Rect(card.gameObject, new Vector2(.5f, .5f), new Vector2(1420, 860));
        CollectionArt(card, "Icon", LoadSprite(TeamIconPath, 1024), new Vector2(.065f, .91f), new Vector2(84, 84));
        ui.teamSummary = CollectionText(card, "TeamSummary", "", font, 26, Cyan, new Vector2(.5f, .8f), new Vector2(1250, 50));
        ui.teamSlots = new Button[5];
        ui.teamSlotLabels = new TMP_Text[5];
        ui.teamSlotArt = new Image[5];
        for (int i = 0; i < 5; i++)
        {
            Button slot = CollectionButton(card, "TeamSlot" + i, "", font, new Vector2(.124f + i * .188f, .54f), new Vector2(244, 326));
            Outline(slot.gameObject, Hex("9B6ECB", .6f), new Vector2(2, -2));
            ui.teamSlots[i] = slot;
            ui.teamSlotArt[i] = CollectionArt(slot.transform, "Portrait", ui.unknownMonsterArt, new Vector2(.5f, .62f), new Vector2(224, 226));
            TMP_Text label = slot.GetComponentInChildren<TMP_Text>();
            Rect(label.gameObject, new Vector2(.5f, .14f), new Vector2(220, 72));
            label.fontSize = 22;
            label.overflowMode = TextOverflowModes.Truncate;
            ui.teamSlotLabels[i] = label;
        }
        ui.teamSelection = CollectionText(card, "TeamSelection", "", font, 24, White, new Vector2(.5f, .29f), new Vector2(1270, 48));
        ui.teamSelection.overflowMode = TextOverflowModes.Truncate;
        ui.moveLeft = CollectionButton(card, "MoveLeft", "< ĐỔI VỊ TRÍ", font, new Vector2(.135f, .20f), new Vector2(286, 66));
        ui.moveRight = CollectionButton(card, "MoveRight", "ĐỔI VỊ TRÍ >", font, new Vector2(.365f, .20f), new Vector2(286, 66));
        ui.removeMember = CollectionButton(card, "RemoveMember", "BỎ KHỎI ĐỘI", font, new Vector2(.595f, .20f), new Vector2(286, 66));
        ui.addMember = CollectionButton(card, "AddMember", "+ THÊM QUÁI", font, new Vector2(.83f, .20f), new Vector2(328, 80), frame);
        ui.teamMessage = CollectionText(card, "Message", "", font, 22, Green, new Vector2(.5f, .11f), new Vector2(1280, 42));
        CollectionText(card, "TeamRules", "Tối đa 5 quái • Không trùng quái • Chỉ số sức mạnh chưa được cấu hình", font, 20, Muted, new Vector2(.5f, .05f), new Vector2(1280, 34));
        EditorUtility.SetDirty(ui);
    }

    private static Image CollectionArt(Transform parent, string name, Sprite sprite, Vector2 anchor, Vector2 size)
    {
        GameObject go = Image(name, parent, Color.white, sprite);
        Rect(go, anchor, size);
        Image art = go.GetComponent<Image>();
        art.type = UnityEngine.UI.Image.Type.Simple;
        art.preserveAspect = true;
        return art;
    }

    private static TMP_Text CollectionText(Transform parent, string name, string value, TMP_FontAsset font, float size, Color color, Vector2 anchor, Vector2 dimensions)
    {
        TMP_Text label = Text(name, parent, value, font, size, color, TextAlignmentOptions.Center);
        Rect(label.gameObject, anchor, dimensions);
        return label;
    }

    private static Button CollectionButton(Transform parent, string name, string value, TMP_FontAsset font, Vector2 anchor, Vector2 size, Sprite frame = null)
    {
        Button button = ButtonUI(name, parent, value, font, frame == null ? Hex("37244F") : Color.white, VioletBright, 23, frame);
        Rect(button.gameObject, anchor, size);
        ColorBlock colors = button.colors;
        colors.disabledColor = Hex("A28AB8");
        button.colors = colors;
        return button;
    }

    private static void BuildInventoryPanel(Transform parent, SerializedObject controller, TMP_FontAsset font, Sprite panel)
    {
        GameObject modal = CreateModal(parent, "InventoryPanel", "INVENTORY • BALÔ", font, panel, out Button close);
        Transform card = modal.transform.Find("Card");
        Rect(card.gameObject, new Vector2(0.5f, 0.5f), new Vector2(1320f, 820f));
        GameObject titleIcon = Image("BackpackIcon", card, Color.white, LoadSprite(InventoryIconPath, 512));
        Rect(titleIcon, new Vector2(0.075f, 0.91f), new Vector2(78f, 78f));
        titleIcon.GetComponent<Image>().type = UnityEngine.UI.Image.Type.Simple;
        titleIcon.GetComponent<Image>().preserveAspect = true;
        string[] categories = { "TẤT CẢ", "TIÊU HAO", "NGUYÊN LIỆU", "ĐẶC BIỆT" };
        SerializedProperty tabs = controller.FindProperty("inventoryCategoryButtons");
        tabs.arraySize = categories.Length;
        for (int i = 0; i < categories.Length; i++)
        {
            Button tab = ButtonUI("InventoryCategory" + i, card, categories[i], font, GlassLight, White, 21f);
            Rect(tab.gameObject, new Vector2(0.118f + i * 0.145f, 0.785f), new Vector2(180f, 56f));
            ColorBlock colors = tab.colors;
            colors.disabledColor = Hex("C28BFF");
            tab.colors = colors;
            tabs.GetArrayElementAtIndex(i).objectReferenceValue = tab;
        }
        GameObject grid = Image("ItemGrid", card, Hex("0A0D1B"), panel);
        Rect(grid, new Vector2(0.34f, 0.425f), new Vector2(780f, 490f));
        TMP_Text categoryText = Text("InventoryCategoryTitle", grid.transform, "", font, 20f, Muted, TextAlignmentOptions.Left);
        Rect(categoryText.gameObject, new Vector2(0.5f, 0.925f), new Vector2(724f, 36f));
        for (int row = 0; row < 3; row++)
            for (int col = 0; col < 5; col++)
            {
                GameObject slot = Image("EmptySlot_" + row + "_" + col, grid.transform, Hex("191726"), panel);
                Rect(slot, new Vector2(0.12f + col * 0.19f, 0.73f - row * 0.24f), new Vector2(124f, 100f));
                Outline(slot, Hex("654D81", 0.45f), new Vector2(1f, -1f));
            }
        // This overlay is the actual empty state, not sample inventory contents.
        GameObject emptyOverlay = Image("EmptyState", grid.transform, Hex("080B17", 0.92f), panel);
        Rect(emptyOverlay, new Vector2(0.5f, 0.49f), new Vector2(600f, 134f));
        TMP_Text empty = Text("InventoryEmptyText", emptyOverlay.transform, "", font, 23f, Muted, TextAlignmentOptions.Center);
        Stretch(empty.rectTransform);
        GameObject detail = Image("ItemDetails", card, Hex("21152F"), panel);
        Rect(detail, new Vector2(0.81f, 0.465f), new Vector2(390f, 560f));
        Outline(detail, Hex("8D689E", 0.65f), new Vector2(1f, -1f));
        TMP_Text detailTitle = Text("DetailTitle", detail.transform, "CHI TIẾT VẬT PHẨM", font, 25f, White, TextAlignmentOptions.Center);
        Rect(detailTitle.gameObject, new Vector2(0.5f, 0.91f), new Vector2(350f, 44f));
        GameObject bag = Image("EmptyDetailIcon", detail.transform, new Color(1f, 1f, 1f, 0.65f), LoadSprite(InventoryIconPath, 512));
        Rect(bag, new Vector2(0.5f, 0.65f), new Vector2(180f, 180f));
        bag.GetComponent<Image>().type = UnityEngine.UI.Image.Type.Simple;
        bag.GetComponent<Image>().preserveAspect = true;
        TMP_Text hint = Text("SelectionHint", detail.transform, "Chọn vật phẩm trong balô\nđể xem thông tin và cách sử dụng.", font, 21f, Muted, TextAlignmentOptions.Center);
        Rect(hint.gameObject, new Vector2(0.5f, 0.36f), new Vector2(340f, 110f));
        Button use = ButtonUI("UseItemButton", detail.transform, "SỬ DỤNG", font, GlassLight, White, 22f);
        Rect(use.gameObject, new Vector2(0.5f, 0.12f), new Vector2(290f, 56f));
        use.interactable = false;
        Button shop = ButtonUI("OpenShopButton", card, "ĐẾN SHOP", font, Violet, White, 23f);
        Rect(shop.gameObject, new Vector2(0.34f, 0.075f), new Vector2(300f, 58f));
        Set(controller, "inventoryPanel", modal);
        Set(controller, "closeInventoryButton", close);
        Set(controller, "inventoryShopButton", shop);
        Set(controller, "inventoryCategoryText", categoryText);
        Set(controller, "inventoryEmptyText", empty);
    }

    private static GameObject CreateModal(Transform parent, string name, string title, TMP_FontAsset font, Sprite panel, out Button close)
    {
        GameObject modal = Image(name, parent, Hex("030510", 0.82f), null);
        Stretch(modal.GetComponent<RectTransform>());
        modal.GetComponent<Image>().raycastTarget = true;
        GameObject card = Image("Card", modal.transform, Hex("101121", 0.98f), panel);
        Rect(card, new Vector2(0.5f, 0.5f), new Vector2(900f, 660f));
        Outline(card, VioletBright, new Vector2(2f, -2f));
        TMP_Text heading = Text("Title", card.transform, title, font, 30f, White, TextAlignmentOptions.Center);
        Rect(heading.gameObject, new Vector2(0.48f, 0.91f), new Vector2(720f, 60f));
        close = ButtonUI("CloseButton", card.transform, "X", font, GlassLight, White, 26f);
        Rect(close.gameObject, new Vector2(0.94f, 0.92f), new Vector2(58f, 58f));
        modal.SetActive(false);
        return modal;
    }

    private static Button MainButton(string name, Transform parent, string title, string subtitle, Vector2 anchor, Sprite sprite, Sprite iconSprite, TMP_FontAsset font)
    {
        Button button = ButtonUI(name, parent, $"{title}\n<size=12><color=#D2C7DF>{subtitle}</color></size>", font, Color.white, Hex("DCC5FF"), 20f, sprite);
        Rect(button.gameObject, new Vector2(0.095f + (anchor.x - 0.105f) * 0.92f, anchor.y), new Vector2(310f, 120f));
        AddButtonIcon(button, iconSprite, 64f, 0.17f);
        TMP_Text label = button.GetComponentInChildren<TMP_Text>();
        Anchors(label.gameObject, new Vector2(0.29f, 0f), new Vector2(0.94f, 1f), Vector2.zero, Vector2.zero);
        label.alignment = TextAlignmentOptions.Center;
        return button;
    }

    private static void AddButtonIcon(Button button, Sprite iconSprite, float size, float horizontalAnchor)
    {
        if (iconSprite == null) return;
        GameObject icon = Image("ActionIcon", button.transform, Color.white, iconSprite);
        Rect(icon, new Vector2(horizontalAnchor, 0.5f), new Vector2(size, size));
        Image image = icon.GetComponent<Image>();
        image.type = UnityEngine.UI.Image.Type.Simple;
        image.preserveAspect = true;
    }

    private static Button SideButton(string name, Transform parent, string title, string subtitle, Vector2 anchor, TMP_FontAsset font, Sprite sprite)
    {
        Button button = ButtonUI(name, parent, $"{title}\n<size=12><color=#D2C7DF>{subtitle}</color></size>", font, Color.white, Hex("DCC5FF"), 18f, sprite);
        Rect(button.gameObject, new Vector2(anchor.x < 0.5f ? 0.12f : 0.88f, anchor.y == 0.43f ? 0.50f : 0.64f), new Vector2(340f, 110f));
        return button;
    }

    private static void CreateCurrency(Transform parent, string name, string label, string value, Color accent, Vector2 anchor, TMP_FontAsset font, Sprite panel)
    {
        string source = UiKitPath + (name == "Gold" ? "ui_currency_gold_v1.png" : "ui_currency_ruby_v1.png");
        string trimmed = source.Replace(".png", "_trimmed.png");
        EnsureTrimmedVariant(source, trimmed);
        GameObject card = Image(name, parent, Color.white, LoadSprite(trimmed, 1024));
        Rect(card, anchor, new Vector2(224f, 66f));
        card.GetComponent<Image>().type = UnityEngine.UI.Image.Type.Simple;
        card.GetComponent<Image>().preserveAspect = true;
        TMP_Text valueText = Text(name + "Value", card.transform, value, font, 25f, White, TextAlignmentOptions.Center);
        Rect(valueText.gameObject, new Vector2(0.55f, 0.5f), new Vector2(105f, 38f));
        valueText.fontStyle = FontStyles.Bold;
        valueText.enableAutoSizing = true;
        valueText.fontSizeMin = 16f;
        valueText.fontSizeMax = 25f;
        valueText.textWrappingMode = TextWrappingModes.NoWrap;
        GameObject plusDisc = Image(name + "PlusDisc", card.transform, accent,
            AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"));
        Rect(plusDisc, new Vector2(0.88f, 0.5f), new Vector2(34f, 34f));
        plusDisc.GetComponent<Image>().type = UnityEngine.UI.Image.Type.Simple;
        TMP_Text plus = Text(name + "Plus", plusDisc.transform, "+", font, 28f, Hex("412015"), TextAlignmentOptions.Center);
        Stretch(plus.rectTransform);
        plus.fontStyle = FontStyles.Bold;
    }

    private static void CreateStat(Transform parent, string name, string value, string label, Vector2 anchor, Color color, TMP_FontAsset font)
    {
        TMP_Text number = Text(name, parent, value, font, 28f, color, TextAlignmentOptions.Center);
        Rect(number.gameObject, anchor + new Vector2(0f, 0.06f), new Vector2(110f, 42f));
        number.fontStyle = FontStyles.Bold;
        TMP_Text caption = Text(name + "Label", parent, label, font, 11f, Muted, TextAlignmentOptions.Center);
        Rect(caption.gameObject, anchor - new Vector2(0f, 0.08f), new Vector2(120f, 22f));
    }

    private static void Assign(MainMenuUIController controller, TMP_Text displayName, TMP_Text username, TMP_Text monsterName, TMP_Text monsterStatus,
        TMP_Text owned, TMP_Text matches, TMP_Text winRate, Button monsters, Button team, Button inventory, Button profile,
        Button ranking, Button missions, Button rewards, Button guide, Button settings, Button battle, Button logout, TMP_Text message)
    {
        SerializedObject so = new(controller);
        Set(so, "displayNameText", displayName);
        Set(so, "usernameText", username);
        Set(so, "selectedMonsterNameText", monsterName);
        Set(so, "selectedMonsterStatusText", monsterStatus);
        Set(so, "ownedMonsterCountText", owned);
        Set(so, "totalMatchesText", matches);
        Set(so, "winRateText", winRate);
        Set(so, "monsterNavButton", monsters);
        Set(so, "teamButton", team);
        Set(so, "inventoryButton", inventory);
        Set(so, "profileButton", profile);
        Set(so, "rankingButton", ranking);
        Set(so, "missionsButton", missions);
        Set(so, "rewardsButton", rewards);
        Set(so, "shopButton", guide);
        Set(so, "settingsButton", settings);
        Set(so, "findMatchButton", battle);
        Set(so, "logoutButton", logout);
        Set(so, "actionMessageText", message);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static TMP_Text FindText(GameObject root, string name)
    {
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            if (text.name == name) return text;
        return null;
    }

    private static void Set(SerializedObject so, string name, Object value)
    {
        SerializedProperty property = so.FindProperty(name);
        if (property != null) property.objectReferenceValue = value;
    }

    private static GameObject UI(string name, Transform parent)
    {
        GameObject go = new(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        if (parent != null) go.transform.SetParent(parent, false);
        return go;
    }

    private static GameObject Image(string name, Transform parent, Color color, Sprite sprite)
    {
        GameObject go = UI(name, parent);
        Image image = go.AddComponent<Image>();
        image.color = color;
        image.sprite = sprite;
        image.type = sprite == null
            ? UnityEngine.UI.Image.Type.Simple
            : UnityEngine.UI.Image.Type.Sliced;
        image.raycastTarget = false;
        return go;
    }

    private static TMP_Text Text(string name, Transform parent, string value, TMP_FontAsset font, float size, Color color, TextAlignmentOptions alignment)
    {
        GameObject go = UI(name, parent);
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.font = font;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        return text;
    }

    private static Button ButtonUI(string name, Transform parent, string label, TMP_FontAsset font, Color normal, Color hover, float size, Sprite sprite = null)
    {
        bool preserveAspect = sprite != null;
        sprite ??= AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        GameObject go = Image(name, parent, normal, sprite);
        go.GetComponent<Image>().type = UnityEngine.UI.Image.Type.Simple;
        go.GetComponent<Image>().preserveAspect = preserveAspect;
        go.GetComponent<Image>().raycastTarget = true;
        Button button = go.AddComponent<Button>();
        button.targetGraphic = go.GetComponent<Image>();
        ColorBlock colors = button.colors;
        colors.normalColor = normal;
        colors.highlightedColor = hover;
        colors.selectedColor = hover;
        colors.pressedColor = Color.Lerp(normal, Color.black, 0.2f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        TMP_Text text = Text("Label", go.transform, label, font, size, White, TextAlignmentOptions.Center);
        Stretch(text.rectTransform);
        text.fontStyle = FontStyles.Bold;
        return button;
    }

    private static void Rect(GameObject go, Vector2 anchor, Vector2 size)
    {
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
    }

    private static void Anchors(GameObject go, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
    {
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void Outline(GameObject go, Color color, Vector2 distance)
    {
        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = distance;
    }

    private static void Shadow(GameObject go, Color color, Vector2 distance)
    {
        Shadow shadow = go.AddComponent<Shadow>();
        shadow.effectColor = color;
        shadow.effectDistance = distance;
    }

    private static Sprite LoadSprite(string path, int maxSize)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return null;
        if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single || importer.mipmapEnabled || importer.maxTextureSize != maxSize)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.sRGBTexture = true;
            importer.maxTextureSize = maxSize;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void EnsureTrimmedVariant(string sourcePath, string outputPath)
    {
        TextureImporter sourceImporter = AssetImporter.GetAtPath(sourcePath) as TextureImporter;
        if (sourceImporter == null)
        {
            Debug.LogError($"Không tìm thấy UI nguồn: {sourcePath}");
            return;
        }

        string sourceAbsolute = Path.GetFullPath(sourcePath);
        string outputAbsolute = Path.GetFullPath(outputPath);
        if (File.Exists(outputAbsolute) &&
            File.GetLastWriteTimeUtc(outputAbsolute) >= File.GetLastWriteTimeUtc(sourceAbsolute))
        {
            return;
        }

        bool restoreReadable = !sourceImporter.isReadable;
        if (restoreReadable)
        {
            sourceImporter.isReadable = true;
            sourceImporter.SaveAndReimport();
        }

        Texture2D source = AssetDatabase.LoadAssetAtPath<Texture2D>(sourcePath);
        Color32[] pixels = source.GetPixels32();
        int minX = source.width;
        int minY = source.height;
        int maxX = -1;
        int maxY = -1;

        for (int y = 0; y < source.height; y++)
        {
            for (int x = 0; x < source.width; x++)
            {
                if (pixels[y * source.width + x].a <= 8)
                {
                    continue;
                }

                minX = Mathf.Min(minX, x);
                minY = Mathf.Min(minY, y);
                maxX = Mathf.Max(maxX, x);
                maxY = Mathf.Max(maxY, y);
            }
        }

        if (maxX < minX || maxY < minY)
        {
            Debug.LogError($"UI không có pixel hiển thị: {sourcePath}");
            return;
        }

        const int padding = 8;
        minX = Mathf.Max(0, minX - padding);
        minY = Mathf.Max(0, minY - padding);
        maxX = Mathf.Min(source.width - 1, maxX + padding);
        maxY = Mathf.Min(source.height - 1, maxY + padding);
        int width = maxX - minX + 1;
        int height = maxY - minY + 1;

        Texture2D trimmed = new(width, height, TextureFormat.RGBA32, false);
        Color32[] trimmedPixels = new Color32[width * height];
        for (int y = 0; y < height; y++)
        {
            System.Array.Copy(
                pixels,
                (minY + y) * source.width + minX,
                trimmedPixels,
                y * width,
                width
            );
        }
        trimmed.SetPixels32(trimmedPixels);
        trimmed.Apply(false, false);
        File.WriteAllBytes(outputAbsolute, trimmed.EncodeToPNG());
        Object.DestroyImmediate(trimmed);

        if (restoreReadable)
        {
            sourceImporter.isReadable = false;
            sourceImporter.SaveAndReimport();
        }

        AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceSynchronousImport);
    }

    private static void EnsureShadowFoxCutout()
    {
        TextureImporter importer = AssetImporter.GetAtPath(MonsterPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"Không tìm thấy art quái: {MonsterPath}");
            return;
        }

        string sourceAbsolute = Path.GetFullPath(MonsterPath);
        string outputAbsolute = Path.GetFullPath(MonsterCutoutPath);
        if (File.Exists(outputAbsolute) &&
            File.GetLastWriteTimeUtc(outputAbsolute) >= File.GetLastWriteTimeUtc(sourceAbsolute))
        {
            return;
        }

        bool restoreReadable = !importer.isReadable;
        if (restoreReadable)
        {
            importer.isReadable = true;
            importer.SaveAndReimport();
        }

        Texture2D source = AssetDatabase.LoadAssetAtPath<Texture2D>(MonsterPath);
        Color[] pixels = source.GetPixels();
        Color topLeft = pixels[0];
        Color topRight = pixels[source.width - 1];
        Color bottomLeft = pixels[(source.height - 1) * source.width];
        Color bottomRight = pixels[pixels.Length - 1];
        Color[] output = new Color[pixels.Length];

        for (int y = 0; y < source.height; y++)
        {
            float v = y / (float)(source.height - 1);
            Color left = Color.Lerp(topLeft, bottomLeft, v);
            Color right = Color.Lerp(topRight, bottomRight, v);

            for (int x = 0; x < source.width; x++)
            {
                float u = x / (float)(source.width - 1);
                int index = y * source.width + x;
                Color pixel = pixels[index];
                Color expectedBackground = Color.Lerp(left, right, u);
                float difference = Vector3.Distance(
                    new Vector3(pixel.r, pixel.g, pixel.b),
                    new Vector3(expectedBackground.r, expectedBackground.g, expectedBackground.b)
                );
                Color.RGBToHSV(pixel, out _, out float saturation, out _);

                // Only remove low-saturation pixels close to the studio background.
                // Fur and purple magical details remain opaque.
                float alpha = saturation < 0.14f
                    ? Mathf.InverseLerp(0.075f, 0.145f, difference)
                    : 1f;
                pixel.a *= alpha;
                output[index] = pixel;
            }
        }

        Texture2D cutout = new(source.width, source.height, TextureFormat.RGBA32, false);
        cutout.SetPixels(output);
        cutout.Apply(false, false);
        File.WriteAllBytes(outputAbsolute, cutout.EncodeToPNG());
        Object.DestroyImmediate(cutout);

        if (restoreReadable)
        {
            importer.isReadable = false;
            importer.SaveAndReimport();
        }

        AssetDatabase.ImportAsset(MonsterCutoutPath, ImportAssetOptions.ForceSynchronousImport);
    }

    private static void CreateCamera()
    {
        GameObject go = new("Main Camera");
        go.tag = "MainCamera";
        go.transform.position = new Vector3(0f, 0f, -10f);
        Camera camera = go.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Ink;
        go.AddComponent<AudioListener>();
    }

    private static void CreateEventSystem()
    {
        GameObject go = new("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
    }

    private static void EnsureBuildSettings()
    {
        EditorBuildSettings.scenes = new List<EditorBuildSettingsScene>
        {
            new(LoginScenePath, true),
            new(ScenePath, true),
            new(ArenaScenePath, true)
        }.ToArray();
    }

    private static Color Hex(string hex, float alpha = 1f)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color color);
        color.a = alpha;
        return color;
    }
}
