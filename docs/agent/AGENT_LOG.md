# EduCore — Agent Work Log

Persistent project memory. Every agent session appends an entry. Do not rewrite history.

---

## 2026-08-22 — Discovery, Audit & Planning (session 1)

**Objective:** Full repository understanding, documentation, architecture/security/API/performance
audit per `INITIAL_AUDIT_PROMPT.md`. No code changes in this phase.

**Files inspected:** Entire repository — all 5 projects (~120 source files), `EduCore.sql`,
`Program.cs`, appsettings, `.env`, `.gitignore`, git history/state.

**Findings (summary — full detail in docs/security/audit-2026-08-22.md and ENGINEERING_TODO.md):**
- CRITICAL: enrollment check inverted (`ExpireAt < now`); checkout broken (double `OpenAsync` on open
  connection); `GetPaymentByIdAsync` parses Status from wrong column; bundle mutation endpoints
  unauthenticated; payment endpoints lack ownership checks; hardcoded `sa` connection string in source;
  `.env` secrets committed; `CourseLessons` table missing from schema script; no API endpoint to create
  an order/cart; discount usage counter never incremented.
- HIGH: self-service payment success without gateway/ownership; progress/certificate without enrollment
  checks; audit logs store Connection.Id instead of client IP; courses/lessons cannot be published via API.
- MEDIUM/LOW: inconsistent response contracts, wrong status codes, inverted audit messages, wrong audit
  actor on updates, route/param mismatches in CoursesController, dead `Dtos/` project, 126 build warnings,
  missing indexes, GETDATE vs SYSUTCDATETIME mix, unbounded PageSize.

**Decisions:**
- Follow DISCOVER → DOCUMENT → ANALYZE → PLAN → ASK APPROVAL → IMPLEMENT → TEST → DOCUMENT workflow.
- Fix order approved by owner: baseline commit → docs → Phase 0 (config/runtime landmines) →
  Phase 1 (security) → Phase 2 (commerce correctness) → Phase 3 (consistency) → Phase 4 (tests).
- Commit per major step.

**Modifications:** None (read-only phase). Baseline commit `ac27ee3` created including pre-existing WIP.

**Tests executed:** `dotnet build EduCore.slnx` → 0 errors, 126 warnings.

**Unresolved issues:** Everything in ENGINEERING_TODO.md.

**Next recommended actions:** Create docs tree, then Phase 0 fixes (connection string to config,
payment data bugs, CourseLessons table, route mismatches).

---

## 2026-08-22 — Documentation & Phase 0 fixes (session 1, continued)

**Objective:** Write persistent documentation (audit prompt §9–12), then fix Phase 0 items.

**Modifications:** docs/ tree created; see commits after baseline for per-step details.

---

## 2026-08-22 — Phase 2 commerce correctness (session 2)

**Objective:** Continue Phase 2 (commerce correctness) per ADR 0001. Prior uncommitted work had
already landed C9 (create-cart endpoint), C10 (discount usage increment), H6 (unpublished-product
guard in SP + business layer). This session wired the remaining items and repaired the build.

**Modifications:**
- `EduCore.slnx`: restored relative project paths (file had been corrupted to absolute
  `../../../../../../Documents/EduCore/...` paths → build failed with MSB3202). Kept the `/Agent/`
  folder entry.
- H5 order state machine: `clsOrder.MarkAsCompleted` now requires a succeeded payment
  (`clsPaymentData.HasSucceededPaymentForOrder`) before completing; `clsOrder.Cancel` runs in a
  transaction and expires orphaned pending payments; `clsCheckoutService.CompletePaymentAsync` now
  completes the order and expires remaining pending payments atomically. Added transactional
  `clsOrderData.UpdateStatus(orderId, status, conn, tx)` overload.
- H7 idempotency: `CreatePaymentRequest` gains optional `IdempotencyKey`; `clsPayment.CreatePayment`
  accepts a client key, replays an existing payment for the same key+order instead of duplicating,
  rejects key reuse across orders, and handles the unique-index race (SQL 2627/2601) by re-reading.
  Server still generates a key when none supplied.
- H4 publish endpoints: added `POST /api/courses/{id}/publish|unpublish` and
  `POST /api/lessons/{id}/publish|unpublish` (Instructor,SuperAdmin + InstructorOwnership), backed by
  new `clsCourse.PublishCourse/UnPublishCourse` and `clsLesson.PublishLesson/UnPublishLesson`
  delegating to `clsProduct.Publish/Unpublish`.
- Exposed `clsPayment.DiscountId` (needed by checkout discount increment).
- Fixed missing `using System.Security.Claims` in `OrdersController` (create-cart endpoint).

**Tests executed:** `dotnet build EduCore.slnx` → 0 errors, 51 warnings (down from 126).

**Unresolved issues:** H1 (no real payment-gateway verification), H8 (no DI/test seam), H9 (JWT
revocation), and all open Medium/Low items in ENGINEERING_TODO.md.

**Next recommended actions:** Phase 3 (consistency) — error envelope/problem-details, response
contracts, audit actor/messages, PageSize cap (M9), dead-code removal, indexes, idempotent SQL script.
Then Phase 4 (tests).

---

## 2026-08-22 — Phase 3 consistency (session 3)

**Objective:** Phase 3 (consistency) per ADR 0001. Owner decision recorded: no DI seam (H8 dropped);
Phase 4 will be integration tests via `WebApplicationFactory` against a test SQL Server DB.

**Modifications:**
- M1: `ExceptionMiddleware` now writes RFC 7807 `application/problem+json` (ProblemDetails with
  status/title/detail/type/instance) for all mapped exceptions; guards against response already
  started. 429 rate-limit writer in `Program.cs` also emits problem+json.
- M2: added `Common.Exceptions.ForbiddenException` → 403; enrollment denial in `clsProgress`
  (EnsureEnrolledForLesson/Course) now throws ForbiddenException instead of 401; wrong current
  password now 400 (ValidationException) instead of 409.
- M4: audit actor = acting user. `clsCourse/clsLesson/clsBundle.Save(actionByUserId)` overloads;
  controllers pass the authenticated user. `clsLesson.Delete/UnDelete` use `adminId` (was
  CreatedByUser.Id) and fixed Delete action type (was CreateLesson). Fixed inverted `CourseId == null`
  conditions in lesson audit messages. `clsCourse.Delete/UnDelete` use adminId.
- M6: `clsDiscountCode.IsUnlimited` = NULL or 0 (was only 0); `IsExpired`/`SetExpireAt` use UtcNow;
  `SP_CreateNewPayment` treats NULL AllowedUseNumber as unlimited (`ISNULL(@AllowedUseNumber,0)`).
- M7: `GetCart` authorizes the route userId first, then 404s when no pending order (was returning a
  fake Id=0 order and 403-ing everyone without a cart).
- M8: `clsOrder.DeleteItemFromOrder` throws NotFoundException (404) when the product is not in the
  order (was silent success).
- M9: `clsApiValidators.ValidatePaging` caps PageSize at 100.
- M10/M11: `EduCore.sql` rewritten idempotent — OBJECT_ID guards on all tables, CREATE OR ALTER for
  views/SPs, IF NOT EXISTS for indexes and role seeds. Added missing indexes: IX_Orders_UserId,
  IX_Payments_OrderId, IX_DiscountCodes_DiscountCode, IX_Enrollments_ExpireAt,
  IX_Audits_UserId_DoneAt, IX_Logs_CreatedAt. Unified on SYSUTCDATETIME (GETDATE removed);
  Products.CreatedAt/UpdatedAt → DATETIME2; Users/Orders/Enrollments/Progress timestamps → UTC.
  NOTE: existing databases need the new indexes + column type changes applied manually.
- L1: removed orphaned `Dtos/` project (git rm).
- L2: removed commented-out dead code — old clsPayment copy (~190 lines), CoursesController
  delete/restore blocks, LessonsController GetAllLessons block, clsProgress SavePdf/QR/IssueCertificate
  dead blocks + unused QR directory writes.
- Fixed duplicate using in AuthController (CS0105).

**Tests executed:** `dotnet build EduCore.slnx --no-incremental` → 0 errors, 137 warnings (all
pre-existing nullable CS86xx).

**Unresolved issues:** H1 (no real payment-gateway verification), H9 (JWT revocation), M1/M2 partial
(success contracts still mixed; some 409-for-failure kept for compat), M5/M12/M14/M15/M16, L3–L10.

**Next recommended actions:** Phase 4 — integration tests via WebApplicationFactory against a test
SQL Server DB (no DI seam).
