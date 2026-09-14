namespace Hop.Api.Authorization;

public static class RepairPermissions
{
    public const string ViewOwn = "RepairManagement.ViewOwn";
    public const string Create = "RepairManagement.Create";
    public const string WorkIT = "RepairManagement.WorkIT";
    public const string WorkGeneral = "RepairManagement.WorkGeneral";
    public const string ViewAll = "RepairManagement.ViewAll";
    public const string Manage = "RepairManagement.Manage";
    public static readonly string[] All = [ViewOwn, Create, WorkIT, WorkGeneral, ViewAll, Manage];
}
