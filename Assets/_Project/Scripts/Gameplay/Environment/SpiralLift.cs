using System.Collections.Generic;
using UnityEngine;

namespace VamosAprendiendo.Gameplay
{
    /// <summary>
    /// Escalón único que sube al jugador siguiendo la espiral de la escalera. Cuando el jugador se para sobre él
    /// (en la base o en la cima), sube o baja por una curva suave que pasa por todos los escalones; mientras viaja
    /// el movimiento del jugador se bloquea y se mueve junto con la plataforma. Si nadie lo usa, vuelve solo a la base.
    /// </summary>
    public class SpiralLift : MonoBehaviour
    {
        private enum State { AtStart, Ascending, AtEnd, Descending, Returning }

        [Header("Recorrido")]
        [Tooltip("Posiciones del centro de la plataforma (mundo), de abajo hacia arriba, en el orden de la espiral")]
        [SerializeField] private Vector3[] waypoints;
        [SerializeField] private float speed = 4.5f;
        [Tooltip("Metros al inicio y al final en los que acelera y frena suavemente")]
        [SerializeField] private float easeDistance = 3f;

        [Header("Uso")]
        [Tooltip("Segundos que el jugador debe estar parado encima antes de que arranque")]
        [SerializeField] private float boardDelay = 0.5f;
        [Tooltip("Segundos en la cima sin jugador antes de volver solo a la base")]
        [SerializeField] private float returnDelay = 8f;
        [SerializeField] private float returnSpeedMultiplier = 1.8f;
        [Tooltip("Segundos que tarda el jugador en deslizarse al centro de la plataforma al subir")]
        [SerializeField] private float centerDuration = 0.45f;
        [Tooltip("Si la plataforma se movió en la escena, su nueva posición pasa a ser el inicio del recorrido")]
        [SerializeField] private bool useSceneStartPosition = true;

        private readonly List<Vector3> _samples = new List<Vector3>();
        private readonly List<float> _cumulative = new List<float>();
        private float _total;
        private float _s;
        private State _state = State.AtStart;

        private Vector3 _centerOffset;   // centro visual de la plataforma respecto a su pivote
        private Vector3 _halfExtents;
        private PlayerController _player;
        private CharacterController _controller;
        private bool _riding;
        private float _boardTimer;
        private float _idleTimer;
        private bool _requireLeave;
        private Vector3 _riderOffset; // desplazamiento horizontal del jugador respecto al centro al subirse
        private float _centerBlend;

        [SerializeField, HideInInspector] private string originalName;
        [SerializeField, HideInInspector] private Vector3 originalCenter;

        public string OriginalName => originalName;
        public Vector3 OriginalCenter => originalCenter;

        public void SetWaypoints(Vector3[] points)
        {
            waypoints = points;
        }

        /// <summary>Guarda cómo era el escalón antes de convertirse en plataforma elevadora (lo usa la herramienta del Editor).</summary>
        public void SetSource(string stepName, Vector3 stepCenter)
        {
            originalName = stepName;
            originalCenter = stepCenter;
        }

        private void Awake()
        {
            var r = GetComponentInChildren<Renderer>();
            if (r != null)
            {
                _centerOffset = r.bounds.center - transform.position;
                _halfExtents = r.bounds.extents;
            }
            else
            {
                _halfExtents = new Vector3(1.2f, 0.25f, 1.2f);
            }

            // El inicio del recorrido es donde se colocó la plataforma en la escena. Los puntos guardados se generaron
            // con la posición original, así que si se movió, se reemplaza el primer punto y el resto del recorrido
            // (hasta la plataforma de arriba) se conserva.
            if (useSceneStartPosition && waypoints != null && waypoints.Length >= 2)
            {
                Vector3 placed = transform.position + _centerOffset;
                float moved = Vector3.Distance(placed, waypoints[0]);
                if (moved > 0.05f)
                {
                    waypoints = (Vector3[])waypoints.Clone();
                    waypoints[0] = placed;
                    if (moved > 15f) Debug.LogWarning($"[SpiralLift] '{name}' está a {moved:0.#} m del inicio original; el primer tramo del recorrido será largo.", this);
                }
            }

            BuildPath();
            if (_samples.Count > 0) SetDistance(0f);
        }

        // Curva Catmull-Rom por los puntos, muestreada en pasos cortos con la longitud acumulada.
        private void BuildPath()
        {
            _samples.Clear();
            _cumulative.Clear();
            if (waypoints == null || waypoints.Length < 2) return;

            const int stepsPerSegment = 10;
            for (int i = 0; i < waypoints.Length - 1; i++)
            {
                Vector3 p0 = waypoints[Mathf.Max(i - 1, 0)];
                Vector3 p1 = waypoints[i];
                Vector3 p2 = waypoints[i + 1];
                Vector3 p3 = waypoints[Mathf.Min(i + 2, waypoints.Length - 1)];

                for (int k = 0; k < stepsPerSegment; k++)
                {
                    _samples.Add(CatmullRom(p0, p1, p2, p3, k / (float)stepsPerSegment));
                }
            }
            _samples.Add(waypoints[waypoints.Length - 1]);

            float acc = 0f;
            _cumulative.Add(0f);
            for (int i = 1; i < _samples.Count; i++)
            {
                acc += Vector3.Distance(_samples[i - 1], _samples[i]);
                _cumulative.Add(acc);
            }
            _total = acc;
        }

        private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * ((2f * p1) + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        private Vector3 PositionAt(float distance)
        {
            distance = Mathf.Clamp(distance, 0f, _total);
            int lo = 0, hi = _cumulative.Count - 1;
            while (lo < hi - 1)
            {
                int mid = (lo + hi) / 2;
                if (_cumulative[mid] <= distance) lo = mid; else hi = mid;
            }
            float span = _cumulative[hi] - _cumulative[lo];
            float f = span > 0.0001f ? (distance - _cumulative[lo]) / span : 0f;
            return Vector3.Lerp(_samples[lo], _samples[hi], f);
        }

        private void SetDistance(float distance)
        {
            _s = Mathf.Clamp(distance, 0f, _total);
            Vector3 centerPos = PositionAt(_s);
            Vector3 before = transform.position;
            transform.position = centerPos - _centerOffset;

            if (_riding && _controller != null)
            {
                _controller.Move(transform.position - before); // el jugador acompaña a la plataforma
            }
        }

        private void Update()
        {
            if (_samples.Count == 0) return;
            if (!EnsurePlayer()) return;

            switch (_state)
            {
                case State.AtStart:
                    if (BoardedLongEnough()) StartRide(State.Ascending);
                    break;

                case State.AtEnd:
                    if (BoardedLongEnough())
                    {
                        StartRide(State.Descending);
                    }
                    else
                    {
                        _idleTimer = PlayerOnPlatform() ? 0f : _idleTimer + Time.deltaTime;
                        if (_idleTimer >= returnDelay) _state = State.Returning;
                    }
                    break;

                case State.Ascending:
                    Travel(+1f, 1f, State.AtEnd);
                    break;

                case State.Descending:
                    Travel(-1f, 1f, State.AtStart);
                    break;

                case State.Returning:
                    if (PlayerOnPlatform() && BoardedLongEnough()) { StartRide(State.Descending); break; }
                    Travel(-1f, returnSpeedMultiplier, State.AtStart);
                    break;
            }
        }

        private void Travel(float direction, float speedMultiplier, State finishedState)
        {
            // Acelera al salir y frena al llegar.
            float toEdge = direction > 0f ? _total - _s : _s;
            float fromEdge = direction > 0f ? _s : _total - _s;
            float ease = Mathf.Clamp01(Mathf.Min(toEdge, fromEdge) / Mathf.Max(easeDistance, 0.01f));
            float currentSpeed = speed * speedMultiplier * Mathf.Lerp(0.25f, 1f, ease);

            CenterRider();
            SetDistance(_s + direction * currentSpeed * Time.deltaTime);

            bool arrived = direction > 0f ? _s >= _total - 0.01f : _s <= 0.01f;
            if (arrived)
            {
                ReleasePlayer();
                _state = finishedState;
                _boardTimer = 0f;
                _idleTimer = 0f;
                _requireLeave = true;
            }
        }

        // Desliza suavemente al jugador hasta el centro de la plataforma mientras sube.
        private void CenterRider()
        {
            if (!_riding || _controller == null || _centerBlend >= 1f || _riderOffset.sqrMagnitude < 0.0001f) return;

            float before = Mathf.SmoothStep(0f, 1f, _centerBlend);
            _centerBlend = Mathf.Min(1f, _centerBlend + Time.deltaTime / Mathf.Max(0.05f, centerDuration));
            float after = Mathf.SmoothStep(0f, 1f, _centerBlend);
            _controller.Move(-_riderOffset * (after - before));
        }

        private void StartRide(State state)
        {
            _state = state;
            _boardTimer = 0f;
            _riding = PlayerOnPlatform();
            if (_riding)
            {
                _player.enabled = false; // el movimiento lo pone la plataforma
                Vector3 plate = transform.position + _centerOffset;
                Vector3 feet = _controller.bounds.center;
                _riderOffset = new Vector3(feet.x - plate.x, 0f, feet.z - plate.z);
                _centerBlend = 0f;
            }
        }

        private void ReleasePlayer()
        {
            if (!_riding) return;
            _riding = false;
            _player.enabled = true;
        }

        private bool BoardedLongEnough()
        {
            // Al llegar, el jugador sigue encima: hasta que se baje una vez no cuenta como "subir de nuevo".
            if (_requireLeave)
            {
                if (!PlayerOnPlatform()) _requireLeave = false;
                _boardTimer = 0f;
                return false;
            }

            if (PlayerOnPlatform())
            {
                _boardTimer += Time.deltaTime;
                return _boardTimer >= boardDelay;
            }
            _boardTimer = 0f;
            return false;
        }

        // Los pies del jugador están sobre la superficie de la plataforma.
        private bool PlayerOnPlatform()
        {
            if (_controller == null) return false;
            Bounds b = _controller.bounds;
            Vector3 feet = new Vector3(b.center.x, b.min.y, b.center.z);
            Vector3 center = transform.position + _centerOffset;
            Vector3 local = feet - center;

            float top = _halfExtents.y;
            return Mathf.Abs(local.x) <= _halfExtents.x && Mathf.Abs(local.z) <= _halfExtents.z
                   && local.y >= top - 0.4f && local.y <= top + 0.8f;
        }

        private bool EnsurePlayer()
        {
            if (_player != null) return true;
            _player = FindFirstObjectByType<PlayerController>();
            if (_player == null) return false;
            _controller = _player.GetComponent<CharacterController>();
            return _controller != null;
        }

        private void OnDisable()
        {
            if (_riding && _player != null) _player.enabled = true;
            _riding = false;
        }
    }
}
