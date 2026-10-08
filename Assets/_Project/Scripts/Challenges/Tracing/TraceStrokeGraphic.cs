using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VamosAprendiendo.Challenges.Tracing
{
    /// <summary>
    /// Dibuja polilíneas gruesas dentro de un RectTransform (coordenadas locales respecto al pivote).
    /// Se usa tanto para la letra guía como para el trazo del niño.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class TraceStrokeGraphic : MaskableGraphic
    {
        [SerializeField] private float thickness = 24f;

        private readonly List<List<Vector2>> _strokes = new List<List<Vector2>>();

        public float Thickness
        {
            get => thickness;
            set { thickness = value; SetVerticesDirty(); }
        }

        public void Clear()
        {
            _strokes.Clear();
            SetVerticesDirty();
        }

        public void SetStrokes(IList<List<Vector2>> strokes)
        {
            _strokes.Clear();
            foreach (var s in strokes) _strokes.Add(new List<Vector2>(s));
            SetVerticesDirty();
        }

        public void BeginStroke(Vector2 point)
        {
            _strokes.Add(new List<Vector2> { point });
            SetVerticesDirty();
        }

        public void RemoveLastStroke()
        {
            if (_strokes.Count == 0) return;
            _strokes.RemoveAt(_strokes.Count - 1);
            SetVerticesDirty();
        }

        public void AddPoint(Vector2 point)
        {
            if (_strokes.Count == 0) return;
            _strokes[_strokes.Count - 1].Add(point);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            float half = thickness * 0.5f;

            foreach (var stroke in _strokes)
            {
                if (stroke.Count == 0) continue;

                for (int i = 0; i < stroke.Count; i++)
                {
                    AddSquare(vh, stroke[i], half);
                    if (i == 0) continue;

                    Vector2 a = stroke[i - 1];
                    Vector2 b = stroke[i];
                    Vector2 dir = b - a;
                    if (dir.sqrMagnitude < 0.0001f) continue;

                    Vector2 normal = new Vector2(-dir.y, dir.x).normalized * half;
                    int idx = vh.currentVertCount;
                    vh.AddVert(a - normal, color, Vector2.zero);
                    vh.AddVert(a + normal, color, Vector2.zero);
                    vh.AddVert(b + normal, color, Vector2.zero);
                    vh.AddVert(b - normal, color, Vector2.zero);
                    vh.AddTriangle(idx, idx + 1, idx + 2);
                    vh.AddTriangle(idx, idx + 2, idx + 3);
                }
            }
        }

        // Disco en cada punto: da extremos y uniones redondeados.
        private const int DiscSegments = 12;

        private void AddSquare(VertexHelper vh, Vector2 c, float half)
        {
            int center = vh.currentVertCount;
            vh.AddVert(c, color, Vector2.zero);
            for (int i = 0; i < DiscSegments; i++)
            {
                float a = i / (float)DiscSegments * Mathf.PI * 2f;
                vh.AddVert(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * half, color, Vector2.zero);
            }
            for (int i = 0; i < DiscSegments; i++)
            {
                vh.AddTriangle(center, center + 1 + i, center + 1 + (i + 1) % DiscSegments);
            }
        }
    }
}
