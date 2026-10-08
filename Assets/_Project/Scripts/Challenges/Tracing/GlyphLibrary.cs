using System.Collections.Generic;
using UnityEngine;

namespace VamosAprendiendo.Challenges.Tracing
{
    /// <summary>
    /// Trazos de cada letra (mayúscula y minúscula) y de cada dígito, en un cuadrado normalizado 0..1
    /// (x a la derecha, y hacia arriba). El orden de los trazos es el orden de escritura.
    /// Mayúsculas y dígitos usan y 0.05..0.95. Minúsculas: línea base 0.2, altura x 0.6, ascendente 0.95, descendente 0.0.
    /// </summary>
    public static class GlyphLibrary
    {
        private static readonly Dictionary<string, List<List<Vector2>>> Upper = BuildUpper();
        private static readonly Dictionary<string, List<List<Vector2>>> Lower = BuildLower();
        private static readonly Dictionary<string, List<List<Vector2>>> Digits = BuildDigits();

        public static List<List<Vector2>> GetUpper(string letter) => Upper[letter];
        public static List<List<Vector2>> GetLower(string letter) => Lower[letter];
        public static List<List<Vector2>> GetDigit(string digit) => Digits[digit];

        // ---------- helpers ----------

        private static Vector2 V(float x, float y) => new Vector2(x, y);

        private static List<Vector2> Poly(params float[] xy)
        {
            var pts = new List<Vector2>();
            for (int i = 0; i + 1 < xy.Length; i += 2) pts.Add(V(xy[i], xy[i + 1]));
            return pts;
        }

        // Arco elíptico en grados (0 = derecha, 90 = arriba). a1 < a0 recorre en sentido horario.
        private static List<Vector2> Arc(float cx, float cy, float rx, float ry, float a0, float a1)
        {
            var pts = new List<Vector2>();
            int n = Mathf.Max(8, Mathf.CeilToInt(Mathf.Abs(a1 - a0) / 12f));
            for (int i = 0; i <= n; i++)
            {
                float a = Mathf.Lerp(a0, a1, i / (float)n) * Mathf.Deg2Rad;
                pts.Add(V(cx + Mathf.Cos(a) * rx, cy + Mathf.Sin(a) * ry));
            }
            return pts;
        }

        // Une varias polilíneas en un solo trazo continuo.
        private static List<Vector2> Join(params List<Vector2>[] parts)
        {
            var all = new List<Vector2>();
            foreach (var p in parts) all.AddRange(p);
            return all;
        }

        private static List<List<Vector2>> S(params List<Vector2>[] strokes)
        {
            return new List<List<Vector2>>(strokes);
        }

        private static List<Vector2> Tilde(float cx, float y)
        {
            return Poly(cx - 0.2f, y - 0.04f, cx - 0.08f, y + 0.03f, cx + 0.08f, y - 0.03f, cx + 0.2f, y + 0.04f);
        }

        // ---------- mayúsculas ----------

        private static Dictionary<string, List<List<Vector2>>> BuildUpper()
        {
            var d = new Dictionary<string, List<List<Vector2>>>();

            d["A"] = S(Poly(0.15f, 0.05f, 0.5f, 0.95f), Poly(0.5f, 0.95f, 0.85f, 0.05f), Poly(0.3f, 0.35f, 0.7f, 0.35f));
            d["B"] = S(Poly(0.25f, 0.95f, 0.25f, 0.05f),
                       Join(Poly(0.25f, 0.95f, 0.5f, 0.95f), Arc(0.5f, 0.725f, 0.22f, 0.225f, 90, -90), Poly(0.25f, 0.5f)),
                       Join(Poly(0.25f, 0.5f, 0.5f, 0.5f), Arc(0.5f, 0.275f, 0.27f, 0.225f, 90, -90), Poly(0.25f, 0.05f)));
            d["C"] = S(Arc(0.55f, 0.5f, 0.32f, 0.45f, 45, 315));
            d["D"] = S(Poly(0.25f, 0.95f, 0.25f, 0.05f),
                       Join(Poly(0.25f, 0.95f, 0.45f, 0.95f), Arc(0.45f, 0.5f, 0.33f, 0.45f, 90, -90), Poly(0.25f, 0.05f)));
            d["E"] = S(Poly(0.25f, 0.95f, 0.25f, 0.05f), Poly(0.25f, 0.95f, 0.75f, 0.95f), Poly(0.25f, 0.5f, 0.7f, 0.5f), Poly(0.25f, 0.05f, 0.75f, 0.05f));
            d["F"] = S(Poly(0.25f, 0.95f, 0.25f, 0.05f), Poly(0.25f, 0.95f, 0.75f, 0.95f), Poly(0.25f, 0.55f, 0.65f, 0.55f));
            d["G"] = S(Join(Arc(0.5f, 0.5f, 0.33f, 0.45f, 45, 360), Poly(0.55f, 0.5f)));
            d["H"] = S(Poly(0.25f, 0.95f, 0.25f, 0.05f), Poly(0.75f, 0.95f, 0.75f, 0.05f), Poly(0.25f, 0.5f, 0.75f, 0.5f));
            d["I"] = S(Poly(0.5f, 0.95f, 0.5f, 0.05f), Poly(0.3f, 0.95f, 0.7f, 0.95f), Poly(0.3f, 0.05f, 0.7f, 0.05f));
            d["J"] = S(Join(Poly(0.62f, 0.95f, 0.62f, 0.3f), Arc(0.4f, 0.3f, 0.22f, 0.25f, 0, -165)));
            d["K"] = S(Poly(0.25f, 0.95f, 0.25f, 0.05f), Poly(0.75f, 0.95f, 0.25f, 0.45f), Poly(0.4f, 0.6f, 0.75f, 0.05f));
            d["L"] = S(Poly(0.3f, 0.95f, 0.3f, 0.05f, 0.75f, 0.05f));
            d["M"] = S(Poly(0.15f, 0.05f, 0.15f, 0.95f, 0.5f, 0.35f, 0.85f, 0.95f, 0.85f, 0.05f));
            d["N"] = S(Poly(0.2f, 0.05f, 0.2f, 0.95f, 0.8f, 0.05f, 0.8f, 0.95f));
            d["Ñ"] = S(Poly(0.2f, 0.05f, 0.2f, 0.78f, 0.8f, 0.05f, 0.8f, 0.78f), Tilde(0.5f, 0.92f));
            d["O"] = S(Arc(0.5f, 0.5f, 0.3f, 0.45f, 90, 450));
            d["P"] = S(Poly(0.25f, 0.05f, 0.25f, 0.95f),
                       Join(Poly(0.25f, 0.95f, 0.5f, 0.95f), Arc(0.5f, 0.7f, 0.25f, 0.25f, 90, -90), Poly(0.25f, 0.45f)));
            d["Q"] = S(Arc(0.5f, 0.5f, 0.3f, 0.45f, 90, 450), Poly(0.55f, 0.3f, 0.85f, 0.05f));
            d["R"] = S(Poly(0.25f, 0.05f, 0.25f, 0.95f),
                       Join(Poly(0.25f, 0.95f, 0.5f, 0.95f), Arc(0.5f, 0.7f, 0.25f, 0.25f, 90, -90), Poly(0.25f, 0.45f)),
                       Poly(0.45f, 0.45f, 0.8f, 0.05f));
            d["S"] = S(Join(Arc(0.5f, 0.73f, 0.28f, 0.22f, 30, 270), Arc(0.5f, 0.29f, 0.28f, 0.24f, 90, -150)));
            d["T"] = S(Poly(0.2f, 0.95f, 0.8f, 0.95f), Poly(0.5f, 0.95f, 0.5f, 0.05f));
            d["U"] = S(Join(Poly(0.25f, 0.95f, 0.25f, 0.4f), Arc(0.5f, 0.4f, 0.25f, 0.35f, 180, 360), Poly(0.75f, 0.95f)));
            d["V"] = S(Poly(0.15f, 0.95f, 0.5f, 0.05f, 0.85f, 0.95f));
            d["W"] = S(Poly(0.05f, 0.95f, 0.28f, 0.05f, 0.5f, 0.7f, 0.72f, 0.05f, 0.95f, 0.95f));
            d["X"] = S(Poly(0.2f, 0.95f, 0.8f, 0.05f), Poly(0.8f, 0.95f, 0.2f, 0.05f));
            d["Y"] = S(Poly(0.2f, 0.95f, 0.5f, 0.5f), Poly(0.8f, 0.95f, 0.5f, 0.5f, 0.5f, 0.05f));
            d["Z"] = S(Poly(0.2f, 0.95f, 0.8f, 0.95f, 0.2f, 0.05f, 0.8f, 0.05f));
            return d;
        }

        // ---------- minúsculas ----------

        private static Dictionary<string, List<List<Vector2>>> BuildLower()
        {
            var d = new Dictionary<string, List<List<Vector2>>>();

            d["a"] = S(Arc(0.45f, 0.4f, 0.22f, 0.2f, 60, 420), Poly(0.67f, 0.6f, 0.67f, 0.2f));
            d["b"] = S(Poly(0.3f, 0.95f, 0.3f, 0.2f), Arc(0.5f, 0.4f, 0.2f, 0.2f, 180, -180));
            d["c"] = S(Arc(0.5f, 0.4f, 0.22f, 0.2f, 45, 315));
            d["d"] = S(Arc(0.45f, 0.4f, 0.22f, 0.2f, 60, 420), Poly(0.67f, 0.95f, 0.67f, 0.2f));
            d["e"] = S(Join(Poly(0.28f, 0.4f, 0.72f, 0.4f), Arc(0.5f, 0.4f, 0.22f, 0.2f, 0, 320)));
            d["f"] = S(Join(Arc(0.64f, 0.8f, 0.17f, 0.15f, 30, 180), Poly(0.47f, 0.2f)), Poly(0.3f, 0.6f, 0.68f, 0.6f));
            d["g"] = S(Arc(0.45f, 0.4f, 0.22f, 0.2f, 60, 420), Join(Poly(0.67f, 0.6f, 0.67f, 0.1f), Arc(0.47f, 0.1f, 0.2f, 0.1f, 0, -180)));
            d["h"] = S(Poly(0.3f, 0.95f, 0.3f, 0.2f), Join(Arc(0.5f, 0.42f, 0.2f, 0.18f, 180, 0), Poly(0.7f, 0.2f)));
            d["i"] = S(Poly(0.5f, 0.6f, 0.5f, 0.2f), Poly(0.5f, 0.82f, 0.5f, 0.8f));
            d["j"] = S(Join(Poly(0.5f, 0.6f, 0.5f, 0.1f), Arc(0.35f, 0.1f, 0.15f, 0.1f, 0, -150)), Poly(0.5f, 0.82f, 0.5f, 0.8f));
            d["k"] = S(Poly(0.3f, 0.95f, 0.3f, 0.2f), Poly(0.7f, 0.6f, 0.3f, 0.35f), Poly(0.45f, 0.45f, 0.72f, 0.2f));
            d["l"] = S(Poly(0.5f, 0.95f, 0.5f, 0.2f));
            d["m"] = S(Poly(0.2f, 0.6f, 0.2f, 0.2f),
                       Join(Arc(0.35f, 0.45f, 0.15f, 0.15f, 180, 0), Poly(0.5f, 0.2f)),
                       Join(Arc(0.65f, 0.45f, 0.15f, 0.15f, 180, 0), Poly(0.8f, 0.2f)));
            d["n"] = S(Poly(0.3f, 0.6f, 0.3f, 0.2f), Join(Arc(0.5f, 0.45f, 0.2f, 0.15f, 180, 0), Poly(0.7f, 0.2f)));
            d["ñ"] = S(Poly(0.3f, 0.6f, 0.3f, 0.2f), Join(Arc(0.5f, 0.45f, 0.2f, 0.15f, 180, 0), Poly(0.7f, 0.2f)), Tilde(0.5f, 0.8f));
            d["o"] = S(Arc(0.5f, 0.4f, 0.22f, 0.2f, 90, 450));
            d["p"] = S(Poly(0.3f, 0.6f, 0.3f, 0.0f), Arc(0.5f, 0.4f, 0.2f, 0.2f, 180, -180));
            d["q"] = S(Arc(0.45f, 0.4f, 0.22f, 0.2f, 60, 420), Poly(0.67f, 0.6f, 0.67f, 0.0f));
            d["r"] = S(Poly(0.3f, 0.6f, 0.3f, 0.2f), Arc(0.5f, 0.45f, 0.2f, 0.15f, 180, 60));
            d["s"] = S(Join(Arc(0.5f, 0.5f, 0.18f, 0.1f, 30, 270), Arc(0.5f, 0.3f, 0.2f, 0.1f, 90, -150)));
            d["t"] = S(Join(Poly(0.45f, 0.85f, 0.45f, 0.3f), Arc(0.6f, 0.3f, 0.15f, 0.1f, 180, 270)), Poly(0.3f, 0.6f, 0.68f, 0.6f));
            d["u"] = S(Join(Poly(0.3f, 0.6f, 0.3f, 0.4f), Arc(0.5f, 0.4f, 0.2f, 0.2f, 180, 360)), Poly(0.7f, 0.6f, 0.7f, 0.2f));
            d["v"] = S(Poly(0.25f, 0.6f, 0.5f, 0.2f, 0.75f, 0.6f));
            d["w"] = S(Poly(0.15f, 0.6f, 0.32f, 0.2f, 0.5f, 0.5f, 0.68f, 0.2f, 0.85f, 0.6f));
            d["x"] = S(Poly(0.28f, 0.6f, 0.72f, 0.2f), Poly(0.72f, 0.6f, 0.28f, 0.2f));
            d["y"] = S(Poly(0.28f, 0.6f, 0.5f, 0.2f), Poly(0.72f, 0.6f, 0.38f, 0.0f));
            d["z"] = S(Poly(0.28f, 0.6f, 0.72f, 0.6f, 0.28f, 0.2f, 0.72f, 0.2f));
            return d;
        }

        // ---------- dígitos ----------

        private static Dictionary<string, List<List<Vector2>>> BuildDigits()
        {
            var d = new Dictionary<string, List<List<Vector2>>>();

            d["0"] = S(Arc(0.5f, 0.5f, 0.25f, 0.45f, 90, 450));
            d["1"] = S(Poly(0.35f, 0.8f, 0.55f, 0.95f, 0.55f, 0.05f));
            d["2"] = S(Join(Arc(0.5f, 0.7f, 0.27f, 0.25f, 160, -35), Poly(0.25f, 0.05f, 0.78f, 0.05f)));
            d["3"] = S(Join(Arc(0.48f, 0.74f, 0.25f, 0.21f, 150, -90), Arc(0.48f, 0.3f, 0.28f, 0.25f, 90, -150)));
            d["4"] = S(Poly(0.65f, 0.95f, 0.2f, 0.3f, 0.8f, 0.3f), Poly(0.65f, 0.95f, 0.65f, 0.05f));
            d["5"] = S(Join(Poly(0.72f, 0.95f, 0.32f, 0.95f, 0.3f, 0.56f), Arc(0.48f, 0.35f, 0.27f, 0.27f, 130, -140)));
            d["6"] = S(Join(Poly(0.65f, 0.93f, 0.4f, 0.78f, 0.27f, 0.45f), Arc(0.5f, 0.3f, 0.25f, 0.25f, 180, 540)));
            d["7"] = S(Poly(0.2f, 0.95f, 0.8f, 0.95f, 0.4f, 0.05f));
            d["8"] = S(Arc(0.5f, 0.73f, 0.22f, 0.2f, 90, -270), Arc(0.5f, 0.29f, 0.26f, 0.24f, 90, 450));
            d["9"] = S(Join(Arc(0.5f, 0.68f, 0.25f, 0.25f, 0, 360), Poly(0.75f, 0.68f, 0.7f, 0.35f, 0.45f, 0.07f)));
            return d;
        }
    }
}
