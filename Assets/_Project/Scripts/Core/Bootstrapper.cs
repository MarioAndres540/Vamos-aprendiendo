using UnityEngine;

/// <summary>
/// Garantiza que los servicios de red existan en cualquier escena. Normalmente se crean en LoginScene y persisten
/// entre escenas, pero al dar Play directamente desde otra escena (por ejemplo BaseScene) no existirían y la app
/// no reportaría su presencia al backend.
/// </summary>
public static class Bootstrapper
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureNetworkServices()
    {
        // Cada servicio va en su propio objeto: si ya existe uno de ellos, su Awake no destruye a los demás.
        EnsureService<PlayerSession>("PlayerSession");
        EnsureService<ApiClient>("ApiClient");
        EnsureService<AuthService>("AuthService");
        EnsureService<PresenceService>("PresenceService");
    }

    private static void EnsureService<T>(string objectName) where T : MonoBehaviour
    {
        if (Object.FindFirstObjectByType<T>() != null) return;

        var go = new GameObject(objectName);
        go.AddComponent<T>(); // cada servicio se marca DontDestroyOnLoad en su Awake
        Debug.Log($"[Bootstrapper] {objectName} no existía en la escena: creado automáticamente.");
    }
}
