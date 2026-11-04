using UnityEngine;

namespace VamosAprendiendo.Gameplay
{
    /// <summary>
    /// Animales de granja (Cartoon Animal Pack): descansan en "Idle" y de vez en cuando dan saltitos.
    /// Las animaciones del paquete mueven la raíz del modelo, por eso el modelo vive dentro de este contenedor
    /// y es el contenedor el que se queda fijo en la isla.
    /// </summary>
    public class FarmAnimal : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private string idleState = "Idle";
        [SerializeField] private string actionState = "Jump";
        [Tooltip("Segundos de descanso entre acciones (mín, máx)")]
        [SerializeField] private Vector2 restSeconds = new Vector2(3f, 9f);
        [Tooltip("Cuántas veces repite la acción cada vez (ciclos del clip)")]
        [SerializeField] private Vector2Int actionLoops = new Vector2Int(1, 3);
        [SerializeField, HideInInspector] private bool hasIdle;

        private float _timer;
        private bool _acting;

        public void Configure(Animator model, bool modelHasIdle)
        {
            animator = model;
            hasIdle = modelHasIdle;
        }

        private void Start()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (animator == null) { enabled = false; return; }

            // Cada animal arranca desfasado para que no se muevan todos a la vez.
            _timer = Random.Range(0f, restSeconds.y);
            Rest();
        }

        private void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;

            if (_acting) Rest(); else Act();
        }

        private void Rest()
        {
            _acting = false;
            animator.speed = 1f;
            if (hasIdle) animator.CrossFade(idleState, 0.2f);
            else { animator.Play(actionState, 0, 0f); animator.speed = 0f; } // sin Idle: queda quieto en la pose inicial
            _timer = Random.Range(restSeconds.x, restSeconds.y);
        }

        private void Act()
        {
            _acting = true;
            animator.speed = 1f;
            animator.CrossFade(actionState, 0.1f);
            float clipLength = 1.3f;
            foreach (var c in animator.runtimeAnimatorController.animationClips)
            {
                if (c.name.StartsWith(actionState)) { clipLength = c.length; break; }
            }
            _timer = clipLength * Random.Range(actionLoops.x, actionLoops.y + 1);
        }
    }
}
