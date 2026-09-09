using UnityEngine;

namespace Project.Gameplay.Environment
{
    /// <summary>
    /// Controla la iluminación y la pulsación mágica de las lámparas japonesas.
    /// Ajusta tanto la intensidad de la luz puntual como la emisión del material.
    /// </summary>
    [SelectionBase]
    public class LanternGlow : MonoBehaviour
    {
        [Header("Light Component")]
        [SerializeField] private Light lanternLight;
        [SerializeField] private Color glowColor = new Color(0.3f, 0.93f, 0.95f, 1f); // Cyan luminoso
        [SerializeField] private float baseIntensity = 2.8f;
        [SerializeField] private float lightRange = 5.5f;

        [Header("Pulse / Flicker Effect")]
        [Tooltip("Activa una suave pulsación mágica tipo respiración.")]
        [SerializeField] private bool enablePulse = true;
        [SerializeField] private float pulseSpeed = 1.8f;
        [SerializeField] private float pulseAmount = 0.25f; // Variación del 25%

        [Tooltip("Activa micro-parpadeos sutiles imitando fuego o energía mágica.")]
        [SerializeField] private bool enableFlicker = true;
        [SerializeField] private float flickerSpeed = 8.0f;
        [SerializeField] private float flickerAmount = 0.08f;

        [Header("Material Emission")]
        [SerializeField] private Renderer lanternRenderer;
        [SerializeField] private float emissionIntensity = 2.5f;

        private MaterialPropertyBlock _propBlock;
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private float _noiseOffset;

        private void Awake()
        {
            _noiseOffset = Random.Range(0f, 100f);
            _propBlock = new MaterialPropertyBlock();

            if (lanternLight == null)
            {
                lanternLight = GetComponentInChildren<Light>();
            }

            if (lanternRenderer == null)
            {
                lanternRenderer = GetComponentInChildren<Renderer>();
            }

            if (lanternLight != null)
            {
                lanternLight.color = glowColor;
                lanternLight.range = lightRange;
                lanternLight.intensity = baseIntensity;
            }
        }

        private void Update()
        {
            float intensityMultiplier = 1f;

            if (enablePulse)
            {
                // Pulsación suave senoidal
                float sine = Mathf.Sin((Time.time + _noiseOffset) * pulseSpeed);
                intensityMultiplier += sine * pulseAmount;
            }

            if (enableFlicker)
            {
                // Micro parpadeo orgánico con ruido Perlin
                float perlin = (Mathf.PerlinNoise((Time.time + _noiseOffset) * flickerSpeed, 0f) - 0.5f) * 2f;
                intensityMultiplier += perlin * flickerAmount;
            }

            // Aplicar a la luz puntual
            if (lanternLight != null)
            {
                lanternLight.intensity = Mathf.Max(0.1f, baseIntensity * intensityMultiplier);
            }

            // Aplicar a la emisión del material mediante MaterialPropertyBlock
            if (lanternRenderer != null)
            {
                lanternRenderer.GetPropertyBlock(_propBlock);
                Color currentEmission = glowColor * (emissionIntensity * intensityMultiplier);
                _propBlock.SetColor(EmissionColorId, currentEmission);
                lanternRenderer.SetPropertyBlock(_propBlock);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (lanternLight != null)
            {
                lanternLight.color = glowColor;
                lanternLight.range = lightRange;
                lanternLight.intensity = baseIntensity;
            }
        }
#endif
    }
}
