# API Reference

Navigation: [Overview](00-overview.md) · [Architecture](01-architecture.md) · [Database](02-database.md) · [Business Logic](03-business-logic.md) · [Workflows](04-workflows.md) · [Security](06-security.md) · [Testing](07-testing-and-deployment.md) · [Glossary](08-glossary.md)

Base route prefix: `/api`. Controllers use standard REST verbs. Auth via JWT Bearer except webhook.

## Auth
- POST `/api/auth/login`, `/refresh`, `/logout`.

## Users
- GET `/api/users/students`, `/admins`, `/instructors`, `/{id}`, `/by-email`.
- POST `/api/users/students`, `/{id}/activate`, role changes.
- PUT `/api/users/{id}`, `/{id}/password`.
- DELETE `/api/users/{id}`.

## Courses/Lessons/Bundles
- Courses: GET/POST/PUT, publish/unpublish, lessons assignment, instructors.
- Lessons: GET/POST/PUT, publish/unpublish, delete/restore.
- Bundles: GET/POST/PUT, publish/unpublish, items add/remove.

## Orders/Payments
- Orders: GET, POST, GET cart, items add/remove, complete/cancel.
- Payments: GET, POST, stripe-checkout-session, succeed/fail/expire.
- Webhook: POST `/api/webhooks/stripe` (AllowAnonymous, HMAC).

## Enrollments/Progress/Certificates
- Enrollments: GET my.
- Progress: GET lessons/courses, complete/incomplete, certificate.
- Certificates: GET by code, download.

## Admin
- Discounts, Audits, Logs endpoints.

All endpoints return problem+json on errors. See controllers for exact DTOs.
```