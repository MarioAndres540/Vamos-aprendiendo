using System;
using System.Collections.Generic;
using UnityEngine;

namespace VamosAprendiendo.Challenges.Tracing
{
    /// <summary>
    /// Progreso del trazado, guardado localmente por usuario (PlayerPrefs) y sincronizado con el backend
    /// por <see cref="ProgressSyncService"/>. El almacenamiento local es la fuente de verdad mientras no hay red.
    /// </summary>
    public static class TraceProgress
    {
        /// <summary>Se dispara cuando cambia el progreso (un trazo nuevo completado o datos del servidor).</summary>
        public static event Action Changed;

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
        private static string DirtyKey => $"trace.{UserKey}.pendingSync";

        public static bool IsGlyphDone(TraceGlyph glyph) => PlayerPrefs.GetInt(Key(glyph.Id), 0) == 1;
        public static bool IsGlyphDoneById(string glyphId) => PlayerPrefs.GetInt(Key(glyphId), 0) == 1;

        public static void MarkGlyphDone(TraceGlyph glyph)
        {
            if (IsGlyphDone(glyph)) return;
            PlayerPrefs.SetInt(Key(glyph.Id), 1);
            PlayerPrefs.SetInt(DirtyKey, 1);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        /// <summary>Formas que se trazan en cada casilla según la ruta elegida (solo afecta a las letras).</summary>
        public static List<TraceGlyph> ActiveGlyphs(TraceItem item, TraceCase traceCase)
        {
            if (item.Group == TraceGroup.Digits || traceCase == TraceCase.Both) return item.Glyphs;
            return new List<TraceGlyph> { item.Glyphs[traceCase == TraceCase.Lowercase ? 1 : 0] };
        }

        public static bool IsItemDone(TraceItem item, TraceCase traceCase = TraceCase.Both)
        {
            foreach (var g in ActiveGlyphs(item, traceCase))
            {
                if (!IsGlyphDone(g)) return false;
            }
            return true;
        }

        public static bool IsUnlocked(TraceItem item, TraceCase traceCase = TraceCase.Both)
        {
            if (string.IsNullOrEmpty(item.RequiresId)) return true;
            var required = TraceCatalog.Find(item.RequiresId);
            return required == null || IsItemDone(required, traceCase);
        }

        // ---------- sincronización ----------

        /// <summary>Ids de todas las formas completadas (lo que se envía al backend).</summary>
        public static List<string> CompletedGlyphIds()
        {
            var ids = new List<string>();
            foreach (var group in new[] { TraceGroup.Vowels, TraceGroup.Consonants, TraceGroup.Digits })
                foreach (var item in TraceCatalog.GetGroup(group))
                    foreach (var g in item.Glyphs)
                        if (IsGlyphDone(g)) ids.Add(g.Id);
            return ids;
        }

        /// <summary>Suma lo que el servidor ya tenía (nunca borra lo local). Devuelve true si aprendió algo nuevo.</summary>
        public static bool MergeCompleted(IEnumerable<string> serverIds)
        {
            if (serverIds == null) return false;
            var known = new HashSet<string>();
            foreach (var group in new[] { TraceGroup.Vowels, TraceGroup.Consonants, TraceGroup.Digits })
                foreach (var item in TraceCatalog.GetGroup(group))
                    foreach (var g in item.Glyphs) known.Add(g.Id);

            bool changed = false;
            foreach (var id in serverIds)
            {
                if (string.IsNullOrEmpty(id) || !known.Contains(id)) continue;
                if (PlayerPrefs.GetInt(Key(id), 0) == 1) continue;
                PlayerPrefs.SetInt(Key(id), 1);
                changed = true;
            }
            if (changed)
            {
                PlayerPrefs.Save();
                Changed?.Invoke();
            }
            return changed;
        }

        /// <summary>Hay progreso local que el backend todavía no recibió.</summary>
        public static bool HasPendingSync => PlayerPrefs.GetInt(DirtyKey, 0) == 1;

        public static void ClearPendingSync()
        {
            PlayerPrefs.DeleteKey(DirtyKey);
            PlayerPrefs.Save();
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
            Changed?.Invoke();
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
            Changed?.Invoke();
        }
    }
}
