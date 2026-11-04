using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using VamosAprendiendo.Challenges.Tracing;
using VamosAprendiendo.UI;

namespace VamosAprendiendo.Gameplay
{
    /// <summary>
    /// Signo "?" de fin de misión, sobre el cofre. Está oculto hasta que hay ejercicios completados sin cobrar (no hace falta
    /// terminar la ruta); cuando todo está cobrado, vuelve a ocultarse. Al tocarlo vuela al cofre, lo abre con un efecto mágico y
    /// entrega la experiencia (XP) de esos ejercicios (<see cref="MissionRewardPanel"/>). La experiencia solo cuenta en el juego
    /// desde ese momento: antes de tocar el cofre el HUD no la suma.
    /// </summary>
    public class MissionSign : MissionMarker
    {
        [Header("Cofre")]
        [SerializeField] private HingeOpener chest;
        [Tooltip("Punto sobre el cofre al que vuela el signo y donde estalla el efecto. Si falta, se usa el cofre.")]
        [SerializeField] private Transform chestTarget;

        [Header("Recompensa")]
        [SerializeField] private MissionRewardPanel panelPrefab;

        [Tooltip("Estallido al abrirse el cofre")]
        [SerializeField] private GameObject openPrefab;

        // Hay ejercicios completados cuya experiencia aún no se cobró
        protected override bool ShouldShow() => MissionRewards.HasPending();

        protected override void OnTapped()
        {
            if (!MissionRewards.HasPending()) return;
            StartCoroutine(RewardRoutine());
        }

        private IEnumerator RewardRoutine()
        {
            Busy = true;
            SetColliders(false);

            // 1) El signo vuela al cofre mientras éste se abre
            if (chest != null) chest.Open();
            Vector3 target = chestTarget != null ? chestTarget.position : (chest != null ? chest.transform.position + Vector3.up * 1.2f : HomePosition);
            Vector3 from = transform.position;
            Quaternion fromRot = transform.rotation;
            Vector3 fromScale = transform.localScale;
            const float flight = 0.8f;
            for (float t = 0f; t < flight; t += Time.deltaTime)
            {
                float k = t / flight;
                float ease = k * k * (3f - 2f * k);
                transform.position = Vector3.Lerp(from, target - (Center - transform.position), ease) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.9f;
                transform.rotation = Quaternion.Euler(0f, k * 720f, 0f) * fromRot;
                transform.localScale = Vector3.Lerp(fromScale, HomeScale * 0.15f, ease);
                KeepAuraOnSign();
                yield return null;
            }
            HideNow();
            // Se fija qué ejercicios entran en este cofre: lo que se termine mientras tanto queda para el siguiente
            List<string> claimIds = MissionRewards.AllUnclaimed();
            string label = MissionRewards.RewardLabel();

            // 2) Estalla la magia sobre el cofre abierto
            Spawn(openPrefab, target, 2.2f);
            yield return new WaitForSeconds(0.6f);

            // 3) Panel con la experiencia de la ruta; la cifra llega del servidor mientras entra el panel
            MissionRewardPanel panel = panelPrefab != null ? Instantiate(panelPrefab) : null;
            if (panel != null)
            {
                panel.Open(label);
                XpSummaryDto summary = null;
                bool asked = false;
                StartCoroutine(XpService.Fetch(s => { summary = s; asked = true; }));
                yield return new WaitUntil(() => asked);

                if (summary != null)
                {
                    // El total que se muestra es lo ya cobrado más esto; el XP de ejercicios sin cobrar no se cuenta
                    var claimBackendIds = claimIds.ConvertAll(ProgressSyncService.ExerciseId);
                    int xp, claimedBefore;
                    if (summary.SeparatesClaims)
                    {
                        xp = 0;
                        foreach (var item in summary.pending)
                            if (item != null && claimBackendIds.Contains(item.exerciseId)) xp += item.xp;
                        claimedBefore = summary.totalXp;
                    }
                    else
                    {
                        // Backend anterior: todo el XP aprobado viene en la historia; lo cobrado se calcula con lo guardado en el dispositivo
                        xp = XpService.SumByIds(summary, claimBackendIds);
                        claimedBefore = XpService.SumByIds(summary, new List<string>(MissionRewards.ClaimedGlyphIds()).ConvertAll(ProgressSyncService.ExerciseId));
                    }
                    panel.ShowXp(xp, claimedBefore + xp);
                }
                else panel.ShowWithoutXp();

                bool closed = false;
                panel.Closed += () => closed = true;
                yield return new WaitUntil(() => closed);
            }

            // 4) Se cierra el cofre y esos ejercicios quedan cobrados; si se terminó otro mientras tanto, el signo vuelve
            MissionRewards.MarkClaimed(claimIds);
            // El servidor también lo registra como cobrado (así el dashboard web cuenta esa experiencia desde ahora).
            // Si falla (sin conexión), XpService.FetchSynced lo declara la próxima vez.
            if (ApiClient.Instance != null)
                ApiClient.Instance.StartCoroutine(XpService.Claim(claimIds.ConvertAll(ProgressSyncService.ExerciseId)));
            if (chest != null) chest.Close();
            RestoreHome();
            Busy = false;
            Refresh();
        }
    }
}
