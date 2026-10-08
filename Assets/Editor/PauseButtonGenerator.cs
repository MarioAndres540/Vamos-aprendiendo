#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace VamosAprendiendo.EditorTools
{
    /// <summary>
    /// Genera la imagen del botón de pausa (btn_pause.png) con la paleta del menú de pausa:
    /// borde índigo, aro blanco, cara violeta-azul con brillo y símbolo de pausa blanco.
    /// </summary>
    public static class PauseButtonGenerator
    {
        private const string OUTPUT_PATH = "Assets/_Project/Art/images/btn_pause.png";
        private const string PLAY_PATH = "Assets/_Project/Art/images/btn_play.png";
        private const int SIZE = 512;
        private const int SAMPLES = 3; // supersampling por eje, para bordes suaves

        private static readonly Color Indigo = Hex("3B3DB3");
        private static readonly Color IndigoDark = Hex("2A2B8F");
        private static readonly Color RingTop = Hex("FFFFFF");
        private static readonly Color RingBottom = Hex("D5DAFF");
        private static readonly Color RimDark = Hex("4A4CC6");
        private static readonly Color FaceTop = Hex("9A86FF");
        private static readonly Color FaceBottom = Hex("4752DD");
        private static readonly Color BarTop = Hex("FFFFFF");
        private static readonly Color BarBottom = Hex("DCE0FF");

        [MenuItem("Tools/Art/Generate Pause Button", false, 50)]
        public static void Generate()
        {
            Render(false, OUTPUT_PATH);
            Render(true, PLAY_PATH);
        }

        private static void Render(bool play, string outputPath)
        {
            var pixels = new Color[SIZE * SIZE];
            for (int y = 0; y < SIZE; y++)
            {
                for (int x = 0; x < SIZE; x++)
                {
                    Color acc = Color.clear;
                    for (int sy = 0; sy < SAMPLES; sy++)
                    {
                        for (int sx = 0; sx < SAMPLES; sx++)
                        {
                            float px = x + (sx + 0.5f) / SAMPLES;
                            float py = y + (sy + 0.5f) / SAMPLES;
                            acc += Shade(px, py, play);
                        }
                    }
                    acc /= SAMPLES * SAMPLES;
                    // De premultiplicado a alpha recto, que es lo que espera el PNG.
                    if (acc.a > 0.0001f) acc = new Color(acc.r / acc.a, acc.g / acc.a, acc.b / acc.a, acc.a);
                    pixels[y * SIZE + x] = acc;
                }
            }

            var tex = new Texture2D(SIZE, SIZE, TextureFormat.RGBA32, false);
            tex.SetPixels(pixels);
            tex.Apply();

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            File.WriteAllBytes(outputPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(outputPath);
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
            Debug.Log($"[PauseButtonGenerator] Imagen generada: {outputPath}");
        }

        // Devuelve el color (premultiplicado por alpha) del punto (px,py), con y hacia arriba.
        private static Color Shade(float px, float py, bool play)
        {
            const float c = SIZE * 0.5f;
            float dx = px - c, dy = py - c;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            float v = Mathf.Clamp01((py - 16f) / (SIZE - 32f)); // 0 abajo, 1 arriba

            Color col = Color.clear;

            // Sombra proyectada, suave y desplazada hacia abajo
            float sr = Mathf.Sqrt(dx * dx + (dy + 12f) * (dy + 12f));
            float shadow = Mathf.Clamp01((246f - sr) / 22f) * 0.28f;
            col = Over(col, Premul(new Color(0.08f, 0.08f, 0.3f, 1f), shadow));

            // Borde exterior índigo
            if (r < 238f)
            {
                col = Over(col, Premul(Color.Lerp(IndigoDark, Indigo, v), Edge(238f - r)));
            }

            // Aro blanco
            if (r < 224f)
            {
                col = Over(col, Premul(Color.Lerp(RingBottom, RingTop, v), Edge(224f - r)));
            }

            // Reborde oscuro de la cara
            if (r < 186f)
            {
                col = Over(col, Premul(RimDark, Edge(186f - r)));
            }

            // Cara con degradado vertical + luz radial arriba a la izquierda
            if (r < 176f)
            {
                Color face = Color.Lerp(FaceBottom, FaceTop, v);
                float lx = px - (c - 55f), ly = py - (c + 70f);
                float glow = Mathf.Clamp01(1f - Mathf.Sqrt(lx * lx + ly * ly) / 190f);
                face = Color.Lerp(face, Hex("C9BEFF"), glow * 0.45f);
                col = Over(col, Premul(face, Edge(176f - r)));
            }

            // Brillo superior (cristal)
            if (r < 176f)
            {
                float ex = dx / 118f, ey = (py - (c + 112f)) / 52f;
                float e = 1f - Mathf.Sqrt(ex * ex + ey * ey);
                float gloss = Mathf.Clamp01(e * 6f) * Mathf.Lerp(0.15f, 0.42f, Mathf.Clamp01(e));
                col = Over(col, new Color(gloss, gloss, gloss, gloss));
            }

            // Sombra del símbolo
            var shadowColor = new Color(0.1f, 0.1f, 0.45f);
            col = Over(col, Premul(shadowColor, Symbol(px, py + 9f, play) * 0.4f));

            // Símbolo: dos barras (pausa) o triángulo (play), blancos y redondeados
            float bar = Symbol(px, py, play);
            if (bar > 0f)
            {
                float bv = Mathf.Clamp01((py - (c - 78f)) / 156f);
                col = Over(col, Premul(Color.Lerp(BarBottom, BarTop, bv), bar));
            }

            // Destello pequeño (punto) arriba a la izquierda
            float hx = px - (c - 108f), hy = py - (c + 112f);
            float dot = Mathf.Clamp01((13f - Mathf.Sqrt(hx * hx + hy * hy)) / 2f) * 0.9f;
            col = Over(col, new Color(dot, dot, dot, dot));

            return col;
        }

        // Cobertura del símbolo central: dos barras redondeadas o un triángulo redondeado.
        private static float Symbol(float px, float py, bool play)
        {
            const float c = SIZE * 0.5f;
            if (!play)
            {
                return Mathf.Max(
                    RoundRectEdge(px, py, c - 46f, c, 27f, 78f, 15f),
                    RoundRectEdge(px, py, c + 46f, c, 27f, 78f, 15f));
            }

            // Triángulo redondeado: triángulo reducido + radio, desplazado a la derecha por equilibrio óptico.
            const float r = 16f;
            var a = new Vector2(c - 38f, c + 58f);
            var b = new Vector2(c - 38f, c - 58f);
            var t = new Vector2(c + 58f, c);
            float d = TriangleDistance(new Vector2(px, py), a, b, t) - r;
            return Mathf.Clamp01(0.5f - d);
        }

        // Distancia con signo a un triángulo (negativa dentro).
        private static float TriangleDistance(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            Vector2 e0 = b - a, e1 = c - b, e2 = a - c;
            Vector2 v0 = p - a, v1 = p - b, v2 = p - c;
            Vector2 pq0 = v0 - e0 * Mathf.Clamp01(Vector2.Dot(v0, e0) / Vector2.Dot(e0, e0));
            Vector2 pq1 = v1 - e1 * Mathf.Clamp01(Vector2.Dot(v1, e1) / Vector2.Dot(e1, e1));
            Vector2 pq2 = v2 - e2 * Mathf.Clamp01(Vector2.Dot(v2, e2) / Vector2.Dot(e2, e2));
            float s = Mathf.Sign(e0.x * e2.y - e0.y * e2.x);
            float d0 = Mathf.Min(Vector2.Dot(pq0, pq0), Mathf.Min(Vector2.Dot(pq1, pq1), Vector2.Dot(pq2, pq2)));
            float s0 = Mathf.Min(s * (v0.x * e0.y - v0.y * e0.x), Mathf.Min(s * (v1.x * e1.y - v1.y * e1.x), s * (v2.x * e2.y - v2.y * e2.x)));
            return -Mathf.Sqrt(d0) * Mathf.Sign(s0);
        }

        // Cobertura antialias de un borde a distancia d (en píxeles) hacia adentro.
        private static float Edge(float d) => Mathf.Clamp01(d);

        // Cobertura de un rectángulo redondeado centrado en (cx,cy) con semiancho hw, semialto hh y radio rad.
        private static float RoundRectEdge(float px, float py, float cx, float cy, float hw, float hh, float rad)
        {
            float qx = Mathf.Abs(px - cx) - (hw - rad);
            float qy = Mathf.Abs(py - cy) - (hh - rad);
            float outside = Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f));
            float inside = Mathf.Min(Mathf.Max(qx, qy), 0f);
            float dist = outside + inside - rad;
            return Mathf.Clamp01(0.5f - dist);
        }

        private static Color Premul(Color c, float coverage)
        {
            float a = c.a * coverage;
            return new Color(c.r * a, c.g * a, c.b * a, a);
        }

        // Composición "over" con colores premultiplicados.
        private static Color Over(Color dst, Color src)
        {
            float inv = 1f - src.a;
            return new Color(src.r + dst.r * inv, src.g + dst.g * inv, src.b + dst.b * inv, src.a + dst.a * inv);
        }

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out Color c);
            return c;
        }
    }
}
#endif
