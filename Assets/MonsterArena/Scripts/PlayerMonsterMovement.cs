using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMonsterMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float rotationSpeed = 720f;
    [SerializeField] private float gravity = -20f;

    private CharacterController characterController;
    private Transform cameraTransform;
    private float verticalSpeed;
    private bool controlEnabled = true;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void Update()
    {
        ApplyGravity();

        if (!controlEnabled || Keyboard.current == null)
        {
            Move(Vector3.zero);
            return;
        }

        Vector2 input = ReadMovementInput();
        Vector3 direction = CalculateCameraDirection(input);

        RotateTowards(direction);
        Move(direction);
    }

    private Vector2 ReadMovementInput()
    {
        Vector2 input = Vector2.zero;

        if (Keyboard.current.wKey.isPressed) input.y += 1f;
        if (Keyboard.current.sKey.isPressed) input.y -= 1f;
        if (Keyboard.current.aKey.isPressed) input.x -= 1f;
        if (Keyboard.current.dKey.isPressed) input.x += 1f;

        return Vector2.ClampMagnitude(input, 1f);
    }

    private Vector3 CalculateCameraDirection(Vector2 input)
    {
        if (cameraTransform == null)
        {
            return new Vector3(input.x, 0f, input.y);
        }

        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();

        return (cameraForward * input.y + cameraRight * input.x).normalized;
    }

    private void RotateTowards(Vector3 direction)
    {
        if (direction.sqrMagnitude <= 0f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    private void ApplyGravity()
    {
        if (characterController.isGrounded && verticalSpeed < 0f)
        {
            verticalSpeed = -2f;
        }

        verticalSpeed += gravity * Time.deltaTime;
    }

    private void Move(Vector3 direction)
    {
        Vector3 velocity = direction * moveSpeed;
        velocity.y = verticalSpeed;

        characterController.Move(velocity * Time.deltaTime);
    }

    public void SetControlEnabled(bool enabled)
    {
        controlEnabled = enabled;
    }
}