using System;

[Serializable]
public class UserDataDto
{
    public string id;
    public string email;
    public string role;
    public string trialEndsAt;
    public bool isActive;
    public bool hasCompletedSetup;
}

[Serializable]
public class ProfileDataDto
{
    public string firstName;
    public string lastName;
}

[Serializable]
public class LoginResponseDto
{
    public string accessToken;
    public string refreshToken;
    public int expiresIn;
    public string profileType;   // tutor | adulto | tester | profesor | admin
    public UserDataDto user;
    public ProfileDataDto profile;
}
