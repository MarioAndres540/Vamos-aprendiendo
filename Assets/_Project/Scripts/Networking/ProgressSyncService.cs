using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using VamosAprendiendo.Challenges.Tracing;

/// <summary>Un intento de ejercicio pendiente de enviar. Se guarda en el dispositivo hasta que el backend lo recibe.</summary>
[Serializable]
public class PendingExerciseAttempt
{
    public string exerciseId;       // "escritura.lo_a", "matematicas.d_7"
    public string clientAttemptId;  // se reutiliza al reintentar: así el backend no duplica el intento ni el XP
    public string studentId;        // hijo que jugaba al hacer el intento (vacío = la propia cuenta)
    public float coverage;          // 0..1 (TraceValidator.Result.Coverage)
    public float accuracy;          // 0..1 (TraceValidator.Result.Accuracy)
}

[Serializable]
public class PendingAttemptQueue
{
    public List<PendingExerciseAttempt> items = new List<PendingExerciseAttempt>();
}

/// <summary>Respuesta de GET /telemetry/route-progress (solo lo que usa el juego).</summary>
[Serializable]
public class RouteProgressDto
{
    public RouteProgressRouteDto[] routes;
}

[Serializable]
public class RouteProgressRouteDto
{
    public string id;
    public int total;       // ejercicios disponibles en la ruta
    public int completed;   // ejercicios aprobados
    public string[] completedExerciseIds;
}

/// <summary>Respuesta de POST /telemetry/exercises (solo lo que usa el juego).</summary>
[Serializable]
public class ExerciseResultDto
{
    public bool passed;
    public bool awarded;
    public int xpAwarded;
    public int totalXp;
    public bool duplicate;
}

/// <summary>Respuesta de POST /telemetry/exercises/import (solo lo que usa el juego).</summary>
[Serializable]
public class ImportResultDto
{
    public int imported;
    public int skipped;
    public int unknown;
    public int xpAwarded;
    public int totalXp;
}

/// <summary>
/// Sincroniza el progreso de los ejercicios con el backend. Primero local: el juego siempre funciona con lo guardado
/// en el dispositivo y el servidor se pone al día cuando hay red.
/// - <see cref="ReportAttempt"/>: cada vez que se evalúa un trazo (aprobado o no) se encola un intento y se envía a
///   POST /telemetry/exercises. El backend decide si aprobó y entrega el XP una sola vez por ejercicio.
/// - <see cref="Fetch"/>: tras el login baja los ejercicios aprobados (GET /telemetry/route-progress) y los suma al local.
/// - <see cref="Push"/>: envía los intentos que quedaron pendientes (sin red, por ejemplo).
/// Contrato: vamos-aprendiendo-web/backend/docs/unity-exercises.md
/// </summary>
public static class ProgressSyncService
{
    public const string FetchEndpoint = "/telemetry/route-progress";
    public const string ExercisesEndpoint = "/telemetry/exercises";
    public const string ImportEndpoint = "/telemetry/exercises/import";

    // El backend acepta hasta 100 ejercicios por importación
    private const int MaxImportBatch = 100;

    // Tope de la cola local para no crecer sin límite si el servidor nunca responde
    private const int MaxQueued = 200;

    private static bool _flushing;

    /// <summary>XP que entregó el backend: (puntos de este intento, total acumulado).</summary>
    public static event Action<int, int> XpAwarded;

    private static bool CanSync =>
        ApiClient.Instance != null && PlayerSession.Instance != null
        && PlayerSession.Instance.IsLoggedIn && !PlayerSession.Instance.IsOfflineMode;

    /// <summary>Id del ejercicio en el backend: "<ruta>.<glyphId>". Los números van a Matemáticas y las letras a Escritura.</summary>
    public static string ExerciseId(string glyphId) =>
        (glyphId.StartsWith("d_", StringComparison.Ordinal) ? "matematicas." : "escritura.") + glyphId;

    // ---------- intentos ----------

    /// <summary>Encola un intento evaluado (aprobado o no) y lo envía en segundo plano.</summary>
    public static void ReportAttempt(string glyphId, float coverage, float accuracy)
    {
        var queue = LoadQueue();
        if (queue.items.Count >= MaxQueued) queue.items.RemoveAt(0);

        queue.items.Add(new PendingExerciseAttempt
        {
            exerciseId = ExerciseId(glyphId),
            clientAttemptId = Guid.NewGuid().ToString(),
            studentId = PlayerSession.Instance != null ? PlayerSession.Instance.CurrentStudentId : null,
            coverage = Mathf.Clamp01(coverage),
            accuracy = Mathf.Clamp01(accuracy)
        });
        SaveQueue(queue);

        if (CanSync) ApiClient.Instance.StartCoroutine(Push());
    }

    /// <summary>Baja los ejercicios aprobados del servidor y los suma al local. <paramref name="onDone"/> recibe true si el servidor respondió.</summary>
    public static IEnumerator Fetch(Action<bool> onDone = null)
    {
        if (!CanSync) { onDone?.Invoke(false); yield break; }

        string studentId = PlayerSession.Instance.CurrentStudentId;
        string endpoint = string.IsNullOrEmpty(studentId) ? FetchEndpoint : $"{FetchEndpoint}?studentId={Uri.EscapeDataString(studentId)}";

        bool ok = false;
        HashSet<string> serverIds = null;
        yield return ApiClient.Instance.Send("GET", endpoint, null,
            onSuccess: raw =>
            {
                var dto = ParseRouteProgress(raw);
                if (dto == null) return;
                serverIds = new HashSet<string>(GlyphIds(dto));
                TraceProgress.MergeCompleted(serverIds);
                ok = true;
            },
            onError: error => Debug.LogWarning($"[Progress] No se pudo bajar el progreso: {error.message}"));

        // Los intentos que quedaron sin enviar (por ejemplo, sin conexión) se envían ahora.
        if (ok) yield return Push();
        // Lo que el dispositivo ya tenía completado y el servidor no conoce (progreso de antes de esta versión) se declara una vez.
        if (ok) yield return ImportLocalCompleted(serverIds);
        onDone?.Invoke(ok);
    }

    /// <summary>
    /// Declara al backend los ejercicios que el dispositivo tiene completados y el servidor no (el progreso anterior a
    /// que Unity enviara intentos). No tienen cobertura ni exactitud: el backend los registra como "importados" y los
    /// omite si ya estaban aprobados, así que repetirlo no duplica nada.
    /// </summary>
    private static IEnumerator ImportLocalCompleted(HashSet<string> serverIds)
    {
        // Mientras haya intentos por enviar se espera: el backend los registrará con sus datos reales.
        if (LoadQueue().items.Count > 0) yield break;

        // Las consonantes ya son una ruta activa en el backend (migración 20261103_consonants_route.sql): se declaran como las demás.
        var missing = new List<string>();
        foreach (var id in TraceProgress.CompletedGlyphIds())
            if (!serverIds.Contains(id)) missing.Add(ExerciseId(id));
        if (missing.Count == 0) yield break;

        string studentId = PlayerSession.Instance.CurrentStudentId;
        for (int start = 0; start < missing.Count; start += MaxImportBatch)
        {
            var batch = missing.GetRange(start, Mathf.Min(MaxImportBatch, missing.Count - start));
            var sb = new StringBuilder("{");
            if (!string.IsNullOrEmpty(studentId)) sb.Append("\"studentId\":\"").Append(studentId).Append("\",");
            sb.Append("\"exerciseIds\":[");
            for (int i = 0; i < batch.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append('"').Append(batch[i]).Append('"'); // solo letras, dígitos, punto y guion bajo
            }
            sb.Append("]}");

            bool ok = false;
            yield return ApiClient.Instance.Send("POST", ImportEndpoint, sb.ToString(),
                onSuccess: raw => { ok = true; NotifyImport(raw); },
                onError: error => Debug.LogWarning($"[Progress] No se pudo importar el progreso anterior ({error.status}): {error.message}"));
            if (!ok) yield break; // se reintenta en el siguiente Fetch
        }
    }

    private static void NotifyImport(string raw)
    {
        try
        {
            var result = JsonUtility.FromJson<ImportResultDto>(raw);
            if (result != null && result.xpAwarded > 0) XpAwarded?.Invoke(result.xpAwarded, result.totalXp);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Progress] Respuesta de importación no válida: {e.Message}");
        }
    }

    /// <summary>Envía los intentos pendientes. Devuelve true (por <paramref name="onDone"/>) si no queda ninguno por enviar.</summary>
    public static IEnumerator Push(Action<bool> onDone = null)
    {
        if (!CanSync) { onDone?.Invoke(false); yield break; }

        // Si ya hay un envío en curso se espera a que termine y luego se revisa la cola de nuevo.
        while (_flushing) yield return null;
        _flushing = true;
        try
        {
            while (true)
            {
                var queue = LoadQueue();
                if (queue.items.Count == 0) break;

                var attempt = queue.items[0];
                bool sent = false, drop = false;
                yield return ApiClient.Instance.Send("POST", ExercisesEndpoint, BuildBody(attempt),
                    onSuccess: raw => { sent = true; NotifyReward(raw); },
                    onError: error =>
                    {
                        // 400/403/404: el backend lo rechazó y reintentarlo no cambiaría nada (ej. ejercicio aún no habilitado).
                        drop = error.status == 400 || error.status == 403 || error.status == 404;
                        Debug.LogWarning($"[Progress] Intento de {attempt.exerciseId} no enviado ({error.status}): {error.message}");
                    });

                if (!sent && !drop) break; // sin red o error del servidor: queda en la cola para el siguiente envío

                // Se relee la cola: pudo llegar otro intento mientras se esperaba la respuesta.
                queue = LoadQueue();
                queue.items.RemoveAll(i => i.clientAttemptId == attempt.clientAttemptId);
                SaveQueue(queue);
            }
        }
        finally
        {
            _flushing = false;
        }

        bool empty = LoadQueue().items.Count == 0;
        if (empty) TraceProgress.ClearPendingSync();
        onDone?.Invoke(empty);
    }

    /// <summary>Lanza <see cref="Push"/> sin esperar.</summary>
    public static void PushInBackground()
    {
        if (CanSync) ApiClient.Instance.StartCoroutine(Push());
    }

    // ---------- JSON y cola local ----------

    private static string BuildBody(PendingExerciseAttempt a)
    {
        var body = new JsonBody()
            .Add("exerciseId", a.exerciseId)
            .Add("clientAttemptId", a.clientAttemptId)
            .Add("studentId", a.studentId)
            .Build();
        // coverage y accuracy son números: se añaden aparte, con punto decimal sea cual sea el idioma del dispositivo
        var sb = new StringBuilder(body.Substring(0, body.Length - 1));
        // "R" conserva el valor exacto: redondear podría subir 0,8499 a 0,85 y aprobar de más
        sb.Append(",\"coverage\":").Append(a.coverage.ToString("R", CultureInfo.InvariantCulture));
        sb.Append(",\"accuracy\":").Append(a.accuracy.ToString("R", CultureInfo.InvariantCulture));
        return sb.Append('}').ToString();
    }

    private static void NotifyReward(string raw)
    {
        try
        {
            var result = JsonUtility.FromJson<ExerciseResultDto>(raw);
            if (result != null && result.awarded && !result.duplicate) XpAwarded?.Invoke(result.xpAwarded, result.totalXp);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Progress] Respuesta de ejercicio no válida: {e.Message}");
        }
    }

    private static RouteProgressDto ParseRouteProgress(string raw)
    {
        try { return JsonUtility.FromJson<RouteProgressDto>(raw); }
        catch (Exception e)
        {
            Debug.LogWarning($"[Progress] Respuesta de progreso no válida: {e.Message}");
            return null;
        }
    }

    // "escritura.lo_a" -> "lo_a" (los ids locales no llevan la ruta)
    private static IEnumerable<string> GlyphIds(RouteProgressDto dto)
    {
        if (dto.routes == null) yield break;
        foreach (var route in dto.routes)
        {
            if (route.completedExerciseIds == null) continue;
            foreach (var id in route.completedExerciseIds)
            {
                int dot = id != null ? id.IndexOf('.') : -1;
                if (dot >= 0) yield return id.Substring(dot + 1);
            }
        }
    }

    // La cola es por usuario (igual que el progreso local) para no mezclar cuentas en un mismo dispositivo.
    private static string QueueKey
    {
        get
        {
            var session = PlayerSession.Instance;
            string user = session != null && session.CurrentUser != null && !string.IsNullOrEmpty(session.CurrentUser.id)
                ? session.CurrentUser.id : "guest";
            return $"attempts.{user}.queue";
        }
    }

    private static PendingAttemptQueue LoadQueue()
    {
        try
        {
            string raw = PlayerPrefs.GetString(QueueKey, "");
            if (!string.IsNullOrEmpty(raw))
            {
                var queue = JsonUtility.FromJson<PendingAttemptQueue>(raw);
                if (queue != null && queue.items != null) return queue;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Progress] La cola de intentos no se pudo leer y se reinicia: {e.Message}");
        }
        return new PendingAttemptQueue();
    }

    private static void SaveQueue(PendingAttemptQueue queue)
    {
        if (queue.items.Count == 0) PlayerPrefs.DeleteKey(QueueKey);
        else PlayerPrefs.SetString(QueueKey, JsonUtility.ToJson(queue));
        PlayerPrefs.Save();
    }
}
