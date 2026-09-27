using UnityEngine;
using UnityEngine.InputSystem;

public class MonsterSkillController : MonoBehaviour
{
    [Header("Test Input")]
    [SerializeField] private bool acceptKeyboardInput = true;
    [SerializeField] private Key activationKey = Key.Q;

    [Header("Skill")]
    [SerializeField] private SkillProjectile projectilePrefab;
    [SerializeField] private Transform target;
    [SerializeField] private int damage = 20;
    [SerializeField] private float cooldown = 2f;

    [Header("Camera")]
    [SerializeField] private BattleCameraController battleCamera;

    private float nextUseTime;

    private void Update()
    {
        if (!acceptKeyboardInput ||
            Keyboard.current == null ||
            activationKey == Key.None)
        {
            return;
        }

        if (Keyboard.current[activationKey].wasPressedThisFrame)
        {
            TryUseSkill();
        }
    }

    public void TryUseSkill()
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
                $"{gameObject.name} chưa được gán Projectile hoặc Target."
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

        if (!cinematicStarted)
        {
            projectile.Launch();
        }

        nextUseTime = Time.time + cooldown;
    }
}