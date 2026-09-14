using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class ApiClient : MonoBehaviour
{
    public static ApiClient Instance { get; private set; }

    [Header("Configuración de entorno")]
    [SerializeField] private string baseUrl = "http://localhost:3000/api/v1";

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public IEnumerator Post<TRequest, TResponse>(
        string endpoint, TRequest body,
        Action<TResponse> onSuccess, Action<string> onError)
    {
        string json = JsonUtility.ToJson(body);
        string url = $"{baseUrl}{endpoint}";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                string serverMessage = ExtractErrorMessage(request.downloadHandler.text);
                onError?.Invoke(string.IsNullOrEmpty(serverMessage) ? request.error : serverMessage);
                yield break;
            }

            try
            {
                TResponse response = JsonUtility.FromJson<TResponse>(request.downloadHandler.text);
                onSuccess?.Invoke(response);
            }
            catch (Exception e)
            {
                onError?.Invoke($"Error al procesar la respuesta: {e.Message}");
            }
        }
    }

    private string ExtractErrorMessage(string rawJson)
    {
        if (string.IsNullOrEmpty(rawJson)) return null;
        try { return JsonUtility.FromJson<NestErrorDto>(rawJson)?.message; }
        catch { return null; }
    }
}