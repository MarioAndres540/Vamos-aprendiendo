using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VamosAprendiendo.UI
{
    /// <summary>
    /// Reparte las tarjetas en el espacio disponible según la proporción de la pantalla: en horizontal (tabletas y móviles)
    /// van en una sola fila; en vertical o en pantallas casi cuadradas, en dos columnas. El tamaño de cada tarjeta se
    /// calcula para llenar el área sin pasar de una proporción razonable. Va junto a un GridLayoutGroup.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(GridLayoutGroup))]
    public class ResponsiveCardGrid : UIBehaviour
    {
        [Tooltip("Relación ancho/alto del área desde la que se usa una sola fila")]
        [SerializeField] private float singleRowMinAspect = 1.5f;
        [SerializeField] private float spacing = 28f;
        [Tooltip("Proporción alto/ancho de una tarjeta: mínimo y máximo")]
        [SerializeField] private Vector2 cardRatioRange = new Vector2(0.95f, 1.35f);

        private GridLayoutGroup _grid;

        protected override void OnEnable()
        {
            base.OnEnable();
            Refresh();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            Refresh();
        }

        public void Refresh()
        {
            if (_grid == null) _grid = GetComponent<GridLayoutGroup>();
            var rt = (RectTransform)transform;
            Vector2 area = rt.rect.size;
            int count = CountActiveChildren();
            if (count == 0 || area.x <= 0f || area.y <= 0f) return;

            int columns = area.x / area.y >= singleRowMinAspect ? count : Mathf.Min(2, count);
            int rows = Mathf.CeilToInt(count / (float)columns);

            float cellW = (area.x - spacing * (columns - 1)) / columns;
            float cellH = (area.y - spacing * (rows - 1)) / rows;

            // Tarjetas ni muy bajas ni muy altas: se limita la proporción y el resto del espacio queda como margen.
            float ratio = Mathf.Clamp(cellH / cellW, cardRatioRange.x, cardRatioRange.y);
            if (cellH / cellW > ratio) cellH = cellW * ratio;
            else cellW = cellH / ratio;

            _grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _grid.constraintCount = columns;
            _grid.cellSize = new Vector2(Mathf.Floor(cellW), Mathf.Floor(cellH));
            _grid.spacing = new Vector2(spacing, spacing);
            _grid.childAlignment = TextAnchor.MiddleCenter;
        }

        private int CountActiveChildren()
        {
            int n = 0;
            foreach (Transform c in transform) if (c.gameObject.activeSelf) n++;
            return n;
        }
    }
}
