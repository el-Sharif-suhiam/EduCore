# EduCore — API Endpoint Inventory (actual implementation, 2026-08-22)

Auth column: `anon` = [AllowAnonymous]/no auth · `auth` = any authenticated user · role lists as marked.
Issues reference docs/agent/ENGINEERING_TODO.md ids.

## Auth — `api/auth` (AuthController)
| Method | Route | Auth | Purpose | Request | Response | Issues |
|---|---|---|---|---|---|---|
| POST | /login | anon (rate-limited) | Verify credentials, issue JWT+refresh | LoginRequest(email,password) | TokenResponse | audit IP = Connection.Id (H3) |
| POST | /refresh | anon (rate-limited) | Rotate refresh, new JWT | RefreshRequest(email,refreshToken) | TokenResponse | same |
| POST | /logout | anon | Revoke refresh token | LogoutRequest(email,refreshToken) | raw string | inconsistent body type (M1) |

## Users — `api/users` (UsersController)
| Method | Route | Auth | Purpose | Issues |
|---|---|---|---|---|
| GET | /students | Admin,SuperAdmin | Paged students (+search) | |
| GET | /admins | Admin,SuperAdmin | Paged admins (+search) | includes SuperAdmins via LIKE '%Admin' |
| GET | /instructors | Admin,SuperAdmin | Paged instructors (+search) | |
| GET | /{id} | auth + UserOwnerOrAdmin | Get user profile | |
| GET | /by-email | Admin,SuperAdmin | Lookup by email | |
| POST | /students | anon | Register student | no rate limit (M13); GUID refresh token (M15) |
| DELETE | /{id} | Admin,SuperAdmin | Deactivate user (soft) | |
| GET | /email-exists | anon | Email enumeration | enumeration surface |
| PUT | /{id} | auth + UserOwnerOnly | Update name/email/birthdate | email conflict → 500 via SQL exception |
| PUT | /{id}/password | auth + UserOwnerOnly | Change password (old required) | |
| POST | /toInstructor/{id} | Admin,SuperAdmin | Grant Instructor role | |
| POST | /toInstructor/{id}/remove | Admin,SuperAdmin | Remove Instructor role | |
| POST | /toAdmin/{id} | SuperAdmin | Grant Admin role | |
| POST | /toAdmin/{id}/remove | SuperAdmin | Remove Admin role | no last-admin guard |

## Courses — `api/courses` (CoursesController)
| Method | Route | Auth | Purpose | Issues |
|---|---|---|---|---|
| GET | / | anon | Paged courses w/ instructors (+search) | search-by-instructor applied after paging |
| GET | /{id} | anon | Course by id | |
| POST | / | Instructor,SuperAdmin | Create course (unpublished) | no publish endpoint exists (H4) |
| PUT | /{id} | Instructor,SuperAdmin + InstructorOwnership | Update course | |
| POST | /{id}/lessons | Instructor,SuperAdmin + InstructorOwnership | Create lesson inside course (price forced 0) | |
| PUT | /{courseId}/lessons/{lessonId} | Instructor,SuperAdmin + InstructorOwnership | Update course lesson | |
| GET | /{id}/lessons | anon | Lessons of course | |
| GET | /{id}/exists | anon | Existence check | |
| POST | /{id}/instructors | Admin,SuperAdmin | Assign instructor | |
| GET | /{id}/instructors | Admin,SuperAdmin | List instructors | route/param mismatch → 400 (M3) |
| DELETE | /{id}/instructors | Admin,SuperAdmin | Remove instructor | route/param mismatch → 400 (M3) |

## Lessons — `api/lessons` (LessonsController)
| Method | Route | Auth | Purpose | Issues |
|---|---|---|---|---|
| GET | / | anon | Paged independent lessons (+search) | |
| GET | /{id} | auth | Lesson by id (instructor-owner OR enrolled OR admin) | first-role-claim issue (M5) |
| POST | / | Instructor,SuperAdmin | Create independent lesson | |
| PUT | /{id} | Instructor,SuperAdmin + InstructorOwnership | Update lesson | |
| DELETE | /{id} | Instructor,SuperAdmin + InstructorOwnership | Soft delete | |
| PUT | /{id}/restore | Instructor,SuperAdmin + InstructorOwnership | Restore | |

## Bundles — `api/bundles` (BundlesController)
| Method | Route | Auth | Purpose | Issues |
|---|---|---|---|---|
| GET | / | anon | Published bundles view | |
| GET | /{id}/items | anon | Bundle with courses | |
| GET | /{id} | anon | Bundle by id | |
| GET | /{id}/exists | anon | Existence check | hardcoded `B.Id = 1` bug in BundleExists |
| POST | / | Instructor,SuperAdmin | Create bundle | |
| POST | /{id}/publish | Instructor,SuperAdmin + InstructorOwnership | Publish | |
| POST | /{id}/unpublish | Instructor,SuperAdmin + InstructorOwnership | Unpublish | |
| PUT | /{id} | **NONE** | Update bundle | C4 |
| POST | /{id}/items | **NONE** | Add course to bundle | C4 |
| DELETE | /{id}/items/{courseId} | **NONE** | Remove course from bundle | C4 |

## Orders — `api/orders` (OrdersController)
| Method | Route | Auth | Purpose | Issues |
|---|---|---|---|---|
| — | — | — | **create cart endpoint MISSING** | C9 |
| GET | /{id} | auth + UserOwnerOrAdmin(order.UserId) | Order by id | |
| GET | /cart/{userId} | auth + UserOwnerOrAdmin | Current pending cart | fake empty order when none (M7) |
| POST | /{OrderId}/items/{ItemId} | auth + UserOwnerOrAdmin | Add product (SP) | unpublished products → NULL price (H6) |
| DELETE | /{id}/items/{productId} | auth + UserOwnerOrAdmin | Remove item (SP) | silent success when absent (M8) |
| PUT | /{id}/complete | auth + UserOwnerOrAdmin | Mark Completed | bypasses payment (H5) |
| PUT | /{id}/cancel | auth + UserOwnerOrAdmin | Cancel | orphans pending payments (H5) |

## Payments — `api/payments` (PaymentsController)
| Method | Route | Auth | Purpose | Issues |
|---|---|---|---|---|
| GET | /{id} | **NONE** | Payment details | C5 (leaks TransactionId/IdempotencyKey) |
| POST | / | auth | Create payment (optional discount code) | no order-ownership check (C5); idempotency decorative (H7) |
| PUT | /{id}/checkOut-succeed | auth | Mark Succeeded + create enrollments | no ownership; self-service (C5/H1); runtime-broken (C2) |
| PUT | /{id}/fail | auth | Mark Failed | no ownership (C5) |
| PUT | /{id}/expire | auth | Mark Expired | no ownership (C5) |

## Progress — `api/progress` (ProgressController, class-level [Authorize])
| Method | Route | Auth | Purpose | Issues |
|---|---|---|---|---|
| GET | /lessons/{lessonId} | auth | Lesson progress | no enrollment check (H2) |
| GET | /courses/{courseId} | auth | Course progress % | depends on CourseLessons (C8) |
| GET | /courses/{courseId}/is-completed | auth | 100%? | |
| GET | /courses/{courseId}/certificate | auth | Generate PDF certificate | no persistence/verify endpoint (M14) |
| PUT | /lessons/{lessonId}/complete | auth | Mark complete | no enrollment check (H2) |
| PUT | /lessons/{lessonId}/incomplete | auth | Mark incomplete | same |

## Discounts — `api/discount-codes` (DiscountCodesController)
| Method | Route | Auth | Purpose | Issues |
|---|---|---|---|---|
| GET | /valid | Admin,SuperAdmin | Valid codes list | |
| GET | /{id} | Admin,SuperAdmin | Code by id | |
| GET | /?discountCode= | auth | Lookup by code string | |
| GET | /{id}/is-valid | auth | Validity check | |
| POST | / | SuperAdmin | Create code | |
| PUT | /{id} | SuperAdmin | Update code | |
| DELETE | /{id} | SuperAdmin | Delete code | hard delete; FK from Payments blocks if used |

## Audit — `api/audit` (AuditController)
| GET | / | Admin,SuperAdmin | Paged audits | no paging validation call |
| GET | /{id} | Admin,SuperAdmin | Audit by id | |

## Logs — `api/logs` (LogsController)
| GET | / | Admin,SuperAdmin | Paged logs | no paging validation call |
| GET | /{id} | Admin,SuperAdmin | Log by id | |

## Missing endpoints (needed for coherent product)
- POST /api/orders (create cart) — C9
- publish/unpublish for courses and lessons — H4
- GET current-user enrollments / payments (DAL exists, never exposed) — L9
- certificate verify/download — M14
