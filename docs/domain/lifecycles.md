# EduCore — Domain Model & Lifecycles (actual implementation, current)

## Roles
`Roles` table seeded: Admin(1), Instructor(2), Student(3), SuperAdmin(4) — matches `enRoles`.
- Registration always assigns Student.
- Admin can grant/remove Instructor. SuperAdmin can grant/remove Admin.
- No API to grant/remove SuperAdmin. No last-admin guard.
- Role checks in DB helpers require `Users.IsActive = 1`.

## Users
- Register (anonymous): name/email/birthdate/password → BCrypt hash → user + Student role + audit (transactional).
- Email unique (DB + pre-check). Deactivation = `IsActive=0` (soft); deactivated users cannot log in
  (lookup filters IsActive) but live JWTs remain valid up to 30 min.
- Password policy: ≥12 chars, upper+lower+digit (clsValidation.ValidatePassword).
- Refresh tokens: 64-byte RNG at login (GUID at registration — inconsistent), BCrypt-hashed, 3-day expiry,
  rotated on refresh, revocable via RefreshTokenRevokedAt.

## Products (Courses / Lessons / Bundles)
- `Products` is the sellable base row; Course/Lesson/Bundle tables extend it 1:1 via ProductId.
- Created unpublished (`IsPublished` default 0). Publish/unpublish endpoints exist for courses, lessons, and bundles.
- Courses and lessons support soft delete (IsDeleted/DeletedAt/DeletedById) + restore; bundles do not.
- Lessons are either independent (`CourseId NULL`, own price) or belong to a course (created via
  `POST /api/courses/{id}/lessons` with price forced to 0).
- Instructor ownership: lessons → `Lessons.InstructorId`; courses → `CoursesInstructors` rows;
  bundles → `Products.CreatedByUser`.
- Publication visibility: public feeds serve published items only; drafts are visible in
  `/all` and `/{id}` only to admins or the owning instructor (SQL-enforced, covered by tests).

## Orders (cart)
- Statuses (code enum): Pending, Completed, Cancelled, Empty. DB column is free varchar (no CHECK).
- Filtered unique index: at most ONE Pending order per user (`UX_Orders_User_Pending`).
- `POST /api/orders` creates the cart (returns existing pending cart if any).
- **State machine enforced in BOTH layers:** item add/remove allowed only in Pending/Empty —
  validated in `clsOrder` (business ConflictException) AND in the SPs (THROW 50011), so no
  caller can flip a Completed/Cancelled order back to Pending.
- Item add prices only published products (rejected otherwise, order untouched); remove of the
  last Pending item transitions the order to `Empty`.
- `PUT /{id}/complete` requires a succeeded payment; `PUT /{id}/cancel` expires pending payments.

## Payments
- Statuses: Pending, Succeeded, Failed, Expired, Cancelled (CHECK constraint in DB).
- Created via SP_CreateNewPayment: validates order Pending, computes discount (expiry + usage-limit check),
  inserts Pending payment with the client-supplied IdempotencyKey (unique index; replays return the
  existing payment instead of duplicating).
- `FinalPrice` = Price - ISNULL(DiscountPrice,0) persisted computed column; CHECKs keep it ≥ 0.
- Completion flow (clsCheckoutService.CompletePaymentAsync, transactional + race-safe):
  - Guard: only a Pending payment may drive the chain (replays/terminal states → 409).
  - MarkAsSucceeded uses a **conditional UPDATE (WHERE Status='Pending')**; the concurrent loser
    re-checks stored state, adopts nothing, and aborts before re-applying side effects.
  - Discount usage incremented once; enrollments created per order item (deduped by
    UNIQUE(UserId,ProductId)); order → Completed; other pending payments → Expired.
  - Failure/Expiry also conditional on Pending — Succeeded payments can never be flipped.
- Discount usage counter incremented once per successful completion.

## Enrollments
- Created per OrderItem product on payment success; unique (UserId, ProductId); ExpireAt = now + 12 months.
- Access check `IsUserEnrolled` verified by tests.
- Bundle access policy: an active bundle enrollment grants access to included,
  non-deleted courses even when the course is unpublished; deleted courses are
  denied; unrelated courses are denied; expired enrollments are denied.
- Direct enrollment to a soft-deleted course/lesson is denied; unpublished
  content remains accessible to existing purchasers.

## Progress & Certificates
- Progress rows per (UserId, LessonId), upserted; CompletedDate set on completion.
- Course progress computed from the course-lessons join → % complete; eligibility for a
  certificate requires an active enrollment AND 100% lesson completion (verified server-side
  in `SP_IssueCertificate`, not just in C#).
- Certificates (full lifecycle): one row per (UserId, CourseId) with a GUID-"N" code;
  issuance is idempotent (repeat calls return the same code; one PDF on disk under
  `CERTIFICATES_STORAGE_PATH`). API: `GET /api/certificates/{code}` (public verify — valid
  flag + student name + course title) and `GET /api/certificates/{code}/download`
  (owner or admin, `application/pdf`). QR on the PDF points to the verification URL.

## Discounts
- Code string, rate (1–100), optional expiry, optional usage limit (0/NULL = unlimited — inconsistent
  between C# IsValid and SQL, M6). Created/updated/deleted by SuperAdmin only. Hard delete conflicts
  with Payments FK if the code was ever used.

## Audits & Logs
- Audits: actor UserId, action enum, entity type/id, description, IP, UA. Written inline or inside the
  caller's transaction.
- Logs: written by ExceptionMiddleware for every exception (type/message/stack/IP/UA/path).
