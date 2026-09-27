using UnityEngine;

public class MonsterHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;

    private MonsterHitReaction hitReaction;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => currentHealth <= 0;

    private void Awake()
    {
        currentHealth = maxHealth;
        hitReaction = GetComponent<MonsterHitReaction>();
    }

    public void TakeDamage(int damage)
    {
        if (IsDead || damage <= 0)
        {
            return;
        }

        currentHealth = Mathf.Max(
            currentHealth - damage,
            0
        );

        if (hitReaction != null)
        {
            hitReaction.PlayHitReaction();
        }

        Debug.Log(
            $"{gameObject.name} nhận {damage} sát thương. " +
            $"HP còn lại: {currentHealth}/{maxHealth}"
        );

        if (IsDead)
        {
            Debug.Log($"{gameObject.name} đã bị hạ.");
        }
    }
}