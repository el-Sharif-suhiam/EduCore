# EduCore — Engineering TODO

Status legend: [ ] open · [x] done · [~] in progress

## Critical
Security, data integrity, serious logic errors.

### C1. Enrollment check inverted
- **Issue:** `IsUserEnrolled` returns true only for EXPIRED enrollments (`ExpireAt < SYSUTCDATETIME()`);
  same inversion in `GetAllUserEnrollments` (`ExpireAt IS NULL OR ExpireAt < GETDATE()`).
- **Location:** `EduCore_DataAccess/clsEnrollmentsData.cs` (IsUserEnrolled, GetAllUserEnrollments)
- **Explanation:** Active students are denied lesson access; expired students are granted access.
- **Recommended solution:** `ExpireAt IS NULL OR ExpireAt > SYSUTCDATETIME()`; use UTC consistently.
- **Dependencies:** None. **Status:** [x] fixed phase 1 (50f2de1)

### C2. Checkout always fails — double OpenAsync
- **Issue:** `UpdateStatusWithTransaction` calls `conn.OpenAsync()` on the connection already opened by
  `clsGeneralData.ExecuteTransaction` → `InvalidOperationException` → payment success + enrollment creation never complete.
- **Location:** `EduCore_DataAccess/clsPaymentData.cs` (UpdateStatusWithTransaction)
- **Recommended solution:** Open only when `conn.State != ConnectionState.Open`.
- **Dependencies:** None. **Status:** [x] fixed phase 0 (91bf602)

### C3. GET payment by id parses wrong column
- **Issue:** `Status = Enum.Parse(..., reader.GetString(paymentMethodIndex))` — reads PaymentMethod as Status → throws for any real row.
- **Location:** `EduCore_DataAccess/clsPaymentData.cs` (GetPaymentByIdAsync)
- **Recommended solution:** Use `statusIndex`.
- **Dependencies:** None. **Status:** [x] fixed phase 0 (91bf602)

### C4. Bundle mutation endpoints unauthenticated
- **Issue:** `PUT /api/bundles/{id}`, `POST /api/bundles/{id}/items`, `DELETE /api/bundles/{id}/items/{courseId}` have no `[Authorize]`.
- **Location:** `EduCoreAPI/Controllers/BundlesController.cs`
- **Recommended solution:** `[Authorize(Roles="Instructor,SuperAdmin")]` + `InstructorOwnership` policy on update; admin/superadmin for item add/remove.
- **Dependencies:** None. **Status:** [x] fixed phase 1 (50f2de1)

### C5. Payment endpoints lack ownership checks
- **Issue:** `GET /api/payments/{id}` anonymous; create/succeed/fail/expire accept any payment/order from any authenticated user.
- **Location:** `EduCoreAPI/Controllers/PaymentsController.cs`
- **Recommended solution:** Require auth on GET; verify order owner (or Admin) on all mutations; restrict succeed/fail/expire to owner/admin.
- **Dependencies:** None. **Status:** [x] fixed phase 1 (50f2de1)

### C6. Hardcoded DB credentials in source
- **Issue:** `sa`/`sa123456` connection string committed in code; `.env` `DB_CONNECTION` unused.
- **Location:** `EduCore_DataAccess/clsDataAccessSettings.cs`
- **Recommended solution:** Read from environment (`DB_CONNECTION`); fail fast if missing; rotate exposed password.
- **Dependencies:** None. **Status:** [x] fixed phase 0 (91bf602); password rotation is an external action

### C7. Secrets committed
- **Issue:** `.env` with JWT secret + API key present in working tree (gitignored, but secret values weak/committed historically).
- **Location:** `EduCoreAPI/.env`
- **Recommended solution:** Provide `.env.example`, rotate secrets, never log them.
- **Dependencies:** None. **Status:** [x] `.env.example` added phase 0 (91bf602); secret rotation is an external action

### C8. `CourseLessons` table missing from schema
- **Issue:** Code queries/inserts `CourseLessons` (clsProgressData) and `CoursesLessons` (clsCourseLessonData, clsCoursesData) — table never created; names inconsistent.
- **Location:** `EduCore.sql`, `clsProgressData.cs`, `clsCourseLessonData.cs`, `clsCoursesData.cs`
- **Recommended solution:** Add table to script; unify on one name in code.
- **Dependencies:** None. **Status:** [x] fixed phase 0 (91bf602)

### C9. No API endpoint to create an order (cart)
- **Issue:** `clsOrder.CreateAsync` exists but nothing calls it; `AddItemToOrder` needs an existing order id → checkout unreachable.
- **Location:** `EduCoreAPI/Controllers/OrdersController.cs`
- **Recommended solution:** `POST /api/orders` creating a pending cart for the current user (respecting one-pending-per-user).
- **Dependencies:** None. **Status:** [x] fixed phase 2

### C10. Discount usage never incremented
- **Issue:** `TotalUserNumber` checked in `SP_CreateNewPayment` but never updated anywhere → usage limits unenforceable.
- **Location:** `EduCore.sql` (SP_CreateNewPayment), payment success flow
- **Recommended solution:** Increment usage atomically when payment succeeds (inside checkout transaction).
- **Dependencies:** Checkout flow. **Status:** [x] fixed phase 2 (IncrementUsage inside checkout transaction)

## High
Important architectural or functional problems.

- H1. Self-service payment success: `PUT /api/payments/{id}/checkOut-succeed` accepts client-supplied TransactionId with no gateway verification → free enrollments. Restrict to admin/gateway. (`PaymentsController.cs`) [~] phase 1 added auth+ownership; still no gateway verification — needs real gateway integration
- H2. `MarkAsComplete`/certificate have no enrollment check → any user can complete any lesson/course. (`clsProgress.cs`, `ProgressController.cs`) [x] fixed phase 1 (50f2de1)
- H3. Audit IP field stores `HttpContext.Connection.Id` (GUID) instead of remote IP in all controllers. [x] fixed phase 1 (50f2de1)
- H4. Courses/lessons cannot be published via API (only bundles have publish endpoints) → unsellable. [x] fixed phase 2 (publish/unpublish endpoints for courses + lessons)
- H5. Order state machine: `complete` bypasses payment and then blocks payment forever; `cancel` orphans pending payments. [x] fixed phase 2 (complete requires succeeded payment; cancel expires pending payments; checkout completes order atomically)
- H6. `SP_AddNewItemToOrder` adds unpublished products with NULL PriceAtPurchase. [x] fixed phase 2 (SP throws when product unavailable; business-layer guard too)
- H7. Idempotency decorative: server-side fresh GUID per call; retries create duplicate payments. [x] fixed phase 2 (client-supplied key; replay returns existing payment; unique-index race handled)
- H8. No DI for business/data services → untestable; introduce connection-factory seam. [ ]
- H9. Deactivated users: login path filters IsActive (good) but role changes don't invalidate live JWTs (30 min window). Document/accept or add jti revocation. [ ]

## Medium
Maintainability/performance/consistency issues.

- M1. Inconsistent response contracts (anonymous objects vs classes vs raw strings); error body is bare string (problem-details commented out). [ ]
- M2. Wrong status codes: business failures as 409 where 400/422 fits; `UnauthorizedAccessException` overloaded. [ ]
- M3. Route/param mismatches: `GET/DELETE /api/courses/{id}/instructors` bind `courseId` vs route token `id` → 400. (`CoursesController.cs`) [x] fixed phase 1 (50f2de1)
- M4. Inverted audit messages + wrong action type in `clsLesson` update/delete; audit actor is original creator, not acting user (course/lesson/bundle). [ ]
- M5. `GetLessonById` uses only first role claim; multi-role users misrouted. (`LessonsController.cs`) [x] fixed phase 1 (50f2de1)
- M6. `clsDiscountCode.IsValid` treats NULL AllowedUseNumber as invalid while SQL treats it as unlimited; `DateTime.Now` vs `UtcNow` mixed. [ ]
- M7. `FindOrderbyUserId` returns fake empty order (Id=0) instead of 404/empty contract. [ ]
- M8. `RemoveItemFromOrder` succeeds silently when item absent. [ ]
- M9. `PageSize` uncapped (only min validated) → client can request huge pages. [ ]
- M10. Missing indexes: `Orders.UserId` (script has a TODO comment), `DiscountCodes.DiscountCode`, `Enrollments.ExpireAt`, `Audits.UserId/DoneAt`, `Logs.CreatedAt`. [ ]
- M11. `EduCore.sql` not idempotent (re-run fails); UTC inconsistency (GETDATE vs SYSUTCDATETIME); `Products.CreatedAt` is DATE. [ ]
- M12. N+1: every `clsProduct.Find` loads creator user; order endpoints load order before authorization. [ ]
- M13. Rate limiting only on login/refresh; registration, email-exists, certificate generation unlimited. [x] fixed phase 1 (50f2de1) — registration + certificate rate-limited
- M14. Certificate: no persistence table, no verification endpoint, QR points to nonexistent route, unbounded disk writes. [ ]
- M15. `CreateStudent` uses GUID refresh token + 5h expiry (inconsistent with login's 64-byte RNG + 3 days). [ ]
- M16. `clsProduct.SetCreatedByUser` silently no-ops for non-instructors → misleading later error. [ ]
- M17. 429 body-writer middleware writes login-specific message for all rate-limited endpoints, after response may have started. [x] fixed phase 1 (50f2de1) — generic 429 message

## Low
Minor improvements and cleanup.

- L1. Orphaned `Dtos/` project (duplicate of Common/Dtos, not in slnx) — remove. [ ]
- L2. Commented-out code: old clsPayment copy (~190 lines), controller blocks, SavePdf variants. [ ]
- L3. `EduCoreAPI.http` still references template `weatherforecast`. [ ]
- L4. Folder structure ≠ namespaces (`Helpers/Models/*` → `Helpers.Dtos.*`). Align eventually. [ ]
- L5. Typos in identifiers (`Respone`, `Assigan`, `Complated`, `Independnt`) — keep for API compat, fix internal-only ones over time. [ ]
- L6. `AddWithValue` usage (clsUsersData.IsEmailExist). [ ]
- L7. README.md is empty — fill from docs/. [ ]
- L8. `Bundles.Id smallint` / `DiscountCodes.Id smallint` capacity cliffs; consider int in a future migration. [ ]
- L9. Dead DAL methods never exposed: `GetAllUserEnrollments`, `GetEnrollmentByUserId`, `GetAllPaymentsForUser`, `GetAllProducts`, `GetAllPublishedProductsByPage`, `clsAudit.GetByUserAsync`, `clsLog.GetByTypeAsync`. [ ]
- L10. `DotNetEnv.Env.Load()` called per login/refresh request (re-reads file). [ ]
