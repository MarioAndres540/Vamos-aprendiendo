using UnityEngine;

namespace VamosAprendiendo.Challenges.Tracing
{
    /// <summary>Qué formas se trazan al entrar al reto de letras: las dos, solo minúsculas o solo mayúsculas.</summary>
    public enum TraceCase { Both, Lowercase, Uppercase }

    /// <summary>Las tarjetas de la pantalla de actividades.</summary>
    public enum ActivityKind { Numbers, LowercaseVowels, UppercaseVowels, Consonants }

    /// <summary>
    /// Lo que la pantalla de actividades le dice a la escena de trazado: qué formas trazar y a dónde volver.
    /// Se usa una sola vez (<see cref="Take"/>): si el reto se abre desde un portal, queda el comportamiento de siempre.
    /// </summary>
    public static class ActivityRoute
    {
        public static TraceCase Case { get; set; } = TraceCase.Both;
        public static string ReturnScene { get; set; }

        /// <summary>Escena del reto cuyo portal debe visitar el personaje al volver al mundo (la elige una tarjeta). Se usa una sola vez.</summary>
        public static string PendingPortalScene { get; set; }

        /// <summary>La tarjeta Consonantes abre el reto de letras directamente en la pestaña de consonantes. Se usa una sola vez.</summary>
        public static bool StartOnConsonants { get; set; }

        public static bool TakeStartOnConsonants()
        {
            bool value = StartOnConsonants;
            StartOnConsonants = false;
            return value;
        }

        public static string TakePendingPortal()
        {
            string scene = PendingPortalScene;
            PendingPortalScene = null;
            return scene;
        }

        public static (TraceCase traceCase, string returnScene) Take()
        {
            var taken = (Case, ReturnScene);
            Case = TraceCase.Both;
            ReturnScene = null;
            return taken;
        }
    }

    /// <summary>
    /// Porcentaje de cada tarjeta a partir del progreso guardado (<see cref="TraceProgress"/>).
    /// Cada vocal completada en minúscula o en mayúscula vale el 20 % de su tarjeta (5 vocales = 100 %);
    /// cada número del 0 al 9 vale el 10 %; cada consonante en mayúscula o minúscula vale 1/44 de su tarjeta.
    /// </summary>
    public static class ActivityProgress
    {
        public static int Total(ActivityKind kind)
        {
            switch (kind)
            {
                case ActivityKind.Numbers: return TraceCatalog.Digits.Count;
                case ActivityKind.LowercaseVowels:
                case ActivityKind.UppercaseVowels: return TraceCatalog.Vowels.Count;
                // Cada consonante se traza en mayúscula y en minúscula: 22 x 2 = 44 ejercicios (igual que en el backend)
                case ActivityKind.Consonants: return TraceCatalog.Consonants.Count * 2;
                default: return 0;
            }
        }

        public static int Done(ActivityKind kind)
        {
            int done = 0;
            switch (kind)
            {
                case ActivityKind.Numbers:
                    foreach (var item in TraceCatalog.Digits) if (TraceProgress.IsGlyphDone(item.Glyphs[0])) done++;
                    break;
                case ActivityKind.LowercaseVowels:
                    foreach (var item in TraceCatalog.Vowels) if (TraceProgress.IsGlyphDone(item.Glyphs[1])) done++;
                    break;
                case ActivityKind.UppercaseVowels:
                    foreach (var item in TraceCatalog.Vowels) if (TraceProgress.IsGlyphDone(item.Glyphs[0])) done++;
                    break;
                case ActivityKind.Consonants:
                    foreach (var item in TraceCatalog.Consonants)
                        foreach (var glyph in item.Glyphs) if (TraceProgress.IsGlyphDone(glyph)) done++;
                    break;
            }
            return done;
        }

        /// <summary>0 a 100. Una tarjeta sin contenido queda en 0.</summary>
        public static int Percent(ActivityKind kind)
        {
            int total = Total(kind);
            return total == 0 ? 0 : Mathf.RoundToInt(Done(kind) * 100f / total);
        }
    }
}
