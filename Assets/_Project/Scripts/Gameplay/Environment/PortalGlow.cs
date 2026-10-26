using UnityEngine;

namespace VamosAprendiendo.Gameplay
{
    /// <summary>
    /// Resplandor cálido alrededor de un portal: un halo (quad con degradado radial aditivo) y una luz puntual
    /// que respiran suavemente entre una intensidad baja y una alta, cada uno con su propio desfase.
    /// </summary>
    public class PortalGlow : MonoBehaviour
    {
        [Header("Elementos")]
        [SerializeField] private Renderer halo;
        [SerializeField] private Light glowLight;

        [Header("Color")]
        [SerializeField] private Color color = new Color(1f, 0.72f, 0.32f, 1f);
        [Tooltip("Multiplicador del brillo del halo (HDR)")]
        [SerializeField] private float haloBrightness = 0.9f;
        [SerializeField] private float lightIntensity = 4f;

        [Header("Pulso")]
        [Tooltip("Fracción de la intensidad máxima en el punto más bajo del pulso")]
        [Range(0.1f, 1f)]
        [SerializeField] private float minLevel = 0.5f;
        [Tooltip("Segundos de un ciclo completo (más intenso → menos intenso → más intenso)")]
        [SerializeField] private float period = 4.5f;
        [Tooltip("Cuánto crece el halo en el punto más intenso (0,06 = 6 %)")]
        [SerializeField] private float haloBreathing = 0.06f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private MaterialPropertyBlock _block;
        private Vector3 _haloBaseScale;
        private float _phase;

        public void Configure(Renderer haloRenderer, Light lightSource)
        {
            halo = haloRenderer;
            glowLight = lightSource;
        }

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            _phase = Random.value * Mathf.PI * 2f; // cada portal respira desfasado
            if (halo != null) _haloBaseScale = halo.transform.localScale;
            Apply(1f);
        }

        private void Update()
        {
            // Dos ondas lentas sumadas: el pulso principal y una variación más larga, para que no sea mecánico.
            float t = Time.time;
            float main = 0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 2f / Mathf.Max(period, 0.1f) + _phase);
            float slow = 0.5f + 0.5f * Mathf.Sin(t * 0.37f + _phase * 1.7f);
            float k = Mathf.Lerp(main, slow, 0.25f);
            Apply(Mathf.Lerp(minLevel, 1f, Mathf.SmoothStep(0f, 1f, k)));
        }

        private void Apply(float level)
        {
            if (halo != null)
            {
                halo.GetPropertyBlock(_block);
                Color c = color * (haloBrightness * level);
                c.a = 1f;
                _block.SetColor(BaseColorId, c);
                halo.SetPropertyBlock(_block);
                halo.transform.localScale = _haloBaseScale * (1f + haloBreathing * level);
            }

            if (glowLight != null)
            {
                glowLight.color = color;
                glowLight.intensity = lightIntensity * level;
            }
        }
    }
}
