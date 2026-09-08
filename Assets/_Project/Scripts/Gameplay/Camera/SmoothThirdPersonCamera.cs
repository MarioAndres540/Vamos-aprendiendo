using UnityEngine;
using UnityEngine.InputSystem;

namespace VamosAprendiendo.Gameplay
{
    [DefaultExecutionOrder(500)]
    public class SmoothThirdPersonCamera : MonoBehaviour
    {
        [Header("Target to Follow")]
        [Tooltip("El transform del jugador a seguir (si está vacío, busca automáticamente el tag 'Player' o PlayerController)")]
        [SerializeField] private Transform target;

        [Header("Look At Target Offset")]
        [Tooltip("Punto de altura del jugador al que enfoca la cámara")]
        [SerializeField] private Vector3 lookAtOffset = new Vector3(0f, 1.4f, 0f);

        [Header("Distance & Zoom Settings")]
        [Tooltip("Distancia por defecto de la cámara al jugador")]
        [SerializeField] private float defaultDistance = 8.5f;

        [Tooltip("Distancia mínima al hacer zoom")]
        [SerializeField] private float minDistance = 2.5f;

        [Tooltip("Distancia máxima al alejar zoom")]
        [SerializeField] private float maxDistance = 20.0f;

        [Tooltip("Sensibilidad del zoom de la rueda del ratón")]
        [SerializeField] private float zoomSpeed = 2.0f;

        [Tooltip("Suavizado de zoom")]
        [SerializeField] private float zoomSmoothTime = 0.08f;

        [Header("Orbit & Rotation Settings")]
        [Tooltip("Sensibilidad horizontal al orbitar con clic derecho")]
        [SerializeField] private float orbitSensitivityX = 0.25f;

        [Tooltip("Sensibilidad vertical al orbitar con clic derecho")]
        [SerializeField] private float orbitSensitivityY = 0.20f;

        [Tooltip("Ángulo vertical mínimo (mirar hacia arriba)")]
        [SerializeField] private float minPitch = -12.0f;

        [Tooltip("Ángulo vertical máximo (vista inclinada hacia abajo)")]
        [SerializeField] private float maxPitch = 70.0f;

        [Header("Auto-Follow Behind Character")]
        [Tooltip("Activar seguimiento automático por detrás del personaje al girar/caminar")]
        [SerializeField] private bool autoAlignBehindCharacter = true;

        [Tooltip("Velocidad con la que la cámara se alinea detrás del personaje al moverse")]
        [SerializeField] private float autoFollowSpeed = 3.5f;

        [Header("Smooth Movement Settings")]
        [Tooltip("Suavizado de seguimiento de posición")]
        [SerializeField] private float positionSmoothTime = 0.10f;

        [Tooltip("Suavizado de rotación")]
        [SerializeField] private float rotationSmoothTime = 0.05f;

        [Header("Camera Auto-Fix")]
        [SerializeField] private bool ensurePerspective = true;
        [SerializeField] private float targetFov = 60.0f;

        // Orbit angles
        private float _currentYaw = 0f;
        private float _currentPitch = 22f;
        private float _targetYaw = 0f;
        private float _targetPitch = 22f;

        // Zoom state
        private float _targetDistance;
        private float _currentDistance;
        private float _zoomVelocity;

        // Smooth state
        private Vector3 _currentVelocity;
        private Camera _cam;
        private PlayerController _playerController;
        private float _freeLookTimer = 0f;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            if (_cam == null) _cam = Camera.main;

            if (_cam != null && ensurePerspective)
            {
                _cam.orthographic = false;
                _cam.fieldOfView = targetFov;
            }

            _targetDistance = defaultDistance;
            _currentDistance = defaultDistance;

            FindTargetIfNull();

            if (target != null)
            {
                _targetYaw = target.eulerAngles.y;
                _currentYaw = _targetYaw;
                SnapToTarget();
            }
        }

        private void Start()
        {
            if (target == null)
            {
                FindTargetIfNull();
            }

            if (target != null)
            {
                _targetYaw = target.eulerAngles.y;
                _currentYaw = _targetYaw;
                SnapToTarget();
            }
        }

        public void FindTargetIfNull()
        {
            if (target != null)
            {
                if (_playerController == null) _playerController = target.GetComponent<PlayerController>();
                return;
            }

            // 1. Tag "Player"
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
                _playerController = player.GetComponent<PlayerController>();
                return;
            }

            // 2. PlayerController component
            _playerController = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            if (_playerController != null)
            {
                target = _playerController.transform;
                return;
            }

            // 3. Name "Player"
            player = GameObject.Find("Player");
            if (player != null)
            {
                target = player.transform;
                _playerController = player.GetComponent<PlayerController>();
                return;
            }

            // 4. Any CharacterController in scene
            CharacterController cc = UnityEngine.Object.FindAnyObjectByType<CharacterController>();
            if (cc != null)
            {
                target = cc.transform;
                _playerController = cc.GetComponent<PlayerController>();
            }
        }

        private void Update()
        {
            HandleOrbitInput();
            HandleZoomInput();
            HandleAutoFollowBehind();
        }

        private void HandleOrbitInput()
        {
            if (Mouse.current == null) return;

            if (Mouse.current.rightButton.isPressed)
            {
                _freeLookTimer = 0.5f; // Pausa el auto-seguimiento mientras el usuario explora libremente con el mouse
                Vector2 mouseDelta = Mouse.current.delta.ReadValue();
                _targetYaw += mouseDelta.x * orbitSensitivityX;
                _targetPitch -= mouseDelta.y * orbitSensitivityY;
                _targetPitch = Mathf.Clamp(_targetPitch, minPitch, maxPitch);
            }
            else if (_freeLookTimer > 0f)
            {
                _freeLookTimer -= Time.deltaTime;
            }
        }

        private void HandleZoomInput()
        {
            if (Mouse.current == null) return;

            float rawScroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(rawScroll) > 0.01f)
            {
                float scrollDelta = Mathf.Sign(rawScroll);
                _targetDistance = Mathf.Clamp(_targetDistance - (scrollDelta * zoomSpeed), minDistance, maxDistance);
            }
        }

        private void HandleAutoFollowBehind()
        {
            if (!autoAlignBehindCharacter || target == null) return;

            // Si el usuario no está manipulando la cámara con clic derecho
            if (_freeLookTimer <= 0f)
            {
                bool isMoving = false;
                if (_playerController != null)
                {
                    isMoving = _playerController.IsMoving;
                }

                // Cuando el jugador se desplaza (W, A, S, D) o gira 180°, la cámara se alinea suavemente detrás del personaje
                if (isMoving)
                {
                    float characterYaw = target.eulerAngles.y;
                    _targetYaw = Mathf.LerpAngle(_targetYaw, characterYaw, Time.deltaTime * autoFollowSpeed);
                }
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                FindTargetIfNull();
                if (target == null) return;
            }

            // 1. Suavizar ángulos de órbita
            _currentYaw = Mathf.LerpAngle(_currentYaw, _targetYaw, Time.deltaTime * (1f / Mathf.Max(rotationSmoothTime, 0.01f)));
            _currentPitch = Mathf.Lerp(_currentPitch, _targetPitch, Time.deltaTime * (1f / Mathf.Max(rotationSmoothTime, 0.01f)));

            // 2. Suavizar distancia de zoom
            _currentDistance = Mathf.SmoothDamp(_currentDistance, _targetDistance, ref _zoomVelocity, zoomSmoothTime);

            // 3. Calcular rotación y posición deseada exactamente detrás del punto de enfoque
            Quaternion targetRotation = Quaternion.Euler(_currentPitch, _currentYaw, 0f);
            Vector3 focusPoint = target.position + lookAtOffset;
            Vector3 desiredPosition = focusPoint - (targetRotation * Vector3.forward * _currentDistance);

            // 4. Mover la cámara suavemente
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _currentVelocity, positionSmoothTime);
            transform.rotation = targetRotation;
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null)
            {
                _playerController = target.GetComponent<PlayerController>();
                _targetYaw = target.eulerAngles.y;
                _currentYaw = _targetYaw;
                SnapToTarget();
            }
        }

        public void SnapToTarget()
        {
            if (target == null) return;

            _targetDistance = defaultDistance;
            _currentDistance = defaultDistance;

            Quaternion targetRotation = Quaternion.Euler(_currentPitch, _currentYaw, 0f);
            Vector3 focusPoint = target.position + lookAtOffset;
            transform.position = focusPoint - (targetRotation * Vector3.forward * _currentDistance);
            transform.rotation = targetRotation;
        }
    }
}
