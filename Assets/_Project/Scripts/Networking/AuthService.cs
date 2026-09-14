using UnityEngine;
using System;

public class AuthService : MonoBehaviour
{
    public static AuthService Instance {get; private set;}
    
        private void Awake()
        {
            if (Instance != null && Instance != this) {Destroy(gameObject); return;}
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Login(string email, string password, Action<LoginResponseDto> onSuccess, Action<string> onError)
        {
            var requestBody = new LoginRequestDto{email = email, password = password};

            StartCoroutine(ApiClient.Instance.Post<LoginRequestDto, LoginResponseDto>(
                "/auth/login",
                requestBody,
                (response) => 
                {
                    PlayerSession.Instance.SetSession(response);
                    onSuccess?.Invoke(response);
                },
                onError
            ));
        }
    
}
