using System.Collections;
using Fusion;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Local-only introduction screen. It is shown once when this client can see
/// both network monsters in the same Fusion match.
/// </summary>
public class MatchFoundVSUI : MonoBehaviour
{
    [SerializeField] private float displayDuration = 3f;

    private const string BluePortraitPath = "MonsterArena/UI/BlueTrainerPortrait";
    private const string RedPortraitPath = "MonsterArena/UI/RedTrainerPortrait";

    private bool hasPlayed;
    private GameObject screen;

    private void Update()
    {
        if (hasPlayed || !BothPlayersAreReady())
        {
            return;
        }

        hasPlayed = true;
        StartCoroutine(ShowMatchFoundScreen());
    }

    private bool BothPlayersAreReady()
    {
        NetworkMonsterMovement[] monsters =
            FindObjectsByType<NetworkMonsterMovement>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        bool bluePlayerFound = false;
        bool redPlayerFound = false;

        foreach (NetworkMonsterMovement monster in monsters)
        {
            if (monster == null || monster.Object == null ||
                !monster.Object.IsValid)
            {
                continue;
            }

            int playerId = monster.Object.StateAuthority.PlayerId;

            if (playerId == 1)
            {
                bluePlayerFound = true;
            }
            else if (playerId == 2)
            {
                redPlayerFound = true;
            }
        }

        return bluePlayerFound && redPlayerFound;
    }

    private IEnumerator ShowMatchFoundScreen()
    {
        screen = CreateScreen();
        yield return new WaitForSecondsRealtime(displayDuration);

        if (screen != null)
        {
            Destroy(screen);
        }
    }

    private GameObject CreateScreen()
    {
        GameObject canvasObject = new GameObject(
            "MatchFoundVSUI",
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        CreateImage(
            canvasObject.transform,
            "BlueTeamBackground",
            new Vector2(0f, 0f),
            new Vector2(0.5f, 1f),
            new Color(0.02f, 0.13f, 0.31f, 0.96f)
        );

        CreateImage(
            canvasObject.transform,
            "RedTeamBackground",
            new Vector2(0.5f, 0f),
            new Vector2(1f, 1f),
            new Color(0.35f, 0.03f, 0.08f, 0.96f)
        );

        CreateImage(
            canvasObject.transform,
            "CenterShade",
            new Vector2(0.44f, 0f),
            new Vector2(0.56f, 1f),
            new Color(0f, 0f, 0f, 0.30f)
        );

        CreatePortrait(
            canvasObject.transform,
            "BlueTrainer",
            Resources.Load<Sprite>(BluePortraitPath),
            new Vector2(0.08f, 0.16f),
            new Vector2(0.48f, 0.83f),
            new Color(0.12f, 0.78f, 1f, 1f)
        );

        CreatePortrait(
            canvasObject.transform,
            "RedTrainer",
            Resources.Load<Sprite>(RedPortraitPath),
            new Vector2(0.52f, 0.16f),
            new Vector2(0.92f, 0.83f),
            new Color(1f, 0.27f, 0.30f, 1f)
        );

        CreateText(
            canvasObject.transform,
            "BlueLabel",
            "PHE XANH",
            new Vector2(0.06f, 0.08f),
            new Vector2(0.44f, 0.18f),
            TextAnchor.MiddleCenter,
            new Color(0.25f, 0.85f, 1f, 1f),
            58
        );

        CreateText(
            canvasObject.transform,
            "RedLabel",
            "PHE DO",
            new Vector2(0.56f, 0.08f),
            new Vector2(0.94f, 0.18f),
            TextAnchor.MiddleCenter,
            new Color(1f, 0.35f, 0.38f, 1f),
            58
        );

        CreateText(
            canvasObject.transform,
            "MatchFound",
            "DA TIM THAY DOI THU",
            new Vector2(0.22f, 0.86f),
            new Vector2(0.78f, 0.94f),
            TextAnchor.MiddleCenter,
            Color.white,
            42
        );

        CreateText(
            canvasObject.transform,
            "Versus",
            "VS",
            new Vector2(0.405f, 0.39f),
            new Vector2(0.595f, 0.61f),
            TextAnchor.MiddleCenter,
            new Color(1f, 0.86f, 0.20f, 1f),
            150
        );

        return canvasObject;
    }

    private static void CreateImage(
        Transform parent,
        string objectName,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Color color
    )
    {
        GameObject imageObject = new GameObject(objectName, typeof(Image));
        imageObject.transform.SetParent(parent, false);

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        imageObject.GetComponent<Image>().color = color;
    }

    private static void CreatePortrait(
        Transform parent,
        string objectName,
        Sprite portrait,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Color fallbackColor
    )
    {
        GameObject portraitObject = new GameObject(objectName, typeof(Image));
        portraitObject.transform.SetParent(parent, false);

        RectTransform rect = portraitObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = portraitObject.GetComponent<Image>();
        image.sprite = portrait;
        image.preserveAspect = true;
        image.color = portrait == null ? fallbackColor : Color.white;
    }

    private static void CreateText(
        Transform parent,
        string objectName,
        string value,
        Vector2 anchorMin,
        Vector2 anchorMax,
        TextAnchor alignment,
        Color color,
        int fontSize
    )
    {
        GameObject textObject = new GameObject(objectName, typeof(Text));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = value;
        text.alignment = alignment;
        text.color = color;
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
    }
}
