namespace Hop.Api.Authorization;

public static class LeavePermissions
{
    public const string ViewOwn = "LeaveRequest.ViewOwn";
    public const string ViewPendingApproval = "LeaveRequest.ViewPendingApproval";
    public const string ViewDepartment = "LeaveRequest.ViewDepartment";
    public const string ViewAll = "LeaveRequest.ViewAll";
    public const string Create = "LeaveRequest.Create";
    public const string EditOwn = "LeaveRequest.EditOwn";
    public const string CancelOwn = "LeaveRequest.CancelOwn";
    public const string ApproveCurrentStep = "LeaveApproval.ApproveCurrentStep";
    public const string Delegate = "LeaveApproval.Delegate";
    public const string Override = "LeaveApproval.Override";
    public const string DelegationManage = "LeaveApprovalDelegation.Manage";
    public const string EscalationManage = "LeaveApprovalEscalation.Manage";
    public const string SupportViewAll = "LeaveSupport.ViewAll";
    public const string ManageTypes = "LeaveAdmin.ManageTypes";
    public const string ManageBalances = "LeaveAdmin.ManageBalances";
    public const string RolloverBalances = "LeaveBalance.Rollover";
    public const string ManageHolidays = "LeaveAdmin.ManageHolidays";
    public const string ManageApprovalChains = "LeaveAdmin.ManageApprovalChains";

    public static readonly string[] ViewAny =
    [
        ViewOwn,
        ViewPendingApproval,
        ViewDepartment,
        ViewAll,
        SupportViewAll
    ];
}
