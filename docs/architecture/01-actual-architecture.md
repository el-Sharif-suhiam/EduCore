# EduCore — Actual Architecture (as implemented, 2026-08-22)

## Layers
```
EduCoreAPI (ASP.NET Core Web API, net10.0)
   │  controllers, authorization handlers/policies, mappers, request/response records, ExceptionMiddleware
   ▼
EduCore_BusinessLayer
   │  cls* Active-Record classes (clsUser, clsCourse, clsLesson, clsBundle, clsOrder, clsPayment,
   │  clsDiscountCode, clsProgress, clsEnrollment, clsUsersRoles, clsCoursesInstructors, clsAudit,
   │  clsLog, clsProduct, clsCheckoutService, clsGeneralRules)
   ▼
EduCore_DataAccess
   │  static cls*Data classes, raw ADO.NET (Microsoft.Data.SqlClient), stored procedures,
   │  clsGeneralData.ExecuteTransaction, clsDataAccessSettings (connection string)
   ▼
Common (DTOs, enums, exceptions, clsValidation/clsCompare, ViewModels)
```
`Dtos/` project is an orphaned duplicate of `Common/Dtos` (not in solution, unreferenced).

## Dependency direction
API → Business → Data → Common. Respected. Business layer references DataAccess directly (static calls).

## Dependency injection
Almost none. Only `IAuthorizationHandler` registrations + framework services. Business/data classes are
static or `new`-ed in controllers. **Consequence: no unit-testability without refactoring (see H8).**

## Request flow
```
HTTP → ExceptionMiddleware → (rate limiter) → Authentication (JWT) → Authorization (roles/policies)
→ Controller action → static cls* business class → static cls*Data → SqlConnection / SP
→ DTO → (mapper) → anonymous object / response record → JSON
```
Exceptions bubble to `ExceptionMiddleware`:
- `ValidationException` → 400 · `NotFoundException` → 404 · `ConflictException` → 409
- `UnauthorizedAccessException` → 401 · anything else → 500 ("Internal server error")
Every exception is also written to the `Logs` table (type, message, stack trace, IP, UA, path).

## Cross-cutting concerns
- **AuthN:** JWT Bearer HS256; issuer `EduCoreApi`, audience `EduCoreApiUsers`; secret env `JWT_SECRET_KEY`
  via DotNetEnv `.env`. Access token 30 min; refresh token 3 days, BCrypt-hashed, rotated on refresh,
  revocable (RefreshTokenRevokedAt).
- **AuthZ:** role attributes + 4 custom resource-based policies (UserOwnerOrAdmin, UserOwnerOnly,
  InstructorOwnership, IsUserEnrolledOrAdmin) evaluated via `IAuthorizationService` inside actions.
- **Rate limiting:** fixed window 5/min/IP (`AuthLimiter`) on login + refresh only.
- **CORS:** `https://localhost:7009`, `http://localhost:5087` (dev frontends).
- **Auditing:** `clsAudit.LogAsync` writes to `Audits` (optionally inside caller's transaction).
- **Transactions:** `clsGeneralData.ExecuteTransaction` (open conn → begin tx → work → commit/rollback).
  Used for: user create (+role+audit), course/lesson/bundle create/update (product+entity+audit),
  checkout (payment success + enrollments). NOT used for: role add/remove, order item add/remove
  (SPs have their own transactions), discount CRUD.

## Controller responsibilities (actual)
Controllers parse claims, invoke authorization policies, call business classes, shape anonymous-object
responses. Some validation leaks into controllers (e.g. `request.BasePrice > 0` guards). No request
DTO validation attributes exist; all validation is imperative inside business `Set*` methods.

## Data access patterns
- One `SqlConnection` per call (connection pooling relies on default pool).
- Stored procedures for order item add/remove, payment creation, enrollment creation.
- Ad-hoc parameterized SQL everywhere else (no SQL injection found; parameters used consistently).
- Reader mapping by `GetOrdinal` per query (verbose, duplicated across methods).
- Views `vwLessonsWithOutCourses` / `vwLessonsWithCourses` back lesson listings.

## Known architectural problems (details in ENGINEERING_TODO.md)
1. No DI/testability seam (H8) — severity: high for maintainability.
2. Active-Record + static calls mix business rules across API handlers and business classes
   (e.g. price>0 guards in controllers, ownership rules split between handlers and clsCoursesInstructors).
3. Orphaned Dtos project, folder≠namespace mismatch, dead code (L1/L2/L4).
4. Inconsistent API contracts and error envelope (M1/M2).
5. Dual course↔lesson representation (`Lessons.CourseId` vs `CourseLessons` join table) (C8).
