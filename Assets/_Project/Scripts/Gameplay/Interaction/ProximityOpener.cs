using UnityEngine;

namespace VamosAprendiendo.Gameplay
{
    /// <summary>
    /// Abre un <see cref="HingeOpener"/> cuando el jugador se acerca y lo cierra cuando se aleja (con histéresis para
    /// que no parpadee en el límite). Puede abrir hacia el lado contrario al que viene el jugador.
    /// </summary>
    public class ProximityOpener : MonoBehaviour
    {
        [SerializeField] private HingeOpener opener;
        [Tooltip("Distancia horizontal a la que se abre")]
        [SerializeField] private float openDistance = 4f;
        [Tooltip("Distancia a la que vuelve a cerrarse (mayor que la de abrir)")]
        [SerializeField] private float closeDistance = 6f;
        [Tooltip("Diferencia de altura máxima: no se abre si el jugador está en otra plataforma")]
        [SerializeField] private float maxHeightDifference = 3f;
        [Tooltip("Abre las hojas hacia el lado contrario al del jugador")]
        [SerializeField] private bool openAwayFromPlayer = true;

        private PlayerController _player;

        private void Update()
        {
            if (opener == null) return;
            if (_player == null)
            {
                _player = FindFirstObjectByType<PlayerController>();
                if (_player == null) return;
            }

            Vector3 delta = _player.transform.position - transform.position;
            bool sameLevel = Mathf.Abs(delta.y) <= maxHeightDifference;
            delta.y = 0f;
            float distance = delta.magnitude;

            if (!opener.IsOpen)
            {
                if (sameLevel && distance <= openDistance) opener.Open(SideSign());
            }
            else if (!sameLevel || distance >= closeDistance)
            {
                opener.Close();
            }
        }

        // Las hojas giran hacia +Z local con +1: si el jugador está en el lado -Z se abren hacia +Z (alejándose de él).
        private float SideSign()
        {
            if (!openAwayFromPlayer) return 1f;
            float localZ = transform.InverseTransformPoint(_player.transform.position).z;
            return localZ < 0f ? 1f : -1f;
        }
    }
}
