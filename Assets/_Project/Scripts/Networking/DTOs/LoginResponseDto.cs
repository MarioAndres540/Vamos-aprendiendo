using System;

[Serializable]
public class UserDataDto
{
    public string id;
    public string email;
    public string role;
    public string trialEndsAt;
    public bool isActive;
}

[Serializable]
public class LoginResponseDto
{
    public string accessToken;
    public string refreshToken;
    public int expiresIn;
    public UserDataDto user;
}