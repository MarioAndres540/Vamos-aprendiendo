using System;
using UnityEngine;
using UnityEngine.EventSystems;
using VamosAprendiendo.Gameplay;

namespace VamosAprendiendo.UI
{
    /// <summary>Zona táctil (mitad derecha) para girar la cámara arrastrando el dedo.</summary>
    public class TouchLookArea : MonoBehaviour, IDragHandler
    {
        [Tooltip("Grados de giro por píxel arrastrado")]
        [SerializeField] private float sensitivity = 0.15f;

        /// <summary>Se dispara en cada arrastre sobre la zona (lo usa el tutorial para saber que el jugador ya probó la cámara).</summary>
        public static event Action<Vector2> Dragged;

        private SmoothThirdPersonCamera _camera;

        public void OnDrag(PointerEventData eventData)
        {
            if (_camera == null && Camera.main != null)
            {
                _camera = Camera.main.GetComponent<SmoothThirdPersonCamera>();
            }
            if (_camera != null) _camera.AddOrbitDelta(eventData.delta * sensitivity);
            Dragged?.Invoke(eventData.delta);
        }
    }
}
