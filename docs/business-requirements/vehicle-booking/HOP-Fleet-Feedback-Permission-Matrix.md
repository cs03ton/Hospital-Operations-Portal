# HOP Fleet Feedback Permission Matrix

| Capability | Participant | Driver | Dispatcher | Admin Reviewer | Admin / Admin Support | Director |
| --- | --- | --- | --- | --- | --- | --- |
| Create eligible feedback | Eligibility + `FleetFeedback.Create` | No for own Trip | No | No | When eligible | When eligible |
| View own feedback | `FleetFeedback.ViewOwn` | Own participant feedback only | No | No | Own | Own |
| Management report | No | No | No | No automatic access | `FleetFeedback.ViewManagement` | `FleetFeedback.ViewManagement` |
| View identity | No | No | No | No | `FleetFeedback.ViewIdentity` | Only when explicitly granted |
| Manage | No | No | No | No | `FleetFeedback.Manage` | No by default |

M1 migration เพิ่ม permission catalog เท่านั้นและไม่ assign production roles อัตโนมัติ การ mapping role จะทำพร้อม management endpoints โดยต้องตรวจ role จริงก่อน rollout
