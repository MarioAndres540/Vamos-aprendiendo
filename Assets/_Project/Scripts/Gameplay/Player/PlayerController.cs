using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Gravedad y suelo")]
   [SerializeField] private float gravity = -9.81f;
[SerializeField] private Transform groundCheck;
[SerializeField] private float groundDistance = 0.2f;
[SerializeField] private LayerMask groundMask;

    [Header("Cámara")]
    [SerializeField] private Transform cameraTransform;

    private CharacterController controller;
    private Vector2 inputMovement;
    private Vector3 verticalVelocity;
    private bool isGrounded;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void Update()
    {
        HandleMovementAndGravity();
    }

    private void HandleMovementAndGravity()
    {
        isGrounded = controller.isGrounded;

        // Mantener al jugador pegado al suelo si está en tierra
        if (isGrounded && verticalVelocity.y < 0f)
        {
            verticalVelocity.y = -2f;
        }
        else
        {
            verticalVelocity.y += gravity * Time.deltaTime;
        }

        // Calcular dirección con respecto a la cámara (o mundo si no hay cámara)
        Vector3 moveDirection = Vector3.zero;
        if (inputMovement.sqrMagnitude > 0.01f)
        {
            if (cameraTransform != null)
            {
                Vector3 camForward = cameraTransform.forward;
                Vector3 camRight = cameraTransform.right;
                camForward.y = 0f;
                camRight.y = 0f;
                camForward.Normalize();
                camRight.Normalize();

                moveDirection = (camForward * inputMovement.y + camRight * inputMovement.x).normalized;
            }
            else
            {
                moveDirection = new Vector3(inputMovement.x, 0f, inputMovement.y).normalized;
            }

            // Rotar hacia la dirección del movimiento
            float targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0f, targetAngle, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // COMBINAR movimiento y gravedad en una ÚNICA llamada a controller.Move
        Vector3 finalVelocity = (moveDirection * moveSpeed) + verticalVelocity;
        controller.Move(finalVelocity * Time.deltaTime);
    }

    public void OnMove(InputValue value)
    {
        inputMovement = value.Get<Vector2>();
    }
}
