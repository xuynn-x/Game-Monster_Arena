using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSkillController : MonoBehaviour
{
    [Header("Skill Q")]
    [SerializeField] private SkillProjectile projectilePrefab;
    [SerializeField] private Transform target;
    [SerializeField] private int damage = 20;
    [SerializeField] private float cooldown = 2f;

    [Header("Camera")]
    [SerializeField] private BattleCameraController battleCamera;

    private float nextUseTime;

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            TryUseSkillQ();
        }
    }

    private void TryUseSkillQ()
    {
        if (Time.time < nextUseTime)
        {
            return;
        }

        if (battleCamera != null && battleCamera.IsBusy)
        {
            return;
        }

        if (projectilePrefab == null || target == null)
        {
            Debug.LogWarning(
                "Skill Q chưa được gán Projectile hoặc Target."
            );

            return;
        }

        MonsterHealth targetHealth =
            target.GetComponent<MonsterHealth>();

        if (targetHealth != null && targetHealth.IsDead)
        {
            return;
        }

        Vector3 attackDirection =
            target.position - transform.position;

        attackDirection.y = 0f;

        if (attackDirection.sqrMagnitude <= 0.001f)
        {
            return;
        }

        attackDirection.Normalize();

        // Quay monster về phía mục tiêu.
        transform.rotation =
            Quaternion.LookRotation(attackDirection);

        Vector3 spawnPosition =
            transform.position
            + Vector3.up * 0.5f
            + attackDirection * 1.2f;

        SkillProjectile projectile = Instantiate(
            projectilePrefab,
            spawnPosition,
            Quaternion.LookRotation(attackDirection)
        );

        projectile.Initialize(target, damage);

        bool cinematicStarted =
            battleCamera != null &&
            battleCamera.TryPlaySkillCinematic(
                transform,
                target,
                projectile
            );

        // Nếu chưa gán camera, projectile vẫn hoạt động bình thường.
        if (!cinematicStarted)
        {
            projectile.Launch();
        }

        nextUseTime = Time.time + cooldown;
    }
}