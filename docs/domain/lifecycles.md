# EduCore — Domain Model & Lifecycles (actual implementation, 2026-08-22)

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
- Created unpublished (`IsPublished` default 0). **Only bundles have publish/unpublish API endpoints.**
- Courses and lessons support soft delete (IsDeleted/DeletedAt/DeletedById) + restore; bundles do not.
- Lessons are either independent (`CourseId NULL`, own price) or belong to a course (created via
  `POST /api/courses/{id}/lessons` with price forced to 0).
- Instructor ownership: lessons → `Lessons.InstructorId`; courses → `CoursesInstructors` rows;
  bundles → `Products.CreatedByUser`.
- **Dual representation bug:** course↔lesson also modeled by a `CourseLessons` join table in
  progress/course-lesson data code, but the table was absent from `EduCore.sql` (C8).

## Orders (cart)
- Statuses (code enum): Pending, Completed, Cancelled, Empty. DB column is free varchar (no CHECK).
- Filtered unique index: at most ONE Pending order per user (`UX_Orders_User_Pending`).
- **No API endpoint creates an order** (C9). Item add/remove go through SPs that recompute TotalPrice
  and flip Status Empty↔Pending.
- `PUT /{id}/complete` sets Completed without payment → then SP_CreateNewPayment rejects the order
  forever (requires Pending) → dead end (H5). Cancel leaves pending payments orphaned.
- SP_AddNewItemToOrder prices only published products; unpublished → NULL PriceAtPurchase inserted (H6).

## Payments
- Statuses: Pending, Succeeded, Failed, Expired, Cancelled (CHECK constraint in DB).
- Created via SP_CreateNewPayment: validates order Pending, computes discount (expiry + usage-limit check),
  inserts Pending payment with server-generated IdempotencyKey (fresh GUID per call → no real idempotency, H7).
- `FinalPrice` = Price − ISNULL(DiscountPrice,0) persisted computed column; CHECKs keep it ≥ 0.
- Success flow (clsCheckoutService.CompletePaymentAsync, transactional):
  MarkAsSucceeded (sets TransactionId/PaidAt) → SP_CreateEnrollmentsFromPaidOrder.
  **Broken at runtime by C2 (double OpenAsync).**
- Discount usage counter `TotalUserNumber` is never incremented anywhere (C10).

## Enrollments
- Created per OrderItem product on payment success; unique (UserId, ProductId); ExpireAt = now + 12 months.
- Access check `IsUserEnrolled` was inverted (C1) — fixed per TODO.

## Progress & Certificates
- Progress rows per (UserId, LessonId), upserted; CompletedDate set on completion.
- **No enrollment validation** on mark-complete (H2).
- Course progress computed from `CourseLessons` join (C8) → % complete; certificate at 100%.
- Certificate: QuestPDF A4 landscape + QR (QRCoder) → written to `<cwd>/certificates/{guid}.pdf`.
  No Certificates table, no verification/download endpoint, QR points to nonexistent route (M14).

## Discounts
- Code string, rate (1–100), optional expiry, optional usage limit (0/NULL = unlimited — inconsistent
  between C# IsValid and SQL, M6). Created/updated/deleted by SuperAdmin only. Hard delete conflicts
  with Payments FK if the code was ever used.

## Audits & Logs
- Audits: actor UserId, action enum, entity type/id, description, IP, UA. Written inline or inside the
  caller's transaction. Known defects: IP actually Connection.Id in controllers (H3); wrong actor on
  updates (M4); inverted messages in lesson flows (M4).
- Logs: written by ExceptionMiddleware for every exception (type/message/stack/IP/UA/path).
