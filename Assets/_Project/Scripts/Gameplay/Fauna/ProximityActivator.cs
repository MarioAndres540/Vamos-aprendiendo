using UnityEngine;

namespace VamosAprendiendo.Gameplay
{
    /// <summary>
    /// Ahorra rendimiento en móvil: muestra el modelo solo cuando el jugador está cerca y lo oculta cuando está lejos.
    /// La lógica de movimiento del objeto raíz sigue funcionando, solo se desactiva el modelo (renderers y animación).
    /// </summary>
    public class ProximityActivator : MonoBehaviour
    {
        [Tooltip("Hijo con el modelo y sus renderers")]
        [SerializeField] private GameObject model;
        [Tooltip("Distancia a la que se oculta el modelo")]
        [SerializeField] private float hideDistance = 90f;
        [Tooltip("Margen para evitar parpadeo al cruzar la distancia")]
        [SerializeField] private float hysteresis = 8f;
        [SerializeField] private float checkInterval = 0.5f;

        private Transform _player;
        private float _timer;

        private void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer > 0f || model == null) return;
            _timer = checkInterval + Random.value * 0.1f; // desfasa las comprobaciones entre objetos

            if (_player == null)
            {
                var pc = FindFirstObjectByType<PlayerController>();
                if (pc == null) return;
                _player = pc.transform;
            }

            float d = Vector3.Distance(_player.position, transform.position);
            bool visible = model.activeSelf;
            if (visible && d > hideDistance + hysteresis) model.SetActive(false);
            else if (!visible && d < hideDistance - hysteresis) model.SetActive(true);
        }
    }
}
