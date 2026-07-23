namespace Hop.Api.Models;

public static class EmploymentTypes
{
    public const string CivilServant = "CIVIL_SERVANT";
    public const string PermanentEmployee = "PERMANENT_EMPLOYEE";
    public const string GovernmentEmployee = "GOVERNMENT_EMPLOYEE";
    public const string MophEmployee = "MOPH_EMPLOYEE";
    public const string TemporaryEmployeeMonthly = "TEMPORARY_EMPLOYEE_MONTHLY";
    public const string TemporaryEmployeeDaily = "TEMPORARY_EMPLOYEE_DAILY";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        CivilServant,
        PermanentEmployee,
        GovernmentEmployee,
        MophEmployee,
        TemporaryEmployeeMonthly,
        TemporaryEmployeeDaily
    };

    public static string GetThaiLabel(string? value)
    {
        return value switch
        {
            CivilServant => "ข้าราชการ",
            PermanentEmployee => "ลูกจ้างประจำ",
            GovernmentEmployee => "พนักงานราชการ",
            MophEmployee => "พนักงานกระทรวงสาธารณสุข",
            TemporaryEmployeeMonthly => "ลูกจ้างชั่วคราวรายเดือน",
            TemporaryEmployeeDaily => "ลูกจ้างชั่วคราวรายวัน",
            _ => "ไม่ระบุ"
        };
    }
}
