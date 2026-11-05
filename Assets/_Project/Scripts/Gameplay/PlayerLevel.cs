using System;
using UnityEngine;

namespace VamosAprendiendo.Gameplay
{
    /// <summary>
    /// Nivel del jugador. El nivel 1 reúne todas las actividades (números, vocales y consonantes): al cobrar la experiencia de todas
    /// se abre el portal; al cruzarlo se activa la plataforma verde, que sube al nivel 2. El nivel se guarda en el dispositivo por
    /// cuenta e hijo, igual que el progreso local.
    /// </summary>
    public static class PlayerLevel
    {
        /// <summary>XP que entrega cada ejercicio aprobado (regla del backend).</summary>
        public const int XpPerExercise = 10;

        public static event Action Changed;

        /// <summary>Experiencia total del nivel 1: la que se logra al cobrar todas las actividades.</summary>
        public static int Level1Xp => MissionRewards.TotalExercises() * XpPerExercise;

        public static int Level => Mathf.Max(1, PlayerPrefs.GetInt(Key("level"), 1));

        /// <summary>El portal ya se cruzó: la plataforma verde queda activa.</summary>
        public static bool PortalPassed => Level >= 2 || PlayerPrefs.GetInt(Key("portal"), 0) == 1;

        /// <summary>Todas las actividades del nivel 1 están hechas y su experiencia cobrada en el cofre.</summary>
        public static bool Level1Completed =>
            Level >= 2 || DebugUnlock || MissionRewards.ClaimedCount() >= MissionRewards.TotalExercises();

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
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        public static void SetLevel(int level)
        {
            PlayerPrefs.SetInt(Key("level"), Mathf.Max(1, level));
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        /// <summary>Para pruebas: vuelve al nivel 1 con el portal sin cruzar.</summary>
        public static void Reset()
        {
            PlayerPrefs.DeleteKey(Key("level"));
            PlayerPrefs.DeleteKey(Key("portal"));
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        private static string Key(string name) => $"level.{name}.{MissionRewards.Scope()}";
    }
}
