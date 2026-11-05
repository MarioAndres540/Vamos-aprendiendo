using System;
using System.Collections;
using System.Text;
using UnityEngine;
using VamosAprendiendo.Gameplay;

/// <summary>Nivel del jugador guardado en el servidor. Respuesta de GET y PUT /telemetry/level.</summary>
[Serializable]
public class LevelDto
{
    public string studentId;
    public int level;
    public bool portalPassed;
    public bool level1Completed;
    public int completedExercises;
    public int totalExercises;
}

/// <summary>
/// El nivel del jugador vive en la base de datos (GET y PUT /telemetry/level), por cuenta e hijo, para que no se pierda al
/// cambiar de dispositivo. En el dispositivo solo queda una copia para jugar sin conexión (<see cref="PlayerLevel"/>): el avance
/// hecho sin conexión queda pendiente y se envía en la siguiente sincronización. El servidor decide: solo deja avanzar si el
/// estudiante cobró todas las actividades del nivel 1, y el nivel nunca baja.
/// </summary>
public static class LevelService
{
    public const string Endpoint = "/telemetry/level";

    public static bool CanQuery => XpService.CanQuery;

    /// <summary>
    /// Envía el avance pendiente (si lo hay) y deja la copia local igual que el servidor. No hace nada sin sesión con el servidor.
    /// </summary>
    public static IEnumerator Sync()
    {
        // La sesión puede tardar unos instantes en estar lista al arrancar la escena
        float wait = 0f;
        while ((PlayerSession.Instance == null || !PlayerSession.Instance.IsLoggedIn) && wait < 10f)
        {
            wait += 0.5f;
            yield return new WaitForSecondsRealtime(0.5f);
        }
        if (!CanQuery) yield break;

        if (PlayerLevel.PendingSync && !PlayerLevel.DebugUnlock)
        {
            // El servidor solo acepta el avance si ya tiene cobradas las actividades: se declaran los cobros hechos aquí primero
            yield return XpService.FetchSynced(null);
            yield return Push();
            yield break;
        }

        yield return Fetch();
    }

    /// <summary>Lee el nivel guardado en el servidor y lo aplica a la copia local.</summary>
    public static IEnumerator Fetch()
    {
        if (!CanQuery) yield break;
        LevelDto dto = null;
        yield return ApiClient.Instance.Send("GET", Endpoint + StudentQuery(), null,
            onSuccess: raw => dto = Parse(raw),
            onError: error => Debug.LogWarning($"[Nivel] No se pudo consultar el nivel ({error.status}): {error.message}"));
        if (dto != null) PlayerLevel.ApplyServer(dto.level, dto.portalPassed);
    }

    /// <summary>Guarda en el servidor el nivel y el portal de la copia local. Si falla, el avance sigue pendiente.</summary>
    public static IEnumerator Push()
    {
        if (!CanQuery || PlayerLevel.DebugUnlock) yield break;

        var body = new StringBuilder("{");
        string studentId = PlayerSession.Instance.CurrentStudentId;
        if (!string.IsNullOrEmpty(studentId)) body.Append("\"studentId\":\"").Append(studentId).Append("\",");
        body.Append("\"level\":").Append(PlayerLevel.Level)
            .Append(",\"portalPassed\":").Append(PlayerLevel.PortalPassed ? "true" : "false").Append('}');

        LevelDto dto = null;
        yield return ApiClient.Instance.Send("PUT", Endpoint, body.ToString(),
            onSuccess: raw => dto = Parse(raw),
            onError: error => Debug.LogWarning(
                $"[Nivel] No se pudo guardar el nivel ({error.status}): {error.message}. Se reintenta en la próxima sincronización."));
        if (dto == null) yield break;

        PlayerLevel.ClearPending();
        PlayerLevel.ApplyServer(dto.level, dto.portalPassed);
    }

    private static string StudentQuery()
    {
        string studentId = PlayerSession.Instance != null ? PlayerSession.Instance.CurrentStudentId : null;
        return string.IsNullOrEmpty(studentId) ? "" : "?studentId=" + Uri.EscapeDataString(studentId);
    }

    private static LevelDto Parse(string raw)
    {
        try { return JsonUtility.FromJson<LevelDto>(raw); }
        catch (Exception e) { Debug.LogWarning($"[Nivel] Respuesta no válida: {e.Message}"); return null; }
    }
}
