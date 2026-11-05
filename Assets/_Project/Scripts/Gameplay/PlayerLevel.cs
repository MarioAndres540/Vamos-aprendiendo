using System;
using UnityEngine;

namespace VamosAprendiendo.Gameplay
{
    /// <summary>
    /// Nivel del jugador. El nivel 1 reúne todas las actividades (números, vocales y consonantes): al cobrar la experiencia de todas
    /// se abre el portal; al cruzarlo se activa la plataforma que sube al nivel 2. El nivel se guarda en la base de datos
    /// (<see cref="LevelService"/>, por cuenta e hijo); aquí solo hay una copia para jugar sin conexión y una marca de avance
    /// pendiente de enviar.
    /// </summary>
    public static class PlayerLevel
    {
        /// <summary>XP que entrega cada ejercicio aprobado (regla del backend).</summary>
        public const int XpPerExercise = 10;

        public static event Action Changed;

        /// <summary>Experiencia total del nivel 1: la que se logra al cobrar todas las actividades.</summary>
        public static int Level1Xp => MissionRewards.TotalExercises() * XpPerExercise;

        public static int Level => Mathf.Max(1, PlayerPrefs.GetInt(Key("level"), 1));

        /// <summary>El portal ya se cruzó: la plataforma queda activa.</summary>
        public static bool PortalPassed => Level >= 2 || PlayerPrefs.GetInt(Key("portal"), 0) == 1;

        /// <summary>Todas las actividades del nivel 1 están hechas y su experiencia cobrada en el cofre.</summary>
        public static bool Level1Completed =>
            Level >= 2 || DebugUnlock || MissionRewards.ClaimedCount() >= MissionRewards.TotalExercises();

        /// <summary>Hay un avance de nivel guardado solo en este dispositivo, que falta enviar al servidor.</summary>
        public static bool PendingSync => PlayerPrefs.GetInt(Key("pending"), 0) == 1;

        /// <summary>Para pruebas: trata el nivel 1 como completo sin tocar el progreso real (menú Tools/Level).</summary>
        public static bool DebugUnlock
        {
            get => PlayerPrefs.GetInt("level.debugUnlock", 0) == 1;
            set { PlayerPrefs.SetInt("level.debugUnlock", value ? 1 : 0); PlayerPrefs.Save(); Changed?.Invoke(); }
        }

        public static void MarkPortalPassed()
        {
            if (PortalPassed) return;
            PlayerPrefs.SetInt(Key("portal"), 1);
            MarkPending();
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        public static void SetLevel(int level)
        {
            level = Mathf.Max(1, level);
            if (level <= Level) return;   // el nivel nunca baja
            PlayerPrefs.SetInt(Key("level"), level);
            MarkPending();
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        /// <summary>Deja la copia local igual que el servidor, que es quien manda. No toca un avance aún pendiente de enviar.</summary>
        public static void ApplyServer(int level, bool portalPassed)
        {
            if (PendingSync) return;
            level = Mathf.Max(1, level);
            if (level == Level && portalPassed == (PlayerPrefs.GetInt(Key("portal"), 0) == 1)) return;
            PlayerPrefs.SetInt(Key("level"), level);
            PlayerPrefs.SetInt(Key("portal"), portalPassed ? 1 : 0);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        public static void ClearPending()
        {
            PlayerPrefs.DeleteKey(Key("pending"));
            PlayerPrefs.Save();
        }

        /// <summary>Para pruebas: vuelve al nivel 1 con el portal sin cruzar en este dispositivo (el servidor no baja el nivel).</summary>
        public static void Reset()
        {
            PlayerPrefs.DeleteKey(Key("level"));
            PlayerPrefs.DeleteKey(Key("portal"));
            PlayerPrefs.DeleteKey(Key("pending"));
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        // Con el modo de prueba el avance no es real: no se manda al servidor
        private static void MarkPending()
        {
            if (!DebugUnlock) PlayerPrefs.SetInt(Key("pending"), 1);
        }

        private static string Key(string name) => $"level.{name}.{MissionRewards.Scope()}";
    }
}
