# EduCore — Learning Management Platform (Backend)

<div align="center">

**A secure, production-grade e-learning backend — database design, T-SQL, layered .NET API, authenticatable APIs with Stripe payments and certificates.**

![.NET](https://img.shields.io/badge/.NET%2010-512BD4?logo=.net&logoColor=white&style=for-the-badge)
![C#](https://img.shields.io/badge/C%23-239120?logo=csharp&logoColor=white&style=for-the-badge)
![SQL Server](https://img.shields.io/badge/SQL%20Server-CC2927?logo=microsoftsqlserver&logoColor=white&style=for-the-badge)
![JWT](https://img.shields.io/badge/JWT%20Auth-000000?logo=jsonwebtokens&logoColor=white&style=for-the-badge)
![Stripe](https://img.shields.io/badge/Stripe%20Checkout-635BFF?logo=stripe&logoColor=white&style=for-the-badge)

**ADO.NET · T-SQL · BCrypt · Rate Limiting · QuestPDF · Swagger/OpenAPI · xUnit**

</div>

> **Status:** backend-only project. Any client (browser app, mobile app, Postman)
> connects through the REST API and `CORS_ALLOWED_ORIGINS`.

---

## Table of Contents

- [Overview](#overview)
- [Why This Project](#why-this-project)
- [Architecture — Three-Tier](#architecture--three-tier)
- [Tech Stack](#tech-stack)
- [Features](#features)
- [Security Implementation](#security-implementation)
- [Database Design](#database-design)
- [API Surface](#api-surface)
- [Testing](#testing)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
- [Stripe Payments (End-to-End)](#stripe-payments-end-to-end)
- [Documentation](#documentation)

---

## Overview

**EduCore** is a complete learning management platform (LMS) **backend**: students discover,
purchase, and complete courses — and earn verifiable certificates — entirely through the REST API.

The API supports:

- Catalog of **courses, standalone lessons, and bundles** (publication-aware)
- Cart and orders with **discount codes**
- Stripe hosted checkout (HMAC-verified webhooks, idempotent completion)
- Lesson progress tracking (per-lesson completion)
- **Certificates** at 100% course completion — persisted, publicly verifiable, PDF download
- User management with **Instructor / Admin roles**
- Discount-code management, audit trail, and structured logging

> Built as a final software development project — the focus is on **engineering discipline**:
> requirements analysis → database design (T-SQL) → layered backend with security best practices →
> a clean, tested REST API.

---

## Why This Project

This project demonstrates the complete, hands-on skillset expected of a backend developer:

| Skill | Where it's proven |
|---|---|
| **Requirements & system design** | `docs/decisions/0001-discovery-phase.md`, domain lifecycle models |
| **Database design & T-SQL** | `EduCore.sql` — 19 tables, constraints, filtered indexes, stored procedures, seed data |
| **3-tier architecture** | API → Business → Data layers with a shared `Common` library |
| **Raw ADO.NET (no ORM)** | `EduCore_DataAccess/cls*Data.cs` — parameterized commands, transactions, stored procedures |
| **Security** | JWT authentication, resource-based authorization, BCrypt, rate limiting, HMAC webhook verification, exception middleware |
| **Payments integration** | Stripe hosted checkout + verified webhooks with atomic, idempotent enrollment completion |
| **Testing** | 79 xUnit tests: real SQL Server integration tests + in-process API tests + crypto unit tests + concurrency regression tests |

---

## Architecture — Three-Tier

The system follows a strict **3-tier / layered architecture**, keeping concerns separated:

```
+--------------------------------------------------------------+
|                      PRESENTATION LAYER                      |
|                      EduCoreAPI (Web API)                    |
|  Controllers - JWT Auth - Authorization Handlers - Swagger   |
+--------------------------------------------------------------+
                    |  API Responses / DTOs
                    v
+--------------------------------------------------------------+
|                       BUSINESS LAYER                         |
|                     EduCore_BusinessLayer                    |
|  clsUser - clsOrder - clsPayment - clsCheckoutService        |
|  clsStripeGateway - clsProgress (certificates)               |
+--------------------------------------------------------------+
                    |  Domain calls
                    v
+--------------------------------------------------------------+
|                      DATA ACCESS LAYER                       |
|                      EduCore_DataAccess                      |
|  cls*Data (static ADO.NET classes) - SqlClient               |
|  Parameterized SQL - Stored Procedures - Shared transactions |
+--------------------------------------------------------------+
                    |  T-SQL
                    v
            +------------------+
            |    SQL Server    |
            | tables, views,   |
            |       SPs        |
            +------------------+
```

**Plus a shared cross-cutting library:**

```
Common  (DTOs · Enums · Exceptions · Validation utilities)
```

- **Presentation:** controllers parse HTTP, run authorization policies, and delegate.
- **Business:** Active-Record–style classes (`clsUser`, `clsOrder`, …) that own domain rules —
  hashing, discount validation, enrollment expiry, checkout completion.
- **Data:** static ADO.NET classes execute parameterized SQL and stored procedures.
  `clsGeneralData.ExecuteTransaction` shares one connection + transaction for multi-step DB
  operations (registration, payment completion).
- **Common:** shared DTOs, enums (`enRoles`, `enOrderStatus`, …), typed exceptions
  (`ValidationException`, `NotFoundException`, `ConflictException`), and validators — with no
  upward dependencies.

### Project dependencies (strictly one-way)

| Project | Project references (`.csproj`) |
|---|---|
| `EduCoreAPI` | `EduCore_BusinessLayer`, `Common` |
| `EduCore_BusinessLayer` | `EduCore_DataAccess`, `Common` |
| `EduCore_DataAccess` | `Common` |
| `Common` | — |

Communication never skips or reverses a layer: controllers call business classes only,
business classes are the only callers of `EduCore_DataAccess`, and `Common` provides shared
contracts without enabling any bypass of these boundaries.

> **Data flow example (purchase):**
> `PaymentsController` → `clsOrder`/`clsPayment` (business) → `cls*Data` (ADO.NET) →
> `SP_CreateNewPayment` (SQL Server) → Stripe checkout → verified webhook →
> `clsCheckoutService` atomically creates enrollments.

---

## Tech Stack

| Technology | Purpose |
|---|---|
| **.NET 10 / ASP.NET Core Web API** | REST API host (`net10.0`) |
| **C#** | Implementation language |
| **Microsoft.Data.SqlClient** | Raw ADO.NET data access (no ORM) |
| **SQL Server + T-SQL** | Relational storage, constraints, stored procedures |
| **JWT Bearer (HS256)** | Stateless authentication (30-min access tokens) |
| **BCrypt.Net-Next** | Password hashing (work factor 12) + refresh-token hashing |
| **ASP.NET Core Rate Limiting** | Fixed-window limiters (login/refresh/certificates 5/min, registration 3/min) |
| **Swashbuckle / OpenAPI** | Swagger UI (Development only) |
| **QuestPDF + QRCoder** | A4 certificate PDF generation |
| **Stripe REST (raw HttpClient)** | Hosted checkout + HMAC-verified webhooks (no SDK) |
| **DotNetEnv** | `.env`-based configuration (secrets never in source) |
| **xUnit** | Unit + integration + API tests |

---

## Features

### Student Experience
- Register / Login with secure JWT sessions (access + rotating refresh tokens)
- Browse published courses, lessons, and bundles
- Shopping cart with **discount codes** (expiry + usage limits)
- Stripe hosted checkout with success/cancellation flow
- **12-month enrollments** created atomically on validated payment
- Lesson video metadata + **progress tracking** (per-lesson completion)
- Course-completion percentage and **printable, verifiable certificate**

### Instructor & Admin Experience
- Full course builder: create courses, lessons, publishing workflow
- Bundle management with publication workflow (drafts invisible to the public)
- Manage users, grant/revoke **Instructor** and **Admin** roles
- Discount-code management (SuperAdmin)
- **Audit log** viewer — track administrative actions
- **System log** viewer — structured error/monitoring log

### Cross-Cutting
- Centralized exception middleware → RFC `problem+json` responses
- Resource-based authorization (owners, instructors, enrolled users, admins)
- Role-based access (Admin / Instructor / Student / SuperAdmin)
- User deactivation (`Users.IsActive`) and soft deletion for courses/lessons (`IsDeleted`); full audit trail

---

## Security Implementation

Security is a first-class concern, not an afterthought:

| Control | Implementation |
|---|---|
| **Password hashing** | BCrypt (work factor 12) via `BCrypt.Net-Next` — never stored in plaintext |
| **JWT authentication** | HS256-signed access tokens; the API **refuses to start** without a strong secret (≥ 32 bytes) |
| **Refresh-token rotation** | Refresh tokens BCrypt-hashed, 3-day expiry, rotated on each refresh, revocable on logout (`clsUser`) |
| **Rate limiting** | Fixed-window per IP: **5 req/min** on login/refresh/certificates, **3 req/min** on registration — 429 `problem+json` |
| **Resource-based authorization** | Custom `IAuthorizationHandler`s: `InstructorOwnership`, `UserOwnerOrAdmin`, `UserOwnerOnly`, `IsUserEnrolledOrAdmin` |
| **Publication visibility** | Draft courses/lessons/bundles are filtered in SQL; drafts return 404 to non-owners — no client-side filtering |
| **No data oracles** | The anonymous email-existence endpoint was removed (account enumeration surface) |
| **Defense against SQL injection** | 100% parameterized ADO.NET commands / stored procedures |
| **Stripe webhook HMAC** | Request signatures verified with HMAC-SHA256 + `whsec_` secret + timestamp tolerance before any side effect |
| **Payment state machine** | Status transitions are conditional on `Pending` (DB-level); a succeeded payment can never be failed/expired; concurrent webhook deliveries cannot duplicate enrollments or discount usage |
| **Secret management** | Zero secrets in source — `.env` (gitignored) + environment variables |
| **Centralized error handling** | `ExceptionMiddleware` maps exceptions → proper HTTP statuses, prevents stack-trace leakage |
| **Deactivation / soft deletes** | Users are deactivated via `Users.IsActive`; courses and lessons are soft-deleted via `IsDeleted`; products have no soft-delete column (visibility is controlled by `IsPublished`) — auditability preserved |

---

## Database Design

The full schema lives in **`EduCore.sql`** — clean enough to read line-by-line.

### Core tables

| Table | Purpose |
|---|---|
| `Users` / `Roles` / `UserRoles` | Users, roles (4 seeded), M:N role assignment |
| `Products` | Polymorphic sellable base row (`ProductType`: Lesson / Course / Bundle) |
| `Courses` / `Bundles` / `Lessons` | 1:1 product extensions + content |
| `CoursesInstructors` | Course → instructor ownership (M:N) |
| `BundlesItems` | Bundle contents |
| `Orders` / `OrderItems` | Order state machine + product snapshots (`PriceAtPurchase`) |
| `Payments` | Idempotency-keyed payment lifecycle, `FinalPrice` computed |
| `Enrollments` | Access grant with `ExpireAt`, unique per `(UserId, ProductId)` |
| `Progress` | Per-lesson completion (upsert) |
| `Certificates` | One certificate per (user, course) with a public verification code |
| `DiscountCodes` | Rate (1–100), expiry, usage limits |
| `Audits` / `Logs` | Administrative actions + system events/errors |

### Database engineering highlights

- **Native constraints** — `CHECK` on payment statuses, filtered unique indexes
  (one pending order per user, unique transaction ids), computed `FinalPrice ≥ 0`
- **Stored procedures** — `SP_AddNewItemToOrder`, `SP_RemoveItemFromOrder`,
  `SP_CreateNewPayment`, `SP_CreateEnrollmentsFromPaidOrder`, `SP_IssueCertificate`
- **Server-side rules** — prices, discount application, order state, enrollment and
  certificate eligibility are enforced in T-SQL so clients cannot bypass them
- **Idempotent script** — guarded CREATEs; safe to re-run; integration tests apply it
  to a disposable database
- **Seed data** — demo instructors/products (`docs/database/seed-demo-courses.sql`)

---

## API Surface

14 controllers, all reachable under `/api/*` (full inventory: `docs/api/endpoint-inventory.md`).

| Area | Highlights | Auth |
|---|---|---|
| **Auth** | `POST /auth/login` · `/auth/refresh` · `/auth/logout` | Anonymous (rate-limited) |
| **Users** | List, get, update, change password, promote to instructor | Owner / Admin |
| **Courses** | Full CRUD, lesson management, publish/unpublish | Instructor ownership / Admin |
| **Lessons** | Full CRUD + publish/unpublish | Instructor ownership |
| **Bundles** | CRUD + publish/unpublish (drafts owner/admin-only) | Instructor ownership / Admin |
| **Catalog** | `GET /courses`, `GET /lessons`, `GET /bundles` | Public (published only) |
| **Enrollments** | `GET /enrollments/my` | Student |
| **Progress** | mark lesson complete, course %, certificate issuance | Enrolled user / Admin |
| **Certificates** | public verification + owner/admin PDF download | Anonymous (verify) |
| **Cart & Orders** | Create order, add/remove items, complete/cancel | Student (owner) |
| **Payments** | Create payment (idempotency key), Stripe checkout session, fail/expire | Student / Admin |
| **Webhooks** | `POST /webhooks/stripe` | HMAC-verified |
| **Discounts** | Create/validate/delete codes | SuperAdmin (validate = auth) |
| **Audit / Logs** | `GET /audit` · `GET /logs` | Admin (paging validated) |

> Swagger UI with JWT "Authorize" is available at `/swagger` in Development.

---

## Testing

79 xUnit tests across three styles:

| Style | What it covers |
|---|---|
| **Integration (real SQL Server)** | The test fixture creates a disposable `EduCoreTests` database from the repo's own `EduCore.sql` (dropped and recreated per run — your dev database is never touched), seeds known data via ADO.NET, and exercises the real stored procedures and readers |
| **In-process API** | `WebApplicationFactory<Program>` boots the real pipeline (JWT auth, authorization handlers, exception middleware) against the test database |
| **Unit (no I/O)** | Stripe webhook HMAC verification: valid signatures, tampering, wrong secrets, replay tolerance, malformed headers |

Regression suites include: course-progress SQL mapping, the order state machine
(business + stored-procedure level), payment transitions (conditional on Pending,
idempotent replays), bundle draft visibility (anonymous/student/owner/other-instructor/admin),
and the certificate lifecycle (issuance eligibility, idempotency, public verify,
owner-only download).

```bash
dotnet test EduCore.IntegrationTests
```

- With a local SQL Server: everything runs against `EduCoreTests`.
- Without SQL Server: DB/API tests are **skipped** (reported as blocked), config and
  crypto unit tests still run.
- Custom server (e.g. CI container): set `EDUCORE_TEST_DB_CONNECTION`.
- CI / strict mode: set `EDUCORE_REQUIRE_DB=1` to make an unreachable SQL Server
  **fail** the run instead of skipping the database tests.

---

## Project Structure

```
EduCore/
├── EduCore.slnx                    .NET solution (5 projects)
├── EduCore.sql                     Full DB schema + views + stored procedures + seed
├── .github/workflows/ci.yml        Build + test on push/PR
│
├── Common/                         — Cross-cutting layer —
│   ├── Dtos/                       Request/response contracts
│   ├── Enums/                      enRoles, enOrderStatus, enPaymentStatus, ...
│   ├── Exceptions/                 Typed exceptions mapped to HTTP statuses
│   ├── Utils/                      Email / password / URL / price validators
│   └── ViewModels/
│
├── EduCoreAPI/                     — PRESENTATION LAYER —
│   ├── Controllers/                14 REST controllers
│   ├── Authorization/              Custom policy handlers (ownership / enrollment)
│   ├── Middlewares/                ExceptionMiddleware (problem+json)
│   ├── Program.cs                  JWT, rate limiting, CORS, Swagger, pipeline
│   └── .env.example                Env template (secrets live here, not source)
│
├── EduCore_BusinessLayer/          — BUSINESS LAYER —
│   ├── clsUser.cs                  BCrypt, refresh-token rotation
│   ├── clsOrder.cs / clsPayment.cs / clsEnrollment.cs ...
│   ├── clsCheckoutService.cs       Transactional, idempotent payment-success completion
│   ├── clsStripeGateway.cs         Raw Stripe REST + webhook HMAC verification
│   └── clsProgress.cs              Progress rules + QuestPDF certificates
│
├── EduCore_DataAccess/             — DATA ACCESS LAYER —
│   ├── clsDataAccessSettings.cs    Connection string from env (never appsettings)
│   ├── cls*Data.cs                 Parameterized ADO.NET + stored procedures
│   └── clsGeneralData.cs           Shared ExecuteTransaction helper
│
├── EduCore.IntegrationTests/       xUnit: DB integration + API + unit tests
└── docs/
    ├── api/endpoint-inventory.md   Every endpoint + auth level
    ├── architecture/               Actual architecture notes
    ├── database/                   Schema docs + demo seed + catalog reset
    ├── decisions/                  ADRs (0001-discovery-phase)
    ├── deployment/                 Production checklist
    └── domain/                     Lifecycle models
```

---

## Getting Started

### Prerequisites

- **.NET 10 SDK**
- **SQL Server** (Express, Developer, or any reachable instance)
- *(Optional)* [Stripe CLI](https://stripe.com/docs/stripe-cli) for local payment testing

### 1. Database (once)

```bash
sqlcmd -S . -I -d EduCore -i EduCore.sql
```

`EduCore.sql` creates the database objects — all tables, views, and stored procedures — and seeds base roles.
The `-I` flag (QUOTED_IDENTIFIER ON) is required: the schema uses persisted computed
columns and filtered indexes.

### 2. Backend

```bash
cd EduCoreAPI
copy .env.example .env        # then fill in real values
dotnet run
```

`.env` needs:
```ini
JWT_SECRET_KEY=<long random string, >=64 chars>
DB_CONNECTION=Server=.;Database=EduCore;User Id=...;Password=...;TrustServerCertificate=True
STRIPE_SECRET_KEY=sk_test_...        # optional for checkout testing
STRIPE_WEBHOOK_SECRET=whsec_...      # optional
FRONTEND_URL=http://localhost:3000   # used for Stripe redirects + certificate QR
```

Generate a JWT secret quickly:
```powershell
[Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Maximum 256 }))
```

- HTTP: `http://localhost:5087` · HTTPS: `https://localhost:7009`
- Swagger: `https://localhost:7009/swagger` (Development only)

---

## Stripe Payments (End-to-End)

The full purchase loop:

```
cart → create payment → Stripe hosted checkout → redirect → webhook (HMAC-verified)
     → transactional completion → enrollments created atomically
```

Details in `docs/deployment/PRODUCTION_CHECKLIST.md` (webhook registration, live keys,
FRONTEND_URL for success/cancel redirects).

---

## Documentation

System guide: [docs/system-guide/00-overview.md](docs/system-guide/00-overview.md)

- `docs/api/endpoint-inventory.md` — every endpoint + auth level
- `docs/architecture/01-actual-architecture.md` — how the system actually works
- `docs/domain/lifecycles.md` — order/payment/certificate state machines
- `docs/database/schema.md` — tables, views, SPs
- `docs/deployment/PRODUCTION_CHECKLIST.md` — go-live checklist

---

## License

Released under the **GNU GPL v3** — see [LICENSE](LICENSE).
