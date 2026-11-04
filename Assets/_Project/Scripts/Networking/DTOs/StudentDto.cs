using System;

/// <summary>Un hijo del tutor en la respuesta de GET /students/mine (solo los campos que usa la app).</summary>
[Serializable]
public class StudentSummaryDto
{
    public string id;
    public string firstName;
    public string lastName;
    public string grade;
    public int age;   // 0 si el backend no la tiene
}

/// <summary>Respuesta de GET /students/mine.</summary>
[Serializable]
public class MyStudentsResponseDto
{
    public int totalStudents;
    public StudentSummaryDto[] students;
}
