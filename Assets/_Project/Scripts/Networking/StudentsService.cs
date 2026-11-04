using System;
using System.Collections;
using System.Linq;
using UnityEngine;

/// <summary>
/// Hijos del tutor (GET /students/mine) y elección del hijo que juega. Con su id, la sesión de app y la telemetría
/// quedan a nombre del niño. Las corrutinas se ejecutan desde quien las llama (no necesita un GameObject propio).
/// </summary>
public static class StudentsService
{
    /// <summary>
    /// El tutor tiene varios hijos y no hay uno elegido en este dispositivo: la UI debe mostrar un selector y llamar
    /// a <see cref="PlayerSession.SelectStudent"/> con el elegido.
    /// </summary>
    public static event Action<StudentSummaryDto[]> StudentSelectionRequired;

    /// <summary>Última lista recibida del backend (vacía si aún no se ha consultado).</summary>
    public static StudentSummaryDto[] LastStudents { get; private set; } = new StudentSummaryDto[0];

    public static IEnumerator FetchMyStudents(Action<StudentSummaryDto[]> onSuccess, Action<ApiError> onError)
    {
        if (ApiClient.Instance == null)
        {
            onError?.Invoke(new ApiError { message = "ApiClient no está en la escena" });
            yield break;
        }

        yield return ApiClient.Instance.Send("GET", "/students/mine", null,
            onSuccess: raw =>
            {
                var dto = JsonUtility.FromJson<MyStudentsResponseDto>(raw);
                LastStudents = dto?.students ?? new StudentSummaryDto[0];
                onSuccess?.Invoke(LastStudents);
            },
            onError: onError);
    }

    /// <summary>
    /// Deja elegido el hijo que juega, antes de abrir la sesión de app. Solo aplica al tutor:
    /// - un hijo: se elige solo;
    /// - varios: el último elegido en este dispositivo o, si no hay, se dispara <see cref="StudentSelectionRequired"/>;
    /// - ninguno: la sesión se abre sin hijo;
    /// - error de red: se usa el último hijo elegido en el dispositivo, si hay.
    /// </summary>
    public static IEnumerator PrepareCurrentStudent()
    {
        var session = PlayerSession.Instance;
        if (session == null || session.IsOfflineMode || !session.IsLoggedIn || !session.IsTutor) yield break;

        StudentSummaryDto[] students = null;
        yield return FetchMyStudents(
            list => students = list,
            error => Debug.LogWarning($"[Students] No se pudo consultar los hijos del tutor: {error}"));

        // Sin red: se sigue con el último hijo elegido (si hay), sin borrar lo guardado
        if (students == null)
        {
            string saved = session.CurrentStudentId ?? session.LoadSavedStudentId();
            if (saved != null) session.SelectStudent(saved);
            yield break;
        }

        if (students.Length == 0)
        {
            session.SelectStudent(null);
            yield break;
        }

        if (students.Length == 1)
        {
            session.SelectStudent(students[0].id);
            yield break;
        }

        string preferred = session.CurrentStudentId ?? session.LoadSavedStudentId();
        if (preferred != null && students.Any(s => s.id == preferred))
        {
            session.SelectStudent(preferred);
            yield break;
        }

        session.SelectStudent(null);
        if (StudentSelectionRequired != null) StudentSelectionRequired.Invoke(students);
        else Debug.LogWarning($"[Students] El tutor tiene {students.Length} hijos y no hay selector en la escena: la sesión se abre sin hijo.");
    }
}
