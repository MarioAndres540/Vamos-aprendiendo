using System.Collections.Generic;
using UnityEngine;

namespace VamosAprendiendo.Challenges.Tracing
{
    /// <summary>
    /// Compara el trazo del niño con la plantilla de una letra (ambos en el cuadrado normalizado 0..1).
    /// Cobertura: qué parte de la plantilla quedó cubierta. Precisión: qué parte del trazo cae sobre la plantilla.
    /// </summary>
    public static class TraceValidator
    {
        public const float Tolerance = 0.09f;
        public const float MinCoverage = 0.85f;
        public const float MinAccuracy = 0.97f;
        private const float SampleStep = 0.02f;

        // Distancia máxima a la guía mientras se dibuja: un poco más que el borde visible (~0.07).
        public const float OffPathLimit = 0.10f;

        public static List<Vector2> Sample(IList<List<Vector2>> strokes) => Densify(strokes);

        public static bool IsOnPath(Vector2 p, List<Vector2> templateSamples)
        {
            return MinDistance(p, templateSamples) <= OffPathLimit;
        }

        /// <summary>Comprueba el tramo entre dos puntos para que un movimiento rápido no se salte la línea.</summary>
        public static bool IsSegmentOnPath(Vector2 a, Vector2 b, List<Vector2> templateSamples)
        {
            int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a, b) / SampleStep));
            for (int i = 1; i <= steps; i++)
            {
                if (!IsOnPath(Vector2.Lerp(a, b, i / (float)steps), templateSamples)) return false;
            }
            return true;
        }

        public struct Result
        {
            public float Coverage;
            public float Accuracy;
            public bool Passed;
        }

        public static Result Evaluate(IList<List<Vector2>> template, IList<List<Vector2>> user)
        {
            var templateSamples = Densify(template);
            var userSamples = Densify(user);

            var result = new Result();
            if (templateSamples.Count == 0 || userSamples.Count == 0) return result;

            int covered = 0;
            foreach (var t in templateSamples)
            {
                if (MinDistance(t, userSamples) <= Tolerance) covered++;
            }

            int onPath = 0;
            foreach (var u in userSamples)
            {
                if (MinDistance(u, templateSamples) <= OffPathLimit) onPath++;
            }

            result.Coverage = covered / (float)templateSamples.Count;
            result.Accuracy = onPath / (float)userSamples.Count;
            result.Passed = result.Coverage >= MinCoverage && result.Accuracy >= MinAccuracy;
            return result;
        }

        private static List<Vector2> Densify(IList<List<Vector2>> strokes)
        {
            var samples = new List<Vector2>();
            foreach (var stroke in strokes)
            {
                if (stroke.Count == 0) continue;
                samples.Add(stroke[0]);
                for (int i = 1; i < stroke.Count; i++)
                {
                    Vector2 a = stroke[i - 1];
                    Vector2 b = stroke[i];
                    float len = Vector2.Distance(a, b);
                    int steps = Mathf.Max(1, Mathf.CeilToInt(len / SampleStep));
                    for (int s = 1; s <= steps; s++)
                    {
                        samples.Add(Vector2.Lerp(a, b, s / (float)steps));
                    }
                }
            }
            return samples;
        }

        private static float MinDistance(Vector2 p, List<Vector2> points)
        {
            float best = float.MaxValue;
            for (int i = 0; i < points.Count; i++)
            {
                float d = (points[i] - p).sqrMagnitude;
                if (d < best) best = d;
            }
            return Mathf.Sqrt(best);
        }
    }
}
