using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Velocidad máxima de movimiento")]
    [SerializeField] private float moveSpeed = 6.0f;

    [Tooltip("Velocidad de aceleración y desaceleración (suavizado de inercia)")]
    [SerializeField] private float speedChangeRate = 12.0f;

    [Header("Rotation Settings")]
    [Tooltip("Tiempo de amortiguación para el giro (0.10s a 0.15s es el estándar de oro para control suave)")]
    [Range(0.05f, 0.3f)]
    [SerializeField] private float rotationSmoothTime = 0.12f;

    [Header("Jump & Gravity")]
    [Tooltip("Altura del salto en metros")]
    [SerializeField] private float jumpHeight = 1.25f;

    [Tooltip("Fuerza de gravedad")]
    [SerializeField] private float gravity = -16.0f;

    [Tooltip("Gravedad cuando el personaje está en el suelo para mantenerlo pegado a rampas")]
    [SerializeField] private float groundedGravity = -2.0f;

    [Header("Camera Reference")]
    [SerializeField] private Transform cameraTransform;

    // References
    private CharacterController _controller;

    // State
    private Vector2 _inputMovement;
    private bool _jumpRequested;
    private float _currentSpeed;
    private float _targetRotation;
    private float _rotationVelocity;
    private float _verticalVelocity;

    public bool IsMoving => _inputMovement.sqrMagnitude > 0.01f || (_controller != null && _controller.velocity.sqrMagnitude > 0.1f);
    public Vector2 InputMovement => _inputMovement;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        SetupCameraFollow();
    }

    private void Start()
    {
        SetupCameraFollow();
    }

    private void SetupCameraFollow()
    {
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
            var smoothCam = Camera.main.GetComponent<VamosAprendiendo.Gameplay.SmoothThirdPersonCamera>();
            if (smoothCam == null)
            {
                smoothCam = Camera.main.gameObject.AddComponent<VamosAprendiendo.Gameplay.SmoothThirdPersonCamera>();
            }
            smoothCam.SetTarget(this.transform);
        }
    }

    private void Update()
    {
        CheckJumpInput();
        ApplyGravity();
        ApplyMovementAndRotation();
    }

    private void CheckJumpInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            _jumpRequested = true;
        }

        if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
        {
            _jumpRequested = true;
        }
#endif
    }

    private void ApplyGravity()
    {
        if (_controller.isGrounded)
        {
            if (_verticalVelocity < 0.0f)
            {
                _verticalVelocity = groundedGravity;
            }

            // Realizar salto si fue solicitado y estamos en el suelo
            if (_jumpRequested)
            {
                _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                _jumpRequested = false;
            }
        }
        else
        {
            // Aplicar aceleración por gravedad en el aire
            _verticalVelocity += gravity * Time.deltaTime;
            _jumpRequested = false;
        }
    }

    private void ApplyMovementAndRotation()
    {
        float targetSpeed = 0f;
        Vector3 targetDirection = Vector3.zero;

        // 1. Calcular dirección de movimiento relativa a la orientación de la cámara
        if (_inputMovement.sqrMagnitude > 0.01f)
        {
            targetSpeed = moveSpeed * Mathf.Clamp01(_inputMovement.magnitude);

            Transform cam = cameraTransform != null ? cameraTransform : (Camera.main != null ? Camera.main.transform : null);

            if (cam != null)
            {
                Vector3 camForward = cam.forward;
                Vector3 camRight = cam.right;

                camForward.y = 0f;
                camRight.y = 0f;
                camForward.Normalize();
                camRight.Normalize();

                targetDirection = (camForward * _inputMovement.y + camRight * _inputMovement.x).normalized;
            }
            else
            {
                targetDirection = new Vector3(_inputMovement.x, 0f, _inputMovement.y).normalized;
            }

            // 2. Giro suave del personaje (SmoothDampAngle)
            _targetRotation = Mathf.Atan2(targetDirection.x, targetDirection.z) * Mathf.Rad2Deg;
            float smoothYaw = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, rotationSmoothTime);
            transform.rotation = Quaternion.Euler(0.0f, smoothYaw, 0.0f);
        }

        // 3. Aceleración e inercia suave
        _currentSpeed = Mathf.Lerp(_currentSpeed, targetSpeed, Time.deltaTime * speedChangeRate);
        if (Mathf.Abs(_currentSpeed) < 0.01f) _currentSpeed = 0f;

        // 4. Mover CharacterController
        Vector3 movementVector = (targetDirection * _currentSpeed) + new Vector3(0.0f, _verticalVelocity, 0.0f);
        _controller.Move(movementVector * Time.deltaTime);
    }

    // Input System Callbacks
    public void OnMove(InputValue value)
    {
        _inputMovement = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        if (value.isPressed)
        {
            _jumpRequested = true;
        }
    }

    public void SetInputMovement(Vector2 input)
    {
        _inputMovement = input;
    }

    public void RequestJump()
    {
        _jumpRequested = true;
    }
}
