using UnityEngine;

namespace VamosAprendiendo.Challenges.Tracing
{
    /// <summary>
    /// Progreso del trazado, guardado localmente por usuario (PlayerPrefs).
    /// Más adelante se puede sincronizar con el backend desde aquí.
    /// </summary>
    public static class TraceProgress
    {
        private static string UserKey
        {
            get
            {
                var session = PlayerSession.Instance;
                return session != null && session.CurrentUser != null && !string.IsNullOrEmpty(session.CurrentUser.id)
                    ? session.CurrentUser.id
                    : "guest";
            }
        }

        private static string Key(string glyphId) => $"trace.{UserKey}.{glyphId}";

        public static bool IsGlyphDone(TraceGlyph glyph) => PlayerPrefs.GetInt(Key(glyph.Id), 0) == 1;

        public static void MarkGlyphDone(TraceGlyph glyph)
        {
            PlayerPrefs.SetInt(Key(glyph.Id), 1);
            PlayerPrefs.Save();
        }

        public static bool IsItemDone(TraceItem item)
        {
            foreach (var g in item.Glyphs)
            {
                if (!IsGlyphDone(g)) return false;
            }
            return true;
        }

        public static bool IsUnlocked(TraceItem item)
        {
            if (string.IsNullOrEmpty(item.RequiresId)) return true;
            var required = TraceCatalog.Find(item.RequiresId);
            return required == null || IsItemDone(required);
        }

        public static void ResetAll()
        {
            foreach (var group in new[] { TraceGroup.Vowels, TraceGroup.Consonants, TraceGroup.Digits })
            {
                foreach (var item in TraceCatalog.GetGroup(group))
                {
                    foreach (var g in item.Glyphs) PlayerPrefs.DeleteKey(Key(g.Id));
                }
            }
            PlayerPrefs.Save();
        }

        public static void CompleteAll()
        {
            foreach (var group in new[] { TraceGroup.Vowels, TraceGroup.Consonants, TraceGroup.Digits })
            {
                foreach (var item in TraceCatalog.GetGroup(group))
                {
                    foreach (var g in item.Glyphs) PlayerPrefs.SetInt(Key(g.Id), 1);
                }
            }
            PlayerPrefs.Save();
        }
    }
}
