using Hop.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Data;

internal static class FleetModelConfiguration
{
    public static void ConfigureFleet(this ModelBuilder modelBuilder)
    {
        var statusSeededAt = new DateTime(2026, 8, 11, 0, 0, 0, DateTimeKind.Utc);
        modelBuilder.Entity<FleetStatusDefinition>(entity =>
        {
            entity.ToTable("fleet_status_definitions");
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.Domain).HasColumnName("domain").HasMaxLength(40);
            entity.Property(x => x.Code).HasColumnName("code").HasMaxLength(60);
            entity.Property(x => x.ThaiName).HasColumnName("thai_name").HasMaxLength(200);
            entity.Property(x => x.Description).HasColumnName("description").HasMaxLength(1000);
            entity.Property(x => x.SortOrder).HasColumnName("sort_order");
            entity.Property(x => x.IsActive).HasColumnName("is_active");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(x => new { x.Domain, x.Code }).IsUnique();
            entity.HasIndex(x => new { x.Domain, x.IsActive, x.SortOrder });
            entity.HasData(
                FleetStatus("019fd100-0000-7000-8000-000000000001", FleetRequestStatuses.Draft, "แบบร่าง", 10, statusSeededAt),
                FleetStatus("019fd100-0000-7000-8000-000000000002", FleetRequestStatuses.PendingDispatch, "รอจัดรถและคนขับ", 20, statusSeededAt),
                FleetStatus("019fd100-0000-7000-8000-000000000003", FleetRequestStatuses.PendingAdminReview, "รอหัวหน้าฝ่ายบริหารตรวจสอบ", 30, statusSeededAt),
                FleetStatus("019fd100-0000-7000-8000-000000000004", FleetRequestStatuses.PendingDirector, "รอผู้อำนวยการอนุมัติ", 40, statusSeededAt),
                FleetStatus("019fd100-0000-7000-8000-000000000005", FleetRequestStatuses.Approved, "อนุมัติแล้ว", 50, statusSeededAt),
                FleetStatus("019fd100-0000-7000-8000-000000000006", FleetRequestStatuses.PendingDriverAck, "รอคนขับรับทราบ", 60, statusSeededAt),
                FleetStatus("019fd100-0000-7000-8000-000000000007", FleetRequestStatuses.Ready, "พร้อมเดินทาง", 70, statusSeededAt),
                FleetStatus("019fd100-0000-7000-8000-000000000008", FleetRequestStatuses.InProgress, "กำลังปฏิบัติงาน", 80, statusSeededAt),
                FleetStatus("019fd100-0000-7000-8000-000000000009", FleetRequestStatuses.Completed, "เสร็จสิ้น", 90, statusSeededAt),
                FleetStatus("019fd100-0000-7000-8000-000000000010", FleetRequestStatuses.CancellationPending, "รอพิจารณายกเลิก", 100, statusSeededAt),
                FleetStatus("019fd100-0000-7000-8000-000000000011", FleetRequestStatuses.Returned, "ส่งกลับแก้ไข", 110, statusSeededAt),
                FleetStatus("019fd100-0000-7000-8000-000000000012", FleetRequestStatuses.Rejected, "ไม่รับคำขอ", 120, statusSeededAt),
                FleetStatus("019fd100-0000-7000-8000-000000000013", FleetRequestStatuses.Cancelled, "ยกเลิก", 130, statusSeededAt),
                FleetStatus("019fd100-0000-7000-8000-000000000014", FleetRequestStatuses.Aborted, "ยุติการเดินทาง", 140, statusSeededAt));
        });

        modelBuilder.Entity<FleetVehicleType>(entity =>
        {
            entity.ToTable("fleet_vehicle_types", table => table.HasCheckConstraint("ck_fleet_vehicle_types_sort_order", "sort_order >= 0"));
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.Code).HasColumnName("code").HasMaxLength(50);
            entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
            entity.Property(x => x.Description).HasColumnName("description").HasMaxLength(1000);
            entity.Property(x => x.SortOrder).HasColumnName("sort_order");
            entity.Property(x => x.IsActive).HasColumnName("is_active");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(x => x.Code).IsUnique();
            entity.HasIndex(x => new { x.IsActive, x.SortOrder });
        });

        modelBuilder.Entity<FleetVehicle>(entity =>
        {
            entity.ToTable("fleet_vehicles", table =>
            {
                table.HasCheckConstraint("ck_fleet_vehicles_capacity", "seat_capacity_total > 0 AND passenger_capacity > 0 AND passenger_capacity <= seat_capacity_total");
                table.HasCheckConstraint("ck_fleet_vehicles_mileage", "current_mileage >= 0");
            });
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.VehicleCode).HasColumnName("vehicle_code").HasMaxLength(50);
            entity.Property(x => x.RegistrationNumber).HasColumnName("registration_number").HasMaxLength(50);
            entity.Property(x => x.RegistrationProvince).HasColumnName("registration_province").HasMaxLength(100);
            entity.Property(x => x.VehicleTypeId).HasColumnName("vehicle_type_id");
            entity.Property(x => x.Brand).HasColumnName("brand").HasMaxLength(100);
            entity.Property(x => x.Model).HasColumnName("model").HasMaxLength(100);
            entity.Property(x => x.ManufactureYear).HasColumnName("manufacture_year");
            entity.Property(x => x.SeatCapacityTotal).HasColumnName("seat_capacity_total");
            entity.Property(x => x.PassengerCapacity).HasColumnName("passenger_capacity");
            entity.Property(x => x.FuelType).HasColumnName("fuel_type").HasMaxLength(50);
            entity.Property(x => x.CurrentMileage).HasColumnName("current_mileage").HasPrecision(12, 2);
            entity.Property(x => x.OwningDepartmentId).HasColumnName("owning_department_id");
            entity.Property(x => x.ResponsibleUserId).HasColumnName("responsible_user_id");
            entity.Property(x => x.Status).HasColumnName("status").HasMaxLength(40);
            entity.Property(x => x.IsActive).HasColumnName("is_active");
            entity.Property(x => x.Note).HasColumnName("note").HasMaxLength(2000);
            entity.Property(x => x.ImagePath).HasColumnName("image_path").HasMaxLength(1000);
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            entity.Property(x => x.UpdatedByUserId).HasColumnName("updated_by_user_id");
            entity.HasIndex(x => x.VehicleCode).IsUnique();
            entity.HasIndex(x => x.RegistrationNumber).IsUnique();
            entity.HasIndex(x => new { x.IsActive, x.Status, x.VehicleTypeId });
            entity.HasOne(x => x.VehicleType).WithMany(x => x.Vehicles).HasForeignKey(x => x.VehicleTypeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.OwningDepartment).WithMany().HasForeignKey(x => x.OwningDepartmentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ResponsibleUser).WithMany().HasForeignKey(x => x.ResponsibleUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.UpdatedByUser).WithMany().HasForeignKey(x => x.UpdatedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FleetDriverProfile>(entity =>
        {
            entity.ToTable("fleet_driver_profiles");
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.UserId).HasColumnName("user_id");
            entity.Property(x => x.LicenseNumber).HasColumnName("license_number").HasMaxLength(100);
            entity.Property(x => x.LicenseType).HasColumnName("license_type").HasMaxLength(100);
            entity.Property(x => x.LicenseIssueDate).HasColumnName("license_issue_date");
            entity.Property(x => x.LicenseExpiryDate).HasColumnName("license_expiry_date");
            entity.Property(x => x.CanDriveSedan).HasColumnName("can_drive_sedan");
            entity.Property(x => x.CanDrivePickup).HasColumnName("can_drive_pickup");
            entity.Property(x => x.CanDriveVan).HasColumnName("can_drive_van");
            entity.Property(x => x.CanDriveAmbulance).HasColumnName("can_drive_ambulance");
            entity.Property(x => x.CanDriveOther).HasColumnName("can_drive_other");
            entity.Property(x => x.DriverStatus).HasColumnName("driver_status").HasMaxLength(40);
            entity.Property(x => x.IsActive).HasColumnName("is_active");
            entity.Property(x => x.Note).HasColumnName("note").HasMaxLength(2000);
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            entity.Property(x => x.UpdatedByUserId).HasColumnName("updated_by_user_id");
            entity.HasIndex(x => x.UserId).IsUnique();
            entity.HasIndex(x => new { x.IsActive, x.DriverStatus });
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.UpdatedByUser).WithMany().HasForeignKey(x => x.UpdatedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FleetVehicleUnavailability>(entity =>
        {
            entity.ToTable("fleet_vehicle_unavailability", table => table.HasCheckConstraint("ck_fleet_vehicle_unavailability_range", "end_at > start_at"));
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.VehicleId).HasColumnName("vehicle_id");
            entity.Property(x => x.StartAt).HasColumnName("start_at");
            entity.Property(x => x.EndAt).HasColumnName("end_at");
            entity.Property(x => x.Reason).HasColumnName("reason").HasMaxLength(1000);
            entity.Property(x => x.Type).HasColumnName("type").HasMaxLength(50);
            entity.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(x => new { x.VehicleId, x.StartAt, x.EndAt });
            entity.HasOne(x => x.Vehicle).WithMany(x => x.UnavailabilityPeriods).HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FleetDriverUnavailability>(entity =>
        {
            entity.ToTable("fleet_driver_unavailability", table => table.HasCheckConstraint("ck_fleet_driver_unavailability_range", "end_at > start_at"));
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.UserId).HasColumnName("user_id");
            entity.Property(x => x.StartAt).HasColumnName("start_at");
            entity.Property(x => x.EndAt).HasColumnName("end_at");
            entity.Property(x => x.Reason).HasColumnName("reason").HasMaxLength(1000);
            entity.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(x => new { x.UserId, x.StartAt, x.EndAt });
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FleetRequest>(entity =>
        {
            entity.ToTable("fleet_requests", table =>
            {
                table.HasCheckConstraint("ck_fleet_requests_time_range", "expected_return_at > departure_at");
                table.HasCheckConstraint("ck_fleet_requests_passenger_count", "passenger_count > 0");
                table.HasCheckConstraint("ck_fleet_requests_priority", "priority IN ('NORMAL','URGENT','EMERGENCY')");
                table.HasCheckConstraint("ck_fleet_requests_emergency_reason", "priority <> 'EMERGENCY' OR emergency_reason IS NOT NULL AND reported_by_user_id IS NOT NULL AND reported_at IS NOT NULL");
            });
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.RequestNo).HasColumnName("request_no").HasMaxLength(30);
            entity.Property(x => x.RequesterUserId).HasColumnName("requester_user_id");
            entity.Property(x => x.RequesterDepartmentId).HasColumnName("requester_department_id");
            entity.Property(x => x.RequestDate).HasColumnName("request_date");
            entity.Property(x => x.Purpose).HasColumnName("purpose").HasMaxLength(2000);
            entity.Property(x => x.MissionType).HasColumnName("mission_type").HasMaxLength(100);
            entity.Property(x => x.RequestedVehicleTypeId).HasColumnName("requested_vehicle_type_id");
            entity.Property(x => x.Destination).HasColumnName("destination").HasMaxLength(1000);
            entity.Property(x => x.ContactPersonName).HasColumnName("contact_person_name").HasMaxLength(200);
            entity.Property(x => x.ContactPhone).HasColumnName("contact_phone").HasMaxLength(50);
            entity.Property(x => x.DepartureAt).HasColumnName("departure_at");
            entity.Property(x => x.ExpectedReturnAt).HasColumnName("expected_return_at");
            entity.Property(x => x.PassengerCount).HasColumnName("passenger_count");
            entity.Property(x => x.SpecialRequirement).HasColumnName("special_requirement").HasMaxLength(2000);
            entity.Property(x => x.IsUrgent).HasColumnName("is_urgent");
            entity.Property(x => x.UrgentReason).HasColumnName("urgent_reason").HasMaxLength(1000);
            entity.Property(x => x.Priority).HasColumnName("priority").HasMaxLength(20).HasDefaultValue(FleetPriorities.Normal);
            entity.Property(x => x.EmergencyReason).HasColumnName("emergency_reason").HasMaxLength(2000);
            entity.Property(x => x.ReportedByUserId).HasColumnName("reported_by_user_id");
            entity.Property(x => x.ReportedAt).HasColumnName("reported_at");
            entity.Property(x => x.IncidentLocation).HasColumnName("incident_location").HasMaxLength(1000);
            entity.Property(x => x.RequestedDepartureAt).HasColumnName("requested_departure_at");
            entity.Property(x => x.EmergencyPolicyCode).HasColumnName("emergency_policy_code").HasMaxLength(80);
            entity.Property(x => x.EmergencyPolicyId).HasColumnName("emergency_policy_id");
            entity.Property(x => x.ResponseTargetMinutesSnapshot).HasColumnName("response_target_minutes_snapshot");
            entity.Property(x => x.DispatchTargetMinutesSnapshot).HasColumnName("dispatch_target_minutes_snapshot");
            entity.Property(x => x.DriverAcknowledgementTargetMinutesSnapshot).HasColumnName("driver_ack_target_minutes_snapshot");
            entity.Property(x => x.ApprovalBypassAllowedSnapshot).HasColumnName("approval_bypass_allowed_snapshot");
            entity.Property(x => x.PostReviewRequiredSnapshot).HasColumnName("post_review_required_snapshot");
            entity.Property(x => x.RequiresPostReview).HasColumnName("requires_post_review");
            entity.Property(x => x.EmergencyDeclaredByUserId).HasColumnName("emergency_declared_by_user_id");
            entity.Property(x => x.EmergencyDeclaredAt).HasColumnName("emergency_declared_at");
            entity.Property(x => x.Status).HasColumnName("status").HasMaxLength(50);
            entity.Property(x => x.ReturnTarget).HasColumnName("return_target").HasMaxLength(30);
            entity.Property(x => x.SubmittedAt).HasColumnName("submitted_at");
            entity.Property(x => x.CancelledAt).HasColumnName("cancelled_at");
            entity.Property(x => x.CancellationReason).HasColumnName("cancellation_reason").HasMaxLength(1000);
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            entity.Property(x => x.UpdatedByUserId).HasColumnName("updated_by_user_id");
            entity.Property(x => x.ConcurrencyToken).HasColumnName("concurrency_token").IsConcurrencyToken();
            entity.HasIndex(x => x.RequestNo).IsUnique();
            entity.HasIndex(x => new { x.RequesterUserId, x.Status });
            entity.HasIndex(x => new { x.Status, x.DepartureAt });
            entity.HasIndex(x => new { x.Priority, x.Status, x.SubmittedAt });
            entity.HasOne(x => x.RequesterUser).WithMany().HasForeignKey(x => x.RequesterUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.RequesterDepartment).WithMany().HasForeignKey(x => x.RequesterDepartmentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.RequestedVehicleType).WithMany().HasForeignKey(x => x.RequestedVehicleTypeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.EmergencyPolicy).WithMany().HasForeignKey(x => x.EmergencyPolicyId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.UpdatedByUser).WithMany().HasForeignKey(x => x.UpdatedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FleetRequestPassenger>(entity =>
        {
            entity.ToTable("fleet_request_passengers");
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.FleetRequestId).HasColumnName("fleet_request_id");
            entity.Property(x => x.UserId).HasColumnName("user_id");
            entity.Property(x => x.FullName).HasColumnName("full_name").HasMaxLength(200);
            entity.Property(x => x.PositionOrOrganization).HasColumnName("position_or_organization").HasMaxLength(300);
            entity.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(50);
            entity.Property(x => x.PassengerType).HasColumnName("passenger_type").HasMaxLength(20);
            entity.Property(x => x.IsRequester).HasColumnName("is_requester");
            entity.Property(x => x.SortOrder).HasColumnName("sort_order");
            entity.HasIndex(x => new { x.FleetRequestId, x.SortOrder });
            entity.HasOne(x => x.FleetRequest).WithMany(x => x.Passengers).HasForeignKey(x => x.FleetRequestId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FleetRequestStatusHistory>(entity =>
        {
            entity.ToTable("fleet_request_status_histories");
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.FleetRequestId).HasColumnName("fleet_request_id");
            entity.Property(x => x.FromStatus).HasColumnName("from_status").HasMaxLength(50);
            entity.Property(x => x.ToStatus).HasColumnName("to_status").HasMaxLength(50);
            entity.Property(x => x.Action).HasColumnName("action").HasMaxLength(100);
            entity.Property(x => x.ReturnTarget).HasColumnName("return_target").HasMaxLength(30);
            entity.Property(x => x.Reason).HasColumnName("reason").HasMaxLength(2000);
            entity.Property(x => x.ActorUserId).HasColumnName("actor_user_id");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.CorrelationId).HasColumnName("correlation_id").HasMaxLength(100);
            entity.HasIndex(x => new { x.FleetRequestId, x.CreatedAt });
            entity.HasOne(x => x.FleetRequest).WithMany(x => x.StatusHistories).HasForeignKey(x => x.FleetRequestId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.ActorUser).WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FleetAssignment>(entity =>
        {
            entity.ToTable("fleet_assignments");
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.FleetRequestId).HasColumnName("fleet_request_id");
            entity.Property(x => x.VehicleId).HasColumnName("vehicle_id");
            entity.Property(x => x.DriverUserId).HasColumnName("driver_user_id");
            entity.Property(x => x.AssignedByUserId).HasColumnName("assigned_by_user_id");
            entity.Property(x => x.AssignedAt).HasColumnName("assigned_at");
            entity.Property(x => x.AssignmentStatus).HasColumnName("assignment_status").HasMaxLength(30);
            entity.Property(x => x.AssignmentReason).HasColumnName("assignment_reason").HasMaxLength(1000);
            entity.Property(x => x.ReplacedAssignmentId).HasColumnName("replaced_assignment_id");
            entity.Property(x => x.IsActive).HasColumnName("is_active");
            entity.Property(x => x.ConcurrencyToken).HasColumnName("concurrency_token").IsConcurrencyToken();
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Ignore(x => x.ReplacementOfAssignmentId);
            entity.HasIndex(x => x.FleetRequestId).IsUnique().HasFilter("is_active = true");
            entity.HasIndex(x => new { x.VehicleId, x.IsActive });
            entity.HasIndex(x => new { x.DriverUserId, x.IsActive });
            entity.HasOne(x => x.FleetRequest).WithMany(x => x.Assignments).HasForeignKey(x => x.FleetRequestId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Vehicle).WithMany().HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.DriverUser).WithMany().HasForeignKey(x => x.DriverUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.AssignedByUser).WithMany().HasForeignKey(x => x.AssignedByUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ReplacedAssignment).WithMany().HasForeignKey(x => x.ReplacedAssignmentId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FleetCancellationRequest>(entity =>
        {
            entity.ToTable("fleet_cancellation_requests");
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.FleetRequestId).HasColumnName("fleet_request_id");
            entity.Property(x => x.RequestedByUserId).HasColumnName("requested_by_user_id");
            entity.Property(x => x.PreviousStatus).HasColumnName("previous_status").HasMaxLength(50);
            entity.Property(x => x.Reason).HasColumnName("reason").HasMaxLength(1000);
            entity.Property(x => x.Status).HasColumnName("status").HasMaxLength(30);
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.CompletedAt).HasColumnName("completed_at");
            entity.Property(x => x.ReviewedByUserId).HasColumnName("reviewed_by_user_id");
            entity.Property(x => x.ReviewedAt).HasColumnName("reviewed_at");
            entity.Property(x => x.ReviewReason).HasColumnName("review_reason").HasMaxLength(1000);
            entity.Property(x => x.ConcurrencyToken).HasColumnName("concurrency_token").IsConcurrencyToken();
            entity.HasIndex(x => new { x.FleetRequestId, x.CreatedAt });
            entity.HasOne(x => x.FleetRequest).WithMany(x => x.CancellationRequests).HasForeignKey(x => x.FleetRequestId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.RequestedByUser).WithMany().HasForeignKey(x => x.RequestedByUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ReviewedByUser).WithMany().HasForeignKey(x => x.ReviewedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FleetTripRecord>(entity =>
        {
            entity.ToTable("fleet_trip_records", table =>
            {
                table.HasCheckConstraint("ck_fleet_trip_mileage", "start_mileage IS NULL OR start_mileage >= 0 AND (end_mileage IS NULL OR end_mileage >= start_mileage)");
                table.HasCheckConstraint("ck_fleet_trip_time", "actual_start_at IS NULL OR actual_end_at IS NULL OR actual_end_at >= actual_start_at");
            });
            entity.Property(x => x.Id).HasColumnName("id"); entity.Property(x => x.FleetRequestId).HasColumnName("fleet_request_id"); entity.Property(x => x.AssignmentId).HasColumnName("assignment_id"); entity.Property(x => x.DriverUserId).HasColumnName("driver_user_id");
            entity.Property(x => x.ActualStartAt).HasColumnName("actual_start_at"); entity.Property(x => x.ActualEndAt).HasColumnName("actual_end_at"); entity.Property(x => x.StartMileage).HasColumnName("start_mileage").HasPrecision(12, 2); entity.Property(x => x.EndMileage).HasColumnName("end_mileage").HasPrecision(12, 2);
            entity.Property(x => x.FuelAmount).HasColumnName("fuel_amount").HasPrecision(10, 2); entity.Property(x => x.FuelCost).HasColumnName("fuel_cost").HasPrecision(12, 2); entity.Property(x => x.TripNotes).HasColumnName("trip_notes").HasMaxLength(2000); entity.Property(x => x.CompletionNotes).HasColumnName("completion_notes").HasMaxLength(2000); entity.Property(x => x.StartIdempotencyKey).HasColumnName("start_idempotency_key").HasMaxLength(200); entity.Property(x => x.CompletionIdempotencyKey).HasColumnName("completion_idempotency_key").HasMaxLength(200);
            entity.Property(x => x.CompletedByUserId).HasColumnName("completed_by_user_id"); entity.Property(x => x.OverrideReason).HasColumnName("override_reason").HasMaxLength(1000); entity.Property(x => x.IsAborted).HasColumnName("is_aborted"); entity.Property(x => x.AbortedAt).HasColumnName("aborted_at"); entity.Property(x => x.AbortedByUserId).HasColumnName("aborted_by_user_id"); entity.Property(x => x.AbortReason).HasColumnName("abort_reason").HasMaxLength(1000); entity.Property(x => x.ConcurrencyToken).HasColumnName("concurrency_token").IsConcurrencyToken(); entity.Property(x => x.CreatedAt).HasColumnName("created_at"); entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(x => x.FleetRequestId).IsUnique(); entity.HasIndex(x => x.AssignmentId).IsUnique(); entity.HasIndex(x => x.StartIdempotencyKey).IsUnique().HasFilter("start_idempotency_key IS NOT NULL"); entity.HasIndex(x => x.CompletionIdempotencyKey).IsUnique().HasFilter("completion_idempotency_key IS NOT NULL");
            entity.HasOne(x => x.FleetRequest).WithMany().HasForeignKey(x => x.FleetRequestId).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.Assignment).WithMany().HasForeignKey(x => x.AssignmentId).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.DriverUser).WithMany().HasForeignKey(x => x.DriverUserId).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.CompletedByUser).WithMany().HasForeignKey(x => x.CompletedByUserId).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.AbortedByUser).WithMany().HasForeignKey(x => x.AbortedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FleetTripParticipant>(entity =>
        {
            entity.ToTable("fleet_trip_participants", table =>
                table.HasCheckConstraint("ck_fleet_trip_participants_type", "participant_type IN ('EMPLOYEE','EXTERNAL')"));
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.TripId).HasColumnName("trip_id");
            entity.Property(x => x.UserId).HasColumnName("user_id");
            entity.Property(x => x.IsRequester).HasColumnName("is_requester");
            entity.Property(x => x.ParticipantType).HasColumnName("participant_type").HasMaxLength(20);
            entity.Property(x => x.IsActualParticipant).HasColumnName("is_actual_participant");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.HasIndex(x => new { x.TripId, x.UserId }).IsUnique().HasFilter("user_id IS NOT NULL");
            entity.HasIndex(x => new { x.UserId, x.IsActualParticipant });
            entity.HasOne(x => x.Trip).WithMany(x => x.Participants).HasForeignKey(x => x.TripId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FleetTripFeedback>(entity =>
        {
            entity.ToTable("fleet_trip_feedbacks", table =>
            {
                table.HasCheckConstraint("ck_fleet_trip_feedback_ratings", "punctuality_rating BETWEEN 1 AND 5 AND safety_rating BETWEEN 1 AND 5 AND service_rating BETWEEN 1 AND 5 AND overall_rating BETWEEN 1 AND 5 AND vehicle_condition_rating BETWEEN 1 AND 5 AND vehicle_cleanliness_rating BETWEEN 1 AND 5");
                table.HasCheckConstraint("ck_fleet_trip_feedback_incident", "(has_incident = FALSE AND incident_category IS NULL) OR (has_incident = TRUE AND incident_category IS NOT NULL AND comment IS NOT NULL)");
                table.HasCheckConstraint("ck_fleet_trip_feedback_incident_category", "incident_category IS NULL OR incident_category IN ('DRIVING','PUNCTUALITY','SERVICE','VEHICLE_CONDITION','CLEANLINESS','OTHER')");
            });
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.TripId).HasColumnName("trip_id");
            entity.Property(x => x.FleetRequestId).HasColumnName("fleet_request_id");
            entity.Property(x => x.VehicleAssignmentId).HasColumnName("vehicle_assignment_id");
            entity.Property(x => x.VehicleId).HasColumnName("vehicle_id");
            entity.Property(x => x.DriverUserId).HasColumnName("driver_user_id");
            entity.Property(x => x.SubmittedByUserId).HasColumnName("submitted_by_user_id");
            entity.Property(x => x.PunctualityRating).HasColumnName("punctuality_rating");
            entity.Property(x => x.SafetyRating).HasColumnName("safety_rating");
            entity.Property(x => x.ServiceRating).HasColumnName("service_rating");
            entity.Property(x => x.OverallRating).HasColumnName("overall_rating");
            entity.Property(x => x.VehicleConditionRating).HasColumnName("vehicle_condition_rating");
            entity.Property(x => x.VehicleCleanlinessRating).HasColumnName("vehicle_cleanliness_rating");
            entity.Property(x => x.HasIncident).HasColumnName("has_incident");
            entity.Property(x => x.IncidentCategory).HasColumnName("incident_category").HasMaxLength(40);
            entity.Property(x => x.Comment).HasColumnName("comment").HasMaxLength(2000);
            entity.Property(x => x.SubmittedAt).HasColumnName("submitted_at");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            entity.Property(x => x.UpdatedByUserId).HasColumnName("updated_by_user_id");
            entity.Property(x => x.ConcurrencyToken).HasColumnName("concurrency_token").IsConcurrencyToken();
            entity.HasIndex(x => new { x.TripId, x.SubmittedByUserId }).IsUnique();
            entity.HasIndex(x => new { x.SubmittedByUserId, x.SubmittedAt });
            entity.HasIndex(x => new { x.DriverUserId, x.SubmittedAt });
            entity.HasIndex(x => new { x.VehicleId, x.SubmittedAt });
            entity.HasOne(x => x.Trip).WithMany(x => x.Feedbacks).HasForeignKey(x => x.TripId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.FleetRequest).WithMany().HasForeignKey(x => x.FleetRequestId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.VehicleAssignment).WithMany().HasForeignKey(x => x.VehicleAssignmentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Vehicle).WithMany().HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.DriverUser).WithMany().HasForeignKey(x => x.DriverUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.SubmittedByUser).WithMany().HasForeignKey(x => x.SubmittedByUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.UpdatedByUser).WithMany().HasForeignKey(x => x.UpdatedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DomainEventRecord>(entity =>
        {
            entity.ToTable("domain_events"); entity.HasKey(x => x.EventId); entity.Property(x => x.EventId).HasColumnName("event_id"); entity.Property(x => x.EventType).HasColumnName("event_type").HasMaxLength(160); entity.Property(x => x.Scope).HasColumnName("scope").HasMaxLength(30); entity.Property(x => x.AggregateType).HasColumnName("aggregate_type").HasMaxLength(100); entity.Property(x => x.AggregateId).HasColumnName("aggregate_id"); entity.Property(x => x.OccurredAt).HasColumnName("occurred_at"); entity.Property(x => x.ActorUserId).HasColumnName("actor_user_id"); entity.Property(x => x.CorrelationId).HasColumnName("correlation_id").HasMaxLength(100); entity.Property(x => x.Payload).HasColumnName("payload").HasColumnType("jsonb"); entity.HasIndex(x => new { x.Scope, x.EventType, x.OccurredAt });
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("outbox_messages"); entity.Property(x => x.Id).HasColumnName("id"); entity.Property(x => x.EventId).HasColumnName("event_id"); entity.Property(x => x.EventType).HasColumnName("event_type").HasMaxLength(160); entity.Property(x => x.Scope).HasColumnName("scope").HasMaxLength(30); entity.Property(x => x.Payload).HasColumnName("payload").HasColumnType("jsonb"); entity.Property(x => x.CreatedAt).HasColumnName("created_at"); entity.Property(x => x.AvailableAt).HasColumnName("available_at"); entity.Property(x => x.ProcessedAt).HasColumnName("processed_at"); entity.Property(x => x.AttemptCount).HasColumnName("attempt_count"); entity.Property(x => x.LastError).HasColumnName("last_error").HasMaxLength(2000); entity.Property(x => x.Status).HasColumnName("status").HasMaxLength(30); entity.Property(x => x.ConcurrencyToken).HasColumnName("concurrency_token").IsConcurrencyToken(); entity.HasIndex(x => x.EventId).IsUnique(); entity.HasIndex(x => new { x.Status, x.AvailableAt }); entity.HasOne(x => x.DomainEvent).WithOne().HasForeignKey<OutboxMessage>(x => x.EventId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<NotificationDelivery>(entity =>
        {
            entity.ToTable("notification_deliveries"); entity.Property(x => x.Id).HasColumnName("id"); entity.Property(x => x.OutboxMessageId).HasColumnName("outbox_message_id"); entity.Property(x => x.RecipientUserId).HasColumnName("recipient_user_id"); entity.Property(x => x.Channel).HasColumnName("channel").HasMaxLength(30); entity.Property(x => x.Status).HasColumnName("status").HasMaxLength(30); entity.Property(x => x.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(300); entity.Property(x => x.AttemptCount).HasColumnName("attempt_count"); entity.Property(x => x.LastError).HasColumnName("last_error").HasMaxLength(2000); entity.Property(x => x.CreatedAt).HasColumnName("created_at"); entity.Property(x => x.ProcessedAt).HasColumnName("processed_at"); entity.HasIndex(x => x.IdempotencyKey).IsUnique(); entity.HasIndex(x => new { x.Status, x.CreatedAt }); entity.HasOne(x => x.OutboxMessage).WithMany(x => x.Deliveries).HasForeignKey(x => x.OutboxMessageId).OnDelete(DeleteBehavior.Cascade); entity.HasOne(x => x.RecipientUser).WithMany().HasForeignKey(x => x.RecipientUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FleetMaintenanceType>(entity =>
        {
            entity.ToTable("fleet_maintenance_types", t => { t.HasCheckConstraint("ck_fleet_maintenance_types_reminders", "default_reminder_days IS NULL OR default_reminder_days >= 0 AND (default_reminder_mileage IS NULL OR default_reminder_mileage >= 0)"); t.HasCheckConstraint("ck_fleet_maintenance_types_basis", "is_date_based OR is_mileage_based"); });
            entity.Property(x => x.Id).HasColumnName("id"); entity.Property(x => x.Code).HasColumnName("code").HasMaxLength(50); entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(200); entity.Property(x => x.Description).HasColumnName("description").HasMaxLength(1000); entity.Property(x => x.Category).HasColumnName("category").HasMaxLength(50); entity.Property(x => x.IsDateBased).HasColumnName("is_date_based"); entity.Property(x => x.IsMileageBased).HasColumnName("is_mileage_based"); entity.Property(x => x.BlocksAvailabilityWhenOverdue).HasColumnName("blocks_availability_when_overdue"); entity.Property(x => x.DefaultReminderDays).HasColumnName("default_reminder_days"); entity.Property(x => x.DefaultReminderMileage).HasColumnName("default_reminder_mileage").HasPrecision(12, 2); entity.Property(x => x.IsActive).HasColumnName("is_active"); entity.Property(x => x.SortOrder).HasColumnName("sort_order"); entity.Property(x => x.ConcurrencyToken).HasColumnName("concurrency_token").IsConcurrencyToken(); entity.Property(x => x.CreatedAt).HasColumnName("created_at"); entity.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id"); entity.Property(x => x.UpdatedAt).HasColumnName("updated_at"); entity.Property(x => x.UpdatedByUserId).HasColumnName("updated_by_user_id"); entity.HasIndex(x => x.Code).IsUnique(); entity.HasIndex(x => new { x.IsActive, x.SortOrder });
        });
        modelBuilder.Entity<FleetVehicleMaintenanceSchedule>(entity =>
        {
            entity.ToTable("fleet_vehicle_maintenance_schedules", t => t.HasCheckConstraint("ck_fleet_maintenance_schedule_values", "(due_mileage IS NULL OR due_mileage >= 0) AND (reminder_days IS NULL OR reminder_days >= 0) AND (reminder_mileage IS NULL OR reminder_mileage >= 0) AND (recurrence_days IS NULL OR recurrence_days > 0) AND (recurrence_mileage IS NULL OR recurrence_mileage > 0)"));
            entity.Property(x => x.Id).HasColumnName("id"); entity.Property(x => x.VehicleId).HasColumnName("vehicle_id"); entity.Property(x => x.MaintenanceTypeId).HasColumnName("maintenance_type_id"); entity.Property(x => x.DueDate).HasColumnName("due_date"); entity.Property(x => x.DueMileage).HasColumnName("due_mileage").HasPrecision(12, 2); entity.Property(x => x.ReminderDays).HasColumnName("reminder_days"); entity.Property(x => x.ReminderMileage).HasColumnName("reminder_mileage").HasPrecision(12, 2); entity.Property(x => x.RecurrenceDays).HasColumnName("recurrence_days"); entity.Property(x => x.RecurrenceMileage).HasColumnName("recurrence_mileage").HasPrecision(12, 2); entity.Property(x => x.Status).HasColumnName("status").HasMaxLength(30); entity.Property(x => x.IsActive).HasColumnName("is_active"); entity.Property(x => x.Notes).HasColumnName("notes").HasMaxLength(2000); entity.Property(x => x.LastCompletedRecordId).HasColumnName("last_completed_record_id"); entity.Property(x => x.ConcurrencyToken).HasColumnName("concurrency_token").IsConcurrencyToken(); entity.Property(x => x.CreatedAt).HasColumnName("created_at"); entity.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id"); entity.Property(x => x.UpdatedAt).HasColumnName("updated_at"); entity.Property(x => x.UpdatedByUserId).HasColumnName("updated_by_user_id"); entity.HasIndex(x => new { x.VehicleId, x.IsActive, x.Status }); entity.HasIndex(x => x.DueDate); entity.HasIndex(x => x.DueMileage); entity.HasOne(x => x.Vehicle).WithMany().HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.MaintenanceType).WithMany().HasForeignKey(x => x.MaintenanceTypeId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<FleetVehicleMaintenanceRecord>(entity =>
        {
            entity.Property(x => x.StartMileage).HasColumnName("start_mileage").HasPrecision(12, 2);
            entity.ToTable("fleet_vehicle_maintenance_records", t => { t.HasCheckConstraint("ck_fleet_maintenance_record_cost", "cost IS NULL OR cost >= 0"); t.HasCheckConstraint("ck_fleet_maintenance_record_mileage", "completed_mileage IS NULL OR completed_mileage >= 0"); t.HasCheckConstraint("ck_fleet_maintenance_record_start_mileage", "start_mileage IS NULL OR start_mileage >= 0"); t.HasCheckConstraint("ck_fleet_maintenance_record_dates", "completed_at IS NULL OR completed_at >= started_at"); });
            entity.Property(x => x.Id).HasColumnName("id"); entity.Property(x => x.VehicleId).HasColumnName("vehicle_id"); entity.Property(x => x.MaintenanceScheduleId).HasColumnName("maintenance_schedule_id"); entity.Property(x => x.MaintenanceTypeId).HasColumnName("maintenance_type_id"); entity.Property(x => x.StartedAt).HasColumnName("started_at"); entity.Property(x => x.CompletedAt).HasColumnName("completed_at"); entity.Property(x => x.CompletedMileage).HasColumnName("completed_mileage").HasPrecision(12, 2); entity.Property(x => x.Cost).HasColumnName("cost").HasPrecision(14, 2); entity.Property(x => x.Vendor).HasColumnName("vendor").HasMaxLength(300); entity.Property(x => x.InvoiceNumber).HasColumnName("invoice_number").HasMaxLength(100); entity.Property(x => x.Description).HasColumnName("description").HasMaxLength(2000); entity.Property(x => x.Result).HasColumnName("result").HasMaxLength(2000); entity.Property(x => x.PerformedBy).HasColumnName("performed_by").HasMaxLength(300); entity.Property(x => x.NextDueDate).HasColumnName("next_due_date"); entity.Property(x => x.NextDueMileage).HasColumnName("next_due_mileage").HasPrecision(12, 2); entity.Property(x => x.IsCancelled).HasColumnName("is_cancelled"); entity.Property(x => x.CancellationReason).HasColumnName("cancellation_reason").HasMaxLength(1000); entity.Property(x => x.ConcurrencyToken).HasColumnName("concurrency_token").IsConcurrencyToken(); entity.Property(x => x.CreatedAt).HasColumnName("created_at"); entity.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id"); entity.Property(x => x.UpdatedAt).HasColumnName("updated_at"); entity.Property(x => x.UpdatedByUserId).HasColumnName("updated_by_user_id"); entity.HasIndex(x => new { x.VehicleId, x.StartedAt }); entity.HasIndex(x => x.MaintenanceScheduleId); entity.HasOne(x => x.Schedule).WithMany(x => x.Records).HasForeignKey(x => x.MaintenanceScheduleId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<FleetMaintenanceAttachment>(entity =>
        {
            entity.ToTable("fleet_maintenance_attachments"); entity.Property(x => x.Id).HasColumnName("id"); entity.Property(x => x.MaintenanceRecordId).HasColumnName("maintenance_record_id"); entity.Property(x => x.OriginalFileName).HasColumnName("original_file_name").HasMaxLength(260); entity.Property(x => x.StoredFileName).HasColumnName("stored_file_name").HasMaxLength(100); entity.Property(x => x.ContentType).HasColumnName("content_type").HasMaxLength(100); entity.Property(x => x.FilePath).HasColumnName("file_path").HasMaxLength(1000); entity.Property(x => x.FileSize).HasColumnName("file_size"); entity.Property(x => x.IsDeleted).HasColumnName("is_deleted"); entity.Property(x => x.DeletedAt).HasColumnName("deleted_at"); entity.Property(x => x.DeletedByUserId).HasColumnName("deleted_by_user_id"); entity.Property(x => x.CreatedAt).HasColumnName("created_at"); entity.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id"); entity.HasIndex(x => new { x.MaintenanceRecordId, x.IsDeleted }); entity.HasOne(x => x.MaintenanceRecord).WithMany(x => x.Attachments).HasForeignKey(x => x.MaintenanceRecordId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<FleetVehicleDocument>(entity =>
        {
            entity.ToTable("fleet_vehicle_documents", t => t.HasCheckConstraint("ck_fleet_vehicle_document_dates", "issued_at IS NULL OR expires_at IS NULL OR expires_at >= issued_at")); entity.Property(x => x.Id).HasColumnName("id"); entity.Property(x => x.VehicleId).HasColumnName("vehicle_id"); entity.Property(x => x.DocumentType).HasColumnName("document_type").HasMaxLength(50); entity.Property(x => x.DocumentNumber).HasColumnName("document_number").HasMaxLength(200); entity.Property(x => x.IssuedAt).HasColumnName("issued_at"); entity.Property(x => x.ExpiresAt).HasColumnName("expires_at"); entity.Property(x => x.Provider).HasColumnName("provider").HasMaxLength(300); entity.Property(x => x.Notes).HasColumnName("notes").HasMaxLength(2000); entity.Property(x => x.IsRequired).HasColumnName("is_required"); entity.Property(x => x.IsActive).HasColumnName("is_active"); entity.Property(x => x.ConcurrencyToken).HasColumnName("concurrency_token").IsConcurrencyToken(); entity.Property(x => x.CreatedAt).HasColumnName("created_at"); entity.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id"); entity.Property(x => x.UpdatedAt).HasColumnName("updated_at"); entity.Property(x => x.UpdatedByUserId).HasColumnName("updated_by_user_id"); entity.HasIndex(x => new { x.VehicleId, x.IsActive, x.DocumentType }); entity.HasIndex(x => x.ExpiresAt); entity.HasOne(x => x.Vehicle).WithMany().HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FleetCapability>(entity =>
        {
            entity.Property(x=>x.MinimumNumericValue).HasColumnName("minimum_numeric_value").HasPrecision(14,2);
            entity.Property(x=>x.MaximumNumericValue).HasColumnName("maximum_numeric_value").HasPrecision(14,2);
            entity.Property(x=>x.EnumOptionsJson).HasColumnName("enum_options_json").HasColumnType("jsonb");
            entity.ToTable("fleet_capabilities", t => t.HasCheckConstraint("ck_fleet_capability_type", "data_type IN ('BOOLEAN','NUMBER','TEXT','ENUM')"));
            entity.Property(x=>x.Id).HasColumnName("id"); entity.Property(x=>x.Code).HasColumnName("code").HasMaxLength(80); entity.Property(x=>x.Name).HasColumnName("name").HasMaxLength(200); entity.Property(x=>x.Description).HasColumnName("description").HasMaxLength(1000); entity.Property(x=>x.Category).HasColumnName("category").HasMaxLength(80); entity.Property(x=>x.DataType).HasColumnName("data_type").HasMaxLength(20); entity.Property(x=>x.Unit).HasColumnName("unit").HasMaxLength(50); entity.Property(x=>x.IsRequiredSafetyCapability).HasColumnName("is_required_safety_capability"); entity.Property(x=>x.IsActive).HasColumnName("is_active"); entity.Property(x=>x.SortOrder).HasColumnName("sort_order"); entity.Property(x=>x.ConcurrencyToken).HasColumnName("concurrency_token").IsConcurrencyToken(); entity.Property(x=>x.CreatedAt).HasColumnName("created_at"); entity.Property(x=>x.CreatedByUserId).HasColumnName("created_by_user_id"); entity.Property(x=>x.UpdatedAt).HasColumnName("updated_at"); entity.Property(x=>x.UpdatedByUserId).HasColumnName("updated_by_user_id"); entity.HasIndex(x=>x.Code).IsUnique(); entity.HasIndex(x=>new{x.IsActive,x.SortOrder});
        });
        modelBuilder.Entity<FleetVehicleCapability>(entity =>
        {
            entity.ToTable("fleet_vehicle_capabilities", t => { t.HasCheckConstraint("ck_fleet_vehicle_capability_value", "num_nonnulls(boolean_value,numeric_value,text_value,enum_value)=1"); t.HasCheckConstraint("ck_fleet_vehicle_capability_dates", "effective_to IS NULL OR effective_to > effective_from"); });
            entity.Property(x=>x.Id).HasColumnName("id"); entity.Property(x=>x.VehicleId).HasColumnName("vehicle_id"); entity.Property(x=>x.CapabilityId).HasColumnName("capability_id"); entity.Property(x=>x.BooleanValue).HasColumnName("boolean_value"); entity.Property(x=>x.NumericValue).HasColumnName("numeric_value").HasPrecision(14,2); entity.Property(x=>x.TextValue).HasColumnName("text_value").HasMaxLength(1000); entity.Property(x=>x.EnumValue).HasColumnName("enum_value").HasMaxLength(200); entity.Property(x=>x.EffectiveFrom).HasColumnName("effective_from"); entity.Property(x=>x.EffectiveTo).HasColumnName("effective_to"); entity.Property(x=>x.IsActive).HasColumnName("is_active"); entity.Property(x=>x.ConcurrencyToken).HasColumnName("concurrency_token").IsConcurrencyToken(); entity.Property(x=>x.CreatedAt).HasColumnName("created_at"); entity.Property(x=>x.CreatedByUserId).HasColumnName("created_by_user_id"); entity.HasIndex(x=>new{x.VehicleId,x.CapabilityId}).IsUnique().HasFilter("is_active = true AND effective_to IS NULL"); entity.HasOne(x=>x.Vehicle).WithMany().HasForeignKey(x=>x.VehicleId).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x=>x.Capability).WithMany().HasForeignKey(x=>x.CapabilityId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<FleetRequestRequiredCapability>(entity =>
        {
            entity.ToTable("fleet_request_required_capabilities", t => { t.HasCheckConstraint("ck_fleet_request_capability_operator", "operator IN ('EQUALS','GREATER_THAN_OR_EQUAL','LESS_THAN_OR_EQUAL','CONTAINS','IN')"); t.HasCheckConstraint("ck_fleet_request_capability_value", "num_nonnulls(required_boolean_value,required_numeric_value,required_text_value,required_enum_value)=1"); });
            entity.Property(x=>x.Id).HasColumnName("id"); entity.Property(x=>x.FleetRequestId).HasColumnName("fleet_request_id"); entity.Property(x=>x.CapabilityId).HasColumnName("capability_id"); entity.Property(x=>x.Operator).HasColumnName("operator").HasMaxLength(40); entity.Property(x=>x.RequiredBooleanValue).HasColumnName("required_boolean_value"); entity.Property(x=>x.RequiredNumericValue).HasColumnName("required_numeric_value").HasPrecision(14,2); entity.Property(x=>x.RequiredTextValue).HasColumnName("required_text_value").HasMaxLength(1000); entity.Property(x=>x.RequiredEnumValue).HasColumnName("required_enum_value").HasMaxLength(200); entity.Property(x=>x.IsMandatory).HasColumnName("is_mandatory"); entity.Property(x=>x.Notes).HasColumnName("notes").HasMaxLength(1000); entity.Property(x=>x.CreatedAt).HasColumnName("created_at"); entity.HasIndex(x=>new{x.FleetRequestId,x.CapabilityId}).IsUnique(); entity.HasOne(x=>x.FleetRequest).WithMany(x=>x.RequiredCapabilities).HasForeignKey(x=>x.FleetRequestId).OnDelete(DeleteBehavior.Cascade); entity.HasOne(x=>x.Capability).WithMany().HasForeignKey(x=>x.CapabilityId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<FleetCompatibilityOverride>(entity =>
        {
            entity.ToTable("fleet_compatibility_overrides",t=>t.HasCheckConstraint("ck_fleet_compatibility_override_mismatches","jsonb_typeof(mismatch_capability_ids) = 'array' AND jsonb_array_length(mismatch_capability_ids) > 0")); entity.Property(x=>x.Id).HasColumnName("id"); entity.Property(x=>x.FleetRequestId).HasColumnName("fleet_request_id"); entity.Property(x=>x.VehicleId).HasColumnName("vehicle_id"); entity.Property(x=>x.AssignmentId).HasColumnName("assignment_id"); entity.Property(x=>x.MismatchCapabilityIds).HasColumnName("mismatch_capability_ids").HasColumnType("jsonb"); entity.Property(x=>x.Reason).HasColumnName("reason").HasMaxLength(2000); entity.Property(x=>x.ApprovedByUserId).HasColumnName("approved_by_user_id"); entity.Property(x=>x.CreatedAt).HasColumnName("created_at"); entity.Property(x=>x.ConcurrencyToken).HasColumnName("concurrency_token").IsConcurrencyToken(); entity.HasIndex(x=>new{x.FleetRequestId,x.VehicleId,x.CreatedAt}); entity.HasOne(x=>x.FleetRequest).WithMany().HasForeignKey(x=>x.FleetRequestId).OnDelete(DeleteBehavior.Restrict);entity.HasOne(x=>x.Vehicle).WithMany().HasForeignKey(x=>x.VehicleId).OnDelete(DeleteBehavior.Restrict);entity.HasOne(x=>x.Assignment).WithMany().HasForeignKey(x=>x.AssignmentId).OnDelete(DeleteBehavior.Restrict);entity.HasOne(x=>x.ApprovedByUser).WithMany().HasForeignKey(x=>x.ApprovedByUserId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<FleetEmergencyPostReview>(entity =>
        {
            entity.ToTable("fleet_emergency_post_reviews", t=>t.HasCheckConstraint("ck_fleet_emergency_review_outcome", "outcome IN ('ACCEPTABLE','NEEDS_IMPROVEMENT','POLICY_VIOLATION')")); entity.Property(x=>x.Id).HasColumnName("id"); entity.Property(x=>x.FleetRequestId).HasColumnName("fleet_request_id"); entity.Property(x=>x.ReviewedByUserId).HasColumnName("reviewed_by_user_id"); entity.Property(x=>x.ReviewedAt).HasColumnName("reviewed_at"); entity.Property(x=>x.Outcome).HasColumnName("outcome").HasMaxLength(40); entity.Property(x=>x.WasBypassAppropriate).HasColumnName("was_bypass_appropriate"); entity.Property(x=>x.ResponseTimeAssessment).HasColumnName("response_time_assessment").HasMaxLength(2000); entity.Property(x=>x.SafetyIssues).HasColumnName("safety_issues").HasMaxLength(4000); entity.Property(x=>x.FollowUpActions).HasColumnName("follow_up_actions").HasMaxLength(4000); entity.Property(x=>x.Notes).HasColumnName("notes").HasMaxLength(4000); entity.Property(x=>x.ConcurrencyToken).HasColumnName("concurrency_token").IsConcurrencyToken(); entity.Property(x=>x.CreatedAt).HasColumnName("created_at"); entity.HasIndex(x=>x.FleetRequestId).IsUnique();entity.HasOne(x=>x.FleetRequest).WithMany().HasForeignKey(x=>x.FleetRequestId).OnDelete(DeleteBehavior.Restrict);entity.HasOne(x=>x.ReviewedByUser).WithMany().HasForeignKey(x=>x.ReviewedByUserId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<FleetTripAttachment>(entity =>
        {
            entity.Property(x=>x.IsDeleted).HasColumnName("is_deleted");
            entity.Property(x=>x.DeletedAt).HasColumnName("deleted_at");
            entity.Property(x=>x.DeletedByUserId).HasColumnName("deleted_by_user_id");
            entity.ToTable("fleet_trip_attachments",t=>t.HasCheckConstraint("ck_fleet_trip_attachment_size","file_size > 0")); entity.Property(x=>x.Id).HasColumnName("id"); entity.Property(x=>x.TripId).HasColumnName("trip_id"); entity.Property(x=>x.OriginalFileName).HasColumnName("original_file_name").HasMaxLength(260); entity.Property(x=>x.StoredFileName).HasColumnName("stored_file_name").HasMaxLength(100); entity.Property(x=>x.ContentType).HasColumnName("content_type").HasMaxLength(100); entity.Property(x=>x.FilePath).HasColumnName("file_path").HasMaxLength(1000); entity.Property(x=>x.FileSize).HasColumnName("file_size"); entity.Property(x=>x.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(200); entity.Property(x=>x.CreatedAt).HasColumnName("created_at"); entity.Property(x=>x.CreatedByUserId).HasColumnName("created_by_user_id"); entity.HasIndex(x=>x.IdempotencyKey).IsUnique(); entity.HasIndex(x=>x.TripId);entity.HasOne(x=>x.Trip).WithMany().HasForeignKey(x=>x.TripId).OnDelete(DeleteBehavior.Restrict);entity.HasOne(x=>x.CreatedByUser).WithMany().HasForeignKey(x=>x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FleetEmergencyPolicy>(entity =>
        {
            entity.ToTable("fleet_emergency_policies", t => { t.HasCheckConstraint("ck_fleet_emergency_policy_priority", "priority IN ('URGENT','EMERGENCY')"); t.HasCheckConstraint("ck_fleet_emergency_policy_targets", "response_target_minutes > 0 AND dispatch_target_minutes > 0 AND driver_ack_target_minutes > 0"); t.HasCheckConstraint("ck_fleet_emergency_policy_dates", "effective_to IS NULL OR effective_to > effective_from"); });
            entity.Property(x=>x.Id).HasColumnName("id"); entity.Property(x=>x.Code).HasColumnName("code").HasMaxLength(80); entity.Property(x=>x.Name).HasColumnName("name").HasMaxLength(200); entity.Property(x=>x.Priority).HasColumnName("priority").HasMaxLength(20); entity.Property(x=>x.ResponseTargetMinutes).HasColumnName("response_target_minutes"); entity.Property(x=>x.DispatchTargetMinutes).HasColumnName("dispatch_target_minutes"); entity.Property(x=>x.DriverAcknowledgementTargetMinutes).HasColumnName("driver_ack_target_minutes"); entity.Property(x=>x.ApprovalBypassAllowed).HasColumnName("approval_bypass_allowed"); entity.Property(x=>x.PostReviewRequired).HasColumnName("post_review_required"); entity.Property(x=>x.IsActive).HasColumnName("is_active"); entity.Property(x=>x.EffectiveFrom).HasColumnName("effective_from"); entity.Property(x=>x.EffectiveTo).HasColumnName("effective_to"); entity.Property(x=>x.ConcurrencyToken).HasColumnName("concurrency_token").IsConcurrencyToken(); entity.Property(x=>x.CreatedAt).HasColumnName("created_at"); entity.Property(x=>x.CreatedByUserId).HasColumnName("created_by_user_id"); entity.Property(x=>x.UpdatedAt).HasColumnName("updated_at"); entity.Property(x=>x.UpdatedByUserId).HasColumnName("updated_by_user_id"); entity.HasIndex(x=>x.Code).IsUnique(); entity.HasIndex(x=>new{x.Priority,x.IsActive,x.EffectiveFrom});
        });

        modelBuilder.Entity<FleetRolloutSetting>(entity =>
        {
            entity.ToTable("fleet_rollout_settings", table => table.HasCheckConstraint("ck_fleet_rollout_mode", "mode IN ('Disabled', 'UATOnly', 'Enabled')"));
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.Mode).HasColumnName("mode").HasMaxLength(20);
            entity.Property(x => x.UatUserIds).HasColumnName("uat_user_ids").HasColumnType("uuid[]");
            entity.Property(x => x.UatRoleCodes).HasColumnName("uat_role_codes").HasColumnType("text[]");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            entity.Property(x => x.UpdatedByUserId).HasColumnName("updated_by_user_id");
            entity.Property(x => x.ConcurrencyToken).HasColumnName("concurrency_token").IsConcurrencyToken();
            entity.HasIndex(x => x.CreatedAt);
            entity.HasOne(x => x.UpdatedByUser).WithMany().HasForeignKey(x => x.UpdatedByUserId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static FleetStatusDefinition FleetStatus(string id, string code, string thaiName, int sortOrder, DateTime createdAt) => new()
    {
        Id = Guid.Parse(id),
        Domain = FleetStatusDomains.Request,
        Code = code,
        ThaiName = thaiName,
        SortOrder = sortOrder,
        IsActive = true,
        CreatedAt = createdAt
    };
}
