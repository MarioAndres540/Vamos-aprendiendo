using UnityEngine;

namespace VamosAprendiendo.Gameplay
{
    /// <summary>
    /// Conecta el movimiento del jugador con el Animator del personaje (Encapuchado):
    /// parámetro "Speed" según la velocidad real, disparador "Saltar" al despegar y "Festejar" bajo demanda.
    /// Va en el objeto Player; el Animator está en el modelo hijo.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerAnimatorDriver : MonoBehaviour
    {
        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int JumpId = Animator.StringToHash("Saltar");
        private static readonly int CelebrateId = Animator.StringToHash("Festejar");

        [SerializeField] private Animator animator;
        [Tooltip("Suavizado del parámetro Speed (evita parpadeos entre Idle y Caminar)")]
        [SerializeField] private float speedSmoothTime = 0.08f;
        [Tooltip("Velocidad de la animación de caminar respecto a la velocidad de movimiento (1 = la del clip)")]
        [SerializeField] private float walkAnimationScale = 1f;

        private PlayerController _player;
        private float _speed;
        private float _speedVelocity;

        public void SetAnimator(Animator a) => animator = a;

        private void Awake()
        {
            _player = GetComponent<PlayerController>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        private void OnEnable()
        {
            if (_player != null) _player.Jumped += OnJumped;
        }

        private void OnDisable()
        {
            if (_player != null) _player.Jumped -= OnJumped;
        }

        private void Update()
        {
            if (animator == null) return;

            // Mientras la plataforma elevadora mueve al jugador, el PlayerController está desactivado: se queda en Idle.
            float target = _player.enabled ? _player.CurrentSpeed : 0f;
            _speed = Mathf.SmoothDamp(_speed, target, ref _speedVelocity, speedSmoothTime);
            animator.SetFloat(SpeedId, _speed);
            animator.speed = _speed > 0.1f && _player.IsGrounded
                ? Mathf.Clamp(_speed / Mathf.Max(0.01f, _player.MaxSpeed) * walkAnimationScale, 0.5f, 1.4f)
                : 1f;
        }

        private void OnJumped()
        {
            if (animator != null) animator.SetTrigger(JumpId);
        }

        /// <summary>Reproduce la animación de festejo (por ejemplo al completar un reto).</summary>
        public void Celebrate()
        {
            if (animator != null) animator.SetTrigger(CelebrateId);
        }
    }
}
