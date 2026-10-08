using UnityEngine;
using UnityEngine.SceneManagement;

namespace VamosAprendiendo.Gameplay
{
    /// <summary>
    /// Punto de retorno del jugador al volver de una escena de reto.
    /// </summary>
    public static class PortalReturnState
    {
        public static bool HasPoint { get; private set; }
        public static Vector3 Position { get; private set; }
        public static Quaternion Rotation { get; private set; }

        public static void Save(Vector3 position, Quaternion rotation)
        {
            HasPoint = true;
            Position = position;
            Rotation = rotation;
        }

        public static void Clear()
        {
            HasPoint = false;
        }
    }

    /// <summary>
    /// Volumen trigger de un portal: al entrar el jugador carga la escena de reto indicada.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PortalTrigger : MonoBehaviour
    {
        [Tooltip("Nombre de la escena de reto (debe estar en Build Settings)")]
        [SerializeField] private string targetSceneName = "LetterTrace_2D";
        [Tooltip("Metros que se aleja al jugador del portal al volver, para no re-entrar al instante")]
        [SerializeField] private float returnOffset = 4f;

        private bool _used;

        public float ReturnOffset => returnOffset;

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_used) return;
            if (other.GetComponentInParent<PlayerController>() == null) return;

            if (!Application.CanStreamedLevelBeLoaded(targetSceneName))
            {
                Debug.LogError($"[PortalTrigger] La escena '{targetSceneName}' no está en Build Settings.");
                return;
            }

            _used = true;

            // Salida: desde el centro del portal hacia donde entró el jugador.
            Transform player = other.transform;
            Vector3 away = player.position - transform.position;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f) away = -transform.forward;
            Vector3 returnPos = player.position + away.normalized * returnOffset;
            PortalReturnState.Save(returnPos, player.rotation);

            SceneManager.LoadScene(targetSceneName);
        }
    }
}
