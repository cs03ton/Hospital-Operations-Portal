# Fleet Notification Matrix

| Canonical event | Default | Existing source event mapping |
|---|---:|---|
| Fleet.RequestSubmitted | On | Fleet.RequestSubmitted |
| Fleet.AssignmentCreated | On | Fleet.VehicleAssigned / assignment event |
| Fleet.AdminReviewed | Off | Fleet.AdminReviewApproved |
| Fleet.Returned | On | Fleet.RequestReturned |
| Fleet.DirectorApproved | On | Fleet.DirectorApproved |
| Fleet.Rejected | On | Fleet.RequestRejected |
| Fleet.Cancelled | On | Fleet.RequestCancelled / cancellation result |
| Fleet.AssignmentChanged | On | Fleet.AssignmentReplaced |
| Fleet.DriverAcknowledged | Off | Fleet.DriverAccepted |
| Fleet.TripCompleted | On | Fleet.TripCompleted |
| Fleet.TripOverdue | On | evaluator event to be finalized in M2 |

M2 ต้องใช้ mapper กลาง ไม่แก้ชื่อ workflow event เดิม และ template ต้องละข้อมูลสุขภาพ ผู้ป่วย ใบขับขี่ และเหตุผลการลาคนขับ
