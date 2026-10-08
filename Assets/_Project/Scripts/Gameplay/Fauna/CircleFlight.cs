using UnityEngine;

namespace VamosAprendiendo.Gameplay
{
    /// <summary>
    /// Hace que un ave (o bandada) vuele en círculos alrededor de un punto, por ejemplo una isla,
    /// con ligera variación de altura y de radio para que el vuelo no sea mecánico.
    /// </summary>
    public class CircleFlight : MonoBehaviour
    {
        [Header("Órbita")]
        [SerializeField] private Vector3 center;
        [SerializeField] private float radius = 15f;
        [Tooltip("Altura sobre el centro")]
        [SerializeField] private float height = 8f;
        [Tooltip("Grados por segundo; la velocidad lineal es radio × grados × π/180")]
        [SerializeField] private float degreesPerSecond = 18f;
        [SerializeField] private bool clockwise = true;
        [SerializeField] private float startAngle;

        [Header("Variación")]
        [SerializeField] private float bobAmplitude = 0.8f;
        [SerializeField] private float bobSpeed = 0.7f;
        [SerializeField] private float radiusWobble = 1.5f;
        [Tooltip("Inclinación lateral al girar")]
        [SerializeField] private float bankDegrees = 14f;
        [Tooltip("Giro extra en Y si el modelo no mira hacia +Z")]
        [SerializeField] private float facingOffsetY;

        private float _angle;
        private float _phase;

        public void Configure(Vector3 orbitCenter, float orbitRadius, float orbitHeight, float speedDegrees, bool isClockwise, float angle)
        {
            center = orbitCenter;
            radius = orbitRadius;
            height = orbitHeight;
            degreesPerSecond = speedDegrees;
            clockwise = isClockwise;
            startAngle = angle;
        }

        private void Awake()
        {
            _angle = startAngle;
            _phase = Random.value * Mathf.PI * 2f;
            Apply();
        }

        private void Update()
        {
            _angle += (clockwise ? -1f : 1f) * degreesPerSecond * Time.deltaTime;
            Apply();
        }

        private void Apply()
        {
            float rad = _angle * Mathf.Deg2Rad;
            float r = radius + Mathf.Sin(Time.time * 0.3f + _phase) * radiusWobble;
            float bob = Mathf.Sin(Time.time * bobSpeed + _phase) * bobAmplitude;

            Vector3 pos = center + new Vector3(Mathf.Cos(rad) * r, height + bob, Mathf.Sin(rad) * r);

            // Tangente de la órbita en el sentido de giro.
            float dir = clockwise ? -1f : 1f;
            Vector3 tangent = new Vector3(-Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * dir;

            transform.position = pos;
            if (tangent.sqrMagnitude > 0.0001f)
            {
                // Se inclina hacia el interior de la curva.
                float bank = -dir * bankDegrees;
                transform.rotation = Quaternion.LookRotation(tangent, Vector3.up) * Quaternion.Euler(0f, facingOffsetY, bank);
            }
        }
    }
}
