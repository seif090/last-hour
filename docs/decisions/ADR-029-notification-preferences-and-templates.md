# ADR-029: Notification Preferences, Templates, and Policy Resolution Subsystem

## Status
Accepted

## Date
2026-08-20

## Context
Following the completion of external multi-channel notification delivery (ADR-028), the **LastHour Backend** requires a production-grade **Notification Preferences, Templates, Policy Resolution, and Quiet Hours** subsystem.

Critical architectural requirements:
1. User Notification Preferences (`NotificationPreference` aggregate) allowing users to configure channel preferences per notification type.
2. Default Preference Policy: Security notifications (`UserPasswordChanged`, `UserLockedOut`) default to `[InApp, Email]`; standard notifications default to `[InApp]`. SMS and Push are disabled by default.
3. InApp Security Invariant: `InApp` notification delivery for security events cannot be disabled by user opt-out. Email can be opted out by explicit user preference.
4. Notification Templates (`NotificationTemplate` aggregate) supporting deterministic versioning `(TemplateKey, Channel, Version)` and active state configuration.
5. Provider-Neutral Template Rendering: Safe placeholder substitution (`{{VariableName}}`) without reflection, expression evaluation, or external provider SDK dependencies.
6. User Quiet Hours (`NotificationQuietHours` entity): Deferring external channels (Email/SMS/Push) during local quiet hours while leaving InApp delivery immediate.
7. Anti-Leak Security & Authorization: Endpoint parameters must never accept client-supplied `userId` values; authenticated user identity is resolved strictly from JWT `sub` claims (`ClaimsPrincipal.GetUserId()`).
8. Zero PII Observability: Metric dimensions and log templates must never expose rendered template bodies, recipient emails, phone numbers, or user IDs.

## Decisions

### 1. Ownership & Boundary Rules
- Preferences, Templates, and Quiet Hours belong exclusively to the `Notifications` module boundary.
- Third-party provider SDKs remain strictly encapsulated inside `Notifications.Infrastructure`. Domain and Application projects contain zero provider dependencies.

### 2. Preference Policy Engine (`INotificationPreferenceResolver`)
- Candidate channels resolved for a notification type are passed through `INotificationPreferenceResolver`.
- Explicit user opt-outs (`IsEnabled == false`) suppress non-InApp channels for security events, and all channels for standard events.
- Security Invariant: `InApp` channel delivery for security events (`UserPasswordChanged`, `UserLockedOut`) is mandatory and cannot be disabled.

### 3. Template Versioning & Active Resolution (`INotificationTemplateResolver`)
- Templates are stored per `(TemplateKey, Channel, Version)`.
- Active template resolution queries `IsActive == true`. If multiple active templates exist for the same logical key and channel, an explicit data-integrity exception is thrown. If no template exists in the database, a deterministic fallback template is used.

### 4. Safe Template Renderer (`INotificationTemplateRenderer`)
- Placeholders are matched via simple string substitution `{{VarName}}`.
- Context objects (`NotificationTemplateContext`) allow only sanitized parameters (`UserName`, `NotificationTitle`, `NotificationBody`, `CreatedAtUtc`, `ApplicationName`). Credentials (`Password`, `Tokens`, `Hashes`) are strictly forbidden.

### 5. Quiet Hours Resolution (`INotificationQuietHoursResolver`)
- Manages local time window (e.g., 22:00 to 07:00) with midnight-crossing support.
- InApp delivery is immediate; Email/SMS/Push delivery next attempt times are deferred past the quiet hours window.

### 6. API Security
- Endpoints (`/api/v1/notification-preferences`) strictly scope operations to the authenticated user ID (`ClaimsPrincipal.GetUserId()`). Route, body, and query string parameters cannot override the user identity.

## Consequences

### Positive
- Users gain granular control over notification channels.
- Notification body rendering is fully decoupled from third-party provider APIs.
- Security notifications preserve mandatory in-app visibility.
- System remains testable and runnable offline.

### Neutral
- Adds database tables: `notification_preferences`, `notification_templates`, and `notification_quiet_hours`.
