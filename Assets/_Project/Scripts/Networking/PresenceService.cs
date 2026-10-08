using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Reporta al backend cuándo la app está en uso (sesión + latidos) para el dashboard del tutor.
/// Abre una sesión tras el login, envía un latido cada heartbeatIntervalSeconds, la cierra al pasar a segundo plano
/// o al salir, y abre otra al volver. Si no llegan latidos en timeoutSeconds, el backend la cierra por timeout.
/// </summary>
public class PresenceService : MonoBehaviour
{
    public static PresenceService Instance { get; private set; }

    [Tooltip("Se usa hasta que el backend indique su propio intervalo al abrir la sesión")]
    [SerializeField] private float defaultHeartbeatIntervalSeconds = 15f;

    private string _sessionId;
    private float _heartbeatInterval;
    private bool _starting;
    private Coroutine _heartbeatRoutine;

    public bool HasActiveSession => !string.IsNullOrEmpty(_sessionId);

    private static bool IsOffline => PlayerSession.Instance != null && PlayerSession.Instance.IsOfflineMode;
    private static bool CanReport => PlayerSession.Instance != null && PlayerSession.Instance.IsLoggedIn && !IsOffline;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        _heartbeatInterval = defaultHeartbeatIntervalSeconds;

        // En escritorio y en el Editor, sin "Run In Background" la app se pausa al perder el foco
        // (p. ej. al pasar al navegador) y los latidos se detienen. Se mantiene corriendo para que la
        // sesión siga activa mientras la app esté abierta. En el Editor también aplica al Device Simulator,
        // que se hace pasar por un móvil (Application.isMobilePlatform = true).
        if (Application.isEditor || !Application.isMobilePlatform) Application.runInBackground = true;
    }

    private IEnumerator Start()
    {
        // Play directo desde otra escena (sin pasar por el login): reabrir la sesión con el token guardado, si hay.
        if (SceneManager.GetActiveScene().name.ToLowerInvariant().Contains("login")) yield break;
        if (IsOffline) yield break;
        yield return null; // deja que el resto de servicios terminen su Awake

        if (CanReport) { StartAppSession(); yield break; }

        var auth = AuthService.Instance;
        if (auth == null) yield break;

        bool restored = false;
        yield return auth.RestoreSession(ok => restored = ok);

        if (restored)
        {
            Debug.Log("[Presence] Sesión reabierta con el token guardado.");
            StartAppSession();
        }
        else
        {
            Debug.LogWarning("[Presence] No hay sesión iniciada: entra desde LoginScene para que la app aparezca conectada en el dashboard.");
        }
    }

    // ---------- API pública ----------

    /// <summary>Abre la sesión de app (POST /presence/sessions). Llamar tras un login válido.</summary>
    public void StartAppSession()
    {
        if (!CanReport || _starting || HasActiveSession) return;
        StartCoroutine(StartSessionRoutine());
    }

    /// <summary>Cierra la sesión de app (POST /presence/sessions/{id}/end). Motivo: app_closed o logout.</summary>
    public void EndAppSession(string reason = "app_closed")
    {
        StartCoroutine(EndAppSessionRoutine(reason));
    }

    // ---------- Ciclo de vida ----------

    private void OnApplicationPause(bool paused)
    {
        // Solo en un móvil real la pausa significa que la app pasó a segundo plano. En escritorio y en el Editor
        // (incluido el Device Simulator) se dispara al perder el foco de la ventana, y eso no es salir de la app.
        if (!CanReport || Application.isEditor || !Application.isMobilePlatform) return;
        if (paused) EndAppSession("app_closed");   // móvil a segundo plano
        else StartAppSession();                    // vuelve a primer plano: sesión nueva
    }

    private void OnApplicationQuit()
    {
        // Mejor esfuerzo: si no alcanza a enviarse, el backend cierra la sesión por timeout.
        if (HasActiveSession && CanReport) EndAppSession("app_closed");
    }

    // ---------- Corrutinas ----------

    private IEnumerator StartSessionRoutine()
    {
        _starting = true;

        var session = PlayerSession.Instance;
        string body = new JsonBody()
            .Add("client", "unity")
            .Add("studentId", session.CurrentStudentId)
            .Add("deviceInfo", $"{SystemInfo.operatingSystem} · {SystemInfo.deviceModel}", 255)
            .Add("appVersion", Application.version, 50)
            .Build();

        string newSessionId = null;
        yield return ApiClient.Instance.Send("POST", "/presence/sessions", body,
            onSuccess: raw =>
            {
                var res = JsonUtility.FromJson<StartSessionResponseDto>(raw);
                newSessionId = res.sessionId;
                _heartbeatInterval = res.heartbeatIntervalSeconds > 0
                    ? Mathf.Max(5f, res.heartbeatIntervalSeconds)
                    : defaultHeartbeatIntervalSeconds;
            },
            onError: error => Debug.LogWarning($"[Presence] No se pudo abrir la sesión: {error}"));

        _starting = false;
        if (string.IsNullOrEmpty(newSessionId) || !CanReport) yield break;

        _sessionId = newSessionId;
        Debug.Log($"[Presence] Sesión de app abierta: {newSessionId} (latido cada {_heartbeatInterval:0}s).");
        StopHeartbeat();
        _heartbeatRoutine = StartCoroutine(HeartbeatLoop());
    }

    private IEnumerator HeartbeatLoop()
    {
        // Tiempo real: los latidos siguen aunque el juego esté en pausa (Time.timeScale = 0).
        while (HasActiveSession)
        {
            yield return new WaitForSecondsRealtime(_heartbeatInterval);
            if (!HasActiveSession) yield break;

            string id = _sessionId;
            ApiError? failure = null;
            yield return ApiClient.Instance.Send("POST", $"/presence/sessions/{id}/heartbeat", null,
                onSuccess: _ => { },
                onError: error => failure = error);

            if (failure == null) continue;
            var apiError = failure.Value;

            if (apiError.status == 409 || apiError.status == 404)
            {
                // SESSION_ENDED / SESSION_NOT_FOUND: la sesión terminó (p. ej. la app estuvo suspendida): abrir una nueva.
                Debug.Log($"[Presence] La sesión {id} ya no existe en el servidor ({apiError.code}): se abre una nueva.");
                _sessionId = null;
                _heartbeatRoutine = null;
                StartAppSession();
                yield break;
            }

            if (apiError.status == 401)
            {
                // El refresh ya falló: sin sesión válida no se puede seguir reportando.
                Debug.LogWarning("[Presence] Sesión expirada: se detienen los latidos.");
                _sessionId = null;
                _heartbeatRoutine = null;
                yield break;
            }

            // Sin red u otro error: se reintenta en el siguiente ciclo.
        }
    }

    public IEnumerator EndAppSessionRoutine(string reason)
    {
        if (!HasActiveSession) yield break;

        string id = _sessionId;
        _sessionId = null;
        StopHeartbeat();

        if (!CanReport) yield break; // modo de pruebas sin backend, o sesión ya expirada: nada que cerrar en el servidor

        Debug.Log($"[Presence] Cerrando sesión {id} (motivo: {reason}).");
        string body = new JsonBody().Add("reason", reason).Build();
        yield return ApiClient.Instance.Send("POST", $"/presence/sessions/{id}/end", body,
            onSuccess: _ => { },
            onError: error => Debug.LogWarning($"[Presence] No se pudo cerrar la sesión: {error}"));
    }

    private void StopHeartbeat()
    {
        if (_heartbeatRoutine != null) StopCoroutine(_heartbeatRoutine);
        _heartbeatRoutine = null;
    }
}
