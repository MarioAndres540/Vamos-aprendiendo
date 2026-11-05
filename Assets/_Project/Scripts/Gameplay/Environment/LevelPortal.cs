using UnityEngine;
using VamosAprendiendo.UI;

namespace VamosAprendiendo.Gameplay
{
    /// <summary>
    /// Portal del nivel 1. Cerrado (apagado) hasta que el jugador cobra la experiencia de todas las actividades; entonces se
    /// enciende y, al cruzarlo, activa la plataforma verde que sube al nivel 2. Si se intenta cruzar cerrado, avisa cuánto falta.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class LevelPortal : MonoBehaviour
    {
        [Tooltip("Efectos del portal (el aro mágico y su brillo): solo se ven cuando el portal está abierto")]
        [SerializeField] private GameObject[] openOnly;
        [Tooltip("Destello al abrirse el paso")]
        [SerializeField] private GameObject openedBurstPrefab;

        private float _nextWarning;

        private void Reset() => GetComponent<Collider>().isTrigger = true;

        private void OnEnable()
        {
            MissionRewards.Changed += Refresh;
            PlayerLevel.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            MissionRewards.Changed -= Refresh;
            PlayerLevel.Changed -= Refresh;
        }

        private void Refresh()
        {
            bool open = PlayerLevel.Level1Completed;
            if (openOnly == null) return;
            foreach (var go in openOnly)
                if (go != null && go.activeSelf != open) go.SetActive(open);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerController>() == null) return;

            if (!PlayerLevel.Level1Completed)
            {
                if (Time.unscaledTime < _nextWarning) return;
                _nextWarning = Time.unscaledTime + 4f;
                int done = MissionRewards.ClaimedCount(), total = MissionRewards.TotalExercises();
                LevelToast.Show("Portal cerrado",
                    $"Completa y cobra todas las actividades del nivel 1 para abrirlo ({done} de {total}).", 3.5f);
                return;
            }

            if (PlayerLevel.PortalPassed) return;
            PlayerLevel.MarkPortalPassed();
            if (openedBurstPrefab != null) Destroy(Instantiate(openedBurstPrefab, other.bounds.center, Quaternion.identity), 4f);
            LevelToast.Show("¡Portal abierto!", "La plataforma verde ya está activa. ¡Súbete para ir al nivel 2!", 4f);
        }
    }
}
