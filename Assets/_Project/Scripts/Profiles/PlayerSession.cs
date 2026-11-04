using UnityEngine;

public class PlayerSession : MonoBehaviour
{
    private const string RefreshTokenKey = "refreshToken";
    // El hijo elegido se recuerda por cuenta en este dispositivo
    private const string StudentKeyPrefix = "currentStudentId:";

    // Valores de "audience" que envía el backend
    public const string AudienceKids = "ninos";
    public const string AudienceAdults = "adultos";

    public static PlayerSession Instance { get; private set; }

    public string AccessToken { get; private set; }
    public string RefreshToken { get; private set; }
    public UserDataDto CurrentUser { get; private set; }
    public string ProfileType { get; private set; }

    /// <summary>
    /// Contenido que debe mostrar la app: "ninos" (tutor que juega con su hijo) o "adultos".
    /// Vacío: el tester elige la versión (ver <see cref="ChooseAudience"/>); profesor/admin no tienen contenido.
    /// </summary>
    public string Audience { get; private set; }

    public ProfileDataDto Profile { get; private set; }
    public bool IsOfflineMode { get; private set; }
    public bool IsLoggedIn => IsOfflineMode || !string.IsNullOrEmpty(AccessToken);

    public bool IsTutor => ProfileType == "tutor";
    public bool IsKidsAudience => Audience == AudienceKids;
    public bool IsAdultsAudience => Audience == AudienceAdults;
    /// <summary>El tester (o el modo sin backend) debe elegir si juega la versión de niños o de adultos.</summary>
    public bool MustChooseAudience => string.IsNullOrEmpty(Audience) && (ProfileType == "tester" || IsOfflineMode);

    /// <summary>Hijo que juega (solo tutor). Se envía como studentId al abrir la sesión de app.</summary>
    public string CurrentStudentId { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>Sesión local sin backend (solo desarrollo): sin tokens y sin llamadas de red.</summary>
    public void SetOfflineSession(UserDataDto user)
    {
        AccessToken = null;
        RefreshToken = null;
        CurrentUser = user;
        ProfileType = user?.profileType;
        Audience = null;
        IsOfflineMode = true;
    }

    public void SetSession(LoginResponseDto response)
    {
        IsOfflineMode = false;
        AccessToken = response.accessToken;
        RefreshToken = response.refreshToken;
        CurrentUser = response.user;
        ProfileType = response.profileType;
        Audience = Normalize(!string.IsNullOrEmpty(response.audience) ? response.audience : response.user?.audience);
        Profile = response.profile;
        CurrentStudentId = null;

        // Persistencia mínima para poder renovar sesión más adelante (en producción, almacenamiento seguro).
        SaveRefreshToken();
    }

    /// <summary>Recupera el refresh token guardado de un login anterior (para reabrir sesión sin pedir login).</summary>
    public bool TryLoadSavedRefreshToken()
    {
        string saved = PlayerPrefs.GetString(RefreshTokenKey, null);
        if (string.IsNullOrEmpty(saved)) return false;
        RefreshToken = saved;
        return true;
    }

    /// <summary>Guarda los tokens nuevos tras un refresh (el refresh token anterior queda revocado).</summary>
    public void UpdateTokens(string accessToken, string refreshToken)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        SaveRefreshToken();
    }

    /// <summary>
    /// Actualiza el perfil con la respuesta de /auth/refresh. Si el backend no lo envía (versión anterior),
    /// se conserva el que había.
    /// </summary>
    public void UpdateProfile(string profileType, string audience, UserDataDto user)
    {
        if (user != null && !string.IsNullOrEmpty(user.id)) CurrentUser = user;
        if (string.IsNullOrEmpty(profileType)) return;
        ProfileType = profileType;
        Audience = Normalize(audience);
    }

    /// <summary>Para el tester: elige la versión del contenido ("ninos" o "adultos").</summary>
    public void ChooseAudience(string audience)
    {
        if (!MustChooseAudience) return;
        Audience = Normalize(audience);
    }

    /// <summary>Fija el hijo que juega y lo recuerda en el dispositivo para esta cuenta. null lo quita.</summary>
    public void SelectStudent(string studentId)
    {
        CurrentStudentId = string.IsNullOrEmpty(studentId) ? null : studentId;
        string key = StudentKey();
        if (key == null) return;
        if (CurrentStudentId == null) PlayerPrefs.DeleteKey(key);
        else PlayerPrefs.SetString(key, CurrentStudentId);
        PlayerPrefs.Save();
    }

    /// <summary>Hijo elegido la última vez en este dispositivo con esta cuenta (o null).</summary>
    public string LoadSavedStudentId()
    {
        string key = StudentKey();
        if (key == null) return null;
        string saved = PlayerPrefs.GetString(key, null);
        return string.IsNullOrEmpty(saved) ? null : saved;
    }

    /// <summary>Borra la sesión y los tokens guardados (cierre de sesión o sesión expirada).</summary>
    public void Clear()
    {
        AccessToken = null;
        RefreshToken = null;
        CurrentUser = null;
        ProfileType = null;
        Audience = null;
        Profile = null;
        CurrentStudentId = null;
        IsOfflineMode = false;
        PlayerPrefs.DeleteKey(RefreshTokenKey);
        PlayerPrefs.Save();
    }

    private void SaveRefreshToken()
    {
        if (string.IsNullOrEmpty(RefreshToken)) return;
        PlayerPrefs.SetString(RefreshTokenKey, RefreshToken);
        PlayerPrefs.Save();
    }

    private string StudentKey() =>
        CurrentUser != null && !string.IsNullOrEmpty(CurrentUser.id) ? StudentKeyPrefix + CurrentUser.id : null;

    // JsonUtility deja "" cuando el backend envía null: se trata igual que sin valor
    private static string Normalize(string audience) =>
        audience == AudienceKids || audience == AudienceAdults ? audience : null;
}
