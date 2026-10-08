using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VamosAprendiendo.Gameplay;

namespace VamosAprendiendo.UI
{
    /// <summary>
    /// Controles táctiles en pantalla (joystick + botón de salto) que alimentan al PlayerController.
    /// Teclado y gamepad siguen funcionando: el joystick solo escribe el movimiento mientras se toca.
    /// </summary>
    public class MobileControlsUI : MonoBehaviour
    {
        [SerializeField] private GameObject controlsRoot;
        [SerializeField] private VirtualJoystick joystick;
        [SerializeField] private Button jumpButton;
        [Tooltip("Mostrar los controles también en PC (útil para probar con ratón en el Editor)")]
        [SerializeField] private bool showOnDesktop = true;

        private PlayerController _player;
        private SmoothThirdPersonCamera _camera;
        private bool _wasPressed;
        private float _suppressUntil;

        private void Awake()
        {
            if (jumpButton != null)
            {
                jumpButton.onClick.AddListener(OnJumpPressed);
            }
        }

        private void OnEnable()
        {
            if (controlsRoot != null)
            {
                bool hasTouch = Application.isMobilePlatform || Touchscreen.current != null;
                controlsRoot.SetActive(showOnDesktop || hasTouch);
            }
        }

        private void Update()
        {
            if (_player == null)
            {
                _player = FindFirstObjectByType<PlayerController>();
                if (_player == null) return;
            }

            if (_camera == null && Camera.main != null)
            {
                _camera = Camera.main.GetComponent<SmoothThirdPersonCamera>();
            }

            if (joystick == null) return;

            if (joystick.IsPressed)
            {
                _player.SetInputMovement(joystick.Value);
                _wasPressed = true;
            }
            else if (_wasPressed)
            {
                _player.SetInputMovement(Vector2.zero);
                _wasPressed = false;
            }

            // Mientras se usa el joystick la cámara no se alinea sola: evita que el personaje dé vueltas.
            // Se mantiene un instante tras soltar para que la cámara no "dé un latigazo" al frenar el personaje.
            if (joystick.IsPressed) _suppressUntil = Time.unscaledTime + 1f;
            if (_camera != null) _camera.SuppressAutoAlign = Time.unscaledTime < _suppressUntil;
        }

        private void OnDisable()
        {
            if (_camera != null) _camera.SuppressAutoAlign = false;
        }

        private void OnJumpPressed()
        {
            if (_player != null)
            {
                _player.RequestJump();
            }
        }
    }
}
