using UnityEngine;
using System;

[Serializable]
public class NestErrorDto
{
    public string message;
    public string error;
    public string code;      // p. ej. "SESSION_ENDED", "SESSION_NOT_FOUND"
    public int statusCode;
}
