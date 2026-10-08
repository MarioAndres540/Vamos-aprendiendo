using UnityEngine;

namespace VamosAprendiendo.Gameplay
{
    /// <summary>
    /// Mariposa que revolotea sobre una plataforma: elige puntos al azar dentro de un radio alrededor de su
    /// posición base, vuela hacia ellos con aleteo suave y se detiene un momento antes de elegir otro.
    /// El aleteo viene de la animación del paquete (componente Animation, que se pone en bucle aquí).
    /// </summary>
    public class ButterflyWander : MonoBehaviour
    {
        [Header("Zona")]
        [SerializeField] private Vector3 home;
        [SerializeField] private float radius = 6f;
        [Tooltip("Altura mínima y máxima sobre la posición base")]
        [SerializeField] private Vector2 heightRange = new Vector2(0.8f, 2.4f);

        [Header("Movimiento")]
        [SerializeField] private Vector2 speedRange = new Vector2(0.8f, 1.6f);
        [SerializeField] private Vector2 pauseRange = new Vector2(0f, 1.2f);
        [SerializeField] private float turnSpeed = 5f;
        [SerializeField] private float bobAmplitude = 0.12f;
        [Tooltip("Giro extra en Y si el modelo no mira hacia +Z")]
        [SerializeField] private float facingOffsetY;

        [Header("Aleteo")]
        [SerializeField] private Animation flapAnimation;
        [SerializeField] private Vector2 flapSpeedRange = new Vector2(1.2f, 2.0f);

        private Vector3 _target;
        private float _speed;
        private float _pauseTimer;
        private float _phase;

        public void Configure(Vector3 basePosition, float wanderRadius)
        {
            home = basePosition;
            radius = wanderRadius;
        }

        private void Awake()
        {
            _phase = Random.value * Mathf.PI * 2f;
            transform.position = home + Vector3.up * Random.Range(heightRange.x, heightRange.y);
            PickTarget();

            if (flapAnimation == null) flapAnimation = GetComponentInChildren<Animation>(true);
            if (flapAnimation != null) StartFlapping();
        }

        private void OnEnable()
        {
            if (flapAnimation != null) StartFlapping();
        }

        private void StartFlapping()
        {
            flapAnimation.wrapMode = WrapMode.Loop;
            foreach (AnimationState state in flapAnimation)
            {
                state.wrapMode = WrapMode.Loop;
                state.speed = Random.Range(flapSpeedRange.x, flapSpeedRange.y);
                state.time = Random.value * state.length; // cada mariposa empieza en un punto distinto
            }
            if (!flapAnimation.isPlaying) flapAnimation.Play();
        }

        private void Update()
        {
            if (_pauseTimer > 0f)
            {
                _pauseTimer -= Time.deltaTime;
            }
            else
            {
                Vector3 to = _target - transform.position;
                float dist = to.magnitude;
                if (dist < 0.4f)
                {
                    _pauseTimer = Random.Range(pauseRange.x, pauseRange.y);
                    PickTarget();
                }
                else
                {
                    Vector3 step = to / dist * _speed * Time.deltaTime;
                    transform.position += step;

                    Vector3 flat = new Vector3(to.x, 0f, to.z);
                    if (flat.sqrMagnitude > 0.01f)
                    {
                        Quaternion look = Quaternion.LookRotation(flat) * Quaternion.Euler(0f, facingOffsetY, 0f);
                        transform.rotation = Quaternion.Slerp(transform.rotation, look, turnSpeed * Time.deltaTime);
                    }
                }
            }

            // Pequeño vaivén vertical continuo.
            Vector3 p = transform.position;
            p.y += Mathf.Sin(Time.time * 3f + _phase) * bobAmplitude * Time.deltaTime * 6f;
            transform.position = p;
        }

        private void PickTarget()
        {
            Vector2 circle = Random.insideUnitCircle * radius;
            _target = home + new Vector3(circle.x, Random.Range(heightRange.x, heightRange.y), circle.y);
            _speed = Random.Range(speedRange.x, speedRange.y);
        }
    }
}
