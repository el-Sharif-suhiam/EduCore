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
| GET | /students | Admin,SuperAdmin | Paged students (+search); `includeInactive=true` lists deactivated | |
| GET | /admins | Admin,SuperAdmin | Paged admins (+search + includeInactive) | includes SuperAdmins via LIKE '%Admin' |
| GET | /instructors | Admin,SuperAdmin | Paged instructors (+search + includeInactive) | |
| GET | /{id} | auth + UserOwnerOrAdmin | Get user profile | |
| GET | /by-email | Admin,SuperAdmin | Lookup by email | |
| POST | /students | anon | Register student | no rate limit (M13); GUID refresh token (M15) |
| DELETE | /{id} | Admin,SuperAdmin | Deactivate user (soft) | |
| POST | /{id}/activate | Admin,SuperAdmin | Reactivate a deactivated account | added admin console phase |
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
| POST | / | Instructor,SuperAdmin | Create course (unpublished) | |
| PUT | /{id} | Instructor,SuperAdmin + InstructorOwnership | Update course | |
| POST | /{id}/publish | Instructor,SuperAdmin + InstructorOwnership | Publish course | added phase 2 (H4) |
| POST | /{id}/unpublish | Instructor,SuperAdmin + InstructorOwnership | Unpublish course | added phase 2 (H4) |
| POST | /{id}/lessons | Instructor,SuperAdmin + InstructorOwnership | Create lesson inside course (price forced 0) | |
| PUT | /{courseId}/lessons/{lessonId} | Instructor,SuperAdmin + InstructorOwnership | Update course lesson | |
| GET | /{id}/lessons | anon | Lessons of course | |
| GET | /{id}/exists | anon | Existence check | |
| POST | /{id}/instructors | Admin,SuperAdmin | Assign instructor | |
| GET | /{courseId}/instructors | Admin,SuperAdmin | List instructors | |
| DELETE | /{courseId}/instructors | Admin,SuperAdmin | Remove instructor | |

## Lessons — `api/lessons` (LessonsController)
| Method | Route | Auth | Purpose | Issues |
|---|---|---|---|---|
| GET | / | anon | Paged independent lessons (+search) | |
| GET | /{id} | auth | Lesson by id (instructor-owner OR enrolled OR admin) | |
| POST | / | Instructor,SuperAdmin | Create independent lesson | |
| PUT | /{id} | Instructor,SuperAdmin + InstructorOwnership | Update lesson | |
| POST | /{id}/publish | Instructor,SuperAdmin + InstructorOwnership | Publish lesson | added phase 2 (H4) |
| POST | /{id}/unpublish | Instructor,SuperAdmin + InstructorOwnership | Unpublish lesson | added phase 2 (H4) |
| DELETE | /{id} | Instructor,SuperAdmin + InstructorOwnership | Soft delete | |
| PUT | /{id}/restore | Instructor,SuperAdmin + InstructorOwnership | Restore | |

## Bundles — `api/bundles` (BundlesController)
| Method | Route | Auth | Purpose | Issues |
|---|---|---|---|---|
| GET | / | anon | Published bundles view | |
| GET | /all | Instructor,SuperAdmin | All bundles incl. unpublished | added phase (admin bundles console) |
| GET | /{id}/items | anon | Bundle with courses | |
| GET | /{id} | anon | Bundle by id | |
| GET | /{id}/exists | anon | Existence check | |
| POST | / | Instructor,SuperAdmin | Create bundle | |
| POST | /{id}/publish | Instructor,SuperAdmin + InstructorOwnership | Publish | |
| POST | /{id}/unpublish | Instructor,SuperAdmin + InstructorOwnership | Unpublish | |
| PUT | /{id} | Instructor,SuperAdmin + InstructorOwnership | Update bundle | |
| POST | /{id}/items | Instructor,SuperAdmin + InstructorOwnership | Add course to bundle | |
| DELETE | /{id}/items/{courseId} | Instructor,SuperAdmin + InstructorOwnership | Remove course from bundle | |

## Orders — `api/orders` (OrdersController)
| Method | Route | Auth | Purpose | Issues |
|---|---|---|---|---|
| POST | / | auth | Create cart (returns existing pending cart if any) | added phase 2 (C9) |
| GET | /{id} | auth + UserOwnerOrAdmin(order.UserId) | Order by id | |
| GET | / | Admin,SuperAdmin | Paged orders feed (+search by buyer name/email) | added admin console phase; joins Users, newest first |
| GET | /cart/{userId} | auth + UserOwnerOrAdmin | Current pending cart | fake empty order when none (M7) |
| POST | /{OrderId}/items/{ItemId} | auth + UserOwnerOrAdmin | Add product (SP) | unpublished products rejected (H6 fixed) |
| DELETE | /{id}/items/{productId} | auth + UserOwnerOrAdmin | Remove item (SP) | silent success when absent (M8) |
| PUT | /{id}/complete | auth + UserOwnerOrAdmin | Mark Completed | requires succeeded payment (H5 fixed) |
| PUT | /{id}/cancel | auth + UserOwnerOrAdmin | Cancel | expires pending payments (H5 fixed) |

## Payments — `api/payments` (PaymentsController)
| Method | Route | Auth | Purpose | Issues |
|---|---|---|---|---|
| GET | /{id} | auth + UserOwnerOrAdmin(order.UserId) | Payment details | |
| GET | / | Admin,SuperAdmin | Paged payments feed (+search by buyer name/email) | added admin console phase; joins Orders+Users, newest first |
| POST | / | auth + UserOwnerOrAdmin | Create payment (optional discount code + idempotency key) | client key replayed, not duplicated (H7 fixed) |
| PUT | /{id}/checkOut-succeed | auth + UserOwnerOrAdmin | Mark Succeeded + enrollments + complete order | self-service w/o gateway (H1); completes order + expires stale pendings (H5) |
| PUT | /{id}/fail | auth + UserOwnerOrAdmin | Mark Failed | |
| PUT | /{id}/expire | auth + UserOwnerOrAdmin | Mark Expired | |

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
- GET current-user enrollments / payments (DAL exists, never exposed) — L9
- certificate verify/download — M14
