using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MonsterTeamUIController : MonoBehaviour
{
    public GameObject monstersPanel, teamPanel;
    public Button openMonsters, openTeam, closeMonsters, closeTeam;
    public Button previousPage, nextPage, addToTeam, selectCompanion, viewTeam;
    public Button addMember, removeMember, moveLeft, moveRight;
    public Button[] monsterRows, teamSlots;
    public Image[] monsterRowArt, teamSlotArt;
    public TMP_Text[] monsterRowLabels, teamSlotLabels;
    public TMP_Text collectionSummary, pageLabel, monsterName, monsterDetails, monsterMessage;
    public TMP_Text teamSummary, teamSelection, teamMessage;
    public Image monsterArt;
    public Sprite shadowFoxArt, unknownMonsterArt;

    private const int PageSize = 5;
    private LocalAccountData account;
    private List<string> owned = new();
    private string selectedId;
    private int page, selectedSlot = -1;
    private Image homeMonsterArt;

    private void Awake()
    {
        homeMonsterArt = transform.Find("MonsterShowcase/MonsterImage")?.GetComponent<Image>();
        openMonsters.onClick.AddListener(OpenMonsters);
        openTeam.onClick.AddListener(OpenTeam);
        closeMonsters.onClick.AddListener(() => monstersPanel.SetActive(false));
        closeTeam.onClick.AddListener(() => teamPanel.SetActive(false));
        previousPage.onClick.AddListener(() => ChangePage(-1));
        nextPage.onClick.AddListener(() => ChangePage(1));
        addToTeam.onClick.AddListener(AddSelected);
        selectCompanion.onClick.AddListener(SelectCompanion);
        viewTeam.onClick.AddListener(OpenTeam);
        addMember.onClick.AddListener(OpenMonsters);
        removeMember.onClick.AddListener(RemoveSelected);
        moveLeft.onClick.AddListener(() => MoveSelected(-1));
        moveRight.onClick.AddListener(() => MoveSelected(1));
        for (int i = 0; i < monsterRows.Length; i++)
        {
            int row = i;
            monsterRows[i].onClick.AddListener(() => SelectRow(row));
        }
        for (int i = 0; i < teamSlots.Length; i++)
        {
            int slot = i;
            teamSlots[i].onClick.AddListener(() =>
            {
                if (!Reload()) return;
                if (slot >= account.teamMonsterIds.Count) { OpenMonsters(); return; }
                selectedSlot = slot;
                teamMessage.text = "";
                RenderTeam();
            });
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            monstersPanel.SetActive(false);
            teamPanel.SetActive(false);
        }
    }

    public void RefreshHomeArt()
    {
        if (homeMonsterArt != null && LocalAccountService.TryGetCurrentAccount(out var current))
            homeMonsterArt.sprite = Art(current.selectedMonsterId);
    }

    private bool Reload()
    {
        if (!LocalAccountService.TryGetCurrentAccount(out account))
        {
            monsterMessage.text = teamMessage.text = "Phiên đăng nhập không còn hợp lệ.";
            return false;
        }
        owned = account.ownedMonsterIds.Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
        return true;
    }

    public void OpenMonsters()
    {
        if (!Reload()) return;
        teamPanel.SetActive(false);
        monstersPanel.SetActive(true);
        monsterMessage.text = "";
        if (!owned.Contains(selectedId)) selectedId = owned.FirstOrDefault();
        page = selectedId == null ? 0 : owned.IndexOf(selectedId) / PageSize;
        RenderMonsters();
    }

    public void OpenTeam()
    {
        if (!Reload()) return;
        monstersPanel.SetActive(false);
        teamPanel.SetActive(true);
        teamMessage.text = "Thay đổi được lưu tự động trên máy này.";
        selectedSlot = account.teamMonsterIds.Count == 0 ? -1 : 0;
        RenderTeam();
    }

    private static string Name(string id) => id == MonsterCatalog.StarterShadowFoxId ? "SHADOW FOX" : id ?? "CHƯA CHỌN QUÁI";
    private Sprite Art(string id) => id == MonsterCatalog.StarterShadowFoxId ? shadowFoxArt : unknownMonsterArt;

    private void ChangePage(int direction)
    {
        page = Mathf.Clamp(page + direction, 0, Mathf.Max(0, (owned.Count - 1) / PageSize));
        selectedId = owned.Skip(page * PageSize).FirstOrDefault();
        monsterMessage.text = "";
        RenderMonsters();
    }

    private void SelectRow(int row)
    {
        int index = page * PageSize + row;
        if (index >= owned.Count) return;
        selectedId = owned[index];
        monsterMessage.text = "";
        RenderMonsters();
    }

    private void RenderMonsters()
    {
        collectionSummary.text = $"BỘ SƯU TẬP  •  {owned.Count} QUÁI";
        int pages = Mathf.Max(1, (owned.Count + PageSize - 1) / PageSize);
        pageLabel.text = $"{page + 1} / {pages}";
        previousPage.interactable = page > 0;
        nextPage.interactable = page + 1 < pages;
        for (int i = 0; i < monsterRows.Length; i++)
        {
            int index = page * PageSize + i;
            bool visible = index < owned.Count;
            monsterRows[i].gameObject.SetActive(visible);
            if (!visible) continue;
            string id = owned[index];
            monsterRowLabels[i].richText = false;
            monsterRowLabels[i].text = Name(id) + (account.teamMonsterIds.Contains(id) ? "\nTrong đội" : "\nĐã sở hữu");
            monsterRowArt[i].sprite = Art(id);
            monsterRows[i].interactable = id != selectedId;
        }
        bool has = selectedId != null;
        bool inTeam = has && account.teamMonsterIds.Contains(selectedId);
        monsterName.richText = false;
        monsterName.text = Name(selectedId);
        monsterArt.sprite = has ? Art(selectedId) : unknownMonsterArt;
        monsterDetails.text = !has ? "Bạn chưa sở hữu quái nào." :
            $"HỆ: {(selectedId == MonsterCatalog.StarterShadowFoxId ? "BÓNG TỐI / DARK" : "Chưa cấu hình")}\n\n" +
            "LEVEL   --       HP   --\nPOWER   --\n\nKỸ NĂNG\nChưa có dữ liệu kỹ năng riêng.\n\nChỉ số và nâng cấp chưa được cấu hình.";
        addToTeam.interactable = has && !inTeam && account.teamMonsterIds.Count < LocalAccountService.MaxTeamSize;
        addToTeam.GetComponentInChildren<TMP_Text>().text = inTeam ? "ĐÃ TRONG ĐỘI" : account.teamMonsterIds.Count >= 5 ? "ĐỘI ĐÃ ĐỦ 5 QUÁI" : "THÊM VÀO TEAM";
        selectCompanion.interactable = has && account.selectedMonsterId != selectedId;
        selectCompanion.GetComponentInChildren<TMP_Text>().text = has && account.selectedMonsterId == selectedId ? "ĐANG HIỆN Ở HOME" : "HIỂN THỊ Ở HOME";
    }

    private void AddSelected()
    {
        if (!Reload() || !owned.Contains(selectedId)) return;
        var ids = new List<string>(account.teamMonsterIds) { selectedId };
        if (!LocalAccountService.SaveTeam(ids, out string error)) { monsterMessage.text = error; return; }
        Reload();
        RenderMonsters();
        monsterMessage.text = $"Đã thêm vào đội • {account.teamMonsterIds.Count}/5 quái.";
    }

    private void SelectCompanion()
    {
        if (!LocalAccountService.SelectMonster(selectedId, out string error)) { monsterMessage.text = error; return; }
        Reload();
        RenderMonsters();
        GetComponent<MainMenuUIController>().RefreshCurrentAccount();
        monsterMessage.text = "Đã lưu quái hiển thị ở Home.";
    }

    private void RenderTeam()
    {
        int count = account.teamMonsterIds.Count;
        teamSummary.text = $"ĐỘI HÌNH  {count}/5   •   BATTLE POWER: --";
        for (int i = 0; i < teamSlots.Length; i++)
        {
            bool filled = i < count;
            teamSlotArt[i].sprite = filled ? Art(account.teamMonsterIds[i]) : unknownMonsterArt;
            teamSlotArt[i].color = new Color(1f, 1f, 1f, filled ? 1f : 0.2f);
            teamSlotLabels[i].richText = false;
            teamSlotLabels[i].text = filled ? $"VỊ TRÍ {i + 1}\n{Name(account.teamMonsterIds[i])}" : $"VỊ TRÍ {i + 1}\n+ THÊM QUÁI";
            teamSlots[i].interactable = i != selectedSlot;
        }
        bool selected = selectedSlot >= 0 && selectedSlot < count;
        teamSelection.richText = false;
        teamSelection.text = count == 0 ? "Đội đang trống. Chọn một ô để thêm quái." : selected ?
            $"Đang chọn: {Name(account.teamMonsterIds[selectedSlot])} • Vị trí {selectedSlot + 1}" : "Chọn quái để sắp xếp đội hình.";
        removeMember.interactable = selected;
        moveLeft.interactable = selected && selectedSlot > 0;
        moveRight.interactable = selected && selectedSlot + 1 < count;
        addMember.interactable = count < LocalAccountService.MaxTeamSize;
    }

    private void RemoveSelected()
    {
        if (!Reload() || selectedSlot < 0 || selectedSlot >= account.teamMonsterIds.Count) return;
        var ids = new List<string>(account.teamMonsterIds);
        ids.RemoveAt(selectedSlot);
        SaveChanges(ids, Mathf.Min(selectedSlot, ids.Count - 1), "Đã bỏ khỏi đội. Quái vẫn còn trong bộ sưu tập.");
    }

    private void MoveSelected(int direction)
    {
        if (!Reload()) return;
        int target = selectedSlot + direction;
        var ids = new List<string>(account.teamMonsterIds);
        if (selectedSlot < 0 || selectedSlot >= ids.Count || target < 0 || target >= ids.Count) return;
        (ids[selectedSlot], ids[target]) = (ids[target], ids[selectedSlot]);
        SaveChanges(ids, target, "Đã lưu thứ tự đội hình.");
    }

    private void SaveChanges(List<string> ids, int slot, string message)
    {
        if (!LocalAccountService.SaveTeam(ids, out string error)) { teamMessage.text = error; return; }
        Reload();
        selectedSlot = slot;
        RenderTeam();
        teamMessage.text = message;
    }
}
