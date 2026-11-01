# EduCore — System Overview

## 1. What EduCore Is

EduCore is a backend-only learning-commerce platform. It sells educational
products (individual lessons, courses, and bundles of courses) through a
Stripe-hosted checkout flow and, after a verified payment, grants the buyer
time-limited enrollments that unlock course content and lesson progress
tracking. A completed course can earn a verifiable PDF certificate.

The repository contains no frontend application: EduCore is consumed through
its REST API (Swagger is enabled in Development).

## 2. Problem It Solves

EduCore addresses the full lifecycle of selling and delivering online
education:

- Catalog management for lessons, courses, and bundles.
- Order and cart management with price snapshots.
- Discount codes with usage limits and expiry.
- Payment initiation, Stripe hosted checkout, and verified webhook completion.
- Enrollment creation/renewal with expiry.
- Bundle-derived access (a bundle purchase unlocks its included courses).
- Lesson progress tracking and course completion calculation.
- Certificate issuance and public verification.

Without EduCore, these concerns would have to be assembled ad hoc per
project. EduCore centralises them behind a single, transactional,
three-layer .NET backend.

## 3. Primary Actors

| Actor | Capabilities |
|---|---|
| Anonymous visitor | Browse published products, bundles, courses, lessons, verify certificates. |
| Student | Everything an anonymous visitor can do, plus order/cart, checkout, progress, certificates, view own enrollments/payments/orders. |
| Instructor | Create and manage own courses/lessons/bundles, publish/unpublish, manage bundle items and instructors. |
| Admin | User management, audit/log viewing, discount management, order/payment administration (within allowed policies). |
| SuperAdmin | All admin capabilities plus role grants/removals and full bundle/order visibility. |

## 4. System Boundaries

Inside the system:

- User identity, roles, activation.
- Product catalog (lessons, courses, bundles).
- Ordering, pricing, discounts.
- Payments and Stripe integration.
- Enrollments and access control.
- Progress and certificates.
- Audit and logging.

Outside the system (integrated, not owned):

- SQL Server (persistence).
- Stripe (payment processing and hosted checkout).
- File system (certificate PDF storage).
- Email/notification delivery (not implemented in this branch).

## 5. Major Capabilities

1. **Authentication** — JWT access tokens + rotating refresh tokens.
2. **Catalog** — lessons (standalone or course-attached), courses, bundles.
3. **Ordering** — one pending order per user, item add/remove with price snapshots.
4. **Discounts** — percentage discounts, expiry, usage limits, reservation via pending payments.
5. **Checkout** — payment creation, Stripe checkout session, webhook verification, atomic completion.
6. **Enrollments** — direct product enrollment, bundle-derived access, expiry, renewal.
7. **Progress** — per-lesson completion, course progress percentage, course completion.
8. **Certificates** — idempotent issuance, PDF generation, public verification.
9. **Administration** — users, roles, audits, logs, discounts.
10. **Observability** — structured audit trail and error logs.

## 6. Core Domain Concepts

- **Product** — the sellable unit. Every lesson, course, and bundle has exactly
  one Product row carrying name, price, publication flag, and creator.
- **Course** — educational content container; owns lessons through
  `CoursesLessons`.
- **Lesson** — either standalone (`CourseId IS NULL`) or attached to a course.
- **Bundle** — a product that groups courses via `BundlesItems`.
- **Order** — a user's cart; one `Pending` order per user at a time.
- **OrderItem** — a product line in an order with a price snapshot.
- **Payment** — a payment attempt against an order; states Pending/Succeeded/
  Failed/Expired/Cancelled.
- **DiscountCode** — percentage discount with optional expiry and usage limit.
- **Enrollment** — grants access to a product for a user until `ExpireAt`.
- **Progress** — per (user, lesson) completion record.
- **Certificate** — unique per (user, course), verifiable by code.

## 7. Project Relationships

Arrows read "depends on" (each arrow is a `.csproj` project reference):

```
EduCoreAPI  -->  EduCore_BusinessLayer  -->  EduCore_DataAccess  -->  Common
    |                                                            ^
    +------------------------------------------------------------+
```

- `Common` — DTOs, enums, exceptions, ViewModels, utilities. No dependencies.
- `EduCore_DataAccess` — ADO.NET data access, stored procedure calls. Depends on `Common`.
- `EduCore_BusinessLayer` — business rules and orchestration. Depends on `Common` and `EduCore_DataAccess`.
- `EduCoreAPI` — ASP.NET Core controllers, auth, middleware. References `EduCore_BusinessLayer` and `Common` only; all database operations are reached through the Business Layer, never by calling `EduCore_DataAccess` directly.
- `EduCore.IntegrationTests` — SQL-backed integration tests. References `EduCoreAPI`.

## 8. Purchase-to-Access Lifecycle

```
Product -> Order -> OrderItem -> Payment -> Stripe -> Verified Webhook
  -> Payment Success -> Discount Usage -> Enrollment -> Completed Order
  -> Progress -> Certificate
```

1. Buyer (or admin) creates an order and adds products.
2. A discount may be applied at payment creation.
3. `SP_CreateNewPayment` creates a `Pending` payment and reserves a discount slot.
4. Stripe hosted checkout collects the money.
5. Stripe sends a signed webhook.
6. EduCore verifies the signature, loads the payment, and (inside one SQL
   transaction) marks it `Succeeded`, increments discount usage, creates
   enrollments, completes the order, and expires sibling pending payments.
7. The buyer can now access content, track progress, and earn certificates.

## 9. Technology Stack

| Component | Technology | Purpose |
|---|---|---|
| Runtime | .NET 10 | Application runtime. |
| Web framework | ASP.NET Core | HTTP API, middleware, DI. |
| Persistence | SQL Server + ADO.NET | All data; stored procedures for critical paths. |
| Auth | JWT Bearer + refresh tokens | Stateless access + rotating refresh. |
| Payments | Stripe | Hosted checkout + webhooks. |
| PDF | QuestPDF | Certificate generation. |
| QR | QRCoder/ZXing | Certificate QR codes. |
| Passwords | BCrypt | Password hashing. |
| Tests | xUnit | Integration tests against isolated DB. |

## 10. Architectural Characteristics

- Three-layer architecture (API / Business / DataAccess) plus `Common`.
- No ORM; all SQL is explicit ADO.NET.
- Stored procedures for concurrency-critical paths (payments, orders, enrollments, certificates).
- Transactions managed via `clsGeneralData.ExecuteTransaction`.
- Soft deletion for courses and lessons.
- Conditional (compare-and-swap) status transitions.
- Idempotent payment creation and certificate issuance.

## 11. Reading Order

1. `01-architecture.md` — projects, layers, request flow.
2. `02-database.md` — schema reference.
3. `03-business-logic.md` — domain rules.
4. `04-workflows.md` — end-to-end processes.
5. `05-api-reference.md` — HTTP contract.
6. `06-security.md` — security model.
7. `07-testing-and-deployment.md` — running and deploying.
8. `08-glossary.md` — term definitions.