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

---

## 2026-08-25 — Frontend-driven backend addition (session 4, owner-approved)

**Objective:** Unblock the frontend cart (milestone 3): the cart API requires Products.Id
but course endpoints only exposed Courses.Id. Owner approved a minimal additive change.

**Modifications:**
- Common/ViewModels/CourseViewModel.cs: added ProductId to CourseWithInstructorViewModel.
- EduCore_DataAccess/clsCoursesData.cs: GetAllCoursesWithInstructorViewModelInternal —
  SELECT now includes C.ProductId AS ProductId; reader ordinal + assignment added.
- EduCoreAPI/Helpers/Models/ResponeModels/CourseResponse.cs: added ProductId.
- EduCoreAPI/Helpers/Mappers/CourseMapper.cs: maps course.ProductId.
- No DB/schema changes; no existing field renamed or removed (additive only).

**Tests executed:** dotnet build EduCore.slnx → 0 errors (warnings pre-existing).
Frontend milestone 3 built against the updated contract; see docs/frontend/FRONTEND_LOG.md.

**Unresolved issues:** unchanged (H1 payment-gateway verification remains the key blocker
for real checkout; frontend stubs document the integration point in
rontend/src/lib/payments.ts).

---

## 2026-08-25 — Stripe payment gateway (session 5, frontend-driven)

**Objective:** Complete the purchase loop: hosted checkout + verified webhook as the
source of truth (closes audit H1 self-service-success finding).

**Modifications:**
- NEW EduCore_BusinessLayer/clsStripeGateway.cs: raw Stripe REST via static HttpClient
  (no SDK, owner decision). CreateCheckoutSessionAsync — form-encoded, mode=payment,
  client_reference_id/metadata carry our paymentId server-side, Idempotency-Key derived
  from Payments.IdempotencyKey; VerifyWebhookSignature — HMAC-SHA256 over "t.payload",
  constant-time compare against any v1 entry, +/-300 s tolerance.
- EduCoreAPI/Controllers/PaymentsController.cs: added
  POST api/payments/{id}/stripe-checkout-session ([Authorize] + UserOwnerOrAdmin,
  pending-only) returning { url, sessionId }; success/cancel composed from FRONTEND_URL.
  checkOut-succeed action marked DEPRECATED in comments (kept for compat).
- NEW EduCoreAPI/Controllers/StripeWebhookController.cs [AllowAnonymous]:
  verifies signature on RAW body before parsing; checkout.session.completed /
  async_payment_succeeded -> clsCheckoutService.CompletePaymentAsync(order.UserId,
  payment, payment_intent|session_id); expired -> MarkAsExpired;
  async_payment_failed -> MarkAsFailed; replays no-op via pending-state guards;
  unknown events acknowledged. 503 when STRIPE_WEBHOOK_SECRET missing (Stripe retries).
- .env.example: STRIPE_SECRET_KEY, STRIPE_WEBHOOK_SECRET, FRONTEND_URL documented.

**Contract notes:** currency hardcoded usd (const in gateway, marked CHANGE HERE);
amount = FinalPrice * 100 rounded away-from-zero; TransactionId stores PaymentIntent id.

**Tests executed:** dotnet build EduCore.slnx -> 0 errors. Live webhook flow NOT executed
(no Stripe keys on this machine); README section 5 documents exact local test procedure.

---

## 2026-08-25 — Enrollments endpoint (session 6, frontend-driven, L9 closed)

**Objective:** Expose current-user enrollments (ENGINEERING_TODO L9: DAL existed, never
exposed) so the frontend learning dashboard can be built.

**Modifications:**
- NEW Common/ViewModels/EnrollmentViewModel.cs: Id, ProductId, ProductName, ProductTypeId,
  ThumbnailUrl, Summary, EnrolledAt, ExpireAt + CourseId/LessonId deep-link ids
  (LEFT JOIN Courses/Lessons on ProductId; IsDeleted=0 filters).
- EduCore_DataAccess/clsEnrollmentsData.cs: added GetUserEnrollmentViewModels
  (paged OFFSET/FETCH, active-only via ExpireAt filter).
- EduCore_BusinessLayer/clsEnrollment.cs: added GetUserEnrollments (userId validation).
- NEW EduCoreAPI/Controllers/EnrollmentsController.cs: GET api/enrollments/my —
  [Authorize], resolves CURRENT user from claims (no userId route param by design),
  reuses PageRequest + clsApiValidators.ValidatePaging.

**Contract notes:** ProductTypeId is the raw enProductType byte (Lesson=1, Course=2,
Bundle=3). Deep-link ids exist because progress endpoints take Courses.Id while
enrollments store Products.Id.

**Tests executed:** dotnet build EduCore.slnx -> 0 errors. Frontend milestone 4 consumes it.

---

## 2026-09-16 — Admin bundles console + frontend P1/P2/P3 close-out (session ~10)

**Objective:** Finish the frontend: admin bundle management (previously no unpublished-list or
publish-state visibility), public bundle detail page, and the approved P1/P2/P3 polish list.

**Modifications (backend, minimal + additive):**
- `Common/ViewModels/BundleViewModel.cs`: added `IsPublished`.
- `EduCore_DataAccess/clsBundlesData.cs` + `EduCore_BusinessLayer/clsBundle.cs`:
  `GetAllBundlesView(bool includeUnpublished = false)` — filters `IsPublished=1` unless requested;
  SELECT now includes IsPublished.
- `EduCoreAPI/Controllers/BundlesController.cs`: `GET api/bundles/all`
  `[Authorize(Roles = "Instructor,SuperAdmin")]` returning all bundles w/ publish state.
- `EduCoreAPI/Helpers/Mappers/bundleMapper.cs`: map IsPublished. Route order safe (`{id:int}`
  won't capture "all").

**Modifications (frontend):**
- lib/admin.ts: bundle fetchers (list, create, update, publish/unpublish, detail = bundle+items,
  add/remove item). lib/courses.ts: server fetchers for the RSC detail page.
- NEW /admin/bundles + /admin/bundles/[id] builder; NEW public /bundles/[id] (sticky price rail,
  AddToCartButton, deep-linked contents). BundleCard title now links to the detail page.
- Global error/loading/not-found boundaries; landing "View all" -> /courses.
- /register honors ?next=; /admin/people load-more+debounced search+ARIA tabs;
  /learn load-more with full progress fetch (no cap).
- NEW ui/textarea, ui/select, ui/toast (+ToastProvider in root layout, wired into bundle/course
  builder, discounts, people). Modal now traps Tab + restores focus.
- admin-shell: Bundles nav entry; command palette uses router.push. .env.example documents
  NEXT_PUBLIC_SITE_URL.

**Decisions:** Zero new deps kept (native fetch, hand-rolled modal/palette/toast/select). Admin
bundle endpoint restricted to Instructor+SuperAdmin as the family roles — mirrors courses 'all'
behavior, keeps SuperAdmin-only principle for destructive ops.

**Tests executed:** `dotnet build EduCoreAPI` → 0 errors (138 pre-existing nullable warnings);
`npm run lint` → 0; `npm run build` → passes, 24 routes (incl. /admin/bundles, /admin/bundles/[id],
/bundles/[id]).

**Unresolved:** None blocking. Docs updated (endpoint-inventory, FRONTEND_LOG/TODO). Uncommitted —
awaiting owner push approval.

---

## 2026-09-16 - Admin commerce dashboards + publish/reactivate controls (session 11)

**Goal:** Close the remaining admin console gaps surfaced in session ~10: order/payment
dashboards, publish state in the admin course list, and user re-activation. Certificates
explicitly deferred by owner ("???? ????? ???????? ?????").

**Modifications (backend - additive, owner-approved pattern):**
- `CourseViewModel.cs` / `clsCoursesData.cs`: `CourseWithInstructorViewModel.IsPublished` now
  selected in the list query (CourseSummary isPublished consumed by /admin/courses).
- Users: `clsUsersData.ActivateUser(id)`; `clsUser.ReactivateUser(id, actionByUserId)` with audit
  record; `enAuditActionType.UserReactivated` appended as 31 (order untouched). UsersController:
  `includeInactive` (bool?, default false) on GET students/admins/instructors + `POST
  /api/users/{id}/activate` [Admin,SuperAdmin] (mirrors Delete shape).
- NEW `Common/ViewModels/CommerceAdminViewModels.cs`: PaymentAdminViewModel (price, discount,
  tax/Coupon? no - price/discountPrice/finalPrice, method, status, transactionId, paidAt) +
  OrderAdminViewModel (totalPrice, status, createdAt + buyer name/email).
- `clsOrderData.GetAllOrdersView(page,size,search)` and `clsPaymentData.GetAllPaymentsView(...)`:
  JOIN Users, ORDER BY CreatedAt DESC, OFFSET/FETCH, LIKE search on name/email, enums parsed
  from DB strings. BL pass-throughs added (clsOrder/clsPayment). Repaired an earlier edit that
  dropped a brace on GetAllPaymentsForUser (compile caught it on the first build).
- Controllers: `GET api/orders` + `GET api/payments` [Admin,SuperAdmin], PageRequest + optional
  search, validated via clsApiValidators.ValidatePaging (fixed namespace: PageRequest lives in
  EduCoreAPI.Helpers.Dtos.RequestDto, not Models.RequestModels).

**Modifications (frontend):**
- CourseSummary + isPublished; /admin/courses Published/Draft badge + inline Publish/Hide toggle
  (toast feedback), Manage keeps route to builder.
- lib/admin: getUsers(..., includeInactive), activateUser(id); /admin/people requests inactive
  too and renders a Reactivate action (Role icon UserRoundCheck), deactivate confirm copy updated.
- NEW /admin/orders + /admin/payments: paged tables (no totals), detail modals, debounced search
  by buyer; ORDER_STATUS_LABELS / PAYMENT_STATUS_LABELS. admin-shell nav entries (Orders,
  Payments) Admin/SuperAdmin only; AUDIT_ACTION_LABELS gains 31 "Reactivated user".
- Toast usage follows the single `toast({title,description,variant})` API; `qs` param maps are
  string|number|undefined so includeInactive is serialized as "true".

**Tests executed:** `dotnet build` (solution) 0 errors, 0 warnings; `npm run lint` 0; `npm run
build` passes - 26 routes (incl. /admin/orders, /admin/payments). IntegrationTests project built
but exposes no discoverable tests (requires live DB).

**Unresolved:** None blocking. backlog unchanged: certificates (M14), standalone lesson product
sales (no ProductId on LessonsWithOutCoursesViewModel), current-user enrollments/payments feed
(L9). Docs updated; commit pushed to origin/main.
