using System.Collections;
using UnityEngine;

public class BattleCameraController : MonoBehaviour
{
    [Header("Opening Camera Points")]
    [SerializeField] private Transform overviewPoint;
    [SerializeField] private Transform introPoint;
    [SerializeField] private Transform battlePoint;
    [SerializeField] private Transform redBattlePoint;

    [Header("Player")]
    [SerializeField] private PlayerMonsterMovement playerMovement;

    [Header("Opening Timing")]
    [SerializeField] private float startDelay = 0.75f;
    [SerializeField] private float overviewToTrainerDuration = 2f;
    [SerializeField] private float trainerPauseDuration = 0.5f;
    [SerializeField] private float trainerToBattleDuration = 2f;

    [Header("Skill Camera Timing")]
    [SerializeField] private float castTransitionDuration = 0.5f;
    [SerializeField] private float castHoldDuration = 0.5f;
    [SerializeField] private float impactTransitionDuration = 0.25f;
    [SerializeField] private float impactHoldDuration = 0.7f;
    [SerializeField] private float returnDuration = 0.8f;

    private bool isAtOfficialView;
    private bool isBusy = true;

    private void Awake()
    {
        if (!enabled)
        {
            return;
        }

        // VS UI is deliberately local. Each client creates its own overlay
        // after it sees both players in the shared match.
        if (GetComponent<MatchFoundVSUI>() == null)
        {
            gameObject.AddComponent<MatchFoundVSUI>();
        }
    }

    public bool IsBusy => isBusy;

    public void ConfigureForLocalPlayer(int playerId)
    {
        // Player 1 nhìn từ phía xanh; Player 2 nhìn từ phía đỏ.
        if (playerId % 2 == 0 && redBattlePoint != null)
        {
            battlePoint = redBattlePoint;
        }

        if (isAtOfficialView && battlePoint != null)
        {
            transform.SetPositionAndRotation(
                battlePoint.position,
                battlePoint.rotation
            );
        }
    }

    private IEnumerator Start()
    {
        SetPlayerControl(false);

        if (overviewPoint == null ||
            introPoint == null ||
            battlePoint == null)
        {
            Debug.LogError("Camera points have not been assigned.");

            isBusy = false;
            SetPlayerControl(true);
            yield break;
        }

        transform.SetPositionAndRotation(
            overviewPoint.position,
            overviewPoint.rotation
        );

        yield return new WaitForSeconds(startDelay);

        yield return MoveCameraTo(
            introPoint.position,
            introPoint.rotation,
            overviewToTrainerDuration
        );

        yield return new WaitForSeconds(trainerPauseDuration);

        yield return MoveCameraTo(
            battlePoint.position,
            battlePoint.rotation,
            trainerToBattleDuration
        );

        EnterOfficialView();
    }

    public bool TryPlaySkillCinematic(
        Transform attacker,
        Transform target,
        SkillProjectile projectile
    )
    {
        if (isBusy ||
            attacker == null ||
            target == null ||
            projectile == null)
        {
            return false;
        }

        projectile.Pause();

        StartCoroutine(
            PlaySkillCinematic(attacker, target, projectile)
        );

        return true;
    }

    private IEnumerator PlaySkillCinematic(
        Transform attacker,
        Transform target,
        SkillProjectile projectile
    )
    {
        isBusy = true;
        isAtOfficialView = false;
        SetPlayerControl(false);

        Vector3 attackDirection =
            target.position - attacker.position;

        attackDirection.y = 0f;

        if (attackDirection.sqrMagnitude <= 0.001f)
        {
            attackDirection = attacker.forward;
        }

        attackDirection.Normalize();

        Vector3 sideDirection =
            Vector3.Cross(Vector3.up, attackDirection).normalized;

        // Cận cảnh monster đang tung chiêu.
        Vector3 castCameraPosition =
            attacker.position
            - attackDirection * 3f
            + sideDirection * 1.3f
            + Vector3.up * 2.2f;

        Quaternion castCameraRotation = LookAtRotation(
            castCameraPosition,
            attacker.position + Vector3.up * 0.7f
        );

        yield return MoveCameraTo(
            castCameraPosition,
            castCameraRotation,
            castTransitionDuration
        );

        yield return new WaitForSeconds(castHoldDuration);

        // Cho projectile bắt đầu bay.
        if (projectile != null)
        {
            projectile.Launch();
        }

        // Camera bám theo projectile.
        while (projectile != null && !projectile.HasHit)
        {
            Vector3 projectilePosition =
                projectile.transform.position;

            Vector3 flightDirection =
                target.position - projectilePosition;

            if (flightDirection.sqrMagnitude > 0.001f)
            {
                flightDirection.Normalize();
            }
            else
            {
                flightDirection = attackDirection;
            }

            Vector3 followPosition =
                projectilePosition
                - flightDirection * 2.5f
                + sideDirection * 0.8f
                + Vector3.up * 1.6f;

            transform.position = Vector3.Lerp(
                transform.position,
                followPosition,
                10f * Time.deltaTime
            );

            Quaternion followRotation = LookAtRotation(
                transform.position,
                projectilePosition + flightDirection * 2f
            );

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                followRotation,
                10f * Time.deltaTime
            );

            yield return null;
        }

        // Cận cảnh monster bị trúng chiêu.
        Vector3 impactCameraPosition =
            target.position
            - attackDirection * 3f
            + sideDirection * 1.2f
            + Vector3.up * 2f;

        Quaternion impactCameraRotation = LookAtRotation(
            impactCameraPosition,
            target.position + Vector3.up * 0.5f
        );

        yield return MoveCameraTo(
            impactCameraPosition,
            impactCameraRotation,
            impactTransitionDuration
        );

        yield return new WaitForSeconds(impactHoldDuration);

        // Trở lại góc nhìn chính thức.
        yield return MoveCameraTo(
            battlePoint.position,
            battlePoint.rotation,
            returnDuration
        );

        EnterOfficialView();
    }

    private IEnumerator MoveCameraTo(
        Vector3 targetPosition,
        Quaternion targetRotation,
        float duration
    )
    {
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;

        if (duration <= 0f)
        {
            transform.SetPositionAndRotation(
                targetPosition,
                targetRotation
            );

            yield break;
        }

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / duration
            );

            float smoothProgress =
                Mathf.SmoothStep(0f, 1f, progress);

            transform.position = Vector3.Lerp(
                startPosition,
                targetPosition,
                smoothProgress
            );

            transform.rotation = Quaternion.Slerp(
                startRotation,
                targetRotation,
                smoothProgress
            );

            yield return null;
        }

        transform.SetPositionAndRotation(
            targetPosition,
            targetRotation
        );
    }

    private Quaternion LookAtRotation(
        Vector3 cameraPosition,
        Vector3 lookTarget
    )
    {
        Vector3 direction = lookTarget - cameraPosition;

        if (direction.sqrMagnitude <= 0.001f)
        {
            return transform.rotation;
        }

        return Quaternion.LookRotation(direction.normalized);
    }

    private void EnterOfficialView()
    {
        transform.SetPositionAndRotation(
            battlePoint.position,
            battlePoint.rotation
        );

        isAtOfficialView = true;
        isBusy = false;
        SetPlayerControl(true);
    }

    private void SetPlayerControl(bool enabled)
    {
        if (playerMovement != null)
        {
            playerMovement.SetControlEnabled(enabled);
        }
    }

    private void LateUpdate()
    {
        if (!isAtOfficialView || battlePoint == null)
        {
            return;
        }

        transform.SetPositionAndRotation(
            battlePoint.position,
            battlePoint.rotation
        );
    }
}
