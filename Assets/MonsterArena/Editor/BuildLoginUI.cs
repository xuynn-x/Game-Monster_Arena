using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class BuildLoginUI
{
    private const string ScenePath =
        "Assets/MonsterArena/Scenes/Auth/Login.unity";
    private const string BackgroundPath =
        "Assets/MonsterArena/Art/UI/Login/login_arena_background_v1.png";
    private const string AutoBuildKey =
        "MonsterArena.LoginUI.AutoBuild.20260916.v5";

    private static readonly Color BackgroundColor = Hex("071224");
    private static readonly Color CardColor = Hex("111B31", 0.98f);
    private static readonly Color CardBorderColor = Hex("243654");
    private static readonly Color InputColor = Hex("0B1427");
    private static readonly Color CyanColor = Hex("14B8E6");
    private static readonly Color CyanHoverColor = Hex("39C9F1");
    private static readonly Color RedColor = Hex("FF6976");
    private static readonly Color PrimaryTextColor = Hex("F3F7FF");
    private static readonly Color SecondaryTextColor = Hex("AAB8D2");
    private static readonly Color MutedTextColor = Hex("71809D");

    [InitializeOnLoadMethod]
    private static void ScheduleAutomaticBuild()
    {
        EditorApplication.delayCall += TryAutomaticBuild;
    }

    [MenuItem("Monster Arena/Build Login UI")]
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

        if (GameObject.Find("LoginUI_VisualV5") != null)
        {
            SessionState.SetBool(AutoBuildKey, true);
            return;
        }

        Scene currentScene = EditorSceneManager.GetActiveScene();

        if (currentScene.path != ScenePath && currentScene.isDirty)
        {
            Debug.LogWarning(
                "Login UI chưa tự tạo vì scene hiện tại có thay đổi chưa lưu. " +
                "Chọn Monster Arena > Build Login UI để tạo thủ công."
            );
            return;
        }

        SessionState.SetBool(AutoBuildKey, true);
        Build();
    }

    private static void Build()
    {
        SceneAsset loginScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(
            ScenePath
        );

        if (loginScene == null)
        {
            Debug.LogError($"Không tìm thấy scene: {ScenePath}");
            return;
        }

        Scene scene = EditorSceneManager.GetActiveScene();

        if (scene.path != ScenePath)
        {
            scene = EditorSceneManager.OpenScene(
                ScenePath,
                OpenSceneMode.Single
            );
        }

        DestroyExisting("AuthCanvas");
        DestroyExisting("EventSystem");
        DestroyExisting("LoginUIController");

        TMP_FontAsset font = TMP_Settings.defaultFontAsset;
        Sprite panelSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(
            "UI/Skin/Background.psd"
        );
        Sprite inputSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(
            "UI/Skin/InputFieldBackground.psd"
        );
        Sprite circleSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(
            "UI/Skin/Knob.psd"
        );
        Sprite backgroundSprite = LoadBackgroundSprite();

        GameObject canvasObject = CreateUIObject("AuthCanvas", null);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = false;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode =
            CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        LoginUIController controller =
            canvasObject.AddComponent<LoginUIController>();

        GameObject visualVersion = CreateUIObject(
            "LoginUI_VisualV5",
            canvasObject.transform
        );
        visualVersion.SetActive(false);

        GameObject background = CreateImage(
            "Background",
            canvasObject.transform,
            Color.white,
            backgroundSprite,
            false
        );
        Stretch(background.GetComponent<RectTransform>());
        Image backgroundImage = background.GetComponent<Image>();
        backgroundImage.type = Image.Type.Simple;
        backgroundImage.preserveAspect = false;

        GameObject backgroundShade = CreateImage(
            "BackgroundShade",
            background.transform,
            new Color(0.015f, 0.035f, 0.075f, 0.38f),
            null,
            false
        );
        Stretch(backgroundShade.GetComponent<RectTransform>());

        GameObject rightShade = CreateImage(
            "RightSideShade",
            background.transform,
            new Color(0.01f, 0.025f, 0.06f, 0.54f),
            null,
            false
        );
        SetRect(
            rightShade.GetComponent<RectTransform>(),
            new Vector2(0.56f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero
        );

        GameObject topLine = CreateImage(
            "TopAccentLine",
            background.transform,
            CyanColor,
            null,
            false
        );
        SetRect(
            topLine.GetComponent<RectTransform>(),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -3f),
            new Vector2(0f, 6f)
        );

        GameObject safeArea = CreateUIObject(
            "SafeArea",
            canvasObject.transform
        );
        RectTransform safeRect = safeArea.GetComponent<RectTransform>();
        safeRect.anchorMin = Vector2.zero;
        safeRect.anchorMax = Vector2.one;
        safeRect.offsetMin = new Vector2(72f, 52f);
        safeRect.offsetMax = new Vector2(-72f, -52f);

        BuildHeroPanel(safeArea.transform, font);

        GameObject authCard = CreateImage(
            "AuthCard",
            safeArea.transform,
            CardColor,
            panelSprite,
            true
        );
        RectTransform authCardRect = authCard.GetComponent<RectTransform>();
        authCardRect.anchorMin = new Vector2(0.61f, 0.055f);
        authCardRect.anchorMax = new Vector2(0.965f, 0.945f);
        authCardRect.offsetMin = Vector2.zero;
        authCardRect.offsetMax = Vector2.zero;
        AddOutline(authCard, CardBorderColor, new Vector2(1.5f, -1.5f));
        AddShadow(authCard, new Color(0f, 0f, 0f, 0.42f), new Vector2(0f, -14f));

        TMP_Text welcome = CreateText(
            "WelcomeText",
            authCard.transform,
            "CHÀO MỪNG TRỞ LẠI",
            font,
            16f,
            FontStyles.Bold,
            CyanColor,
            TextAlignmentOptions.Center
        );
        SetRect(
            welcome.rectTransform,
            new Vector2(0.08f, 1f),
            new Vector2(0.92f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -46f),
            new Vector2(0f, 28f)
        );
        welcome.characterSpacing = 3f;

        TMP_Text title = CreateText(
            "AuthTitle",
            authCard.transform,
            "Tài khoản Monster Arena",
            font,
            30f,
            FontStyles.Bold,
            PrimaryTextColor,
            TextAlignmentOptions.Center
        );
        SetRect(
            title.rectTransform,
            new Vector2(0.08f, 1f),
            new Vector2(0.92f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -87f),
            new Vector2(0f, 42f)
        );

        TMP_Text subtitle = CreateText(
            "AuthSubtitle",
            authCard.transform,
            "Đăng nhập để tiếp tục hành trình của bạn",
            font,
            16f,
            FontStyles.Normal,
            SecondaryTextColor,
            TextAlignmentOptions.Center
        );
        SetRect(
            subtitle.rectTransform,
            new Vector2(0.08f, 1f),
            new Vector2(0.92f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -125f),
            new Vector2(0f, 28f)
        );

        GameObject tabs = CreateUIObject("AuthTabs", authCard.transform);
        SetRect(
            tabs.GetComponent<RectTransform>(),
            new Vector2(0.08f, 1f),
            new Vector2(0.92f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -186f),
            new Vector2(0f, 54f)
        );

        Button loginTab = CreateButton(
            "LoginTabButton",
            tabs.transform,
            "ĐĂNG NHẬP",
            font,
            new Color(0.08f, 0.69f, 0.92f, 1f),
            CyanHoverColor,
            16f
        );
        SetRect(
            loginTab.GetComponent<RectTransform>(),
            new Vector2(0f, 0f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 0.5f),
            new Vector2(-4f, 0f),
            new Vector2(-8f, 0f)
        );

        Button registerTab = CreateButton(
            "RegisterTabButton",
            tabs.transform,
            "ĐĂNG KÝ",
            font,
            new Color(0.12f, 0.17f, 0.29f, 1f),
            Hex("263650"),
            16f
        );
        SetRect(
            registerTab.GetComponent<RectTransform>(),
            new Vector2(0.5f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 0.5f),
            new Vector2(4f, 0f),
            new Vector2(-8f, 0f)
        );

        GameObject loginPanel = CreateUIObject(
            "LoginPanel",
            authCard.transform
        );
        SetRect(
            loginPanel.GetComponent<RectTransform>(),
            new Vector2(0.08f, 0.13f),
            new Vector2(0.92f, 0.73f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero
        );

        TMP_InputField loginUsername = CreateLabeledInput(
            loginPanel.transform,
            "LoginUsername",
            "TÊN ĐĂNG NHẬP",
            "Nhập tên đăng nhập",
            font,
            inputSprite,
            new Vector2(0f, 174f),
            false,
            out _
        );

        TMP_InputField loginPassword = CreateLabeledInput(
            loginPanel.transform,
            "LoginPassword",
            "MẬT KHẨU",
            "Nhập mật khẩu",
            font,
            inputSprite,
            new Vector2(0f, 60f),
            true,
            out Button showLoginPassword
        );

        TMP_Text showLoginPasswordLabel =
            showLoginPassword.GetComponentInChildren<TMP_Text>();

        TMP_Text loginMessage = CreateText(
            "LoginMessageText",
            loginPanel.transform,
            string.Empty,
            font,
            15f,
            FontStyles.Normal,
            RedColor,
            TextAlignmentOptions.Center
        );
        SetRectCentered(loginMessage.rectTransform, new Vector2(0f, -24f), new Vector2(540f, 42f));

        Button loginButton = CreateButton(
            "LoginButton",
            loginPanel.transform,
            "VÀO ĐẤU TRƯỜNG",
            font,
            CyanColor,
            CyanHoverColor,
            19f
        );
        SetRectCentered(loginButton.GetComponent<RectTransform>(), new Vector2(0f, -83f), new Vector2(540f, 62f));
        AddShadow(loginButton.gameObject, new Color(0f, 0.55f, 0.8f, 0.25f), new Vector2(0f, -5f));

        Button goToRegister = CreateTextButton(
            "GoToRegisterButton",
            loginPanel.transform,
            "Chưa có tài khoản?  <b>Đăng ký ngay</b>",
            font,
            16f
        );
        SetRectCentered(goToRegister.GetComponent<RectTransform>(), new Vector2(0f, -145f), new Vector2(540f, 42f));

        GameObject registerPanel = CreateUIObject(
            "RegisterPanel",
            authCard.transform
        );
        SetRect(
            registerPanel.GetComponent<RectTransform>(),
            new Vector2(0.08f, 0.1f),
            new Vector2(0.92f, 0.73f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero
        );

        TMP_InputField registerUsername = CreateLabeledInput(
            registerPanel.transform,
            "RegisterUsername",
            "TÊN ĐĂNG NHẬP",
            "3–20 ký tự: chữ, số, . hoặc _",
            font,
            inputSprite,
            new Vector2(0f, 220f),
            false,
            out _
        );

        TMP_InputField displayName = CreateLabeledInput(
            registerPanel.transform,
            "DisplayName",
            "TÊN HIỂN THỊ",
            "Tên xuất hiện trong game",
            font,
            inputSprite,
            new Vector2(0f, 116f),
            false,
            out _
        );

        TMP_InputField registerPassword = CreateLabeledInput(
            registerPanel.transform,
            "RegisterPassword",
            "MẬT KHẨU",
            "Tối thiểu 6 ký tự",
            font,
            inputSprite,
            new Vector2(0f, 12f),
            true,
            out Button showRegisterPassword
        );

        TMP_InputField confirmPassword = CreateLabeledInput(
            registerPanel.transform,
            "ConfirmPassword",
            "NHẬP LẠI MẬT KHẨU",
            "Nhập lại mật khẩu",
            font,
            inputSprite,
            new Vector2(0f, -92f),
            true,
            out Button showConfirmPassword
        );

        TMP_Text registerMessage = CreateText(
            "RegisterMessageText",
            registerPanel.transform,
            string.Empty,
            font,
            15f,
            FontStyles.Normal,
            RedColor,
            TextAlignmentOptions.Center
        );
        SetRectCentered(registerMessage.rectTransform, new Vector2(0f, -164f), new Vector2(540f, 36f));

        Button registerButton = CreateButton(
            "RegisterButton",
            registerPanel.transform,
            "TẠO TÀI KHOẢN",
            font,
            CyanColor,
            CyanHoverColor,
            19f
        );
        SetRectCentered(registerButton.GetComponent<RectTransform>(), new Vector2(0f, -215f), new Vector2(540f, 58f));

        Button goToLogin = CreateTextButton(
            "GoToLoginButton",
            registerPanel.transform,
            "Đã có tài khoản?  <b>Đăng nhập</b>",
            font,
            16f
        );
        SetRectCentered(goToLogin.GetComponent<RectTransform>(), new Vector2(0f, -264f), new Vector2(540f, 38f));

        TMP_Text localNotice = CreateText(
            "LocalDataNotice",
            authCard.transform,
            "BẢN THỬ NGHIỆM  •  DỮ LIỆU ĐƯỢC LƯU TRÊN MÁY NÀY",
            font,
            12f,
            FontStyles.Bold,
            MutedTextColor,
            TextAlignmentOptions.Center
        );
        SetRect(
            localNotice.rectTransform,
            new Vector2(0.08f, 0f),
            new Vector2(0.92f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 38f),
            new Vector2(0f, 24f)
        );
        localNotice.characterSpacing = 1.2f;

        registerPanel.SetActive(false);

        AssignControllerReferences(
            controller,
            loginPanel,
            registerPanel,
            loginTab,
            registerTab,
            loginUsername,
            loginPassword,
            showLoginPassword,
            showLoginPasswordLabel,
            loginButton,
            goToRegister,
            loginMessage,
            registerUsername,
            displayName,
            registerPassword,
            confirmPassword,
            showRegisterPassword,
            showRegisterPassword.GetComponentInChildren<TMP_Text>(),
            showConfirmPassword,
            showConfirmPassword.GetComponentInChildren<TMP_Text>(),
            registerButton,
            goToLogin,
            registerMessage
        );

        CreateEventSystem();
        EnsureBuildSettings();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = canvasObject;

        Debug.Log(
            "[Monster Arena] Đã tạo hoàn chỉnh Login UI và nối các nút."
        );
    }

    private static void BuildHeroPanel(
        Transform parent,
        TMP_FontAsset fallbackFont)
    {
        GameObject hero = CreateUIObject("HeroPanel", parent);
        RectTransform heroRect = hero.GetComponent<RectTransform>();
        heroRect.anchorMin = new Vector2(0.025f, 0.07f);
        heroRect.anchorMax = new Vector2(0.57f, 0.93f);
        heroRect.offsetMin = Vector2.zero;
        heroRect.offsetMax = Vector2.zero;

        TMP_FontAsset monsterFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Bangers SDF.asset"
        );
        TMP_FontAsset arenaFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Anton SDF.asset"
        );

        monsterFont ??= fallbackFont;
        arenaFont ??= fallbackFont;

        GameObject logoRoot = CreateUIObject("GameTitleLogo", hero.transform);
        SetRectCentered(
            logoRoot.GetComponent<RectTransform>(),
            new Vector2(-20f, 205f),
            new Vector2(820f, 250f)
        );

        TMP_Text monsterTitle = CreateText(
            "MonsterTitle",
            logoRoot.transform,
            "MONSTER",
            monsterFont,
            104f,
            FontStyles.Normal,
            Color.white,
            TextAlignmentOptions.Center
        );
        SetRectCentered(
            monsterTitle.rectTransform,
            new Vector2(0f, 48f),
            new Vector2(810f, 125f)
        );
        monsterTitle.characterSpacing = 2.5f;
        monsterTitle.enableVertexGradient = true;
        monsterTitle.colorGradient = new VertexGradient(
            Hex("FFE8FF"),
            Hex("FFD6FF"),
            Hex("A95CFF"),
            Hex("7B3DDB")
        );
        monsterTitle.outlineColor = new Color32(43, 16, 78, 255);
        monsterTitle.outlineWidth = 0.24f;
        AddShadow(
            monsterTitle.gameObject,
            new Color(0.2f, 0.02f, 0.42f, 0.95f),
            new Vector2(7f, -8f)
        );

        TMP_Text arenaTitle = CreateText(
            "ArenaTitle",
            logoRoot.transform,
            "A R E N A",
            arenaFont,
            64f,
            FontStyles.Normal,
            Color.white,
            TextAlignmentOptions.Center
        );
        SetRectCentered(
            arenaTitle.rectTransform,
            new Vector2(0f, -55f),
            new Vector2(700f, 86f)
        );
        arenaTitle.characterSpacing = 3f;
        arenaTitle.enableVertexGradient = true;
        arenaTitle.colorGradient = new VertexGradient(
            Hex("BFF8FF"),
            Hex("BFF8FF"),
            Hex("1EC6F2"),
            Hex("1EC6F2")
        );
        arenaTitle.outlineColor = new Color32(8, 43, 71, 255);
        arenaTitle.outlineWidth = 0.18f;
        AddShadow(
            arenaTitle.gameObject,
            new Color(0f, 0.08f, 0.16f, 0.92f),
            new Vector2(5f, -6f)
        );

        GameObject accentLine = CreateImage(
            "TitleAccentLine",
            logoRoot.transform,
            new Color(0.42f, 0.9f, 1f, 0.9f),
            null,
            false
        );
        SetRectCentered(
            accentLine.GetComponent<RectTransform>(),
            new Vector2(0f, -108f),
            new Vector2(340f, 3f)
        );
    }

    private static TMP_InputField CreateLabeledInput(
        Transform parent,
        string name,
        string label,
        string placeholder,
        TMP_FontAsset font,
        Sprite inputSprite,
        Vector2 position,
        bool isPassword,
        out Button visibilityButton)
    {
        TMP_Text labelText = CreateText(
            $"{name}Label",
            parent,
            label,
            font,
            13f,
            FontStyles.Bold,
            SecondaryTextColor,
            TextAlignmentOptions.Left
        );
        SetRectCentered(labelText.rectTransform, position + new Vector2(0f, 43f), new Vector2(540f, 26f));
        labelText.characterSpacing = 1.4f;

        GameObject inputObject = CreateImage(
            $"{name}Input",
            parent,
            InputColor,
            inputSprite,
            true
        );
        SetRectCentered(inputObject.GetComponent<RectTransform>(), position, new Vector2(540f, 58f));
        AddOutline(inputObject, CardBorderColor, new Vector2(1f, -1f));

        TMP_InputField input = inputObject.AddComponent<TMP_InputField>();
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.contentType = isPassword
            ? TMP_InputField.ContentType.Password
            : TMP_InputField.ContentType.Standard;
        input.characterLimit = isPassword ? 64 : 24;
        input.caretColor = CyanColor;
        input.selectionColor = new Color(0.08f, 0.69f, 0.92f, 0.35f);

        GameObject viewport = CreateUIObject("Text Area", inputObject.transform);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(18f, 7f);
        viewportRect.offsetMax = new Vector2(isPassword ? -90f : -18f, -7f);
        viewport.AddComponent<RectMask2D>();

        TMP_Text placeholderText = CreateText(
            "Placeholder",
            viewport.transform,
            placeholder,
            font,
            17f,
            FontStyles.Italic,
            MutedTextColor,
            TextAlignmentOptions.MidlineLeft
        );
        Stretch(placeholderText.rectTransform);

        TMP_Text inputText = CreateText(
            "Text",
            viewport.transform,
            string.Empty,
            font,
            18f,
            FontStyles.Normal,
            PrimaryTextColor,
            TextAlignmentOptions.MidlineLeft
        );
        Stretch(inputText.rectTransform);

        input.textViewport = viewportRect;
        input.textComponent = inputText;
        input.placeholder = placeholderText;

        if (isPassword)
        {
            visibilityButton = CreateTextButton(
                $"Show{name}Button",
                inputObject.transform,
                "HIỆN",
                font,
                12f
            );
            RectTransform buttonRect =
                visibilityButton.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(1f, 0f);
            buttonRect.anchorMax = new Vector2(1f, 1f);
            buttonRect.pivot = new Vector2(1f, 0.5f);
            buttonRect.anchoredPosition = new Vector2(-8f, 0f);
            buttonRect.sizeDelta = new Vector2(76f, -10f);
        }
        else
        {
            visibilityButton = null;
        }

        return input;
    }

    private static Button CreateButton(
        string name,
        Transform parent,
        string label,
        TMP_FontAsset font,
        Color normalColor,
        Color highlightedColor,
        float fontSize)
    {
        GameObject buttonObject = CreateImage(
            name,
            parent,
            normalColor,
            AssetDatabase.GetBuiltinExtraResource<Sprite>(
                "UI/Skin/UISprite.psd"
            ),
            true
        );
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonObject.GetComponent<Image>();

        ColorBlock colors = button.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = highlightedColor;
        colors.pressedColor = Color.Lerp(normalColor, Color.black, 0.18f);
        colors.selectedColor = highlightedColor;
        colors.disabledColor = new Color(0.2f, 0.24f, 0.31f, 0.7f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        TMP_Text text = CreateText(
            "Label",
            buttonObject.transform,
            label,
            font,
            fontSize,
            FontStyles.Bold,
            PrimaryTextColor,
            TextAlignmentOptions.Center
        );
        Stretch(text.rectTransform);
        text.raycastTarget = false;
        text.characterSpacing = 1.1f;

        return button;
    }

    private static Button CreateTextButton(
        string name,
        Transform parent,
        string label,
        TMP_FontAsset font,
        float fontSize)
    {
        GameObject buttonObject = CreateUIObject(name, parent);
        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0f);
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = CyanHoverColor;
        colors.pressedColor = CyanColor;
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(1f, 1f, 1f, 0.35f);
        button.colors = colors;

        TMP_Text text = CreateText(
            "Label",
            buttonObject.transform,
            label,
            font,
            fontSize,
            FontStyles.Normal,
            SecondaryTextColor,
            TextAlignmentOptions.Center
        );
        Stretch(text.rectTransform);
        text.raycastTarget = false;

        return button;
    }

    private static TMP_Text CreateText(
        string name,
        Transform parent,
        string content,
        TMP_FontAsset font,
        float fontSize,
        FontStyles style,
        Color color,
        TextAlignmentOptions alignment)
    {
        GameObject textObject = CreateUIObject(name, parent);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.font = font;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        return text;
    }

    private static GameObject CreateImage(
        string name,
        Transform parent,
        Color color,
        Sprite sprite,
        bool raycastTarget)
    {
        GameObject imageObject = CreateUIObject(name, parent);
        Image image = imageObject.AddComponent<Image>();
        image.color = color;
        image.sprite = sprite;
        image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.raycastTarget = raycastTarget;
        return imageObject;
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject gameObject = new(
            name,
            typeof(RectTransform)
        );
        gameObject.layer = LayerMask.NameToLayer("UI");

        if (parent != null)
        {
            gameObject.transform.SetParent(parent, false);
        }

        return gameObject;
    }

    private static Sprite LoadBackgroundSprite()
    {
        TextureImporter importer =
            AssetImporter.GetAtPath(BackgroundPath) as TextureImporter;

        if (importer == null)
        {
            Debug.LogError($"Không tìm thấy ảnh nền: {BackgroundPath}");
            return null;
        }

        bool requiresImport =
            importer.textureType != TextureImporterType.Sprite ||
            importer.spriteImportMode != SpriteImportMode.Single ||
            importer.mipmapEnabled ||
            importer.maxTextureSize != 2048;

        if (requiresImport)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.sRGBTexture = true;
            importer.alphaIsTransparency = false;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
    }

    private static void CreateDecoration(
        string name,
        Transform parent,
        Sprite sprite,
        Color color,
        Vector2 normalizedPosition,
        Vector2 size)
    {
        GameObject decoration = CreateImage(
            name,
            parent,
            color,
            sprite,
            false
        );
        RectTransform rect = decoration.GetComponent<RectTransform>();
        rect.anchorMin = normalizedPosition;
        rect.anchorMax = normalizedPosition;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
    }

    private static void AddOutline(
        GameObject target,
        Color color,
        Vector2 distance)
    {
        Outline outline = target.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = distance;
        outline.useGraphicAlpha = true;
    }

    private static void AddShadow(
        GameObject target,
        Color color,
        Vector2 distance)
    {
        Shadow shadow = target.AddComponent<Shadow>();
        shadow.effectColor = color;
        shadow.effectDistance = distance;
        shadow.useGraphicAlpha = true;
    }

    private static void SetRect(
        RectTransform rect,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 sizeDelta)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }

    private static void SetRectCentered(
        RectTransform rect,
        Vector2 position,
        Vector2 size)
    {
        SetRect(
            rect,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            position,
            size
        );
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void CreateEventSystem()
    {
        GameObject eventSystemObject = new("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
    }

    private static void AssignControllerReferences(
        LoginUIController controller,
        GameObject loginPanel,
        GameObject registerPanel,
        Button loginTab,
        Button registerTab,
        TMP_InputField loginUsername,
        TMP_InputField loginPassword,
        Button showLoginPassword,
        TMP_Text showLoginPasswordLabel,
        Button loginButton,
        Button goToRegister,
        TMP_Text loginMessage,
        TMP_InputField registerUsername,
        TMP_InputField displayName,
        TMP_InputField registerPassword,
        TMP_InputField confirmPassword,
        Button showRegisterPassword,
        TMP_Text showRegisterPasswordLabel,
        Button showConfirmPassword,
        TMP_Text showConfirmPasswordLabel,
        Button registerButton,
        Button goToLogin,
        TMP_Text registerMessage)
    {
        SerializedObject serialized = new(controller);

        Set(serialized, "loginPanel", loginPanel);
        Set(serialized, "registerPanel", registerPanel);
        Set(serialized, "loginTabButton", loginTab);
        Set(serialized, "registerTabButton", registerTab);
        Set(serialized, "loginTabBackground", loginTab.GetComponent<Image>());
        Set(serialized, "registerTabBackground", registerTab.GetComponent<Image>());
        Set(serialized, "loginTabLabel", loginTab.GetComponentInChildren<TMP_Text>());
        Set(serialized, "registerTabLabel", registerTab.GetComponentInChildren<TMP_Text>());
        Set(serialized, "loginUsernameInput", loginUsername);
        Set(serialized, "loginPasswordInput", loginPassword);
        Set(serialized, "showLoginPasswordButton", showLoginPassword);
        Set(serialized, "showLoginPasswordLabel", showLoginPasswordLabel);
        Set(serialized, "loginButton", loginButton);
        Set(serialized, "goToRegisterButton", goToRegister);
        Set(serialized, "loginMessageText", loginMessage);
        Set(serialized, "registerUsernameInput", registerUsername);
        Set(serialized, "displayNameInput", displayName);
        Set(serialized, "registerPasswordInput", registerPassword);
        Set(serialized, "confirmPasswordInput", confirmPassword);
        Set(serialized, "showRegisterPasswordButton", showRegisterPassword);
        Set(serialized, "showRegisterPasswordLabel", showRegisterPasswordLabel);
        Set(serialized, "showConfirmPasswordButton", showConfirmPassword);
        Set(serialized, "showConfirmPasswordLabel", showConfirmPasswordLabel);
        Set(serialized, "registerButton", registerButton);
        Set(serialized, "goToLoginButton", goToLogin);
        Set(serialized, "registerMessageText", registerMessage);

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);
    }

    private static void Set(
        SerializedObject serialized,
        string propertyName,
        Object value)
    {
        SerializedProperty property =
            serialized.FindProperty(propertyName);

        if (property == null)
        {
            Debug.LogError($"Không tìm thấy field {propertyName}.");
            return;
        }

        property.objectReferenceValue = value;
    }

    private static void EnsureBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new()
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };

        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene.path != ScenePath)
            {
                scenes.Add(scene);
            }
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void DestroyExisting(string name)
    {
        GameObject existing = GameObject.Find(name);

        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }
    }

    private static Color Hex(string hex, float alpha = 1f)
    {
        ColorUtility.TryParseHtmlString($"#{hex}", out Color color);
        color.a = alpha;
        return color;
    }
}
