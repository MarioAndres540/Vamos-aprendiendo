using UnityEngine;

public class PlayerSession : MonoBehaviour
{
    public static PlayerSession Instance { get; private set; }

    public string AccessToken { get; private set; }
    public string RefreshToken { get; private set; }
    public UserDataDto CurrentUser { get; private set; }
    public bool IsLoggedIn => !string.IsNullOrEmpty(AccessToken);

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetSession(LoginResponseDto response)
    {
        AccessToken = response.accessToken;
        RefreshToken = response.refreshToken;
        CurrentUser = response.user;

        // Persistencia mínima para poder renovar sesión más adelante (Paso 8)
        PlayerPrefs.SetString("refreshToken", response.refreshToken);
        PlayerPrefs.Save();
    }
}