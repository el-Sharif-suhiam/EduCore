# EduCore — Workflows

Each workflow lists actor, preconditions, trigger, components, DB reads/writes,
transaction boundaries, and failure paths.

## 1. Registration

- Actor: Anonymous.
- Trigger: `POST /api/users/students` (or admin creates user).
- Components: UsersController -> clsUser -> clsUserData.
- Writes: insert Users row, UserRoles (Student).
- Result: user created (active).

## 2. Login and Refresh-Token Rotation

- Trigger: `POST /api/auth/login`.
- Flow: verify password (BCrypt); check IsActive; issue JWT (short-lived) +
  refresh token (hashed, stored on Users row with expiry).
- Refresh: `POST /api/auth/refresh` validates hash/expiry/revocation, rotates
  refresh token (new hash + expiry, old revoked), issues new JWT.
- Logout: `POST /api/auth/logout` revokes refresh token.
- Failure: invalid credentials -> 401; inactive user -> 401; expired/revoked
  refresh -> 401.

## 3. Product Browsing

- Trigger: `GET /api/courses`, `GET /api/lessons`, `GET /api/bundles`.
- Reads products filtered by IsPublished (public) or ownership (instructor).
- No writes.

## 4. Course Creation

- Actor: Instructor/SuperAdmin.
- Trigger: `POST /api/courses`.
- Components: CoursesController -> clsCourse -> clsCourseData + clsProduct.
- Writes: Products row (Course type), Courses row.
- Transaction: single insert sequence (product then course).

## 5. Lesson Creation and Assignment to Course

- Trigger: `POST /api/lessons` (standalone) or
  `POST /api/courses/{id}/lessons`.
- If assigned to a course, business layer writes both `Lessons.CourseId` and
  a `CoursesLessons` junction row in one transaction.
- Moving/detaching a lesson syncs both representations.

## 6. Bundle Creation and Management

- Trigger: `POST /api/bundles`.
- Writes: Products (Bundle type) + Bundles row.
- Items: `POST /api/bundles/{id}/items` adds course; `DELETE` removes.
- Publish/unpublish toggles product IsPublished.

## 7. Adding/Removing an Order Item

- Trigger: `POST /api/orders/{orderId}/items/{itemId}` /
  `DELETE /api/orders/{id}/items/{productId}`.
- SP: `SP_AddNewItemToOrder` / `SP_RemoveItemFromOrder`.
- Transaction: lock order, validate state, block if pending payment, mutate
  item, recompute total, conditional status update.
- Failure: order not editable (409), pending payment (409), product not
  published (400).

## 8. Applying a Discount

- Discount is not "applied" to an order directly; it is selected at payment
  creation (`discountId`).
- Validation (rate, expiry, usage) occurs in `SP_CreateNewPayment`.

## 9. Creating a Payment

- Trigger: `POST /api/payments`.
- Business: clsPayment.CreatePayment -> clsPaymentData.CreatePaymentAsync ->
  `SP_CreateNewPayment`.
- Flow: lock order; idempotency check; pending-payment guard; discount
  validation/reservation; insert Pending payment.
- Returns payment id + final price.

## 10. Stripe Hosted Checkout

- Trigger: `POST /api/payments/{id}/stripe-checkout-session`.
- Creates a Stripe Checkout Session with the payment's final price and
  metadata (payment id). Returns session URL.
- No DB write except possibly storing session id.

## 11. Stripe Webhook Verification

- Trigger: `POST /api/webhooks/stripe` (Stripe -> API).
- Verifies `Stripe-Signature` HMAC with webhook secret; rejects invalid.
- Dispatches by event type.

## 12. Successful Payment Completion

Full chain:

```
Product -> Order -> OrderItem -> Payment -> Stripe -> Verified Webhook
  -> Payment Success -> Discount Usage -> Enrollment -> Completed Order
```

- `clsCheckoutService.CompletePaymentAsync` in one transaction:
  1. Lock order.
  2. Lock discount (if any).
  3. Mark payment Succeeded (conditional).
  4. Increment discount usage (if discount).
  5. Create enrollments (SP_CreateEnrollmentsFromPaidOrder).
  6. Complete order.
  7. Expire sibling pending payments for the order.
- Commit; on any failure rollback.

## 13. Failed and Expired Payments

- Webhook `payment_intent.payment_failed` -> mark payment Failed (conditional).
- Expiry (manual/admin or scheduled) -> mark Expired.
- Neither increments discount usage nor creates enrollments.

## 14. Enrollment Creation

- Occurs during successful payment completion (see §12).
- SP renews expired enrollments for re-purchased products and inserts new
  ones.

## 15. Enrollment Renewal

- Re-purchase of an expired enrollment updates `ExpireAt` and `PaymentId`
  instead of inserting a duplicate.

## 16. Purchasing an Individual Course

- Create order, add course product, create payment, Stripe checkout, webhook
  success -> enrollment on course product.

## 17. Purchasing a Bundle

- Same as above with bundle product -> enrollment on bundle product.
- Access to included courses derived at access-check time.

## 18. Accessing Courses Through a Bundle

- `IsUserEnrolled(userId, courseProduct)` checks direct enrollment OR an
  active bundle enrollment whose `BundlesItems` include the course and the
  course is not deleted.

## 19. Progress Updates

- Trigger: `PUT /api/progress/lessons/{id}/complete` or `/incomplete`.
- Checks enrollment for the lesson's owning product.
- Upserts Progress row (atomic, transactional).
- `CompletedDate` set on complete; preserved on incomplete.

## 20. Certificate Issuance and Retrieval

- Trigger: `GET /api/progress/courses/{id}/certificate` or
  `GET /api/certificates/{code}`.
- Checks enrollment + 100% completion.
- SP_IssueCertificate (idempotent) inserts certificate if missing.
- PDF generated once; downloadable via `GET /api/certificates/{code}/download`.
- Public verification via `GET /api/certificates/{code}`.