using System;
using System.Collections;
using UnityEngine;

namespace VamosAprendiendo.Gameplay
{
    /// <summary>
    /// Anima el cielo (skybox panorámico de Fantasy Skybox FREE): giro lento para que las nubes se desplacen
    /// y, opcionalmente, un ciclo de momentos del día (amanecer, día, atardecer, noche) con transición suave
    /// que también ajusta la luz del sol.
    /// </summary>
    public class SkyboxAnimator : MonoBehaviour
    {
        [Serializable]
        public class SkyPhase
        {
            public string name = "Día";
            public Material skybox;
            public Color sunColor = Color.white;
            public float sunIntensity = 1.2f;
            [Tooltip("Rotación de la luz direccional (sol o luna) en este momento")]
            public Vector3 sunEuler = new Vector3(50f, -30f, 0f);
            [Tooltip("Color de la bruma del horizonte: debe parecerse al color del cielo en el horizonte")]
            public Color hazeColor = new Color(0.82f, 0.92f, 0.95f);
            [Tooltip("Segundos que dura este momento del día antes de pasar al siguiente")]
            public float holdSeconds = 60f;
        }

        [Header("Cielo")]
        [Tooltip("Skybox inicial (material panorámico). No se modifica el asset: se usa una copia en ejecución")]
        [SerializeField] private Material skybox;
        [Tooltip("Grados por segundo que gira el cielo (las nubes se desplazan despacio)")]
        [SerializeField] private float rotationSpeed = 1.5f;

        [Header("Ciclo de día (opcional)")]
        [SerializeField] private bool enableDayCycle = false;
        [SerializeField] private Light sun;
        [SerializeField] private SkyPhase[] phases;
        [Tooltip("Segundos totales de la transición entre dos momentos (mitad oscureciendo, mitad aclarando)")]
        [SerializeField] private float transitionSeconds = 4f;

        /// <summary>Color de bruma del momento actual (ya atenuado por la transición). a = 0 si no hay ciclo activo.</summary>
        public static Color CurrentHaze { get; private set; } = new Color(0f, 0f, 0f, 0f);

        private static readonly int RotationId = Shader.PropertyToID("_Rotation");
        private static readonly int ExposureId = Shader.PropertyToID("_Exposure");

        private Material _runtime;
        private float _rotation;
        private float _baseExposure = 1f;
        private float _sunIntensity;
        private SkyPhase _current;

        private void Awake()
        {
            Material source = skybox != null ? skybox : RenderSettings.skybox;
            if (source == null) return;

            // Copia para no modificar el material del paquete en disco.
            _runtime = new Material(source) { name = source.name + " (runtime)" };
            RenderSettings.skybox = _runtime;
            if (_runtime.HasProperty(ExposureId)) _baseExposure = _runtime.GetFloat(ExposureId);
        }

        private void Start()
        {
            if (enableDayCycle && phases != null && phases.Length > 1 && sun != null)
            {
                StartCoroutine(DayCycle());
            }
        }

        private void Update()
        {
            if (_runtime == null || !_runtime.HasProperty(RotationId)) return;
            _rotation = Mathf.Repeat(_rotation + rotationSpeed * Time.deltaTime, 360f);
            _runtime.SetFloat(RotationId, _rotation);
        }

        private IEnumerator DayCycle()
        {
            int index = 0;
            ApplyPhase(phases[index]);
            SetBrightness(1f, phases[index]);

            while (true)
            {
                yield return new WaitForSeconds(Mathf.Max(1f, phases[index].holdSeconds));

                int next = (index + 1) % phases.Length;
                float half = Mathf.Max(0.5f, transitionSeconds * 0.5f);

                // Oscurece el momento actual...
                for (float t = 0f; t < half; t += Time.deltaTime)
                {
                    SetBrightness(1f - t / half, phases[index]);
                    yield return null;
                }
                SetBrightness(0f, phases[index]);

                // ...cambia de cielo y de luz...
                ApplyPhase(phases[next]);

                // ...y aclara el nuevo.
                for (float t = 0f; t < half; t += Time.deltaTime)
                {
                    SetBrightness(t / half, phases[next]);
                    yield return null;
                }
                SetBrightness(1f, phases[next]);
                index = next;
            }
        }

        private void ApplyPhase(SkyPhase phase)
        {
            if (phase.skybox != null)
            {
                _runtime.shader = phase.skybox.shader;
                _runtime.CopyPropertiesFromMaterial(phase.skybox);
                if (_runtime.HasProperty(ExposureId)) _baseExposure = _runtime.GetFloat(ExposureId);
                if (_runtime.HasProperty(RotationId)) _runtime.SetFloat(RotationId, _rotation);
            }
            if (sun != null)
            {
                sun.color = phase.sunColor;
                sun.transform.rotation = Quaternion.Euler(phase.sunEuler);
            }
            _current = phase;
            _sunIntensity = phase.sunIntensity;
            DynamicGI.UpdateEnvironment(); // actualiza la luz ambiente con el nuevo cielo
        }

        private void SetBrightness(float k, SkyPhase phase)
        {
            if (_runtime != null && _runtime.HasProperty(ExposureId)) _runtime.SetFloat(ExposureId, _baseExposure * k);
            if (sun != null) sun.intensity = _sunIntensity * k;
            Color h = phase.hazeColor * Mathf.Max(0.04f, k);
            h.a = 1f;
            CurrentHaze = h;
        }

        private void OnDestroy()
        {
            CurrentHaze = new Color(0f, 0f, 0f, 0f);
            if (_runtime != null) Destroy(_runtime);
        }
    }
}
