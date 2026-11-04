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
    public string profileType;   // tutor | adulto | tester | profesor | admin
    public string audience;      // ninos | adultos | vacío (ver LoginResponseDto.audience)
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
    // Contenido que debe mostrar la app: "ninos" (tutor), "adultos" (adulto) o vacío (tester elige; profesor/admin sin contenido)
    public string audience;
    public UserDataDto user;
    public ProfileDataDto profile;
}
