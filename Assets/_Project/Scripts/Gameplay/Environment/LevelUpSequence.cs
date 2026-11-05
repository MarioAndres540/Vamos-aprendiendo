using System.Collections;
using UnityEngine;
using VamosAprendiendo.UI;

namespace VamosAprendiendo.Gameplay
{
    /// <summary>
    /// Une el portal del nivel 1 con la plataforma verde: mantiene la plataforma bloqueada hasta cruzar el portal y, cuando sube al
    /// jugador a la plataforma de arriba, pasa al nivel 2: celebración bajo el personaje, aviso de «en construcción» (el nivel 2
    /// todavía no está hecho) y regreso del personaje al frente del pulpo.
    /// </summary>
    public class LevelUpSequence : MonoBehaviour
    {
        [SerializeField] private SpiralLift lift;
        [Tooltip("Dónde reaparece el personaje: frente al pulpo")]
        [SerializeField] private Transform octopusFront;
        [Tooltip("Mira hacia este punto al reaparecer (el pulpo)")]
        [SerializeField] private Transform lookAt;
        [Tooltip("Efectos de celebración bajo el personaje al llegar al nivel 2")]
        [SerializeField] private GameObject[] celebrationPrefabs;
        [SerializeField] private float celebrationSeconds = 4f;

        private bool _busy;

        private void OnEnable()
        {
            PlayerLevel.Changed += UpdateLock;
            if (lift != null) lift.ReachedEnd += OnReachedTop;
            UpdateLock();
        }

        private void OnDisable()
        {
            PlayerLevel.Changed -= UpdateLock;
            if (lift != null) lift.ReachedEnd -= OnReachedTop;
        }

        // La plataforma verde solo sube a quien ya cruzó el portal (o ya llegó antes al nivel 2)
        private void UpdateLock()
        {
            if (lift != null) lift.Locked = !PlayerLevel.PortalPassed;
        }

        private void OnReachedTop()
        {
            if (!_busy) StartCoroutine(ArriveAtLevel2());
        }

        private IEnumerator ArriveAtLevel2()
        {
            _busy = true;
            bool firstTime = PlayerLevel.Level < 2;
            if (firstTime) PlayerLevel.SetLevel(2);

            var player = FindFirstObjectByType<PlayerController>();
            if (player == null) { _busy = false; yield break; }
            var controller = player.GetComponent<CharacterController>();

            // El personaje se queda quieto mientras dura la celebración y el aviso
            player.enabled = false;
            Vector3 feet = controller != null ? new Vector3(controller.bounds.center.x, controller.bounds.min.y, controller.bounds.center.z) : player.transform.position;
            if (firstTime && celebrationPrefabs != null)
                foreach (var prefab in celebrationPrefabs)
                    if (prefab != null) Destroy(Instantiate(prefab, feet, Quaternion.identity), celebrationSeconds + 2f);

            LevelToast.Show(firstTime ? "¡Nivel 2!" : "Nivel 2",
                "Esta zona está en construcción. ¡Muy pronto tendrás nuevas aventuras!", celebrationSeconds);
            yield return new WaitForSeconds(celebrationSeconds);

            // De vuelta al frente del pulpo
            if (octopusFront != null)
            {
                if (controller != null) controller.enabled = false;
                player.transform.position = octopusFront.position;
                if (lookAt != null)
                {
                    Vector3 dir = lookAt.position - player.transform.position;
                    dir.y = 0f;
                    if (dir.sqrMagnitude > 0.01f) player.transform.rotation = Quaternion.LookRotation(dir);
                }
                if (controller != null) controller.enabled = true;
            }
            player.enabled = true;
            _busy = false;
        }
    }
}
