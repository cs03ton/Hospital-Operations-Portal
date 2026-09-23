namespace Hop.Api.Services;

public static class LeaveBusinessRules
{
    public const string NursingDepartmentName = "กลุ่มงานการพยาบาล";
    public const int VacationAdvanceCalendarDays = 3;

    public static bool IsNursingDepartment(string? departmentName) =>
        string.Equals(departmentName?.Trim(), NursingDepartmentName, StringComparison.OrdinalIgnoreCase);

    public static bool IsVacationLeave(string? code) => Normalize(code) is
        "VACATIONLEAVE" or "ANNUALLEAVE" or "VACATION" or "ANNUAL";

    public static DateOnly EarliestVacationStart(DateOnly submittedOn) =>
        submittedOn.AddDays(VacationAdvanceCalendarDays);

    private static string Normalize(string? value) => new(
        (value ?? string.Empty).Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
}
