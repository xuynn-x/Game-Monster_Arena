using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class BuildMainMenuUI
{
    private const string ScenePath =
        "Assets/MonsterArena/Scenes/MainMenu.unity";
    private const string LoginScenePath =
        "Assets/MonsterArena/Scenes/Auth/Login.unity";
    private const string ArenaScenePath =
        "Assets/MonsterArena/Scenes/Arena.unity";
    private const string MonsterImagePath =
        "Assets/MonsterArena/Art/Monsters/ShadowFox/shadow_fox_art_side.png";
    private const string AvatarImagePath =
        "Assets/MonsterArena/Art/UI/Avatars/avatar_pink_trainer_portrait.png";
    private const string ArenaBackgroundPath =
        "Assets/MonsterArena/Art/UI/Login/login_arena_background_v1.png";
    private const string AutoBuildKey =
        "MonsterArena.MainMenuUI.AutoBuild.20260916.v3";

    private static readonly Color Background = Hex("100D1E");
    private static readonly Color Sidebar = Hex("130D25", 0.92f);
    private static readonly Color Panel = Hex("211734", 0.94f);
    private static readonly Color PanelLight = Hex("302147", 0.96f);
    private static readonly Color Purple = Hex("A95CFF");
    private static readonly Color Pink = Hex("FF82B7");
    private static readonly Color Cyan = Hex("55DBF4");
    private static readonly Color White = Hex("FFF9FF");
    private static readonly Color Secondary = Hex("C8BBD5");
    private static readonly Color Muted = Hex("8D819B");
    private static readonly Color Success = Hex("6DF2B1");

    [InitializeOnLoadMethod]
    private static void ScheduleAutomaticBuild()
    {
        EditorApplication.delayCall += TryAutomaticBuild;
    }

    [MenuItem("Monster Arena/Build Main Menu UI")]
    public static void BuildFromMenu()
    {
        Scene scene = EditorSceneManager.GetActiveScene();

        if (scene.isDirty &&
            !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        Build();
    }

    private static void TryAutomaticBuild()
    {
        if (SessionState.GetBool(AutoBuildKey, false))
        {
            return;
        }

        // Existing scene references this portrait by GUID. It must be imported
        // as a Sprite, not Unity's default Texture type.
        if (LoadSprite(AvatarImagePath, 1024) == null)
        {
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
        {
            SessionState.SetBool(AutoBuildKey, true);
            return;
        }

        Scene scene = EditorSceneManager.GetActiveScene();

        if (scene.isDirty)
        {
            Debug.LogWarning(
                "MainMenu chưa tự tạo vì scene hiện tại có thay đổi chưa lưu. " +
                "Chọn Monster Arena > Build Main Menu UI."
            );
            return;
        }

        SessionState.SetBool(AutoBuildKey, true);
        Build();
    }

    private static void Build()
    {
        TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;
        TMP_FontAsset titleFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Bangers SDF.asset"
        );
        titleFont ??= defaultFont;

        Sprite panelSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(
            "UI/Skin/Background.psd"
        );
        Sprite circleSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(
            "UI/Skin/Knob.psd"
        );
        Sprite monsterSprite = LoadSprite(MonsterImagePath, 1024);
        Sprite avatarSprite = LoadSprite(AvatarImagePath, 1024);
        Sprite arenaBackground = LoadSprite(ArenaBackgroundPath, 2048);

        Scene scene;
        SceneAsset existingScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(
            ScenePath
        );

        if (existingScene == null)
        {
            scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single
            );
            EditorSceneManager.SaveScene(scene, ScenePath);
        }
        else
        {
            scene = EditorSceneManager.OpenScene(
                ScenePath,
                OpenSceneMode.Single
            );

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Object.DestroyImmediate(root);
            }
        }

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        Camera menuCamera = cameraObject.AddComponent<Camera>();
        menuCamera.clearFlags = CameraClearFlags.SolidColor;
        menuCamera.backgroundColor = Background;
        cameraObject.AddComponent<AudioListener>();

        GameObject canvasObject = CreateUIObject("MainMenuCanvas", null);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode =
            CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        MainMenuUIController controller =
            canvasObject.AddComponent<MainMenuUIController>();

        GameObject version = CreateUIObject(
            "MainMenuUI_VisualV2",
            canvasObject.transform
        );
        version.SetActive(false);

        GameObject background = CreateImage(
            "Background",
            canvasObject.transform,
            Color.white,
            arenaBackground,
            false
        );
        Stretch(background.GetComponent<RectTransform>());
        background.GetComponent<Image>().type = Image.Type.Simple;

        GameObject sceneVeil = CreateImage(
            "SceneVeil",
            background.transform,
            Hex("0D0B20", 0.47f),
            null,
            false
        );
        Stretch(sceneVeil.GetComponent<RectTransform>());

        GameObject topGlow = CreateImage(
            "TopGlow",
            background.transform,
            new Color(0.55f, 0.24f, 0.9f, 0.1f),
            circleSprite,
            false
        );
        SetRect(topGlow.GetComponent<RectTransform>(), new Vector2(0.43f, 1.02f), new Vector2(840f, 330f));

        GameObject sidebar = CreateImage(
            "Sidebar",
            canvasObject.transform,
            Sidebar,
            panelSprite,
            true
        );
        RectTransform sidebarRect = sidebar.GetComponent<RectTransform>();
        sidebarRect.anchorMin = Vector2.zero;
        sidebarRect.anchorMax = new Vector2(0.17f, 1f);
        sidebarRect.offsetMin = Vector2.zero;
        sidebarRect.offsetMax = Vector2.zero;
        AddShadow(sidebar, new Color(0f, 0f, 0f, 0.38f), new Vector2(8f, 0f));

        GameObject sidebarEdge = CreateImage(
            "SidebarNeonEdge",
            sidebar.transform,
            Hex("9D59F9", 0.68f),
            null,
            false
        );
        SetAnchoredRect(
            sidebarEdge.GetComponent<RectTransform>(),
            new Vector2(1f, 0.08f),
            new Vector2(1f, 0.92f),
            new Vector2(-3f, 0f),
            Vector2.zero
        );

        TMP_Text brandMonster = CreateText(
            "BrandMonster",
            sidebar.transform,
            "MONSTER",
            titleFont,
            39f,
            White,
            TextAlignmentOptions.Center
        );
        SetRect(brandMonster.rectTransform, new Vector2(0.5f, 0.91f), new Vector2(280f, 62f));
        brandMonster.enableVertexGradient = true;
        brandMonster.colorGradient = new VertexGradient(
            Hex("FFE5FF"), Hex("FFE5FF"), Purple, Pink
        );
        brandMonster.outlineColor = new Color32(55, 20, 80, 255);
        brandMonster.outlineWidth = 0.18f;

        TMP_Text brandArena = CreateText(
            "BrandArena",
            sidebar.transform,
            "A R E N A",
            defaultFont,
            19f,
            Cyan,
            TextAlignmentOptions.Center
        );
        SetRect(brandArena.rectTransform, new Vector2(0.5f, 0.865f), new Vector2(250f, 38f));
        brandArena.fontStyle = FontStyles.Bold;

        TMP_Text navigationLabel = CreateText(
            "NavigationLabel",
            sidebar.transform,
            "ĐIỀU HƯỚNG",
            defaultFont,
            12f,
            Muted,
            TextAlignmentOptions.Left
        );
        SetRect(navigationLabel.rectTransform, new Vector2(0.5f, 0.77f), new Vector2(245f, 28f));
        navigationLabel.fontStyle = FontStyles.Bold;
        navigationLabel.characterSpacing = 2f;

        Button homeButton = CreateButton(
            "HomeButton",
            sidebar.transform,
            "TRANG CHỦ",
            defaultFont,
            Purple,
            Hex("BC7AFF"),
            17f,
            TextAlignmentOptions.Left
        );
        SetRect(homeButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.69f), new Vector2(250f, 58f));

        Button monsterNavButton = CreateButton(
            "MonsterNavButton",
            sidebar.transform,
            "QUÁI CỦA TÔI",
            defaultFont,
            new Color(1f, 1f, 1f, 0.04f),
            new Color(1f, 1f, 1f, 0.09f),
            16f,
            TextAlignmentOptions.Left
        );
        SetRect(monsterNavButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.62f), new Vector2(250f, 58f));

        Button historyNavButton = CreateButton(
            "HistoryNavButton",
            sidebar.transform,
            "LỊCH SỬ TRẬN",
            defaultFont,
            new Color(1f, 1f, 1f, 0.04f),
            new Color(1f, 1f, 1f, 0.09f),
            16f,
            TextAlignmentOptions.Left
        );
        SetRect(historyNavButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.55f), new Vector2(250f, 58f));

        Button logoutButton = CreateButton(
            "LogoutButton",
            sidebar.transform,
            "ĐĂNG XUẤT",
            defaultFont,
            new Color(1f, 0.35f, 0.52f, 0.12f),
            new Color(1f, 0.35f, 0.52f, 0.24f),
            15f,
            TextAlignmentOptions.Center
        );
        SetRect(logoutButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.07f), new Vector2(250f, 54f));
        logoutButton.GetComponentInChildren<TMP_Text>().color = Pink;

        GameObject mainContent = CreateUIObject(
            "MainContent",
            canvasObject.transform
        );
        RectTransform contentRect = mainContent.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.17f, 0f);
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = new Vector2(54f, 44f);
        contentRect.offsetMax = new Vector2(-54f, -42f);

        TMP_Text pageTitle = CreateText(
            "PageTitle",
            mainContent.transform,
            "TRANG CHỦ",
            defaultFont,
            39f,
            White,
            TextAlignmentOptions.Left
        );
        SetRect(pageTitle.rectTransform, new Vector2(0.18f, 0.94f), new Vector2(500f, 52f));
        pageTitle.fontStyle = FontStyles.Bold;

        TMP_Text welcomeText = CreateText(
            "WelcomeText",
            mainContent.transform,
            "Chào mừng trở lại, Huấn luyện viên!",
            defaultFont,
            17f,
            Secondary,
            TextAlignmentOptions.Left
        );
        SetRect(welcomeText.rectTransform, new Vector2(0.18f, 0.895f), new Vector2(500f, 34f));

        GameObject profileChip = CreateImage(
            "ProfileChip",
            mainContent.transform,
            Panel,
            panelSprite,
            false
        );
        SetRect(profileChip.GetComponent<RectTransform>(), new Vector2(0.87f, 0.93f), new Vector2(330f, 88f));

        GameObject avatar = CreateImage(
            "AvatarImage",
            profileChip.transform,
            Color.white,
            avatarSprite,
            false
        );
        SetRect(avatar.GetComponent<RectTransform>(), new Vector2(0.14f, 0.5f), new Vector2(64f, 64f));
        avatar.GetComponent<Image>().type = Image.Type.Simple;
        avatar.GetComponent<Image>().preserveAspect = true;

        TMP_Text displayName = CreateText(
            "DisplayNameText",
            profileChip.transform,
            "Người chơi",
            defaultFont,
            19f,
            White,
            TextAlignmentOptions.Left
        );
        SetRect(displayName.rectTransform, new Vector2(0.65f, 0.62f), new Vector2(210f, 34f));
        displayName.fontStyle = FontStyles.Bold;

        TMP_Text username = CreateText(
            "UsernameText",
            profileChip.transform,
            "@username",
            defaultFont,
            13f,
            Muted,
            TextAlignmentOptions.Left
        );
        SetRect(username.rectTransform, new Vector2(0.65f, 0.3f), new Vector2(210f, 26f));

        GameObject monsterCard = CreateImage(
            "ActiveMonsterCard",
            mainContent.transform,
            Panel,
            panelSprite,
            false
        );
        SetAnchoredRect(monsterCard.GetComponent<RectTransform>(), new Vector2(0f, 0.15f), new Vector2(0.64f, 0.83f), new Vector2(0f, 0f), new Vector2(-18f, 0f));
        AddOutline(monsterCard, new Color(0.64f, 0.36f, 0.95f, 0.45f), new Vector2(1f, -1f));
        AddShadow(monsterCard, new Color(0f, 0f, 0f, 0.32f), new Vector2(0f, -9f));

        GameObject monsterAccent = CreateImage(
            "MonsterCardAccent",
            monsterCard.transform,
            Hex("C079FF", 0.85f),
            null,
            false
        );
        SetAnchoredRect(
            monsterAccent.GetComponent<RectTransform>(),
            new Vector2(0f, 0.16f),
            new Vector2(0f, 0.84f),
            Vector2.zero,
            new Vector2(4f, 0f)
        );

        TMP_Text activeLabel = CreateText(
            "ActiveMonsterLabel",
            monsterCard.transform,
            "QUÁI ĐANG ĐỒNG HÀNH",
            defaultFont,
            14f,
            Purple,
            TextAlignmentOptions.Left
        );
        SetRect(activeLabel.rectTransform, new Vector2(0.29f, 0.87f), new Vector2(430f, 30f));
        activeLabel.fontStyle = FontStyles.Bold;
        activeLabel.characterSpacing = 2f;

        TMP_Text monsterName = CreateText(
            "SelectedMonsterNameText",
            monsterCard.transform,
            "QUÁI TÍM KHỞI ĐẦU",
            defaultFont,
            40f,
            White,
            TextAlignmentOptions.Left
        );
        SetRect(monsterName.rectTransform, new Vector2(0.29f, 0.77f), new Vector2(430f, 72f));
        monsterName.fontStyle = FontStyles.Bold;
        monsterName.enableVertexGradient = true;
        monsterName.colorGradient = new VertexGradient(
            Hex("FFF0FF"), Hex("FFF0FF"), Purple, Pink
        );
        monsterName.outlineColor = new Color32(50, 17, 75, 255);
        monsterName.outlineWidth = 0.15f;

        TMP_Text monsterStatus = CreateText(
            "SelectedMonsterStatusText",
            monsterCard.transform,
            "HỆ BÓNG TỐI • Sẵn sàng chiến đấu",
            defaultFont,
            17f,
            Success,
            TextAlignmentOptions.Left
        );
        SetRect(monsterStatus.rectTransform, new Vector2(0.29f, 0.68f), new Vector2(430f, 34f));

        TMP_Text monsterDescription = CreateText(
            "MonsterDescription",
            monsterCard.transform,
            "Người bạn đầu tiên trong hành trình chinh phục đấu trường.\nNhanh nhẹn, bí ẩn và luôn sẵn sàng bảo vệ bạn.",
            defaultFont,
            17f,
            Secondary,
            TextAlignmentOptions.TopLeft
        );
        SetRect(monsterDescription.rectTransform, new Vector2(0.29f, 0.51f), new Vector2(430f, 94f));
        monsterDescription.lineSpacing = 5f;

        Button chooseMonsterButton = CreateButton(
            "ChooseMonsterButton",
            monsterCard.transform,
            "CHỌN QUÁI",
            defaultFont,
            new Color(0.66f, 0.36f, 1f, 0.18f),
            new Color(0.66f, 0.36f, 1f, 0.32f),
            17f,
            TextAlignmentOptions.Center
        );
        SetRect(chooseMonsterButton.GetComponent<RectTransform>(), new Vector2(0.29f, 0.25f), new Vector2(330f, 60f));
        chooseMonsterButton.GetComponentInChildren<TMP_Text>().color = Hex("DABEFF");

        GameObject monsterImage = CreateImage(
            "MonsterImage",
            monsterCard.transform,
            Color.white,
            monsterSprite,
            false
        );
        SetRect(monsterImage.GetComponent<RectTransform>(), new Vector2(0.79f, 0.48f), new Vector2(320f, 480f));
        monsterImage.GetComponent<Image>().type = Image.Type.Simple;
        monsterImage.GetComponent<Image>().preserveAspect = true;
        AddOutline(monsterImage, Hex("B87BFF", 0.8f), new Vector2(2f, -2f));
        AddShadow(monsterImage, new Color(0f, 0f, 0f, 0.55f), new Vector2(0f, -12f));

        GameObject rightColumn = CreateUIObject(
            "RightColumn",
            mainContent.transform
        );
        SetAnchoredRect(rightColumn.GetComponent<RectTransform>(), new Vector2(0.65f, 0.15f), new Vector2(1f, 0.83f), new Vector2(18f, 0f), Vector2.zero);

        GameObject statsCard = CreateImage(
            "StatsCard",
            rightColumn.transform,
            Panel,
            panelSprite,
            false
        );
        SetAnchoredRect(statsCard.GetComponent<RectTransform>(), new Vector2(0f, 0.61f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

        TMP_Text statsTitle = CreateText(
            "StatsTitle",
            statsCard.transform,
            "THÀNH TÍCH CỦA BẠN",
            defaultFont,
            15f,
            Pink,
            TextAlignmentOptions.Center
        );
        SetRect(statsTitle.rectTransform, new Vector2(0.5f, 0.84f), new Vector2(420f, 30f));
        statsTitle.fontStyle = FontStyles.Bold;
        statsTitle.characterSpacing = 2f;

        TMP_Text ownedValue = CreateStat(statsCard.transform, "OwnedMonsterCountText", "0", "QUÁI SỞ HỮU", defaultFont, new Vector2(0.18f, 0.45f), Purple);
        TMP_Text matchesValue = CreateStat(statsCard.transform, "TotalMatchesText", "0", "TRẬN ĐÃ ĐẤU", defaultFont, new Vector2(0.5f, 0.45f), Pink);
        TMP_Text winRateValue = CreateStat(statsCard.transform, "WinRateText", "0%", "TỶ LỆ THẮNG", defaultFont, new Vector2(0.82f, 0.45f), Cyan);

        GameObject quickCard = CreateImage(
            "QuickActionsCard",
            rightColumn.transform,
            PanelLight,
            panelSprite,
            false
        );
        SetAnchoredRect(quickCard.GetComponent<RectTransform>(), new Vector2(0f, 0.18f), new Vector2(1f, 0.56f), Vector2.zero, Vector2.zero);

        TMP_Text quickTitle = CreateText(
            "QuickTitle",
            quickCard.transform,
            "HOẠT ĐỘNG NHANH",
            defaultFont,
            15f,
            White,
            TextAlignmentOptions.Left
        );
        SetRect(quickTitle.rectTransform, new Vector2(0.5f, 0.8f), new Vector2(430f, 32f));
        quickTitle.fontStyle = FontStyles.Bold;

        Button historyButton = CreateButton(
            "HistoryButton",
            quickCard.transform,
            "LỊCH SỬ TRẬN",
            defaultFont,
            new Color(1f, 1f, 1f, 0.06f),
            new Color(1f, 1f, 1f, 0.12f),
            16f,
            TextAlignmentOptions.Center
        );
        SetRect(historyButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.51f), new Vector2(430f, 58f));

        TMP_Text actionMessage = CreateText(
            "ActionMessageText",
            quickCard.transform,
            "Chọn một hoạt động để bắt đầu.",
            defaultFont,
            14f,
            Secondary,
            TextAlignmentOptions.Center
        );
        SetRect(actionMessage.rectTransform, new Vector2(0.5f, 0.2f), new Vector2(430f, 52f));

        Button findMatchButton = CreateButton(
            "FindMatchButton",
            rightColumn.transform,
            "TÌM TRẬN  1V1",
            defaultFont,
            Purple,
            Hex("BD79FF"),
            22f,
            TextAlignmentOptions.Center
        );
        SetAnchoredRect(findMatchButton.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0.13f), Vector2.zero, Vector2.zero);
        AddShadow(findMatchButton.gameObject, new Color(0.5f, 0.2f, 0.9f, 0.42f), new Vector2(0f, -7f));
        AddOutline(findMatchButton.gameObject, Hex("D7A5FF", 0.72f), new Vector2(2f, -2f));

        Assign(
            controller,
            displayName,
            username,
            monsterName,
            monsterStatus,
            ownedValue,
            matchesValue,
            winRateValue,
            homeButton,
            monsterNavButton,
            historyNavButton,
            findMatchButton,
            chooseMonsterButton,
            historyButton,
            logoutButton,
            actionMessage
        );

        CreateEventSystem();
        EnsureBuildSettings();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = canvasObject;

        Debug.Log(
            "[Monster Arena] Đã tạo MainMenu UI và nối dữ liệu tài khoản."
        );
    }

    private static TMP_Text CreateStat(
        Transform parent,
        string name,
        string value,
        string label,
        TMP_FontAsset font,
        Vector2 anchor,
        Color accent)
    {
        TMP_Text valueText = CreateText(
            name,
            parent,
            value,
            font,
            34f,
            accent,
            TextAlignmentOptions.Center
        );
        SetRect(valueText.rectTransform, anchor + new Vector2(0f, 0.08f), new Vector2(150f, 52f));
        valueText.fontStyle = FontStyles.Bold;

        TMP_Text labelText = CreateText(
            $"{name}Label",
            parent,
            label,
            font,
            11f,
            Muted,
            TextAlignmentOptions.Center
        );
        SetRect(labelText.rectTransform, anchor - new Vector2(0f, 0.1f), new Vector2(150f, 28f));
        labelText.fontStyle = FontStyles.Bold;
        return valueText;
    }

    private static void Assign(
        MainMenuUIController controller,
        TMP_Text displayName,
        TMP_Text username,
        TMP_Text monsterName,
        TMP_Text monsterStatus,
        TMP_Text ownedCount,
        TMP_Text totalMatches,
        TMP_Text winRate,
        Button home,
        Button monsterNav,
        Button historyNav,
        Button findMatch,
        Button chooseMonster,
        Button history,
        Button logout,
        TMP_Text actionMessage)
    {
        SerializedObject serialized = new(controller);
        Set(serialized, "displayNameText", displayName);
        Set(serialized, "usernameText", username);
        Set(serialized, "selectedMonsterNameText", monsterName);
        Set(serialized, "selectedMonsterStatusText", monsterStatus);
        Set(serialized, "ownedMonsterCountText", ownedCount);
        Set(serialized, "totalMatchesText", totalMatches);
        Set(serialized, "winRateText", winRate);
        Set(serialized, "homeButton", home);
        Set(serialized, "monsterNavButton", monsterNav);
        Set(serialized, "historyNavButton", historyNav);
        Set(serialized, "findMatchButton", findMatch);
        Set(serialized, "chooseMonsterButton", chooseMonster);
        Set(serialized, "historyButton", history);
        Set(serialized, "logoutButton", logout);
        Set(serialized, "actionMessageText", actionMessage);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);
    }

    private static void Set(
        SerializedObject serialized,
        string propertyName,
        Object value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);

        if (property == null)
        {
            Debug.LogError($"Không tìm thấy field {propertyName}.");
            return;
        }

        property.objectReferenceValue = value;
    }

    private static Sprite LoadSprite(string path, int maxSize)
    {
        TextureImporter importer =
            AssetImporter.GetAtPath(path) as TextureImporter;

        if (importer == null)
        {
            Debug.LogError($"Không tìm thấy ảnh: {path}");
            return null;
        }

        bool changed =
            importer.textureType != TextureImporterType.Sprite ||
            importer.spriteImportMode != SpriteImportMode.Single ||
            importer.mipmapEnabled ||
            importer.maxTextureSize != maxSize;

        if (changed)
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

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject gameObject = new(name, typeof(RectTransform));
        gameObject.layer = LayerMask.NameToLayer("UI");

        if (parent != null)
        {
            gameObject.transform.SetParent(parent, false);
        }

        return gameObject;
    }

    private static GameObject CreateImage(
        string name,
        Transform parent,
        Color color,
        Sprite sprite,
        bool raycastTarget)
    {
        GameObject gameObject = CreateUIObject(name, parent);
        Image image = gameObject.AddComponent<Image>();
        image.color = color;
        image.sprite = sprite;
        image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.raycastTarget = raycastTarget;
        return gameObject;
    }

    private static TMP_Text CreateText(
        string name,
        Transform parent,
        string value,
        TMP_FontAsset font,
        float size,
        Color color,
        TextAlignmentOptions alignment)
    {
        GameObject gameObject = CreateUIObject(name, parent);
        TextMeshProUGUI text = gameObject.AddComponent<TextMeshProUGUI>();
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

    private static Button CreateButton(
        string name,
        Transform parent,
        string label,
        TMP_FontAsset font,
        Color normal,
        Color highlighted,
        float fontSize,
        TextAlignmentOptions alignment)
    {
        GameObject gameObject = CreateImage(
            name,
            parent,
            normal,
            AssetDatabase.GetBuiltinExtraResource<Sprite>(
                "UI/Skin/UISprite.psd"
            ),
            true
        );
        Button button = gameObject.AddComponent<Button>();
        button.targetGraphic = gameObject.GetComponent<Image>();

        ColorBlock colors = button.colors;
        colors.normalColor = normal;
        colors.highlightedColor = highlighted;
        colors.pressedColor = Color.Lerp(normal, Color.black, 0.16f);
        colors.selectedColor = highlighted;
        colors.disabledColor = new Color(0.25f, 0.22f, 0.3f, 0.65f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        TMP_Text text = CreateText(
            "Label",
            gameObject.transform,
            label,
            font,
            fontSize,
            White,
            alignment
        );
        Stretch(text.rectTransform);
        text.fontStyle = FontStyles.Bold;
        text.margin = alignment == TextAlignmentOptions.Left
            ? new Vector4(22f, 0f, 10f, 0f)
            : Vector4.zero;
        return button;
    }

    private static void CreateEventSystem()
    {
        GameObject eventSystem = new("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<InputSystemUIInputModule>();
    }

    private static void EnsureBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new()
        {
            new EditorBuildSettingsScene(LoginScenePath, true),
            new EditorBuildSettingsScene(ScenePath, true),
            new EditorBuildSettingsScene(ArenaScenePath, true)
        };
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void AddOutline(GameObject target, Color color, Vector2 distance)
    {
        Outline outline = target.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = distance;
        outline.useGraphicAlpha = true;
    }

    private static void AddShadow(GameObject target, Color color, Vector2 distance)
    {
        Shadow shadow = target.AddComponent<Shadow>();
        shadow.effectColor = color;
        shadow.effectDistance = distance;
        shadow.useGraphicAlpha = true;
    }

    private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
    }

    private static void SetAnchoredRect(
        RectTransform rect,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
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

    private static Color Hex(string hex, float alpha = 1f)
    {
        ColorUtility.TryParseHtmlString($"#{hex}", out Color color);
        color.a = alpha;
        return color;
    }
}
