using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>Una recompensa de XP (un ejercicio aprobado). Respuesta de GET /telemetry/xp.</summary>
[Serializable]
public class XpHistoryItemDto
{
    public string exerciseId;   // "escritura.lo_a", "matematicas.d_7"
    public string label;
    public int xp;
    public string awardedAt;
}

/// <summary>Un ejercicio aprobado cuyo XP todavía no se cobró en el cofre.</summary>
[Serializable]
public class XpPendingItemDto
{
    public string exerciseId;
    public int xp;
}

[Serializable]
public class XpSummaryDto
{
    public int totalXp;          // solo lo cobrado en el cofre (con un backend anterior: todo lo aprobado)
    public int rewardsCount;
    public int pendingXp;        // aprobado y por cobrar
    public XpPendingItemDto[] pending;   // null si el backend es anterior al cobro en el cofre
    public XpHistoryItemDto[] history;

    /// <summary>El backend ya separa lo cobrado de lo por cobrar.</summary>
    public bool SeparatesClaims => pending != null;
}

/// <summary>
/// Consulta la experiencia (XP) que lleva el jugador: GET /telemetry/xp. El XP lo entrega el backend al aprobar cada
/// ejercicio (<see cref="ProgressSyncService"/>); aquí solo se lee, por ejemplo para mostrarlo en la recompensa de una ruta.
/// </summary>
public static class XpService
{
    public const string Endpoint = "/telemetry/xp";
    // El backend devuelve hasta 100 recompensas recientes; hay una por ejercicio aprobado (hoy 20 en total)
    private const int HistoryLimit = 100;

    public static bool CanQuery =>
        ApiClient.Instance != null && PlayerSession.Instance != null
        && PlayerSession.Instance.IsLoggedIn && !PlayerSession.Instance.IsOfflineMode;

    /// <summary>Entrega el resumen, o null si no hay sesión con el servidor o la consulta falló.</summary>
    public static IEnumerator Fetch(Action<XpSummaryDto> onDone)
    {
        if (!CanQuery) { onDone?.Invoke(null); yield break; }

        string studentId = PlayerSession.Instance.CurrentStudentId;
        string url = Endpoint + "?limit=" + HistoryLimit
                     + (string.IsNullOrEmpty(studentId) ? "" : "&studentId=" + Uri.EscapeDataString(studentId));

        XpSummaryDto summary = null;
        yield return ApiClient.Instance.Send("GET", url, null,
            onSuccess: raw =>
            {
                try { summary = JsonUtility.FromJson<XpSummaryDto>(raw); }
                catch (Exception e) { Debug.LogWarning($"[XP] Respuesta no válida: {e.Message}"); }
            },
            onError: error => Debug.LogWarning($"[XP] No se pudo consultar la experiencia: {error.message}"));
        onDone?.Invoke(summary);
    }

    public const string ClaimEndpoint = "/telemetry/xp/claim";

    /// <summary>
    /// Igual que <see cref="Fetch"/>, pero antes declara al servidor como cobrado lo que el jugador ya cobró en el cofre y el
    /// servidor aún tiene por cobrar (cobros de antes de este cambio, o hechos sin conexión). Entrega el resumen al día.
    /// </summary>
    public static IEnumerator FetchSynced(Action<XpSummaryDto> onDone)
    {
        XpSummaryDto summary = null;
        yield return Fetch(s => summary = s);

        if (summary != null && summary.SeparatesClaims && summary.pending.Length > 0)
        {
            var claimedBackendIds = new HashSet<string>();
            foreach (var id in VamosAprendiendo.Gameplay.MissionRewards.ClaimedGlyphIds()) claimedBackendIds.Add(ProgressSyncService.ExerciseId(id));

            var toDeclare = new List<string>();
            foreach (var item in summary.pending)
                if (item != null && claimedBackendIds.Contains(item.exerciseId)) toDeclare.Add(item.exerciseId);

            if (toDeclare.Count > 0)
            {
                bool ok = false;
                yield return Claim(toDeclare, success => ok = success);
                if (ok) yield return Fetch(s => { if (s != null) summary = s; });
            }
        }
        onDone?.Invoke(summary);
    }

    /// <summary>Cobra en el servidor el XP de estos ejercicios (ids del backend, p. ej. "escritura.lo_a"). Idempotente.</summary>
    public static IEnumerator Claim(ICollection<string> backendExerciseIds, Action<bool> onDone = null)
    {
        if (!CanQuery || backendExerciseIds == null || backendExerciseIds.Count == 0) { onDone?.Invoke(false); yield break; }

        // El backend acepta hasta 100 por petición
        var all = new List<string>(backendExerciseIds);
        bool allOk = true;
        string studentId = PlayerSession.Instance.CurrentStudentId;
        for (int start = 0; start < all.Count; start += 100)
        {
            int count = Mathf.Min(100, all.Count - start);
            var sb = new StringBuilder("{");
            if (!string.IsNullOrEmpty(studentId)) sb.Append("\"studentId\":\"").Append(studentId).Append("\",");
            sb.Append("\"exerciseIds\":[");
            for (int i = 0; i < count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append('"').Append(all[start + i]).Append('"');   // solo letras, dígitos, punto y guion bajo
            }
            sb.Append("]}");

            bool ok = false;
            yield return ApiClient.Instance.Send("POST", ClaimEndpoint, sb.ToString(),
                onSuccess: _ => ok = true,
                onError: error => Debug.LogWarning($"[XP] No se pudo cobrar la experiencia ({error.status}): {error.message}"));
            allOk &= ok;
        }
        onDone?.Invoke(allOk);
    }

    /// <summary>
    /// Ejercicios aprobados y disponibles en total (suma de todas las rutas), de GET /telemetry/route-progress. Con eso se llena la
    /// barra de experiencia: no hay niveles, así que la meta es lo que hoy se puede completar. Entrega (0, 0) si no se pudo consultar.
    /// </summary>
    public static IEnumerator FetchCompletion(Action<int, int> onDone)
    {
        if (!CanQuery) { onDone?.Invoke(0, 0); yield break; }

        string studentId = PlayerSession.Instance.CurrentStudentId;
        string url = ProgressSyncService.FetchEndpoint
                     + (string.IsNullOrEmpty(studentId) ? "" : "?studentId=" + Uri.EscapeDataString(studentId));

        int completed = 0, total = 0;
        yield return ApiClient.Instance.Send("GET", url, null,
            onSuccess: raw =>
            {
                try
                {
                    var dto = JsonUtility.FromJson<RouteProgressDto>(raw);
                    if (dto?.routes == null) return;
                    foreach (var route in dto.routes) { completed += route.completed; total += route.total; }
                }
                catch (Exception e) { Debug.LogWarning($"[XP] Respuesta de progreso no válida: {e.Message}"); }
            },
            onError: error => Debug.LogWarning($"[XP] No se pudo consultar el avance: {error.message}"));
        onDone?.Invoke(completed, total);
    }

    /// <summary>XP ganado en los ejercicios indicados (ids completos del backend, p. ej. "escritura.up_B").</summary>
    public static int SumByIds(XpSummaryDto summary, ICollection<string> exerciseIds)
    {
        if (summary?.history == null || exerciseIds == null) return 0;
        int total = 0;
        foreach (var item in summary.history)
            if (item != null && item.exerciseId != null && exerciseIds.Contains(item.exerciseId)) total += item.xp;
        return total;
    }

    /// <summary>XP ganado en los ejercicios cuyo id empieza por <paramref name="exercisePrefix"/> (por ruta o forma).</summary>
    public static int SumByPrefix(XpSummaryDto summary, string exercisePrefix)
    {
        if (summary?.history == null) return 0;
        int total = 0;
        foreach (var item in summary.history)
            if (item != null && item.exerciseId != null && item.exerciseId.StartsWith(exercisePrefix, StringComparison.Ordinal))
                total += item.xp;
        return total;
    }
}
