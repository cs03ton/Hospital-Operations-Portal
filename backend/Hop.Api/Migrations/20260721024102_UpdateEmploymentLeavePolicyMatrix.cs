using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hop.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEmploymentLeavePolicyMatrix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE leave_policy_rules rule
                SET min_service_months = 12,
                    updated_at = now()
                FROM leave_types leave_type
                WHERE rule.leave_type_id = leave_type.id
                  AND rule.employment_type = 'GOVERNMENT_EMPLOYEE'
                  AND leave_type.code = 'PERSONAL_LEAVE';

                UPDATE leave_policy_rules rule
                SET carry_over_max_days = 5,
                    max_accumulated_days = 15,
                    first_year_entitlement_days = NULL,
                    updated_at = now()
                FROM leave_types leave_type
                WHERE rule.leave_type_id = leave_type.id
                  AND rule.employment_type IN ('GOVERNMENT_EMPLOYEE', 'MOPH_EMPLOYEE')
                  AND leave_type.code = 'VACATION_LEAVE';

                UPDATE leave_policy_rules rule
                SET first_year_entitlement_days = 6,
                    first_year_paid_days = 6,
                    updated_at = now()
                FROM leave_types leave_type
                WHERE rule.leave_type_id = leave_type.id
                  AND rule.employment_type = 'MOPH_EMPLOYEE'
                  AND leave_type.code = 'PERSONAL_LEAVE';

                UPDATE leave_policy_rules rule
                SET min_service_months = NULL,
                    first_year_entitlement_days = 8,
                    first_year_paid_days = 8,
                    notes = 'ผู้ปฏิบัติงานยังไม่ครบ 6 เดือนรองรับวงเงินสิทธิ 8 วันทำการ',
                    updated_at = now()
                FROM leave_types leave_type
                WHERE rule.leave_type_id = leave_type.id
                  AND rule.employment_type = 'TEMPORARY_EMPLOYEE_MONTHLY'
                  AND leave_type.code = 'SICK_LEAVE';

                UPDATE leave_policy_rules rule
                SET first_year_entitlement_days = NULL,
                    notes = 'ไม่มีสิทธิสะสมวันลาพักผ่อน',
                    updated_at = now()
                FROM leave_types leave_type
                WHERE rule.leave_type_id = leave_type.id
                  AND rule.employment_type = 'TEMPORARY_EMPLOYEE_MONTHLY'
                  AND leave_type.code = 'VACATION_LEAVE';

                UPDATE leave_policy_rules rule
                SET first_year_entitlement_days = NULL,
                    notes = 'ไม่สะสมวันลาพักผ่อน',
                    updated_at = now()
                FROM leave_types leave_type
                WHERE rule.leave_type_id = leave_type.id
                  AND rule.employment_type = 'TEMPORARY_EMPLOYEE_DAILY'
                  AND leave_type.code = 'VACATION_LEAVE';

                UPDATE leave_policy_rules rule
                SET entitlement_days = 90,
                    is_paid = false,
                    social_security_max_days = 90,
                    notes = 'ลาได้ 90 วัน แต่ไม่ได้รับค่าจ้างระหว่างลา ใช้สิทธิประกันสังคมตามเงื่อนไข',
                    updated_at = now()
                FROM leave_types leave_type
                WHERE rule.leave_type_id = leave_type.id
                  AND rule.employment_type = 'TEMPORARY_EMPLOYEE_DAILY'
                  AND leave_type.code = 'MATERNITY_LEAVE';

                WITH policy_values AS (
                    SELECT *
                    FROM (VALUES
                        ('PERMANENT_EMPLOYEE', 'SICK_LEAVE', 60.0, 60.0, false, NULL::numeric, NULL::numeric, NULL::integer, NULL::integer, false, NULL::numeric, NULL::numeric, true, 120.0, NULL::numeric, 'กรณีเกิน 60 วัน ผู้อำนวยการอาจพิจารณาได้รวมไม่เกิน 120 วัน'),
                        ('PERMANENT_EMPLOYEE', 'PERSONAL_LEAVE', 45.0, 45.0, false, NULL::numeric, NULL::numeric, NULL::integer, NULL::integer, false, 15.0, NULL::numeric, true, NULL::numeric, NULL::numeric, NULL::text),
                        ('PERMANENT_EMPLOYEE', 'VACATION_LEAVE', 10.0, 10.0, true, 30.0, 30.0, 6, NULL::integer, false, NULL::numeric, NULL::numeric, true, NULL::numeric, NULL::numeric, NULL::text),
                        ('PERMANENT_EMPLOYEE', 'MATERNITY_LEAVE', 90.0, 90.0, false, NULL::numeric, NULL::numeric, NULL::integer, NULL::integer, false, NULL::numeric, NULL::numeric, true, NULL::numeric, NULL::numeric, NULL::text),
                        ('PERMANENT_EMPLOYEE', 'ORDINATION_LEAVE', 120.0, 120.0, false, NULL::numeric, NULL::numeric, 12, NULL::integer, false, NULL::numeric, NULL::numeric, true, NULL::numeric, NULL::numeric, 'ใช้ตามระเบียบราชการและเงื่อนไขหน่วยงาน')
                    ) AS data(
                        employment_type,
                        leave_type_code,
                        entitlement_days,
                        max_paid_days,
                        allow_carry_over,
                        carry_over_max_days,
                        max_accumulated_days,
                        min_service_months,
                        min_service_years,
                        prorate_if_service_less_than_year,
                        first_year_entitlement_days,
                        first_year_paid_days,
                        is_paid,
                        max_extended_days,
                        social_security_max_days,
                        notes
                    )
                )
                INSERT INTO leave_policy_rules (
                    id,
                    employment_type,
                    leave_type_id,
                    fiscal_year,
                    entitlement_days,
                    max_paid_days,
                    allow_carry_over,
                    carry_over_max_days,
                    max_accumulated_days,
                    min_service_months,
                    min_service_years,
                    prorate_if_service_less_than_year,
                    first_year_entitlement_days,
                    first_year_paid_days,
                    is_paid,
                    max_extended_days,
                    social_security_max_days,
                    notes,
                    is_active,
                    created_at,
                    updated_at
                )
                SELECT
                    (
                        substr(md5(policy_values.employment_type || ':' || policy_values.leave_type_code), 1, 8) || '-' ||
                        substr(md5(policy_values.employment_type || ':' || policy_values.leave_type_code), 9, 4) || '-' ||
                        substr(md5(policy_values.employment_type || ':' || policy_values.leave_type_code), 13, 4) || '-' ||
                        substr(md5(policy_values.employment_type || ':' || policy_values.leave_type_code), 17, 4) || '-' ||
                        substr(md5(policy_values.employment_type || ':' || policy_values.leave_type_code), 21, 12)
                    )::uuid,
                    policy_values.employment_type,
                    leave_types.id,
                    NULL,
                    policy_values.entitlement_days,
                    policy_values.max_paid_days,
                    policy_values.allow_carry_over,
                    policy_values.carry_over_max_days,
                    policy_values.max_accumulated_days,
                    policy_values.min_service_months,
                    policy_values.min_service_years,
                    policy_values.prorate_if_service_less_than_year,
                    policy_values.first_year_entitlement_days,
                    policy_values.first_year_paid_days,
                    policy_values.is_paid,
                    policy_values.max_extended_days,
                    policy_values.social_security_max_days,
                    policy_values.notes,
                    true,
                    now(),
                    now()
                FROM policy_values
                JOIN leave_types ON leave_types.code = policy_values.leave_type_code
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM leave_policy_rules existing
                    WHERE existing.employment_type = policy_values.employment_type
                      AND existing.leave_type_id = leave_types.id
                      AND existing.fiscal_year IS NULL
                      AND existing.is_active
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM leave_policy_rules
                WHERE employment_type = 'PERMANENT_EMPLOYEE';

                UPDATE leave_policy_rules rule
                SET min_service_months = NULL,
                    updated_at = now()
                FROM leave_types leave_type
                WHERE rule.leave_type_id = leave_type.id
                  AND rule.employment_type = 'GOVERNMENT_EMPLOYEE'
                  AND leave_type.code = 'PERSONAL_LEAVE';

                UPDATE leave_policy_rules rule
                SET carry_over_max_days = 15,
                    max_accumulated_days = 15,
                    first_year_entitlement_days = 0,
                    updated_at = now()
                FROM leave_types leave_type
                WHERE rule.leave_type_id = leave_type.id
                  AND rule.employment_type IN ('GOVERNMENT_EMPLOYEE', 'MOPH_EMPLOYEE')
                  AND leave_type.code = 'VACATION_LEAVE';

                UPDATE leave_policy_rules rule
                SET first_year_entitlement_days = NULL,
                    first_year_paid_days = 6,
                    updated_at = now()
                FROM leave_types leave_type
                WHERE rule.leave_type_id = leave_type.id
                  AND rule.employment_type = 'MOPH_EMPLOYEE'
                  AND leave_type.code = 'PERSONAL_LEAVE';

                UPDATE leave_policy_rules rule
                SET min_service_months = 6,
                    first_year_entitlement_days = 8,
                    first_year_paid_days = 8,
                    notes = NULL,
                    updated_at = now()
                FROM leave_types leave_type
                WHERE rule.leave_type_id = leave_type.id
                  AND rule.employment_type = 'TEMPORARY_EMPLOYEE_MONTHLY'
                  AND leave_type.code = 'SICK_LEAVE';

                UPDATE leave_policy_rules rule
                SET first_year_entitlement_days = 0,
                    notes = NULL,
                    updated_at = now()
                FROM leave_types leave_type
                WHERE rule.leave_type_id = leave_type.id
                  AND rule.employment_type = 'TEMPORARY_EMPLOYEE_MONTHLY'
                  AND leave_type.code = 'VACATION_LEAVE';

                UPDATE leave_policy_rules rule
                SET first_year_entitlement_days = 0,
                    notes = 'ไม่สะสมวันลาพักผ่อน',
                    updated_at = now()
                FROM leave_types leave_type
                WHERE rule.leave_type_id = leave_type.id
                  AND rule.employment_type = 'TEMPORARY_EMPLOYEE_DAILY'
                  AND leave_type.code = 'VACATION_LEAVE';

                UPDATE leave_policy_rules rule
                SET entitlement_days = 0,
                    is_paid = false,
                    social_security_max_days = 90,
                    notes = 'ใช้สิทธิประกันสังคมตามเงื่อนไข',
                    updated_at = now()
                FROM leave_types leave_type
                WHERE rule.leave_type_id = leave_type.id
                  AND rule.employment_type = 'TEMPORARY_EMPLOYEE_DAILY'
                  AND leave_type.code = 'MATERNITY_LEAVE';
                """);
        }
    }
}
