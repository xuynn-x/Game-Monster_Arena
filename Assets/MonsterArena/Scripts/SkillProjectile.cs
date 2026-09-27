using UnityEngine;

public class SkillProjectile : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 12f;

    private Transform target;
    private MonsterHealth targetHealth;
    private int damage;
    private bool isLaunched = true;

    public bool HasHit { get; private set; }

    public void Initialize(Transform newTarget, int newDamage)
    {
        target = newTarget;
        damage = newDamage;
        targetHealth = target.GetComponent<MonsterHealth>();

        Destroy(gameObject, 8f);
    }

    public void Pause()
    {
        isLaunched = false;
    }

    public void Launch()
    {
        isLaunched = true;
    }

    private void Update()
    {
        if (!isLaunched || HasHit)
        {
            return;
        }

        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 targetPosition =
            target.position + Vector3.up * 0.5f;

        Vector3 direction =
            targetPosition - transform.position;

        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation =
                Quaternion.LookRotation(direction);
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime
        );

        if (Vector3.Distance(
                transform.position,
                targetPosition
            ) <= 0.1f)
        {
            HitTarget();
        }
    }

    private void HitTarget()
    {
        if (HasHit)
        {
            return;
        }

        HasHit = true;

        if (targetHealth != null)
        {
            targetHealth.TakeDamage(damage);
        }

        Destroy(gameObject);
    }
}