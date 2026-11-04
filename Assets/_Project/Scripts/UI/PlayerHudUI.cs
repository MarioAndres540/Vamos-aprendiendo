using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VamosAprendiendo.Gameplay;

namespace VamosAprendiendo.UI
{
    /// <summary>
    /// HUD de la esquina superior izquierda: nombre de quien juega y barra de experiencia con los puntos dentro.
    /// Si la cuenta es de un tutor se muestra el nombre del hijo que juega; si no, el de la propia cuenta.
    /// La experiencia solo cuenta cuando se cobra en el cofre: el número es el XP de los ejercicios ya cobrados y la barra se llena
    /// con los ejercicios cobrados sobre los disponibles (no hay niveles). Lo que se completa y aún no se cobra no suma.
    /// </summary>
    public class PlayerHudUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private Image fill;
        [SerializeField] private TMP_Text xpLabel;
        [Tooltip("Tiempo de juego de esta sesión (desde que la cuenta entró a la app)")]
        [SerializeField] private TMP_Text timeLabel;
        [Tooltip("Segundos que tarda la barra y el número en llegar al valor nuevo")]
        [SerializeField] private float animationSeconds = 0.8f;

        private int _shownXp;
        private float _shownFill;
        private int _targetXp;
        private float _targetFill;
        private Coroutine _animation;

        // El reloj de la sesión sobrevive a los cambios de escena y se reinicia al entrar otra cuenta o hijo
        private static float _sessionStart = -1f;
        private static string _sessionOwner;
        private int _shownSeconds = -1;

        private void OnEnable()
        {
            MissionRewards.Changed += OnClaimed;
            Refresh();
            EnsureClock();
            ShowTime();
        }

        private void Update()
        {
            // Tiempo sin escala: sigue contando aunque el juego esté en pausa o el tutorial lo detenga
            ShowTime();
        }

        private static string Owner()
        {
            var session = PlayerSession.Instance;
            if (session == null) return "guest";
            string user = session.CurrentUser != null && !string.IsNullOrEmpty(session.CurrentUser.id) ? session.CurrentUser.id : "guest";
            return user + "." + (session.CurrentStudentId ?? "own");
        }

        private static void EnsureClock()
        {
            string owner = Owner();
            if (_sessionStart < 0f || owner != _sessionOwner)
            {
                _sessionStart = Time.realtimeSinceStartup;
                _sessionOwner = owner;
            }
        }

        private void ShowTime()
        {
            if (timeLabel == null || _sessionStart < 0f) return;
            int seconds = Mathf.Max(0, Mathf.FloorToInt(Time.realtimeSinceStartup - _sessionStart));
            if (seconds == _shownSeconds) return;
            _shownSeconds = seconds;
            int h = seconds / 3600, m = seconds % 3600 / 60, s = seconds % 60;
            timeLabel.text = h > 0 ? $"Tiempo  {h}:{m:00}:{s:00}" : $"Tiempo  {m:00}:{s:00}";
        }

        private void OnDisable()
        {
            MissionRewards.Changed -= OnClaimed;
            _animation = null;
        }

        /// <summary>Vuelve a leer el nombre y la experiencia (por ejemplo al cambiar de hijo).</summary>
        public void Refresh()
        {
            if (nameLabel != null) nameLabel.text = ResolveName();
            StartCoroutine(Load());
        }

        private IEnumerator Load()
        {
            // La sesión puede tardar unos instantes en estar lista al arrancar la escena
            float wait = 0f;
            while ((PlayerSession.Instance == null || !PlayerSession.Instance.IsLoggedIn) && wait < 10f)
            {
                wait += 0.5f;
                yield return new WaitForSecondsRealtime(0.5f);
            }
            if (nameLabel != null) nameLabel.text = ResolveName();

            if (!XpService.CanQuery)
            {
                SetTargets(0, 0f, offline: true);
                yield break;
            }

            XpSummaryDto summary = null;
            yield return XpService.FetchSynced(s => summary = s);
            if (summary == null) { SetTargets(0, 0f, offline: true); yield break; }

            // Solo cuenta lo cobrado en el cofre. Un backend que ya separa lo cobrado lo trae en totalXp; con uno anterior
            // se calcula con los ejercicios cobrados guardados en el dispositivo.
            int xp = summary.totalXp;
            if (!summary.SeparatesClaims)
            {
                var claimedIds = new System.Collections.Generic.List<string>();
                foreach (var id in MissionRewards.ClaimedGlyphIds()) claimedIds.Add(ProgressSyncService.ExerciseId(id));
                xp = XpService.SumByIds(summary, claimedIds);
            }
            int total = MissionRewards.TotalExercises();
            float fraction = total > 0 ? Mathf.Clamp01((float)MissionRewards.ClaimedCount() / total) : 0f;
            SetTargets(xp, fraction, offline: false);
        }

        private void OnClaimed() => StartCoroutine(Load());

        private void SetTargets(int xp, float fraction, bool offline)
        {
            _targetXp = xp;
            _targetFill = fraction;
            if (xpLabel != null && offline && _shownXp == 0) xpLabel.text = "Sin conexión";
            if (offline) { if (fill != null) fill.fillAmount = 0f; return; }
            if (_animation != null) StopCoroutine(_animation);
            _animation = StartCoroutine(Animate());
        }

        private IEnumerator Animate()
        {
            int startXp = _shownXp;
            float startFill = _shownFill;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / Mathf.Max(0.05f, animationSeconds);
                float eased = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
                _shownXp = Mathf.RoundToInt(Mathf.Lerp(startXp, _targetXp, eased));
                _shownFill = Mathf.Lerp(startFill, _targetFill, eased);
                Draw();
                yield return null;
            }
            _shownXp = _targetXp;
            _shownFill = _targetFill;
            Draw();
            _animation = null;
        }

        private void Draw()
        {
            if (fill != null) fill.fillAmount = _shownFill;
            if (xpLabel != null) xpLabel.text = _shownXp + " XP";
        }

        public static string ResolveName()
        {
            var session = PlayerSession.Instance;
            if (session == null) return "Jugador";

            if (session.IsTutor && !string.IsNullOrEmpty(session.CurrentStudentId))
            {
                foreach (var student in StudentsService.LastStudents)
                    if (student != null && student.id == session.CurrentStudentId && !string.IsNullOrWhiteSpace(student.firstName))
                        return TitleCase(student.firstName);
            }

            string first = session.Profile?.firstName;
            if (!string.IsNullOrWhiteSpace(first)) return TitleCase(first);

            string email = session.CurrentUser?.email;
            if (!string.IsNullOrEmpty(email))
            {
                int at = email.IndexOf('@');
                return TitleCase(at > 0 ? email.Substring(0, at) : email);
            }
            return "Jugador";
        }

        // Los nombres pueden venir en minúsculas desde el registro ("matias")
        private static string TitleCase(string value)
        {
            value = value.Trim();
            int space = value.IndexOf(' ');
            if (space > 0) value = value.Substring(0, space);   // solo el primer nombre cabe en el HUD
            return char.ToUpperInvariant(value[0]) + value.Substring(1).ToLowerInvariant();
        }
    }
}
