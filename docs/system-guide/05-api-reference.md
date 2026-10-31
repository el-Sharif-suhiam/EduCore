# EduCore — API Reference

Base path: `/api`. Authentication: `Authorization: Bearer <JWT>` unless noted.
Roles: Student, Instructor, Admin, SuperAdmin. Anonymous endpoints are marked.

All error responses use the standard exception mapping (400/401/403/404/409/500).

## Authentication

| Method | Route | Auth | Purpose |
|---|---|---|---|
| POST | /api/auth/login | Anonymous | Login with email/password; returns JWT + refresh token. |
| POST | /api/auth/refresh | Anonymous | Rotate refresh token; returns new JWT + refresh token. |
| POST | /api/auth/logout | Anonymous | Revoke refresh token. |

## Users

| Method | Route | Auth | Purpose |
|---|---|---|---|
| GET | /api/users/students | Admin,SuperAdmin | List students. |
| GET | /api/users/admins | Admin,SuperAdmin | List admins. |
| GET | /api/users/instructors | Admin,SuperAdmin | List instructors. |
| GET | /api/users/{id} | Owner or Admin | Get user by id. |
| GET | /api/users/by-email | Admin,SuperAdmin | Get user by email. |
| POST | /api/users/students | Anonymous | Register student. |
| DELETE | /api/users/{id} | Admin,SuperAdmin | Deactivate/delete user. |
| POST | /api/users/{id}/activate | Admin,SuperAdmin | Activate user. |
| PUT | /api/users/{id} | UserOwnerOnly (owner) | Update user profile. |
| PUT | /api/users/{id}/password | Owner or Admin | Change password. |
| POST | /api/users/toInstructor/{id} | Admin,SuperAdmin | Grant instructor role. |
| POST | /api/users/toInstructor/{id}/remove | Admin,SuperAdmin | Remove instructor role. |
| POST | /api/users/toAdmin/{id} | SuperAdmin | Grant admin role. |
| POST | /api/users/toAdmin/{id}/remove | SuperAdmin | Remove admin role. |

## Products (via Courses/Lessons/Bundles)

Products are managed indirectly through course, lesson, and bundle endpoints.
`ProductType`: 1=Lesson, 2=Course, 3=Bundle.

## Courses

| Method | Route | Auth | Purpose |
|---|---|---|---|
| GET | /api/courses | Anonymous | List published courses. |
| GET | /api/courses/{id} | Anonymous | Get course by id. |
| POST | /api/courses | Instructor,SuperAdmin | Create course. |
| PUT | /api/courses/{id} | Owner or Admin | Update course. |
| POST | /api/courses/{id}/lessons | Instructor,SuperAdmin | Add lesson to course. |
| PUT | /api/courses/{courseId}/lessons/{lessonId} | Instructor,SuperAdmin | Move lesson between courses. |
| GET | /api/courses/{id}/lessons | Anonymous | List course lessons. |
| POST | /api/courses/{id}/publish | Instructor,SuperAdmin | Publish course. |
| POST | /api/courses/{id}/unpublish | Instructor,SuperAdmin | Unpublish course. |
| GET | /api/courses/{id}/exists | Admin,SuperAdmin | Existence check. |
| POST | /api/courses/{id}/instructors | Admin,SuperAdmin | Assign instructor. |
| GET | /api/courses/{courseId}/instructors | Admin,SuperAdmin | List instructors. |
| DELETE | /api/courses/{courseId}/instructors | Admin,SuperAdmin | Remove instructor. |

## Lessons

| Method | Route | Auth | Purpose |
|---|---|---|---|
| GET | /api/lessons | Anonymous | List standalone lessons. |
| GET | /api/lessons/{id}/info | Anonymous | Lesson info. |
| GET | /api/lessons/{id} | Authenticated | Get lesson (enrolled/admin). |
| POST | /api/lessons | Instructor,SuperAdmin | Create lesson. |
| PUT | /api/lessons/{id} | Instructor,SuperAdmin | Update lesson. |
| POST | /api/lessons/{id}/publish | Instructor,SuperAdmin | Publish lesson. |
| POST | /api/lessons/{id}/unpublish | Instructor,SuperAdmin | Unpublish lesson. |
| DELETE | /api/lessons/{id} | Instructor,SuperAdmin | Soft-delete lesson. |
| PUT | /api/lessons/{id}/restore | Instructor,SuperAdmin | Restore soft-deleted lesson. |

## Bundles

| Method | Route | Auth | Purpose |
|---|---|---|---|
| GET | /api/bundles | Anonymous | List published bundles. |
| GET | /api/bundles/all | Instructor,SuperAdmin | List all bundles (owned/all). |
| GET | /api/bundles/{id}/items | Anonymous | Get bundle with courses. |
| GET | /api/bundles/{id} | Anonymous | Get bundle by id. |
| GET | /api/bundles/{id}/exists | Anonymous | Existence check. |
| POST | /api/bundles | Instructor,SuperAdmin | Create bundle. |
| POST | /api/bundles/{id}/publish | Instructor,SuperAdmin | Publish bundle. |
| POST | /api/bundles/{id}/unpublish | Instructor,SuperAdmin | Unpublish bundle. |
| PUT | /api/bundles/{id} | Instructor,SuperAdmin | Update bundle. |
| POST | /api/bundles/{id}/items | Instructor,SuperAdmin | Add course to bundle. |
| DELETE | /api/bundles/{id}/items/{courseId} | Instructor,SuperAdmin | Remove course from bundle. |

## Orders

| Method | Route | Auth | Purpose |
|---|---|---|---|
| GET | /api/orders | Admin,SuperAdmin | List orders. |
| POST | /api/orders | Authenticated | Create order (cart). |
| GET | /api/orders/{id} | Owner or Admin | Get order. |
| GET | /api/orders/cart/{userId} | Owner or Admin | Get pending cart. |
| POST | /api/orders/{orderId}/items/{itemId} | Authenticated | Add item to order. |
| DELETE | /api/orders/{id}/items/{productId} | Authenticated | Remove item from order. |
| PUT | /api/orders/{id}/complete | Authenticated | Complete order (admin hook). |
| PUT | /api/orders/{id}/cancel | Authenticated | Cancel order. |

## Checkout / Payments

| Method | Route | Auth | Purpose |
|---|---|---|---|
| GET | /api/payments | Admin,SuperAdmin | List payments. |
| GET | /api/payments/{id} | Owner or Admin | Get payment. |
| POST | /api/payments | Authenticated | Create payment. |
| POST | /api/payments/{id}/stripe-checkout-session | Authenticated | Create Stripe checkout session. |
| PUT | /api/payments/{id}/checkOut-succeed | Admin,SuperAdmin | Mark payment succeeded (admin hook). |
| PUT | /api/payments/{id}/fail | Admin,SuperAdmin | Mark payment failed. |
| PUT | /api/payments/{id}/expire | Admin,SuperAdmin | Mark payment expired. |

## Stripe Webhooks

| Method | Route | Auth | Purpose |
|---|---|---|---|
| POST | /api/webhooks/stripe | Anonymous | Receive Stripe events. |

## Discounts

| Method | Route | Auth | Purpose |
|---|---|---|---|
| GET | /api/discount-codes/valid | Admin,SuperAdmin | List valid discount codes. |
| GET | /api/discount-codes/{id} | Admin,SuperAdmin | Get discount by id. |
| GET | /api/discount-codes | Authenticated | List discounts. |
| GET | /api/discount-codes/{id}/is-valid | Authenticated | Validate discount. |
| POST | /api/discount-codes | SuperAdmin | Create discount. |
| PUT | /api/discount-codes/{id} | SuperAdmin | Update discount. |
| DELETE | /api/discount-codes/{id} | SuperAdmin | Delete discount. |

## Enrollments

| Method | Route | Auth | Purpose |
|---|---|---|---|
| GET | /api/enrollments/my | Authenticated | List current user's enrollments. |

## Progress

| Method | Route | Auth | Purpose |
|---|---|---|---|
| GET | /api/progress/lessons/{lessonId} | Authenticated | Get progress for a lesson. |
| GET | /api/progress/courses/{courseId} | Authenticated | Get course progress. |
| GET | /api/progress/courses/{courseId}/is-completed | Authenticated | Check course completion. |
| GET | /api/progress/courses/{courseId}/certificate | Authenticated | Issue/get certificate. |
| PUT | /api/progress/lessons/{lessonId}/complete | Authenticated | Mark lesson complete. |
| PUT | /api/progress/lessons/{lessonId}/incomplete | Authenticated | Mark lesson incomplete. |

## Certificates

| Method | Route | Auth | Purpose |
|---|---|---|---|
| GET | /api/certificates/{code} | Anonymous | Verify certificate. |
| GET | /api/certificates/{code}/download | Authenticated | Download certificate PDF. |

## Audit

| Method | Route | Auth | Purpose |
|---|---|---|---|
| GET | /api/audit | Admin,SuperAdmin | List audit entries. |
| GET | /api/audit/{id} | Admin,SuperAdmin | Get audit entry. |

## Logs

| Method | Route | Auth | Purpose |
|---|---|---|---|
| GET | /api/logs | Admin,SuperAdmin | List logs. |
| GET | /api/logs/{id} | Admin,SuperAdmin | Get log entry. |

## Notes

- Request/response bodies are JSON. Controllers bind to DTOs in `Common/Dtos` and
  `Common/ViewModels`.
- Pagination parameters `pageNumber`, `pageSize` are query params where listed
  endpoints support them.
- Search text parameters are query params where supported.

## Request/Response Examples

### Authentication — Login
Request:
```json
{ "email": "student@test.local", "password": "Test1234!" }
```
Response:
```json
{ "token": "<jwt>", "refreshToken": "<refresh>" }
```

### Orders — Create order
`POST /api/orders` (authenticated) returns the new or existing pending order id.

### Orders — Add item
`POST /api/orders/{orderId}/items/{productId}` adds a product with its current
published price snapshot.

### Payments — Create payment
Request:
```json
{ "orderId": 1, "discountCode": "SAVE10", "idempotencyKey": "client-uuid", "paymentMethod": "card" }
```
Response:
```json
{ "paymentId": 42, "finalPrice": 27.00 }
```

### Progress — Mark complete
`PUT /api/progress/lessons/{lessonId}/complete` returns success boolean.

### Certificates — Verify
`GET /api/certificates/{code}` (anonymous) returns validity flag, student name,
course title, and issue date.