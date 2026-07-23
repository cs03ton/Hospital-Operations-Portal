using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hop.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDetailedLeavePolicyMatrix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "allow_request",
                table: "leave_policy_rules",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<decimal>(
                name: "annual_entitlement_days",
                table: "leave_policy_rules",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "carry_forward_limit_days",
                table: "leave_policy_rules",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "day_counting_type",
                table: "leave_policy_rules",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "BusinessDays");

            migrationBuilder.AddColumn<decimal>(
                name: "employer_paid_limit_days",
                table: "leave_policy_rules",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "maximum_leave_days",
                table: "leave_policy_rules",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "maximum_total_available_days",
                table: "leave_policy_rules",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payment_rule_type",
                table: "leave_policy_rules",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "EmployerPaid");

            migrationBuilder.AddColumn<decimal>(
                name: "probation_entitlement_days",
                table: "leave_policy_rules",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "requires_special_approval_after_days",
                table: "leave_policy_rules",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "uses_social_security",
                table: "leave_policy_rules",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("""
                UPDATE leave_policy_rules
                SET annual_entitlement_days = entitlement_days,
                    employer_paid_limit_days = max_paid_days,
                    carry_forward_limit_days = carry_over_max_days,
                    maximum_total_available_days = max_accumulated_days,
                    maximum_leave_days = COALESCE(max_extended_days, entitlement_days),
                    allow_request = true,
                    payment_rule_type = CASE WHEN is_paid THEN 'EmployerPaid' ELSE 'Unpaid' END,
                    day_counting_type = 'BusinessDays',
                    updated_at = now();

                UPDATE leave_policy_rules rule
                SET requires_special_approval_after_days = 60,
                    maximum_leave_days = 120,
                    payment_rule_type = 'EmployerPaidThenSpecialApproval',
                    updated_at = now()
                FROM leave_types leave_type
                WHERE rule.leave_type_id = leave_type.id
                  AND rule.employment_type IN ('CIVIL_SERVANT', 'PERMANENT_EMPLOYEE')
                  AND leave_type.code = 'SICK_LEAVE';

                UPDATE leave_policy_rules rule
                SET carry_forward_limit_days = 5,
                    maximum_total_available_days = 15,
                    updated_at = now()
                FROM leave_types leave_type
                WHERE rule.leave_type_id = leave_type.id
                  AND rule.employment_type IN ('GOVERNMENT_EMPLOYEE', 'MOPH_EMPLOYEE')
                  AND leave_type.code = 'VACATION_LEAVE';

                UPDATE leave_policy_rules rule
                SET uses_social_security = true,
                    social_security_max_days = COALESCE(social_security_max_days, 90),
                    payment_rule_type = 'EmployerPaidThenSocialSecurity',
                    notes = COALESCE(notes, 'ส่วนที่เกินสิทธิ์ได้รับค่าจ้างจากหน่วยงานให้ตรวจสิทธิประกันสังคมตามเงื่อนไข'),
                    updated_at = now()
                FROM leave_types leave_type
                WHERE rule.leave_type_id = leave_type.id
                  AND rule.employment_type IN ('GOVERNMENT_EMPLOYEE', 'MOPH_EMPLOYEE')
                  AND leave_type.code = 'SICK_LEAVE';

                UPDATE leave_policy_rules rule
                SET uses_social_security = true,
                    social_security_max_days = 45,
                    payment_rule_type = 'EmployerPaidThenSocialSecurity',
                    day_counting_type = 'CalendarDays',
                    notes = COALESCE(notes, 'ได้รับค่าจ้างจากหน่วยงานไม่เกิน 45 วัน ส่วนที่เหลือใช้สิทธิประกันสังคมตามเงื่อนไข'),
                    updated_at = now()
                FROM leave_types leave_type
                WHERE rule.leave_type_id = leave_type.id
                  AND rule.employment_type IN ('GOVERNMENT_EMPLOYEE', 'MOPH_EMPLOYEE', 'TEMPORARY_EMPLOYEE_MONTHLY')
                  AND leave_type.code = 'MATERNITY_LEAVE';

                UPDATE leave_policy_rules rule
                SET uses_social_security = true,
                    social_security_max_days = 90,
                    payment_rule_type = 'UnpaidThenSocialSecurity',
                    day_counting_type = CASE WHEN leave_type.code = 'MATERNITY_LEAVE' THEN 'CalendarDays' ELSE day_counting_type END,
                    updated_at = now()
                FROM leave_types leave_type
                WHERE rule.leave_type_id = leave_type.id
                  AND rule.employment_type = 'TEMPORARY_EMPLOYEE_DAILY'
                  AND leave_type.code IN ('SICK_LEAVE', 'MATERNITY_LEAVE');

                UPDATE leave_policy_rules rule
                SET probation_entitlement_days = 8,
                    updated_at = now()
                FROM leave_types leave_type
                WHERE rule.leave_type_id = leave_type.id
                  AND rule.employment_type IN ('TEMPORARY_EMPLOYEE_MONTHLY', 'TEMPORARY_EMPLOYEE_DAILY')
                  AND leave_type.code = 'SICK_LEAVE';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "allow_request",
                table: "leave_policy_rules");

            migrationBuilder.DropColumn(
                name: "annual_entitlement_days",
                table: "leave_policy_rules");

            migrationBuilder.DropColumn(
                name: "carry_forward_limit_days",
                table: "leave_policy_rules");

            migrationBuilder.DropColumn(
                name: "day_counting_type",
                table: "leave_policy_rules");

            migrationBuilder.DropColumn(
                name: "employer_paid_limit_days",
                table: "leave_policy_rules");

            migrationBuilder.DropColumn(
                name: "maximum_leave_days",
                table: "leave_policy_rules");

            migrationBuilder.DropColumn(
                name: "maximum_total_available_days",
                table: "leave_policy_rules");

            migrationBuilder.DropColumn(
                name: "payment_rule_type",
                table: "leave_policy_rules");

            migrationBuilder.DropColumn(
                name: "probation_entitlement_days",
                table: "leave_policy_rules");

            migrationBuilder.DropColumn(
                name: "requires_special_approval_after_days",
                table: "leave_policy_rules");

            migrationBuilder.DropColumn(
                name: "uses_social_security",
                table: "leave_policy_rules");
        }
    }
}
