using System.Collections;
using UnityEngine;
using VamosAprendiendo.UI;

namespace VamosAprendiendo.Gameplay
{
    /// <summary>
    /// Une el portal del nivel 1 con la plataforma: mantiene la plataforma bloqueada hasta cruzar el portal y, cuando sube al
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

        [Header("Plataforma activa")]
        [Tooltip("Aura que se queda sobre la plataforma mientras está activa (viaja con ella)")]
        [SerializeField] private GameObject platformAuraPrefab;
        [Tooltip("Destello al activarse la plataforma")]
        [SerializeField] private GameObject platformBurstPrefab;

        private bool _busy;
        private bool _wasUnlocked;
        private GameObject _aura;

        private void OnEnable()
        {
            PlayerLevel.Changed += UpdateLock;
            if (lift != null) lift.ReachedEnd += OnReachedTop;
            _wasUnlocked = PlayerLevel.PortalPassed;   // al abrir la escena no hay destello: solo al activarse
            UpdateLock();
            // El nivel se lee de la base de datos (y se envía lo que quedó pendiente sin conexión)
            StartCoroutine(LevelService.Sync());
        }

        private void OnDisable()
        {
            PlayerLevel.Changed -= UpdateLock;
            if (lift != null) lift.ReachedEnd -= OnReachedTop;
            if (_aura != null) Destroy(_aura);
        }

        // La plataforma solo sube a quien ya cruzó el portal (o ya llegó antes al nivel 2)
        private void UpdateLock()
        {
            if (lift == null) return;
            bool unlocked = PlayerLevel.PortalPassed;
            lift.Locked = !unlocked;
            if (unlocked == _wasUnlocked && (_aura != null) == unlocked) return;

            if (unlocked)
            {
                if (_aura == null && platformAuraPrefab != null)
                    _aura = Instantiate(platformAuraPrefab, PlatformTop(), Quaternion.identity, lift.transform);
                if (!_wasUnlocked && platformBurstPrefab != null)
                    Destroy(Instantiate(platformBurstPrefab, PlatformTop(), Quaternion.identity), 4f);
            }
            else if (_aura != null)
            {
                Destroy(_aura);
            }
            _wasUnlocked = unlocked;
        }

        // Centro de la cara superior de la plataforma
        private Vector3 PlatformTop()
        {
            var r = lift.GetComponentInChildren<Renderer>();
            return r != null ? new Vector3(r.bounds.center.x, r.bounds.max.y + 0.05f, r.bounds.center.z) : lift.transform.position;
        }

        private void OnReachedTop()
        {
            if (!_busy) StartCoroutine(ArriveAtLevel2());
        }

        private IEnumerator ArriveAtLevel2()
        {
            _busy = true;
            bool firstTime = PlayerLevel.Level < 2;
            if (firstTime)
            {
                PlayerLevel.SetLevel(2);
                StartCoroutine(LevelService.Push());   // el nivel 2 queda guardado en la base de datos
            }

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
