using System.Collections.Generic;
using UnityEngine;

namespace VamosAprendiendo.Challenges.Tracing
{
    public enum TraceGroup
    {
        Vowels,
        Consonants,
        Digits
    }

    /// <summary>Una forma concreta que el niño traza: "A", "a", "7"...</summary>
    public class TraceGlyph
    {
        public string Id;        // clave de progreso: "up_A", "lo_a", "d_7"
        public string Label;     // texto para el panel: "Letra A (mayúscula)"
        public List<List<Vector2>> Strokes;

        public string[] Steps
        {
            get
            {
                if (Strokes.Count == 1)
                {
                    return new[] { "Empieza en el círculo 1.", "Sigue la línea sin soltar el dedo." };
                }
                return new[] { $"Hay {Strokes.Count} trazos.", $"Sigue los círculos del 1 al {Strokes.Count}." };
            }
        }
    }

    /// <summary>Una casilla del menú: una letra (mayúscula + minúscula) o un número.</summary>
    public class TraceItem
    {
        public string Id;          // "A", "Ñ", "7"
        public string Display;     // "A a", "7"
        public TraceGroup Group;
        public string RequiresId;  // casilla que debe estar completa antes; null = libre
        public List<TraceGlyph> Glyphs;
    }

    /// <summary>
    /// Catálogo y reglas de desbloqueo:
    /// vocales A→E→I→O→U; consonantes en orden del abecedario y la primera (B) exige la A;
    /// números 0→1→2→…→9 (el 3 exige el 2, que exige el 1, que exige el 0).
    /// </summary>
    public static class TraceCatalog
    {
        private static readonly string[] VowelIds = { "A", "E", "I", "O", "U" };
        private static readonly string[] ConsonantIds =
            { "B", "C", "D", "F", "G", "H", "J", "K", "L", "M", "N", "Ñ", "P", "Q", "R", "S", "T", "V", "W", "X", "Y", "Z" };
        private static readonly string[] DigitIds = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };

        public static readonly List<TraceItem> Vowels = BuildLetters(VowelIds, TraceGroup.Vowels, "A");
        public static readonly List<TraceItem> Consonants = BuildLetters(ConsonantIds, TraceGroup.Consonants, "A");
        public static readonly List<TraceItem> Digits = BuildDigits();

        public static List<TraceItem> GetGroup(TraceGroup group)
        {
            switch (group)
            {
                case TraceGroup.Vowels: return Vowels;
                case TraceGroup.Consonants: return Consonants;
                default: return Digits;
            }
        }

        public static TraceItem Find(string id)
        {
            foreach (var list in new[] { Vowels, Consonants, Digits })
            {
                var found = list.Find(i => i.Id == id);
                if (found != null) return found;
            }
            return null;
        }

        private static List<TraceItem> BuildLetters(string[] ids, TraceGroup group, string firstRequires)
        {
            var items = new List<TraceItem>();
            for (int i = 0; i < ids.Length; i++)
            {
                string upper = ids[i];
                string lower = upper.ToLowerInvariant();
                items.Add(new TraceItem
                {
                    Id = upper,
                    Display = $"{upper} {lower}",
                    Group = group,
                    RequiresId = i == 0 ? (group == TraceGroup.Vowels ? null : firstRequires) : ids[i - 1],
                    Glyphs = new List<TraceGlyph>
                    {
                        new TraceGlyph { Id = "up_" + upper, Label = $"Letra {upper} (mayúscula)", Strokes = GlyphLibrary.GetUpper(upper) },
                        new TraceGlyph { Id = "lo_" + lower, Label = $"Letra {lower} (minúscula)", Strokes = GlyphLibrary.GetLower(lower) }
                    }
                });
            }
            return items;
        }

        private static List<TraceItem> BuildDigits()
        {
            var items = new List<TraceItem>();
            for (int i = 0; i < DigitIds.Length; i++)
            {
                string id = DigitIds[i];
                items.Add(new TraceItem
                {
                    Id = id,
                    Display = id,
                    Group = TraceGroup.Digits,
                    RequiresId = i == 0 ? null : DigitIds[i - 1],
                    Glyphs = new List<TraceGlyph>
                    {
                        new TraceGlyph { Id = "d_" + id, Label = $"El número {id}", Strokes = GlyphLibrary.GetDigit(id) }
                    }
                });
            }
            return items;
        }
    }
}
