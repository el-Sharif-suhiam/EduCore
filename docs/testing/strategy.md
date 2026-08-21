# EduCore — Testing Strategy (proposed, 2026-08-22)

## Current state
No test projects, no test infrastructure, no CI. Static-class architecture with hidden
`new SqlConnection(...)` calls makes unit testing impossible without a seam.

## Prerequisite seam (minimal, low-risk)
Introduce `IDbConnectionFactory` (or static `clsDataAccessSettings.CreateConnection()` delegate) used by
all `cls*Data` classes instead of `new SqlConnection(ConnectionString)`. Tests can then point at a real
test database. Keep static API otherwise — no large refactor needed up front.

## Proposed projects
1. `EduCore.Tests` (xUnit) — unit tests for pure logic:
   - clsValidation (email/password/url/price/birthdate)
   - clsDiscountCode.IsValid matrix (null/0/expiry/usage)
   - clsCompare.IsProductChanged
   - order/payment status-transition rules once extracted
2. `EduCore.IntegrationTests` (xUnit + WebApplicationFactory + Testcontainers SQL Server):
   - Apply `EduCore.sql` on container start; seed roles/users.
   - Auth flow: register → login → refresh rotation → logout revocation.
   - Authorization matrix: anonymous/student/instructor/admin/superadmin × every endpoint
     (would have caught C4/C5).
   - Checkout: cart create → add item → discount → payment → succeed → enrollments (would catch C1–C3, C10).
   - Progress/certificate rules incl. enrollment requirement.
   - Invalid-input scenarios (bad email, weak password, negative price, huge PageSize).

## Critical flows requiring tests first
1. Payment success transaction (C2 regression test).
2. Enrollment check correctness (C1 regression test).
3. Payment/order ownership (C5 regression tests).
4. Bundle endpoint auth (C4 regression tests).
5. Discount usage increment (C10).
6. One-pending-order-per-user behavior.

## Mock/test data policy (per audit prompt §15)
- Fixtures live only in test projects; never in production code paths.
- Seed helpers create users per role with known credentials for integration tests.

## CI suggestion
- `dotnet build EduCore.slnx` + `dotnet test` on PR; Docker required for Testcontainers.
