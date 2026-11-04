using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

namespace VamosAprendiendo.Gameplay
{
    /// <summary>
    /// Base de los objetos que reaccionan a un toque o clic (cofre, pulpo...). Un toque es una pulsación corta que casi no
    /// se mueve, así arrastrar para mover la cámara no cuenta. Los botones en pantalla tienen prioridad y el jugador
    /// debe estar cerca. Las clases hijas deciden qué pasa en <see cref="OnTapped"/>.
    /// </summary>
    public abstract class TapInteractable : MonoBehaviour
    {
        [Tooltip("Distancia máxima del jugador al objeto para poder usarlo (0 = sin límite)")]
        [SerializeField] private float maxDistance = 12f;
        [SerializeField] private float maxTapSeconds = 0.4f;
        [SerializeField] private float maxTapPixels = 25f;

        private readonly Vector2[] _downPosition = new Vector2[2];
        private readonly float[] _downTime = new float[2];
        private readonly bool[] _tracking = new bool[2];
        private PlayerController _player;
        private readonly List<RaycastResult> _uiHits = new List<RaycastResult>();

        /// <summary>Se llama cuando el toque cayó sobre este objeto. Devuelve false para ignorarlo.</summary>
        protected abstract void OnTapped();

        /// <summary>Permite a la clase hija ignorar toques (por ejemplo, si ya hay una transición en curso).</summary>
        protected virtual bool CanReceiveTap => true;

        protected virtual void Update()
        {
            // Ratón y pantalla táctil por separado: en el simulador de dispositivos el ratón llega como toque, y en PC
            // puede haber ambos; así no depende de cuál fue el último puntero usado.
            if (Mouse.current != null)
                Track(0, Mouse.current.leftButton, Mouse.current.position);
            if (Touchscreen.current != null)
                Track(1, Touchscreen.current.primaryTouch.press, Touchscreen.current.primaryTouch.position);
        }

        private void Track(int source, ButtonControl press, Vector2Control position)
        {
            if (press.wasPressedThisFrame)
            {
                _tracking[source] = true;
                _downPosition[source] = position.ReadValue();
                _downTime[source] = Time.unscaledTime;
            }

            // Sin "else": con fotogramas lentos una pulsación corta puede empezar y terminar en el mismo fotograma.
            if (press.wasReleasedThisFrame && _tracking[source])
            {
                _tracking[source] = false;
                Vector2 up = position.ReadValue();
                bool isTap = Time.unscaledTime - _downTime[source] <= maxTapSeconds && (up - _downPosition[source]).magnitude <= maxTapPixels;
                if (isTap) HandleTap(up);
            }
        }

        private void HandleTap(Vector2 screenPosition)
        {
            if (Time.timeScale == 0f || !CanReceiveTap) return;
            if (HitsUiControl(screenPosition)) return;

            var cam = Camera.main;
            if (cam == null) return;

            // El primer objeto sólido que cruza el rayo (sin contar al jugador) decide si el toque fue sobre este objeto.
            var hits = Physics.RaycastAll(cam.ScreenPointToRay(screenPosition), 200f, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                if (hit.collider.GetComponentInParent<PlayerController>() != null) continue;
                if (hit.collider.transform.IsChildOf(transform) && IsPlayerNear()) OnTapped();
                return;
            }
        }

        private bool IsPlayerNear()
        {
            if (maxDistance <= 0f) return true;
            if (_player == null) _player = FindFirstObjectByType<PlayerController>();
            if (_player == null) return true;
            return (_player.transform.position - transform.position).sqrMagnitude <= maxDistance * maxDistance;
        }

        // Botones y barras de la pantalla (salto, pausa, audio...) no deben activar lo que haya detrás de ellos.
        private bool HitsUiControl(Vector2 screenPosition)
        {
            if (EventSystem.current == null) return false;
            _uiHits.Clear();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = screenPosition }, _uiHits);
            foreach (var r in _uiHits)
                if (r.gameObject.GetComponentInParent<Selectable>() != null) return true;
            return false;
        }
    }
}
