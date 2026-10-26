#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace VamosAprendiendo.EditorTools
{
    /// <summary>
    /// Convierte a URP los materiales de paquetes externos que salen rosados: Standard / Standard (Specular setup),
    /// Legacy Shaders, Mobile/Unlit y los de HDRP Lit. Conserva textura, color, normales, emisión, metálico/brillo
    /// y el modo de transparencia. Se hace en el propio material (como el convertidor de Unity).
    /// </summary>
    public static class PackageMaterialConverter
    {
        private static readonly string[] Folders =
        {
            "Assets/_ThirdParty/Vegetation/A_piece_of_nature", "Assets/_ThirdParty/Textures/Blink", "Assets/_ThirdParty/Animals/Butterfly (Animated)", "Assets/_ThirdParty/Animals/Cartoon Animal Pack 01",
            "Assets/_ThirdParty/Vegetation/Free!Low_Poly_Boxy-Stylized_Trees#0", "Assets/_ThirdParty/VFX/Hovl Studio", "Assets/_ThirdParty/Characters/Kevin Iglesias",
            "Assets/_ThirdParty/Props/Reversed Interactive", "Assets/_ThirdParty/VFX/SineVFX", "Assets/_ThirdParty/Textures/Texture_Me", "Assets/_ThirdParty/Textures/YughuesFreeSandMaterials",
            "Assets/_ThirdParty/Buildings/Selcuk Gerceker", "Assets/_ThirdParty/Buildings/Mushroom house", "Assets/_ThirdParty/Vegetation/Whaleride", "Assets/_ThirdParty/Vegetation/Fantasy Forest Environment Free Sample",
            "Assets/_ThirdParty/Buildings/TriForge Assets", "Assets/_ThirdParty/Vegetation/Polytope Studio", "Assets/_ThirdParty/Vegetation/IL3DN", "Assets/_ThirdParty/Textures/YughuesFreeBarkMaterial"
        };

        // Paquetes cuyos shaders propios son de Built-in: se convierten también (los de SineVFX se dejan como están).
        private static readonly string[] CustomShaderFolders =
        {
            "Assets/_ThirdParty/Buildings/Mushroom house", "Assets/_ThirdParty/Vegetation/Fantasy Forest Environment Free Sample", "Assets/_ThirdParty/Buildings/TriForge Assets",
            "Assets/_ThirdParty/Vegetation/Polytope Studio", "Assets/_ThirdParty/Vegetation/IL3DN", "Assets/_ThirdParty/Buildings/Selcuk Gerceker", "Assets/_ThirdParty/Vegetation/Whaleride"
        };

        [MenuItem("Tools/Materials/Convert Package Materials To URP", false, 60)]
        public static void ConvertAll()
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            var unlit = Shader.Find("Universal Render Pipeline/Unlit");
            var report = new StringBuilder();
            int converted = 0, skipped = 0;

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var guid in AssetDatabase.FindAssets("t:Material", Folders))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (m == null) continue;

                    string kind = Classify(m, path);
                    if (kind == null) { skipped++; continue; }

                    if (kind == "unlit") ConvertUnlit(m, unlit);
                    else if (kind == "hdrp") ConvertHdrp(m, lit);
                    else if (kind == "custom") ConvertCustom(m, lit);
                    else ConvertStandard(m, lit, kind == "specular");
                    EditorUtility.SetDirty(m);
                    converted++;
                    report.AppendLine($"{kind,-9} {path.Replace("Assets/", "")}");
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[MaterialConverter] Convertidos: {converted}. Sin cambios: {skipped}.\n{report}");
        }

        // null = no necesita conversión (ya es URP, es un shader propio que sí funciona, o no es de este tipo)
        private static string Classify(Material m, string path)
        {
            var sh = m.shader;
            string n = sh != null ? sh.name : "";

            if (n == "Standard") return "standard";
            if (n == "Standard (Specular setup)") return "specular";
            if (n.StartsWith("Legacy Shaders/")) return "standard";
            if (n.StartsWith("Mobile/Unlit")) return "unlit";

            // HDRP Lit: shader no disponible en URP + palabras clave propias de HDRP
            bool broken = sh == null || n.Contains("InternalError") || !sh.isSupported;
            if (broken && (SavedHas(m, "_BaseColorMap") || SavedHas(m, "_BaseColor") || HasKeyword(m, "_BLENDMODE") || HasKeyword(m, "_DOUBLESIDED_ON")
                           || m.HasProperty("_MaskMap") || m.HasProperty("_StencilRef") || HasKeywordPrefix(m, "_MATERIAL_FEATURE")))
                return "hdrp";
            // Shader propio de Built-in (sin pase URP) en uno de los paquetes nuevos
            if (sh != null && sh.isSupported && IsInFolders(path, CustomShaderFolders) && !ShaderIsUrp(sh)
                && !n.StartsWith("Skybox/") && !n.StartsWith("Hidden/") && !n.StartsWith("Particles/") && !n.StartsWith("Unlit/")
                && !n.StartsWith("Sprites/") && !n.StartsWith("UI/") && !n.StartsWith("Nature/Terrain") && !n.StartsWith("TextMeshPro"))
                return "custom";
            return null;
        }

        private static bool IsInFolders(string path, string[] folders)
        {
            foreach (var f in folders) if (path.StartsWith(f + "/")) return true;
            return false;
        }

        // ¿El shader tiene pase de URP? Shader Graph y shaders con UniversalForward/UniversalPipeline sí.
        private static bool ShaderIsUrp(Shader s)
        {
            string p = AssetDatabase.GetAssetPath(s);
            if (string.IsNullOrEmpty(p) || p.StartsWith("Packages/")) return true;
            if (p.EndsWith(".shadergraph")) return true;
            if (!p.EndsWith(".shader")) return false;
            string text = System.IO.File.ReadAllText(p);
            return text.Contains("UniversalForward") || text.Contains("UniversalPipeline") || text.Contains("UniversalRenderPipeline");
        }

        // Lectura de propiedades guardadas en el material, sin depender de que su shader esté disponible.
        private static Texture SavedTex(Material m, string prop, out Vector2 scale, out Vector2 offset)
        {
            scale = Vector2.one; offset = Vector2.zero;
            var arr = new SerializedObject(m).FindProperty("m_SavedProperties.m_TexEnvs");
            for (int i = 0; i < arr.arraySize; i++)
            {
                var e = arr.GetArrayElementAtIndex(i);
                if (e.FindPropertyRelative("first").stringValue != prop) continue;
                var v = e.FindPropertyRelative("second");
                scale = v.FindPropertyRelative("m_Scale").vector2Value;
                offset = v.FindPropertyRelative("m_Offset").vector2Value;
                return v.FindPropertyRelative("m_Texture").objectReferenceValue as Texture;
            }
            return null;
        }

        private static float SavedFloat(Material m, string prop, float fallback)
        {
            var arr = new SerializedObject(m).FindProperty("m_SavedProperties.m_Floats");
            for (int i = 0; i < arr.arraySize; i++)
            {
                var e = arr.GetArrayElementAtIndex(i);
                if (e.FindPropertyRelative("first").stringValue == prop) return e.FindPropertyRelative("second").floatValue;
            }
            return fallback;
        }

        private static Color SavedColor(Material m, string prop, Color fallback)
        {
            var arr = new SerializedObject(m).FindProperty("m_SavedProperties.m_Colors");
            for (int i = 0; i < arr.arraySize; i++)
            {
                var e = arr.GetArrayElementAtIndex(i);
                if (e.FindPropertyRelative("first").stringValue == prop) return e.FindPropertyRelative("second").colorValue;
            }
            return fallback;
        }

        private static bool SavedHas(Material m, string prop)
        {
            foreach (var arrName in new[] { "m_TexEnvs", "m_Floats", "m_Colors" })
            {
                var arr = new SerializedObject(m).FindProperty("m_SavedProperties." + arrName);
                for (int i = 0; i < arr.arraySize; i++)
                    if (arr.GetArrayElementAtIndex(i).FindPropertyRelative("first").stringValue == prop) return true;
            }
            return false;
        }
        private static bool HasKeyword(Material m, string k)
        {
            foreach (var s in m.shaderKeywords) if (s == k) return true;
            return false;
        }

        private static bool HasKeywordPrefix(Material m, string k)
        {
            foreach (var s in m.shaderKeywords) if (s.StartsWith(k)) return true;
            return false;
        }

        private static Texture Tex(Material m, string prop) => m.HasProperty(prop) ? m.GetTexture(prop) : null;
        private static Color Col(Material m, string prop, Color fallback) => m.HasProperty(prop) ? m.GetColor(prop) : fallback;
        private static float Flt(Material m, string prop, float fallback) => m.HasProperty(prop) ? m.GetFloat(prop) : fallback;

        private static void ConvertStandard(Material m, Shader lit, bool specular)
        {
            Texture main = Tex(m, "_MainTex");
            Vector2 scale = m.HasProperty("_MainTex") ? m.GetTextureScale("_MainTex") : Vector2.one;
            Vector2 offset = m.HasProperty("_MainTex") ? m.GetTextureOffset("_MainTex") : Vector2.zero;
            Color color = Col(m, "_Color", Color.white);
            Texture bump = Tex(m, "_BumpMap");
            float bumpScale = Flt(m, "_BumpScale", 1f);
            Texture metalMap = Tex(m, "_MetallicGlossMap");
            Texture specMap = Tex(m, "_SpecGlossMap");
            float metallic = Flt(m, "_Metallic", 0f);
            float smoothness = Flt(m, "_Glossiness", Flt(m, "_Smoothness", 0.4f));
            Color emission = Col(m, "_EmissionColor", Color.black);
            Texture emissionMap = Tex(m, "_EmissionMap");
            Texture occlusion = Tex(m, "_OcclusionMap");
            float mode = Flt(m, "_Mode", 0f);
            float cutoff = Flt(m, "_Cutoff", 0.5f);

            m.shader = lit;
            m.shaderKeywords = null;

            m.SetTexture("_BaseMap", main);
            m.SetTextureScale("_BaseMap", scale);
            m.SetTextureOffset("_BaseMap", offset);
            m.SetColor("_BaseColor", color);
            if (bump != null) { m.SetTexture("_BumpMap", bump); m.SetFloat("_BumpScale", bumpScale); m.EnableKeyword("_NORMALMAP"); }
            if (occlusion != null) { m.SetTexture("_OcclusionMap", occlusion); m.EnableKeyword("_OCCLUSIONMAP"); }

            if (!specular && metalMap != null)
            {
                m.SetTexture("_MetallicGlossMap", metalMap);
                m.EnableKeyword("_METALLICSPECGLOSSMAP");
            }
            m.SetFloat("_Metallic", specular ? 0f : metallic);
            m.SetFloat("_Smoothness", specular ? Mathf.Min(smoothness, 0.6f) : smoothness);

            if (emission.maxColorComponent > 0.001f || emissionMap != null)
            {
                m.SetColor("_EmissionColor", emission);
                if (emissionMap != null) m.SetTexture("_EmissionMap", emissionMap);
                m.EnableKeyword("_EMISSION");
            }

            ApplyBlend(m, mode, cutoff, color.a < 0.999f);
        }

        private static void ConvertHdrp(Material m, Shader lit)
        {
            Vector2 scale, offset, ns, no, es, eo;
            Texture main = SavedTex(m, "_BaseColorMap", out scale, out offset);
            if (main == null) main = SavedTex(m, "_MainTex", out scale, out offset);
            Color color = SavedColor(m, "_BaseColor", Color.white);
            Texture normal = SavedTex(m, "_NormalMap", out ns, out no);
            float normalScale = SavedFloat(m, "_NormalScale", 1f);
            Texture emissiveMap = SavedTex(m, "_EmissiveColorMap", out es, out eo);
            Color emissive = SavedColor(m, "_EmissiveColor", Color.black);
            float metallic = Mathf.Min(SavedFloat(m, "_Metallic", 0f), 0.6f);
            float smoothness = SavedFloat(m, "_Smoothness", 0.4f);
            bool transparent = SavedFloat(m, "_SurfaceType", 0f) > 0.5f || HasKeyword(m, "_SURFACE_TYPE_TRANSPARENT");
            bool alphaClip = SavedFloat(m, "_AlphaCutoffEnable", 0f) > 0.5f;
            float cutoff = SavedFloat(m, "_AlphaCutoff", 0.5f);
            bool doubleSided = SavedFloat(m, "_DoubleSidedEnable", 0f) > 0.5f || HasKeyword(m, "_DOUBLESIDED_ON");
            m.shader = lit;
            m.shaderKeywords = null;

            m.SetTexture("_BaseMap", main);
            m.SetTextureScale("_BaseMap", scale);
            m.SetTextureOffset("_BaseMap", offset);
            m.SetColor("_BaseColor", color);
            if (normal != null) { m.SetTexture("_BumpMap", normal); m.SetFloat("_BumpScale", normalScale); m.EnableKeyword("_NORMALMAP"); }
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Smoothness", smoothness);

            if (emissive.maxColorComponent > 0.001f || emissiveMap != null)
            {
                m.SetColor("_EmissionColor", emissive);
                if (emissiveMap != null) m.SetTexture("_EmissionMap", emissiveMap);
                m.EnableKeyword("_EMISSION");
            }

            if (doubleSided) m.SetFloat("_Cull", 0f);
            ApplyBlend(m, transparent ? 3f : (alphaClip ? 1f : 0f), cutoff, transparent);
        }

        // Convierte un material con shader propio de Built-in a URP Lit usando las propiedades guardadas.
        private static void ConvertCustom(Material m, Shader lit)
        {
            string shaderName = m.shader.name;
            string lower = shaderName.ToLowerInvariant();
            Vector2 scale = Vector2.one, offset = Vector2.zero, tmp1, tmp2;

            Texture main = null;
            foreach (var p in new[] { "_MainTex", "_BaseMap", "_BaseTexture", "_BaseColorMap", "_Albedo", "_AlbedoTexture", "_Diffuse", "_DiffuseMap", "_Texture", "_Tex" })
            {
                main = SavedTex(m, p, out scale, out offset);
                if (main != null) break;
            }
            Color color = Color.white;
            foreach (var p in new[] { "_Color", "_BaseColor", "_Tint", "_MainColor", "_Diffuse_Color", "_ColorTint" })
            {
                if (SavedHas(m, p)) { color = SavedColor(m, p, Color.white); break; }
            }
            Texture normal = null;
            foreach (var p in new[] { "_BumpMap", "_NormalMap", "_Normal", "_NormalTexture", "_NormalMapTexture" })
            {
                normal = SavedTex(m, p, out tmp1, out tmp2);
                if (normal != null) break;
            }
            Texture emissionMap = SavedTex(m, "_EmissionMap", out tmp1, out tmp2);
            Color emission = SavedColor(m, "_EmissionColor", Color.black);
            float metallic = Mathf.Clamp(SavedFloat(m, "_Metallic", 0f), 0f, 0.6f);
            float smoothness = SavedFloat(m, "_Glossiness", SavedFloat(m, "_Smoothness", 0.25f));
            float cutoff = SavedFloat(m, "_Cutoff", 0.5f);

            bool foliage = lower.Contains("foliage") || lower.Contains("grass") || lower.Contains("branch") || lower.Contains("leaf")
                           || lower.Contains("plant") || lower.Contains("flower") || lower.Contains("pine") || lower.Contains("tree")
                           || lower.Contains("cutout") || lower.Contains("double sided") || lower.Contains("nocull") || lower.Contains("vegetation");
            bool water = lower.Contains("water");

            m.shader = lit;
            m.shaderKeywords = null;

            m.SetTexture("_BaseMap", main);
            m.SetTextureScale("_BaseMap", scale);
            m.SetTextureOffset("_BaseMap", offset);
            m.SetColor("_BaseColor", color);
            if (normal != null) { m.SetTexture("_BumpMap", normal); m.EnableKeyword("_NORMALMAP"); }
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Smoothness", water ? 0.9f : Mathf.Clamp01(smoothness));
            if (emission.maxColorComponent > 0.001f || emissionMap != null)
            {
                m.SetColor("_EmissionColor", emission);
                if (emissionMap != null) m.SetTexture("_EmissionMap", emissionMap);
                m.EnableKeyword("_EMISSION");
            }

            if (foliage) m.SetFloat("_Cull", 0f); // hojas y pasto se ven por ambos lados
            if (water)
            {
                Color c = color; c.a = Mathf.Clamp(c.a, 0.45f, 0.8f); m.SetColor("_BaseColor", c);
                ApplyBlend(m, 3f, cutoff, true);
            }
            else if (foliage && main != null) ApplyBlend(m, 1f, Mathf.Clamp(cutoff, 0.2f, 0.6f), false);
            else ApplyBlend(m, 0f, cutoff, false);
        }
        private static void ConvertUnlit(Material m, Shader unlit)
        {
            Texture main = Tex(m, "_MainTex");
            Vector2 scale = m.HasProperty("_MainTex") ? m.GetTextureScale("_MainTex") : Vector2.one;
            Vector2 offset = m.HasProperty("_MainTex") ? m.GetTextureOffset("_MainTex") : Vector2.zero;
            Color color = Col(m, "_Color", Color.white);

            m.shader = unlit;
            m.shaderKeywords = null;
            m.SetTexture("_BaseMap", main);
            m.SetTextureScale("_BaseMap", scale);
            m.SetTextureOffset("_BaseMap", offset);
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Surface", 0f);
            m.renderQueue = -1;
        }

        // mode: 0 opaco, 1 recorte (cutout), 2 desvanecer (fade), 3 transparente
        private static void ApplyBlend(Material m, float mode, float cutoff, bool hasAlpha)
        {
            if (mode == 1f)
            {
                m.SetFloat("_Surface", 0f);
                m.SetFloat("_AlphaClip", 1f);
                m.SetFloat("_Cutoff", cutoff);
                m.EnableKeyword("_ALPHATEST_ON");
                m.renderQueue = (int)RenderQueue.AlphaTest;
                m.SetOverrideTag("RenderType", "TransparentCutout");
            }
            else if (mode >= 2f)
            {
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", 0f);
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0f);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = (int)RenderQueue.Transparent;
                m.SetOverrideTag("RenderType", "Transparent");
            }
            else
            {
                m.SetFloat("_Surface", 0f);
                m.renderQueue = -1;
            }
        }
    }
}
#endif
