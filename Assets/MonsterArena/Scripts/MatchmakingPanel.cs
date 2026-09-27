using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Runtime panel reuses the menu's font and button art; no scene or texture replacement.
public sealed class MatchmakingPanel : MonoBehaviour
{
    private TMP_Text status;
    private Button cancel;

    public static void Show(Button menuButton, TMP_Text template)
    {
        var canvas = menuButton.GetComponentInParent<Canvas>();
        var root = new GameObject("Matchmaking Overlay", typeof(RectTransform), typeof(Image), typeof(MatchmakingPanel));
        root.transform.SetParent(canvas.transform, false);
        var rect = (RectTransform)root.transform;
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        root.GetComponent<Image>().color = new Color(.04f, .02f, .09f, .94f);
        root.transform.SetAsLastSibling();
        var panel = root.GetComponent<MatchmakingPanel>();
        var label = new GameObject("Match Status", typeof(RectTransform), typeof(TextMeshProUGUI));
        label.transform.SetParent(root.transform, false);
        panel.status = label.GetComponent<TMP_Text>();
        panel.status.font = template.font;
        panel.status.fontSharedMaterial = template.fontSharedMaterial;
        panel.status.fontSize = 40;
        panel.status.lineSpacing = 12f;
        panel.status.color = Color.white;
        panel.status.alignment = TextAlignmentOptions.Center;
        panel.status.raycastTarget = false;
        panel.status.text = "Đang kết nối…";
        var labelRect = (RectTransform)label.transform;
        labelRect.anchorMin = new Vector2(.1f, .45f); labelRect.anchorMax = new Vector2(.9f, .75f);
        labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
        panel.cancel = Instantiate(menuButton, root.transform);
        panel.cancel.name = "Cancel Matchmaking";
        // The cancel button reuses the frame, but does not carry the Battle swords.
        var battleIcon = panel.cancel.transform.Find("ActionIcon");
        if (battleIcon != null)
        {
            battleIcon.gameObject.SetActive(false);
            Destroy(battleIcon.gameObject);
        }
        panel.cancel.onClick = new Button.ButtonClickedEvent();
        panel.cancel.onClick.AddListener(() => BattleMatchmaking.Instance?.Cancel());
        panel.cancel.navigation = new Navigation { mode = Navigation.Mode.None };
        var buttonRect = (RectTransform)panel.cancel.transform;
        buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(.5f, .35f);
        buttonRect.anchoredPosition = Vector2.zero;
        buttonRect.sizeDelta = new Vector2(460, 120);
        var text = panel.cancel.GetComponentInChildren<TMP_Text>(true);
        if (text != null) { text.text = "HỦY TÌM TRẬN"; text.fontSize = 32; }
        EventSystem.current?.SetSelectedGameObject(panel.cancel.gameObject);
    }

    private void Update()
    {
        var match = BattleMatchmaking.Instance;
        if (match == null) { Destroy(gameObject); return; }
        status.text = match.Message;
        if (match.State == BattleMatchmaking.Phase.Searching)
            status.text += "\n" + Mathf.FloorToInt(match.Elapsed) + " giây";
        cancel.interactable = match.CanCancel;
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) match.Cancel();
    }
}
