using UnityEngine;
using UnityEngine.InputSystem;

public class TrainerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 720f;
    [SerializeField] private float gravity = -20f;

    private CharacterController characterController;
    private float verticalSpeed;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        Vector3 direction = Vector3.zero;

        if (Keyboard.current.wKey.isPressed) direction += Vector3.forward;
        if (Keyboard.current.sKey.isPressed) direction += Vector3.back;
        if (Keyboard.current.aKey.isPressed) direction += Vector3.left;
        if (Keyboard.current.dKey.isPressed) direction += Vector3.right;

        direction = direction.normalized;

        if (direction.sqrMagnitude > 0f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        if (characterController.isGrounded && verticalSpeed < 0f)
        {
            verticalSpeed = -2f;
        }

        verticalSpeed += gravity * Time.deltaTime;

        Vector3 velocity = direction * moveSpeed;
        velocity.y = verticalSpeed;

        characterController.Move(velocity * Time.deltaTime);
    }
}