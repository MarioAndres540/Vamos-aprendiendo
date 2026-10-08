using UnityEngine;
using UnityEngine.EventSystems;

namespace VamosAprendiendo.UI
{
    /// <summary>
    /// Joystick virtual flotante. Va en una zona táctil grande: el mando aparece donde el niño apoya el dedo
    /// y el movimiento se mide desde ese punto, así no hace falta acertar a un círculo fijo.
    /// <see cref="Value"/> es un vector (-1..1) con zona muerta reescalada y adherencia a los ejes,
    /// para que avanzar recto no se desvíe por una pequeña inclinación del pulgar.
    /// </summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("Elementos")]
        [Tooltip("Zona táctil donde se apoya el dedo (normalmente el propio objeto)")]
        [SerializeField] private RectTransform touchArea;
        [SerializeField] private RectTransform background;
        [SerializeField] private RectTransform handle;

        [Header("Ajustes")]
        [Tooltip("Fracción del radio del fondo que recorre el mando; con ese recorrido el valor llega a 1")]
        [Range(0.3f, 1f)]
        [SerializeField] private float handleRange = 0.8f;
        [Tooltip("Zona muerta: por debajo de esta fracción no hay movimiento. El resto se reescala de 0 a 1")]
        [Range(0f, 0.4f)]
        [SerializeField] private float deadZone = 0.2f;
        [Tooltip("Grados alrededor de cada eje (arriba, abajo, izquierda, derecha) en los que el vector se pega al eje")]
        [Range(0f, 25f)]
        [SerializeField] private float axisSnapAngle = 14f;
        [Tooltip("Si es verdadero el mando aparece donde se toca; si no, se mide desde su posición de reposo")]
        [SerializeField] private bool floating = true;
        [Range(0f, 1f)]
        [SerializeField] private float restAlpha = 0.35f;
        [Tooltip("Distancia del mando en reposo a la esquina inferior izquierda de la zona táctil")]
        [SerializeField] private float restMargin = 90f;

        private Vector2 _restPosition;
        private Vector2 _origin;
        private CanvasGroup _group;

        public Vector2 Value { get; private set; }
        public bool IsPressed { get; private set; }

        private float Radius => Mathf.Max(1f, background.rect.width * 0.5f);

        private void Awake()
        {
            if (touchArea == null) touchArea = (RectTransform)transform;
            if (background != null)
            {
                _group = background.GetComponent<CanvasGroup>();
                if (_group == null) _group = background.gameObject.AddComponent<CanvasGroup>();
                _group.alpha = restAlpha;
                _group.blocksRaycasts = false; // los toques los recibe la zona, no el dibujo del mando
            }
        }

        private void Start()
        {
            // Posición de reposo: esquina inferior izquierda de la zona táctil, a modo de pista visual.
            if (background == null) return;
            Rect r = touchArea.rect;
            _restPosition = new Vector2(r.xMin + restMargin + Radius, r.yMin + restMargin + Radius);
            background.anchoredPosition = _restPosition;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!ToLocal(eventData, out Vector2 local)) return;

            IsPressed = true;
            if (floating)
            {
                _origin = ClampInsideArea(local);
                background.anchoredPosition = _origin;
            }
            else
            {
                _origin = _restPosition;
            }

            if (_group != null) _group.alpha = 1f;
            UpdateValue(local);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!IsPressed) return;
            if (ToLocal(eventData, out Vector2 local)) UpdateValue(local);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Release();
        }

        private void OnDisable()
        {
            Release();
        }

        private void Release()
        {
            IsPressed = false;
            Value = Vector2.zero;
            if (handle != null) handle.anchoredPosition = Vector2.zero;
            if (background != null) background.anchoredPosition = _restPosition;
            if (_group != null) _group.alpha = restAlpha;
        }

        private bool ToLocal(PointerEventData eventData, out Vector2 local)
        {
            // Coordenadas respecto al centro (pivote) de la zona táctil; el fondo y el mando se anclan al centro.
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                touchArea, eventData.position, eventData.pressEventCamera, out local);
        }

        private Vector2 ClampInsideArea(Vector2 local)
        {
            // El mando completo debe caber dentro de la zona táctil, aunque se toque cerca del borde.
            Rect r = touchArea.rect;
            float m = Radius;
            return new Vector2(
                Mathf.Clamp(local.x, r.xMin + m, r.xMax - m),
                Mathf.Clamp(local.y, r.yMin + m, r.yMax - m));
        }

        private void UpdateValue(Vector2 local)
        {
            Vector2 delta = local - _origin;
            float travel = Radius * handleRange;

            Vector2 raw = Vector2.ClampMagnitude(delta / travel, 1f);
            if (handle != null) handle.anchoredPosition = raw * travel;

            float mag = raw.magnitude;
            if (mag <= deadZone)
            {
                Value = Vector2.zero;
                return;
            }

            // Reescala para que justo al salir de la zona muerta el valor sea ~0 y no un salto brusco.
            float scaled = Mathf.Clamp01((mag - deadZone) / (1f - deadZone));
            Vector2 dir = SnapToAxis(raw / mag);
            Value = dir * scaled;
        }

        private Vector2 SnapToAxis(Vector2 dir)
        {
            if (axisSnapAngle <= 0f) return dir;

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg; // -180..180, 0 = derecha
            float nearest = Mathf.Round(angle / 90f) * 90f;
            if (Mathf.Abs(Mathf.DeltaAngle(angle, nearest)) <= axisSnapAngle)
            {
                float rad = nearest * Mathf.Deg2Rad;
                return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            }
            return dir;
        }
    }
}
