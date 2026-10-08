using UnityEngine;

namespace VamosAprendiendo.Gameplay
{
    /// <summary>
    /// Atmósfera del mar: bruma lineal a lo lejos para que el borde del agua se funda con el cielo y,
    /// cuando la cámara cae por debajo del nivel del agua, niebla y fondo azul-turquesa de "bajo el agua".
    /// Se usa niebla lineal en ambos casos: así los builds no necesitan variantes de otros modos de niebla.
    /// </summary>
    public class SeaAtmosphere : MonoBehaviour
    {
        [Header("Nivel del agua")]
        [SerializeField] private float waterLevel = -10f;
        [Tooltip("Margen para no alternar de golpe con las olas")]
        [SerializeField] private float hysteresis = 0.4f;

        [Header("Bruma sobre el agua")]
        [SerializeField] private Color hazeColor = new Color(0.82f, 0.92f, 0.95f);
        [SerializeField] private float hazeStart = 90f;
        [SerializeField] private float hazeEnd = 260f;

        [Header("Bajo el agua")]
        [SerializeField] private Color underwaterColor = new Color(0.08f, 0.42f, 0.5f);
        [SerializeField] private float underwaterStart = 0f;
        [SerializeField] private float underwaterEnd = 40f;

        private Camera _camera;
        private bool _underwater;
        private CameraClearFlags _originalClearFlags = CameraClearFlags.Skybox;
        private Color _originalBackground;

        private void Awake()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            ApplyHaze();
        }

        private void LateUpdate()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
                if (_camera == null) return;
                _originalClearFlags = _camera.clearFlags;
                _originalBackground = _camera.backgroundColor;
            }

            float y = _camera.transform.position.y;
            if (!_underwater && y < waterLevel - hysteresis) EnterWater();
            else if (_underwater && y > waterLevel + hysteresis) ExitWater();
        }

        private void EnterWater()
        {
            _underwater = true;
            RenderSettings.fogColor = underwaterColor;
            RenderSettings.fogStartDistance = underwaterStart;
            RenderSettings.fogEndDistance = underwaterEnd;

            // Sin cielo bajo el agua: fondo turquesa del mismo color que la niebla.
            _originalClearFlags = _camera.clearFlags;
            _originalBackground = _camera.backgroundColor;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = underwaterColor;
        }

        private void ExitWater()
        {
            _underwater = false;
            ApplyHaze();
            _camera.clearFlags = _originalClearFlags;
            _camera.backgroundColor = _originalBackground;
        }

        private void ApplyHaze()
        {
            RenderSettings.fogColor = hazeColor;
            RenderSettings.fogStartDistance = hazeStart;
            RenderSettings.fogEndDistance = hazeEnd;
        }

        private void OnDisable()
        {
            if (_underwater && _camera != null)
            {
                _camera.clearFlags = _originalClearFlags;
                _camera.backgroundColor = _originalBackground;
            }
        }
    }
}
