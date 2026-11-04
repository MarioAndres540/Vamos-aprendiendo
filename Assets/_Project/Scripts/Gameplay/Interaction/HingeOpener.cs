using System;
using UnityEngine;
using UnityEngine.Events;

namespace VamosAprendiendo.Gameplay
{
    /// <summary>
    /// Abre y cierra piezas con bisagra (la tapa de un cofre, las hojas de un portón) girándolas con suavidad desde su
    /// rotación inicial. No necesita clips de animación: cada pieza declara cuántos grados gira al abrirse.
    /// Otros scripts (<see cref="TapToOpen"/>, <see cref="ProximityOpener"/>) deciden cuándo abrir o cerrar.
    /// </summary>
    public class HingeOpener : MonoBehaviour
    {
        [Serializable]
        public class Hinge
        {
            [Tooltip("Pieza que gira; su pivote debe estar en la bisagra")]
            public Transform part;
            [Tooltip("Giro local en grados al estar abierta, sumado a su rotación cerrada")]
            public Vector3 openEuler;
            [NonSerialized] public Quaternion closed;
        }

        [SerializeField] private Hinge[] hinges;
        [Tooltip("Segundos que tarda en abrirse o cerrarse")]
        [SerializeField] private float duration = 0.6f;
        [SerializeField] private UnityEvent onOpened;
        [SerializeField] private UnityEvent onClosed;

        private float _progress;       // 0 = cerrado, 1 = abierto
        private bool _wantOpen;
        private float _direction = 1f; // +1 / -1: sentido del giro, para abrir hacia un lado u otro

        public bool IsOpen => _wantOpen;
        public event Action Opened;
        public event Action Closed;

        private void Awake()
        {
            foreach (var h in hinges)
                if (h.part != null) h.closed = h.part.localRotation;
        }

        /// <param name="direction">+1 gira como está configurado; -1 en sentido contrario (p. ej. abrir hacia el otro lado).</param>
        public void Open(float direction = 1f)
        {
            if (_wantOpen) return;
            _wantOpen = true;
            _direction = direction < 0f ? -1f : 1f;
        }

        public void Close() => _wantOpen = false;

        public void Toggle()
        {
            if (_wantOpen) Close(); else Open();
        }

        private void Update()
        {
            float target = _wantOpen ? 1f : 0f;
            if (Mathf.Approximately(_progress, target)) return;

            float before = _progress;
            _progress = Mathf.MoveTowards(_progress, target, Time.deltaTime / Mathf.Max(0.01f, duration));
            float eased = Mathf.SmoothStep(0f, 1f, _progress);

            foreach (var h in hinges)
            {
                if (h.part == null) continue;
                h.part.localRotation = h.closed * Quaternion.Euler(h.openEuler * (_direction * eased));
            }

            if (before < 1f && _progress >= 1f) { onOpened?.Invoke(); Opened?.Invoke(); }
            else if (before > 0f && _progress <= 0f) { onClosed?.Invoke(); Closed?.Invoke(); }
        }
    }
}
