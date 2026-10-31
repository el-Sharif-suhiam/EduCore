# EduCore — Business Logic

## 1. Users and Roles

Roles: `Student`, `Instructor`, `Admin`, `SuperAdmin` (seeded in `Roles`).

- **Registration** creates a user with `Student` role and `IsActive = 1`.
- **Activation/deactivation**: `Users.IsActive`. Deactivation sets
  `IsActive = 0`; deactivated users cannot log in (login checks `IsActive`).
  There is no hard deletion of users.
- **Ownership**: instructors own courses/lessons/bundles they create
  (`CreatedByUser`). Admins/SuperAdmins can manage globally; instructors
  manage only their own content (enforced via `IAuthorizationService`).
- **Role grants**: SuperAdmin (and Admin where policy allows) can promote
  users to Instructor/Admin/SuperAdmin and remove roles.

## 2. Products

- `ProductType`: Lesson (1), Course (2), Bundle (3).
- `IsPublished` controls public catalog visibility.
- `BasePrice` is the current price; order items snapshot `PriceAtPurchase`.
- `Products` has no soft-delete column: deletion of a product is not
  supported as a soft operation; content entities (Course/Lesson) carry
  soft-delete instead.
- Created by a user (`CreatedByUser` FK).

## 3. Courses and Lessons

- A course is created with a published/unpublished product.
- Lessons may be standalone (`CourseId IS NULL`) or attached to a course
  (`CourseId` set). The `CoursesLessons` junction is kept in sync with
  `Lessons.CourseId` inside a transaction by the business layer.
- Soft deletion: `IsDeleted = 1` hides from public views and blocks access
  (see §7).
- Publication of a course/lesson is independent of its product's? Actually
  publication lives on the Product (`IsPublished`). Courses/lessons inherit
  visibility through their product.
- Standalone lessons remain accessible per their existing rules even when
  course context changes.

## 4. Bundles

- A bundle product groups courses via `BundlesItems`.
- Public visibility requires: bundle product published, included course not
  deleted, included course product published.
- Purchased entitlement (for an active, non-expired bundle enrollment)
  grants access to an included course when it is **unpublished but not
  deleted**. Unpublishing does not revoke a legitimate purchase.
- Soft-deleted included courses are **not** accessible, even to purchasers.
- Bundle enrollment does not grant access to unrelated courses.
- Expired bundle enrollment grants no access.

## 5. Orders

- States: `Empty`, `Pending`, `Completed`, `Cancelled`.
- One `Pending` order per user (filtered unique index).
- Item add/remove only when order is `Pending` or `Empty`.
- A pending payment pins the order: items cannot be mutated while a payment
  is `Pending`.
- `TotalPrice` = sum of `OrderItems.PriceAtPurchase`.
- Status becomes `Empty` when total is 0, else `Pending`.
- Completed/Cancelled are final: never modified or reset.

## 6. Discounts

- `DiscountRate` is a percentage (0–100]; validated.
- `ExpireAt` is a DATE; valid through the end of that day (UTC).
- `AllowedUseNumber`: 0/NULL = unlimited.
- `TotalUserNumber`: incremented when an applicable payment **succeeds**.
- Pending payments **reserve** a slot: `SP_CreateNewPayment` counts
  `Pending` payments for the discount plus `TotalUserNumber` and rejects
  when `>= AllowedUseNumber`.
- Unlimited usage: when `AllowedUseNumber` is 0/NULL, no limit check.

## 7. Payments

States: `Pending`, `Succeeded`, `Failed`, `Expired`, `Cancelled`.

- **Creation**: `SP_CreateNewPayment` (idempotent by `IdempotencyKey`).
  At most one `Pending` payment per order.
- **Idempotency**: same key + same order returns existing payment; same key
  + different order -> error.
- **Stripe interaction**: payment creation does not call Stripe; the API
  creates a Stripe checkout session referencing the payment.
- **Webhook verification**: HMAC signature verified before processing.
- **Completion**: conditional transition `Pending -> Succeeded`. Only the
  first concurrent completion applies side effects.
- **Failed/expired**: conditional `Pending -> Failed/Expired`.
- **Transactional guarantees**: completion (discount usage, enrollments,
  order completion, sibling expiry) is one transaction.

## 8. Enrollments and Access

- Direct enrollment on a product grants access until `ExpireAt` (NULL = no
  expiry).
- Bundle enrollment grants access to included, non-deleted courses.
- Renewal: re-purchasing an expired enrollment extends `ExpireAt` and
  updates `PaymentId` (no duplicate row).
- Access check (`IsUserEnrolled`): active enrollment (direct or bundle).
- Deleted course: not accessible even with prior enrollment.
- Unpublished course: accessible to existing purchasers.
- Expired enrollment: no access.

## 9. Progress and Certificates

- Progress is per `(UserId, LessonId)`.
- `CompletedDate` set when marking complete; preserved when marking
  incomplete.
- Course progress % = completed lessons / total non-deleted lessons.
- Certificate eligibility: active enrollment + 100% completion.
- Certificate unique per `(UserId, CourseId)`; repeated issuance returns
  the same code.
- PDF generated once per code into configured storage.

## 10. User Deactivation vs Deletion

- There is no user deletion. Deactivation (`IsActive = 0`) blocks login.
- Content soft-deletion (`IsDeleted`) hides content and blocks access but
  keeps rows for audit/history.