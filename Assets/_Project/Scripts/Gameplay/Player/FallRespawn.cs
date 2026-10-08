using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VamosAprendiendo.Gameplay
{
    /// <summary>
    /// Si el jugador cae de una isla, la pantalla se oscurece poco a poco (la cámara sigue enfocando al personaje)
    /// y, con la pantalla negra, lo devuelve a una posición segura anterior a la caída.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FallRespawn : MonoBehaviour
    {
        [Header("Detección de caída")]
        [Tooltip("Metros por debajo de la última posición en suelo para considerar que está cayendo")]
        [SerializeField] private float fallDepth = 5f;

        [Header("Transición")]
        [Tooltip("Segundos que tarda la pantalla en quedar totalmente oscura")]
        [SerializeField] private float fadeOutDuration = 3f;
        [SerializeField] private float blackHoldDuration = 0.25f;
        [SerializeField] private float fadeInDuration = 0.6f;

        [Header("Punto de retorno")]
        [Tooltip("Cada cuántos segundos se guarda una posición en suelo")]
        [SerializeField] private float recordInterval = 0.25f;
        [Tooltip("Cuántas posiciones antes de la última en suelo se vuelve (4 ≈ 1 segundo antes)")]
        [SerializeField] private int stepsBack = 4;
        [SerializeField] private int historySize = 12;

        private enum State { Normal, FadingOut, Holding, FadingIn }

        private CharacterController _controller;
        private PlayerController _player;
        private readonly List<Vector3> _groundHistory = new List<Vector3>();
        private Vector3 _lastGroundedPosition;
        private Vector3 _initialPosition;
        private Quaternion _initialRotation;
        private float _recordTimer;
        private float _timer;
        private float _fadeInFrom = 1f;
        private State _state = State.Normal;
        private Image _fadeImage;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _player = GetComponent<PlayerController>();
            _initialPosition = transform.position;
            _initialRotation = transform.rotation;
            _lastGroundedPosition = _initialPosition;
            CreateFadeOverlay();
        }

        private void Update()
        {
            switch (_state)
            {
                case State.Normal: UpdateNormal(); break;
                case State.FadingOut: UpdateFadingOut(); break;
                case State.Holding: UpdateHolding(); break;
                case State.FadingIn: UpdateFadingIn(); break;
            }
        }

        private void UpdateNormal()
        {
            if (_controller.isGrounded)
            {
                _lastGroundedPosition = transform.position;
                _recordTimer -= Time.deltaTime;
                if (_recordTimer <= 0f)
                {
                    _recordTimer = recordInterval;
                    _groundHistory.Add(transform.position);
                    if (_groundHistory.Count > historySize) _groundHistory.RemoveAt(0);
                }
            }
            else if (transform.position.y < _lastGroundedPosition.y - fallDepth)
            {
                _state = State.FadingOut;
                _timer = 0f;
            }
        }

        private void UpdateFadingOut()
        {
            // Aterrizó en otra isla más abajo: no era una caída, se cancela el oscurecimiento.
            if (_controller.isGrounded)
            {
                BeginFadeIn();
                return;
            }

            _timer += Time.deltaTime;
            SetAlpha(Mathf.Clamp01(_timer / fadeOutDuration));

            if (_timer >= fadeOutDuration)
            {
                Respawn();
                _state = State.Holding;
                _timer = 0f;
            }
        }

        private void UpdateHolding()
        {
            _timer += Time.deltaTime;
            if (_timer >= blackHoldDuration)
            {
                BeginFadeIn();
            }
        }

        private void BeginFadeIn()
        {
            _fadeInFrom = _fadeImage != null ? _fadeImage.color.a : 1f;
            _state = State.FadingIn;
            _timer = 0f;
        }

        private void UpdateFadingIn()
        {
            _timer += Time.deltaTime;
            SetAlpha(_fadeInFrom * (1f - Mathf.Clamp01(_timer / fadeInDuration)));

            if (_timer >= fadeInDuration)
            {
                SetAlpha(0f);
                _state = State.Normal;
            }
        }

        private void Respawn()
        {
            Vector3 point;
            Quaternion rotation = transform.rotation;

            if (_groundHistory.Count > 0)
            {
                int index = Mathf.Max(0, _groundHistory.Count - 1 - stepsBack);
                point = _groundHistory[index];
            }
            else
            {
                point = _initialPosition;
                rotation = _initialRotation;
            }

            _controller.enabled = false;
            transform.SetPositionAndRotation(point + Vector3.up * 0.1f, rotation);
            _controller.enabled = true;

            if (_player != null) _player.ResetVerticalVelocity();

            _lastGroundedPosition = point;
            _recordTimer = recordInterval;

            // La cámara salta junto al personaje en lugar de recorrer la distancia de la caída.
            var cam = Camera.main != null ? Camera.main.GetComponent<SmoothThirdPersonCamera>() : null;
            if (cam != null) cam.SnapToTarget();
        }

        private void CreateFadeOverlay()
        {
            var canvasObj = new GameObject("FallFadeCanvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200; // por encima del menú de pausa y de los controles táctiles

            var imgObj = new GameObject("Fade", typeof(RectTransform));
            imgObj.transform.SetParent(canvasObj.transform, false);
            var rt = (RectTransform)imgObj.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            _fadeImage = imgObj.AddComponent<Image>();
            _fadeImage.color = new Color(0f, 0f, 0f, 0f);
            _fadeImage.raycastTarget = false;
        }

        private void SetAlpha(float a)
        {
            if (_fadeImage == null) return;
            var c = _fadeImage.color;
            c.a = a;
            _fadeImage.color = c;
        }

        private void OnDestroy()
        {
            if (_fadeImage != null) Destroy(_fadeImage.canvas.gameObject);
        }
    }
}
