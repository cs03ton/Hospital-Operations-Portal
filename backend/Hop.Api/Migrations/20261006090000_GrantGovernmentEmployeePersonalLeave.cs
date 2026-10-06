using Hop.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Hop.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261006090000_GrantGovernmentEmployeePersonalLeave")]
public sealed class GrantGovernmentEmployeePersonalLeave : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        UPDATE leave_policy_rules rule
        SET min_service_months = NULL,
            min_service_years = NULL,
            prorate_if_service_less_than_year = FALSE,
            first_year_entitlement_days = NULL,
            probation_entitlement_days = NULL,
            entitlement_days = 10,
            annual_entitlement_days = 10,
            max_paid_days = 10,
            employer_paid_limit_days = 10,
            allow_carry_over = FALSE,
            carry_over_max_days = 0,
            notes = 'มีสิทธิลากิจ 10 วันทำการต่อปีงบประมาณ',
            updated_at = CURRENT_TIMESTAMP
        FROM leave_types leave_type
        WHERE rule.leave_type_id = leave_type.id
          AND rule.employment_type = 'GOVERNMENT_EMPLOYEE'
          AND leave_type.code = 'PERSONAL_LEAVE';

        UPDATE leave_balances balance
        SET entitled_days = 10,
            carried_over_days = 0,
            updated_at = CURRENT_TIMESTAMP
        FROM users app_user, leave_types leave_type
        WHERE balance.user_id = app_user.id
          AND balance.leave_type_id = leave_type.id
          AND app_user.employment_type = 'GOVERNMENT_EMPLOYEE'
          AND leave_type.code = 'PERSONAL_LEAVE'
          AND balance.year = CASE
              WHEN EXTRACT(MONTH FROM CURRENT_DATE) >= 10 THEN EXTRACT(YEAR FROM CURRENT_DATE)::integer + 1
              ELSE EXTRACT(YEAR FROM CURRENT_DATE)::integer
          END;
        """);

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        UPDATE leave_policy_rules rule
        SET min_service_months = 12,
            prorate_if_service_less_than_year = FALSE,
            employer_paid_limit_days = NULL,
            notes = NULL,
            updated_at = CURRENT_TIMESTAMP
        FROM leave_types leave_type
        WHERE rule.leave_type_id = leave_type.id
          AND rule.employment_type = 'GOVERNMENT_EMPLOYEE'
          AND leave_type.code = 'PERSONAL_LEAVE';
        """);
}
