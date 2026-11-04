using System.Collections;
using UnityEngine;
using VamosAprendiendo.Challenges.Tracing;

namespace VamosAprendiendo.Gameplay
{
    /// <summary>
    /// Base de los signos de misión que flotan en el mundo (la "!" de misión nueva y la "?" de fin de misión): se muestran
    /// u ocultan según el progreso (<see cref="ShouldShow"/>), aparecen con rebote y un destello, se animan mientras esperan,
    /// llevan un aura de Magic Effects FREE y reaccionan al toque (<see cref="TapInteractable"/>). Oculto, el signo no
    /// dibuja nada ni recibe toques.
    /// </summary>
    public abstract class MissionMarker : TapInteractable
    {
        [Header("Efectos mágicos (Magic Effects FREE)")]
        [Tooltip("Brillo que acompaña al signo mientras espera")]
        [SerializeField] private GameObject auraPrefab;
        [Tooltip("Destello al aparecer el signo")]
        [SerializeField] private GameObject appearPrefab;
        [Tooltip("Tamaño del aura (1 = el del efecto). Las partículas de humo del paquete miden 2 m y de cerca se ven como una cúpula: se reduce")]
        [SerializeField] private float auraScale = 1f;

        [Header("Animación")]
        [SerializeField] private float bobHeight = 0.22f;
        [SerializeField] private float bobSpeed = 1.8f;
        [Tooltip("Giro continuo (grados por segundo). 0 = no gira")]
        [SerializeField] private float spinDegreesPerSecond = 50f;
        [Tooltip("Balanceo de lado a lado (grados). Sirve para un signo que debe verse siempre de frente")]
        [SerializeField] private float swayDegrees;
        [Tooltip("Rebota (salta y cae) en lugar de flotar suavemente")]
        [SerializeField] private bool bounce;
        [SerializeField] private float pulse = 0.05f;
        [Tooltip("Altura del centro visual sobre el pivote: de ahí salen el aura y los destellos")]
        [SerializeField] private float centerHeight = 0.5f;

        private Renderer[] _renderers;
        private Collider[] _colliders;
        private GameObject _aura;
        private Coroutine _popIn;
        private float _clock;
        private bool _visible;

        protected Vector3 HomePosition { get; private set; }
        protected Vector3 HomeScale { get; private set; }
        protected Quaternion HomeRotation { get; private set; }
        protected Vector3 Center => transform.position + Vector3.up * centerHeight;
        /// <summary>Mientras es verdadero (una secuencia en curso) el signo no se anima ni se oculta por sí solo.</summary>
        protected bool Busy { get; set; }
        protected bool Shown => _visible;

        /// <summary>¿Debe verse ahora, según el progreso del jugador?</summary>
        protected abstract bool ShouldShow();

        /// <summary>Permite a la clase hija ignorar el toque (por ejemplo, durante una transición de escena).</summary>
        protected virtual bool CanTapNow => true;

        protected override bool CanReceiveTap => _visible && !Busy && CanTapNow;

        protected virtual void Awake()
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
            _colliders = GetComponentsInChildren<Collider>(true);
            HomePosition = transform.position;
            HomeScale = transform.localScale;
            HomeRotation = transform.rotation;
            SetShown(false);
        }

        protected virtual void OnEnable()
        {
            TraceProgress.Changed += Refresh;
            MissionRewards.Changed += Refresh;
            Refresh();
        }

        protected virtual void OnDisable()
        {
            TraceProgress.Changed -= Refresh;
            MissionRewards.Changed -= Refresh;
            DestroyAura();
        }

        protected override void Update()
        {
            base.Update(); // detección de toques
            if (!_visible || Busy) return;

            _clock += Time.deltaTime;
            float wave = Mathf.Sin(_clock * bobSpeed);
            float lift = bounce ? Mathf.Abs(wave) * bobHeight * 2f : bobHeight + wave * bobHeight;
            transform.position = HomePosition + Vector3.up * lift;

            float yaw = _clock * spinDegreesPerSecond + Mathf.Sin(_clock * bobSpeed * 0.8f) * swayDegrees;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f) * HomeRotation;
            transform.localScale = HomeScale * (1f + Mathf.Sin(_clock * bobSpeed * 1.6f) * pulse);
            if (_aura != null) _aura.transform.position = Center;
        }

        /// <summary>Muestra u oculta el signo según <see cref="ShouldShow"/>.</summary>
        protected void Refresh()
        {
            if (Busy) return;
            bool show = ShouldShow();
            if (show && !_visible) Appear();
            else if (!show && _visible) Disappear();
        }

        private void Appear()
        {
            _visible = true;
            _clock = 0f;
            transform.SetPositionAndRotation(HomePosition, HomeRotation);
            SetShown(true);
            Spawn(appearPrefab, Center, 1.6f);
            if (auraPrefab != null && _aura == null)
            {
                _aura = Instantiate(auraPrefab, Center, Quaternion.identity);
                _aura.transform.localScale = Vector3.one * auraScale;
            }
            if (_popIn != null) StopCoroutine(_popIn);
            _popIn = StartCoroutine(PopIn());
        }

        private void Disappear()
        {
            _visible = false;
            SetShown(false);
            DestroyAura();
        }

        /// <summary>Oculta el signo de inmediato (para una secuencia que ya lo hizo desaparecer por su cuenta).</summary>
        protected void HideNow()
        {
            _visible = false;
            SetShown(false);
            DestroyAura();
        }

        /// <summary>Devuelve el signo a su sitio y tamaño originales (tras una secuencia que lo movió).</summary>
        protected void RestoreHome()
        {
            transform.SetPositionAndRotation(HomePosition, HomeRotation);
            transform.localScale = HomeScale;
        }

        protected void SetColliders(bool enabled)
        {
            foreach (var c in _colliders) if (c != null) c.enabled = enabled;
        }

        private void SetShown(bool shown)
        {
            foreach (var r in _renderers) if (r != null) r.enabled = shown;
            // Oculto no debe recibir toques ni tapar lo que hay debajo
            SetColliders(shown);
        }

        protected void KeepAuraOnSign()
        {
            if (_aura != null) _aura.transform.position = Center;
        }

        protected void DestroyAura()
        {
            if (_aura != null) { Destroy(_aura); _aura = null; }
        }

        // Entra desde cero con un pequeño rebote
        private IEnumerator PopIn()
        {
            const float duration = 0.7f;
            for (float t = 0f; t < duration && _visible && !Busy; t += Time.deltaTime)
            {
                transform.localScale = HomeScale * EaseOutBack(t / duration);
                yield return null;
            }
        }

        /// <summary>Un efecto de Magic Effects FREE: los de la demo están en bucle, así que se detiene la emisión a la mitad y se borra.</summary>
        protected void Spawn(GameObject prefab, Vector3 position, float seconds)
        {
            if (prefab == null) return;
            var instance = Instantiate(prefab, position, Quaternion.identity);
            StartCoroutine(StopAndDestroy(instance, seconds));
        }

        private static IEnumerator StopAndDestroy(GameObject instance, float seconds)
        {
            yield return new WaitForSeconds(seconds * 0.45f);
            if (instance == null) yield break;
            foreach (var ps in instance.GetComponentsInChildren<ParticleSystem>())
                ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            yield return new WaitForSeconds(seconds * 0.55f);
            if (instance != null) Destroy(instance);
        }

        protected static float EaseOutBack(float k)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            k = Mathf.Clamp01(k) - 1f;
            return 1f + c3 * k * k * k + c1 * k * k;
        }
    }
}
