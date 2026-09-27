using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class MonsterHealthBarUI : MonoBehaviour
{
    [Header("Network Team")]
    [SerializeField] private int teamPlayerId = 1;

    private Slider healthSlider;
    private NetworkMonsterMovement networkMonster;

    private void Awake()
    {
        healthSlider = GetComponent<Slider>();
    }

    private void Update()
    {
        FindNetworkMonster();
        RefreshHealthBar();
    }

    private void RefreshHealthBar()
    {
        if (networkMonster == null)
        {
            return;
        }

        healthSlider.minValue = 0;
        healthSlider.maxValue = networkMonster.MaxHealth;
        healthSlider.value = networkMonster.CurrentHealth;
    }

    private void FindNetworkMonster()
    {
        if (networkMonster != null && networkMonster.Object != null)
        {
            return;
        }

        NetworkMonsterMovement[] monsters =
            FindObjectsByType<NetworkMonsterMovement>(
                FindObjectsSortMode.None
            );

        foreach (NetworkMonsterMovement monster in monsters)
        {
            if (monster.Object != null &&
                monster.Object.StateAuthority.PlayerId == teamPlayerId)
            {
                networkMonster = monster;
                return;
            }
        }
    }
}
