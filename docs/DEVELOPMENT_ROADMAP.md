# HOP Development Roadmap

This roadmap follows the order defined in `docs/SETUP-PROJECT.md`.

## Phase 0: Project Foundation

Status: Done

- Create root folder structure
- Create frontend scaffold
- Create backend scaffold
- Create database foundation
- Create Docker foundation
- Create deployment folder
- Update documentation

## Phase 1: Core Platform

Status: Foundation Done

Completed:

- Authentication and login
- JWT and refresh token foundation
- User management API foundation
- Department management API foundation
- Role foundation
- Main layout and protected route foundation
- Dashboard placeholder behind authentication

Phase 1.1 completed:

1. Frontend forms for user create/edit
2. Frontend forms for department create/edit
3. Role and permission management UI
4. EF Core migration
5. Dashboard data integration
6. Refresh token auto-retry

Phase 1.2 completed:

1. Fine-grained permission enforcement in backend policies
2. Frontend route/menu/action permission guards
3. Audit log viewer API and page
4. Branding integration with hospital logo and logo-based MUI theme
5. Production environment configuration foundation
6. Leave Management database model
7. Backup strategy documentation

Phase 2 started:

1. Leave Management APIs and Thai UI
2. Basic approval workflow
3. Upload storage for leave attachments
4. Session management and refresh token reuse detection
5. Audit log export and retention execution
6. LINE Messaging placeholder service

Phase 2.1 completed:

1. Configurable multi-step leave approval chains
2. Leave attachment download endpoint with access control
3. Leave balance adjustment admin tools
4. Leave holiday management and working-day validation
5. Overlap and remaining-balance validation on submit
6. LINE delivery log foundation for future retry worker

Recommended next focus:

1. Virus scanning for uploaded/downloaded attachments
2. Background LINE sender worker with retry execution
3. Approval delegation and escalation
4. Leave reporting and export
5. More granular session self-service controls

## Phase 2: Approval and Notification

1. Shared approval engine
2. Approval logs
3. In-app notifications
4. LINE Messaging API integration
5. Notification templates

## Phase 3: Operations Modules

1. Repair management
2. Asset borrowing
3. Material request
4. Vehicle booking
5. Meeting room booking

## Phase 4: Inventory and Reports

1. Inventory management
2. Standard reports
3. Export PDF
4. Export Excel
5. Executive dashboard

## Phase 5: Production Readiness

1. Environment-specific configuration
2. HTTPS and Nginx production hardening
3. Database backup workflow
4. Monitoring
5. Audit log review tools
6. HOSxP integration planning
