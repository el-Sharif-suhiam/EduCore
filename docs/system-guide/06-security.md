# EduCore — Security

## 1. JWT

- Access tokens are JWTs signed with `JWT_SECRET_KEY` (HMAC-SHA256), issued
  with issuer/audience from configuration.
- Lifetime: configured via `JWT_EXPIRES_MINUTES` (default short, e.g. 15-60
  minutes). Validated on every request (signature, issuer, audience, expiry).
- Claims include user id (NameIdentifier) and roles.

## 2. Refresh Tokens

- Issued alongside access tokens; stored as a BCrypt hash on `Users.RefreshTokenHash`
  with `RefreshTokenExpiresAt`.
- Rotation: refresh endpoint validates hash/expiry/revocation, issues new
  access + refresh tokens, revokes the old refresh token
  (`RefreshTokenRevokedAt`).
- Logout revokes the current refresh token.

## 3. Password Hashing

- BCrypt with configurable work factor (tests use 4 for speed).

## 4. Role-Based Authorization

- `[Authorize(Roles = "...")]` on controllers/actions.
- Roles: Student, Instructor, Admin, SuperAdmin.

## 5. Resource Ownership

- Instructors may modify only their own courses/lessons/bundles; enforced
  via `IAuthorizationService` policies (`OwnerOrAdmin`).
- Users may access only their own orders/payments/enrollments/progress.

## 6. Enrollment Authorization

- `IsUserEnrolledOrAdminHandler` grants access to enrolled users or admins.
- Bundle-derived access respected (purchased bundle unlocks included courses).

## 7. Administrative Authorization

- Admin/SuperAdmin roles gate user management, audit/logs, discounts, and
  payment state transitions.

## 8. Rate Limiting

- ASP.NET Core rate limiting middleware configured in `Program.cs` (per-IP
  or per-user limits as configured).

## 9. Input Validation

- Data annotations on DTOs; controllers validate ModelState.
- Business layer validates preconditions and throws `ValidationException`.

## 10. SQL Parameterization

- All SQL uses parameterized commands; no string concatenation of user input.

## 11. Stripe Webhook HMAC

- `Stripe-Signature` header verified against webhook secret before processing.
- Raw request body used for signature verification.

## 12. Payment Completion Validation

- Only `Pending` payments can transition to `Succeeded`.
- Conditional UPDATE (`WHERE Status = 'Pending'`) prevents double completion.
- Idempotency key prevents duplicate payment creation.

## 13. Secret Configuration

- Secrets (`JWT_SECRET_KEY`, Stripe keys, DB connection) come from environment
  variables / configuration; never committed. `.env.example` documents them.

## 14. Error Handling and Information Disclosure

- Unhandled exceptions return generic 500; stack traces are not exposed to
  clients. Details logged to `Logs` table.

## 15. Access to Paid and Unpublished Content

- Paid content requires active enrollment (or admin).
- Unpublished content is hidden from public catalogs but remains accessible
  to existing purchasers (unpublish does not revoke).

## 16. Implemented vs Recommended

Implemented: JWT, refresh rotation, BCrypt, RBAC, ownership, parameterized SQL,
webhook HMAC, conditional transitions, rate limiting.

Recommended/limitations: HTTPS enforcement in production, secret rotation,
audit log retention, IP allow-listing for webhooks, 2FA (not implemented).