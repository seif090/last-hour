# ADR-027: Notification Lifecycle, Read-State Invariants, and Query Operations

## Status
Accepted

## Date
2026-08-20

## Context
In Sprints 10.14-A through 10.16, the **LastHour Backend** established a production-grade domain event processing architecture where business events across modules (`Merchant` and `Identity`) reliably produce notification business effects in the `notifications` table via transactional outbox dispatching and idempotent consumer handlers.

However, the `Notification` subsystem previously only supported notification generation without user-facing lifecycle management (read/unread states), query APIs, pagination, filtering, or mark-as-read commands. A complete notification subsystem requires:
1. Explicit aggregate lifecycle management (`Unread` and `Read` states).
2. Domain invariants governing read state and UTC timestamps.
3. Strict authorization boundaries preventing cross-user data leakage.
4. Deterministic, performant paginated querying with sorting and filtering.
5. Idempotent single-item and bulk mark-as-read operations.
6. High-performance database indexing tailored to PostgreSQL query patterns.

## Decisions

### 1. Notification Lifecycle and Domain Invariants
- The `Notification` aggregate root owns the read state through private setters and domain methods:
  - `IsRead` (`bool`, default `false`)
  - `ReadAtUtc` (`DateTime?`, default `null`)
- State transition rule:
  - `MarkAsRead(DateTime utcNow)`: When `IsRead == false`, sets `IsRead = true` and `ReadAtUtc = utcNow`.
  - When `IsRead == true`, the operation is an idempotent no-op, preserving the original `ReadAtUtc` timestamp.
- Domain Invariant: `IsRead == true` if and only if `ReadAtUtc != null`.

### 2. User Ownership and Authorization Boundary
- Notifications are strictly partitioned by `RecipientId` (the user identifier).
- API endpoints resolve the current user identifier exclusively from validated JWT claims (`ClaimsPrincipal.GetUserId()`) via `sub`. Client-supplied user identifiers in URLs, request bodies, or query strings are strictly forbidden.
- Cross-User Isolation:
  - Query operations only return records where `RecipientId == AuthenticatedUserId`.
  - Mutation operations (`MarkNotificationAsRead`) return `404 Not Found` if a requested notification does not belong to the authenticated user, preventing existence discovery attacks.
- Sensitive Data Elimination: Notification responses expose only user-facing fields (`Id`, `Type`, `Title`, `Body`, `IsRead`, `CreatedAtUtc`, `ReadAtUtc`). Internal authentication secrets, passwords, tokens, or event dispatcher internals are never exposed.

### 3. Query Design and Pagination
- Endpoint `GET /api/v1/notifications`:
  - Query parameters: `isRead` (`bool?`), `type` (`string?`), `page` / `pageNumber` (`int`), `pageSize` (`int`).
  - Default page size: `20`, maximum allowed: `100`.
  - Deterministic ordering: `CreatedAtUtc DESC` then `Id DESC`.
  - Standardized response envelope: `PagedResult<NotificationDto>` with items, page number, page size, total count, and total pages.

### 4. Mark-As-Read Operations
- **Single Notification**: `PUT /api/v1/notifications/{id}/read` (and `PATCH` alias):
  - Validates user ownership.
  - Idempotently marks notification as read.
  - Returns `204 No Content`.
- **Bulk Notification**: `POST /api/v1/notifications/read-all` (and `PATCH` alias):
  - Identifies unread notifications belonging to the authenticated recipient.
  - Transitions all unread items to read state in a single unit of work.
  - Returns `200 OK` with payload `MarkAllNotificationsAsReadResponse` containing `updatedCount`.

### 5. Database Schema and Index Optimization
- Added migration `20260820060000_AddNotificationReadStateAndIndexes`:
  - Columns: `IsRead` (`boolean`, not null, default `false`), `ReadAtUtc` (`timestamptz`, null).
  - Composite Index 1: `IX_notifications_recipient_created_at` (`RecipientId`, `CreatedAtUtc DESC`, `Id DESC`) for standard paginated feed queries.
  - Composite Index 2: `IX_notifications_recipient_is_read_created_at` (`RecipientId`, `IsRead`, `CreatedAtUtc DESC`) for unread count and unread-only feed queries.

### 6. Concurrency and Atomicity
- Concurrency conflicts during simultaneous `MarkAsRead` requests are resolved naturally and idempotently without errors.
- Bulk updates are scoped by `RecipientId` within standard database transactions without distributed locks.

## Consequences

### Positive
- Production-ready user notification lifecycle with clean domain modeling and CQRS separation.
- Total protection against unauthorized cross-user access and timing/existence attacks.
- High-efficiency PostgreSQL indexing for scalable pagination.
- Full test coverage with unit, integration, concurrency, and architecture verification.

### Negative / Trade-offs
- Bulk updates on very large inboxes are bounded by transactional unit-of-work scope. Future high-volume scale can introduce dedicated SQL `ExecuteUpdateAsync` if batch sizes exceed standard user inbox volumes.
