using System;
using System.Collections.Generic;
using UnityEngine;
using VamosAprendiendo.Challenges.Tracing;

namespace VamosAprendiendo.Gameplay
{
    /// <summary>
    /// Qué ejercicios completados todavía no se cobraron en el cofre. La experiencia solo se entrega al tocar el cofre (el "?"):
    /// cada ejercicio terminado queda "por cobrar" y el cofre los cobra juntos, sin esperar a que la ruta llegue al 100 %.
    /// Lo cobrado se guarda en el dispositivo por cuenta e hijo (igual que el progreso local) y es lo único que el HUD cuenta como experiencia.
    /// </summary>
    public static class MissionRewards
    {
        // En este orden se listan las rutas al cobrar (y cuentan para el signo "!" de misión nueva)
        private static readonly ActivityKind[] Routes =
            { ActivityKind.LowercaseVowels, ActivityKind.UppercaseVowels, ActivityKind.Numbers, ActivityKind.Consonants };

        /// <summary>Se dispara al cobrar.</summary>
        public static event Action Changed;

        // ---------- ejercicios de cada ruta ----------

        /// <summary>Ids locales ("d_4", "lo_a", "up_A") de los ejercicios de una ruta.</summary>
        public static List<string> GlyphIds(ActivityKind kind)
        {
            var ids = new List<string>();
            switch (kind)
            {
                case ActivityKind.Numbers:
                    foreach (var item in TraceCatalog.Digits) ids.Add(item.Glyphs[0].Id);
                    break;
                case ActivityKind.LowercaseVowels:
                    foreach (var item in TraceCatalog.Vowels) ids.Add(item.Glyphs[1].Id);
                    break;
                case ActivityKind.UppercaseVowels:
                    foreach (var item in TraceCatalog.Vowels) ids.Add(item.Glyphs[0].Id);
                    break;
                case ActivityKind.Consonants:
                    foreach (var item in TraceCatalog.Consonants)
                        foreach (var glyph in item.Glyphs) ids.Add(glyph.Id);
                    break;
            }
            return ids;
        }

        /// <summary>Todos los ejercicios del juego (para la barra de experiencia).</summary>
        public static int TotalExercises()
        {
            int total = 0;
            foreach (var route in Routes) total += GlyphIds(route).Count;
            return total;
        }

        /// <summary>Cuántos ejercicios del juego ya se cobraron (para llenar la barra de experiencia).</summary>
        public static int ClaimedCount()
        {
            var claimed = ClaimedGlyphIds();
            int count = 0;
            foreach (var route in Routes)
                foreach (var id in GlyphIds(route))
                    if (claimed.Contains(id)) count++;
            return count;
        }

        // ---------- por cobrar ----------

        /// <summary>Ejercicios completados de una ruta que aún no se cobraron.</summary>
        public static List<string> Unclaimed(ActivityKind kind)
        {
            var claimed = ClaimedGlyphIds();
            var result = new List<string>();
            foreach (var id in GlyphIds(kind))
                if (TraceProgress.IsGlyphDoneById(id) && !claimed.Contains(id)) result.Add(id);
            return result;
        }

        /// <summary>Todo lo que el cofre cobraría ahora, de todas las rutas.</summary>
        public static List<string> AllUnclaimed()
        {
            var claimed = ClaimedGlyphIds();
            var result = new List<string>();
            foreach (var route in Routes)
                foreach (var id in GlyphIds(route))
                    if (TraceProgress.IsGlyphDoneById(id) && !claimed.Contains(id)) result.Add(id);
            return result;
        }

        public static bool HasPending() => AllUnclaimed().Count > 0;

        /// <summary>Nombre de lo que se cobra: la ruta, o "Tus rutas" si el cofre cobra de varias a la vez.</summary>
        public static string RewardLabel()
        {
            int routesWithPending = 0;
            ActivityKind first = default;
            foreach (var route in Routes)
            {
                if (Unclaimed(route).Count == 0) continue;
                if (routesWithPending++ == 0) first = route;
            }
            return routesWithPending == 1 ? RouteName(first) : "Tus rutas";
        }

        // ---------- lo cobrado ----------

        /// <summary>Ids locales de los ejercicios ya cobrados (incluye lo cobrado con versiones anteriores del juego).</summary>
        public static HashSet<string> ClaimedGlyphIds()
        {
            var claimed = new HashSet<string>(PlayerPrefs.GetString(ClaimedKey(), "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));

            // Versiones anteriores: una ruta cobrada completa guardaba "1"; las consonantes, la lista de ids
            foreach (var route in Routes)
            {
                string legacy = LegacyKey(route);
                if (route == ActivityKind.Consonants)
                {
                    foreach (var id in PlayerPrefs.GetString(legacy, "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)) claimed.Add(id);
                }
                else if (PlayerPrefs.GetInt(legacy, 0) == 1)
                {
                    foreach (var id in GlyphIds(route)) claimed.Add(id);
                }
            }
            return claimed;
        }

        /// <summary>Marca como cobrados los ejercicios indicados (los que se mostraron en el panel de recompensa).</summary>
        public static void MarkClaimed(ICollection<string> glyphIds)
        {
            var all = ClaimedGlyphIds();
            foreach (var id in glyphIds) all.Add(id);
            PlayerPrefs.SetString(ClaimedKey(), string.Join(",", all));
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        /// <summary>Solo para pruebas: deja todo sin cobrar.</summary>
        public static void ResetClaims()
        {
            PlayerPrefs.DeleteKey(ClaimedKey());
            foreach (var route in Routes) PlayerPrefs.DeleteKey(LegacyKey(route));
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        // ---------- misión nueva ("!") ----------

        /// <summary>
        /// ¿Hay una ruta nueva por empezar y el jugador no está a mitad de otra? Es verdadero al empezar (ninguna ruta hecha) y
        /// al terminar la ruta en la que se estaba, cuando queda otra sin empezar. Es falso mientras hay una ruta empezada sin
        /// terminar y cuando ya están todas completas.
        /// </summary>
        public static bool HasNewMissionAvailable() => HasNewMissionAvailable(ActivityProgress.Percent);

        /// <param name="percentOf">Porcentaje (0 a 100) de cada ruta; se inyecta para poder comprobar la regla sin tocar el progreso real.</param>
        public static bool HasNewMissionAvailable(Func<ActivityKind, int> percentOf)
        {
            bool anyNotStarted = false, anyInProgress = false;
            foreach (var route in Routes)
            {
                int percent = percentOf(route);
                if (percent <= 0) anyNotStarted = true;
                else if (percent < 100) anyInProgress = true;
            }
            return anyNotStarted && !anyInProgress;
        }

        public static string RouteName(ActivityKind kind)
        {
            switch (kind)
            {
                case ActivityKind.Numbers: return "Números";
                case ActivityKind.LowercaseVowels: return "Letras minúsculas";
                case ActivityKind.UppercaseVowels: return "Letras mayúsculas";
                case ActivityKind.Consonants: return "Consonantes";
                default: return "Ruta";
            }
        }

        // ---------- claves ----------

        private static string Scope()
        {
            var session = PlayerSession.Instance;
            string user = session != null && session.CurrentUser != null && !string.IsNullOrEmpty(session.CurrentUser.id)
                ? session.CurrentUser.id : "guest";
            string student = session != null && !string.IsNullOrEmpty(session.CurrentStudentId) ? session.CurrentStudentId : "own";
            return $"{user}.{student}";
        }

        private static string ClaimedKey() => $"mission.claimedIds.{Scope()}";
        private static string LegacyKey(ActivityKind kind) => $"mission.claimed.{Scope()}.{kind}";
    }
}
