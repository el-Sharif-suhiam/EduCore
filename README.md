# 🎓 EduCore — Learning Management Platform (Backend)

<div align="center">

**A secure, production-grade e-learning backend — database design, T-SQL, layered .NET Core API, authenticatable APIs with Stripe payments and certificates.**

![.NET](https://img.shields.io/badge/.NET%2010-512BD4?logo=.net&logoColor=white&style=for-the-badge)
![C#](https://img.shields.io/badge/C%23-239120?logo=csharp&logoColor=white&style=for-the-badge)
![SQL Server](https://img.shields.io/badge/SQL%20Server-CC2927?logo=microsoftsqlserver&logoColor=white&style=for-the-badge)
![JWT](https://img.shields.io/badge/JWT%20Auth-000000?logo=jsonwebtokens&logoColor=white&style=for-the-badge)
![Stripe](https://img.shields.io/badge/Stripe%20Checkout-635BFF?logo=stripe&logoColor=white&style=for-the-badge)

**ADO.NET · T-SQL · BCrypt · Rate Limiting · QuestPDF · Swagger/OpenAPI**

</div>

> **Status:** backend-only at this stage. The frontend (Next.js) is being
> developed in a separate phase and will be folded into this repository later.

---

## 📖 Table of Contents

- [Overview](#-overview)
- [Why This Project](#-why-this-project)
- [Architecture — Three-Tier](#-architecture--three-tier)
- [Tech Stack](#-tech-stack)
- [Features](#-features)
- [Security Implementation](#-security-implementation)
- [Database Design](#-database-design)
- [API Surface](#-api-surface)
- [Project Structure](#-project-structure)
- [Getting Started](#-getting-started)
- [Stripe Payments (End-to-End)](#-stripe-payments-end-to-end)
- [Documentation](#-documentation)
- [Troubleshooting](#-troubleshooting)

---

## 🌟 Overview

**EduCore** is a complete learning management platform (LMS) **backend**: students discover,
purchase, and complete courses — and earn printable certificates — entirely through the REST API.

The API supports:

- 🔍 Catalog of **courses, standalone lessons, and bundles**
- 🛒 Cart and orders with **discount codes**
- 💳 Stripe hosted checkout (HMAC-verified webhooks)
- ▶️ Lesson progress tracking (per-lesson completion)
- 📜 **Certificate** generation at 100% course completion (PDF)
- 👥 User management with **Instructor / Admin roles**
- 🎫 Discount-code management and auditing

> Built as a final software development project — the focus is on **engineering discipline**:
> requirements analysis → database design (T-SQL) → layered backend with security best practices →
> a clean, testable REST API.

---

## 🎯 Why This Project

This project demonstrates the complete, hands-on skillset expected of a backend developer:

| Skill | Where it's proven |
|---|---|
| **Requirements & system design** | `docs/decisions/0001-discovery-phase.md`, domain lifecycle models |
| **Database design & T-SQL** | `EduCore.sql` — 15+ tables, constraints, filtered indexes, stored procedures, seed data |
| **3-tier architecture** | API → Business → Data layers with a shared `Common` library |
| **Raw ADO.NET (no ORM)** | `EduCore_DataAccess/cls*Data.cs` — parameterized commands, transactions, stored procedures |
| **Security** | JWT authentication, resource-based authorization, BCrypt, rate limiting, HMAC webhook verification, exception middleware |
| **Payments integration** | Stripe hosted checkout + verified webhooks with atomic enrollment completion |

---

## 🏗 Architecture — Three-Tier

The system follows a strict **3-tier / layered architecture**, keeping concerns separated and testable:

```
┌────────────────────────────────────────────────────────────────────────────┐
│                         PRESENTATION LAYER                                 │
│                         EduCoreAPI  (Web API)                               │
│    Controllers · JWT Auth · Authorization Handlers · Middleware · Swagger   │
└───────────────────────────────┬────────────────────────────────────────────┘
                                │  API Responses / DTOs
                                ▼
┌────────────────────────────────────────────────────────────────────────────┐
│                         BUSINESS LAYER                                      │
│                       EduCore_BusinessLayer                                 │
│   clsUser · clsOrder · clsPayment · clsCheckoutService · clsStripeGateway · │
│          clsProgress (certificates) · discount rules · domain validation    │
└───────────────────────────────┬────────────────────────────────────────────┘
                                │  Domain calls
                                ▼
┌────────────────────────────────────────────────────────────────────────────┐
│                         DATA ACCESS LAYER                                   │
│                      EduCore_DataAccess                                     │
│   cls*Data (static ADO.NET classes) · Microsoft.Data.SqlClient              │
│   Parameterized SQL · Stored Procedures · Shared transactions               │
└───────────────────────────────┬────────────────────────────────────────────┘
                                │  T-SQL
                                ▼
                    ┌─────────────────────────┐
                    │      SQL Server         │
                    │   tables · views · SPs   │
                    └─────────────────────────┘
```

**Plus a shared cross-cutting library:**

```
        Common  (DTOs · Enums · Exceptions · Validation utilities)
```

- **Presentation:** controllers only parse HTTP, run authorization policies, and delegate. No business logic leaks in.
- **Business:** Active-Record–style classes (`clsUser`, `clsOrder`, …) that own domain rules — hashing, discount validation, enrollment expiry, checkout completion.
- **Data:** static ADO.NET classes execute parameterized SQL and stored procedures. `clsGeneralData.ExecuteTransaction` shares one connection + transaction for multi-step DB operations (registration, payment completion).
- **Common:** shared DTOs, enums (`enRoles`, `enOrderStatus`, …), typed exceptions (`clsValidationException`, `clsNotFoundException`, `clsConflictException`, `clsUnAuthorizedAccess`), and validators — with no upward dependencies.

> 🔗 **Data flow example (purchase):**
> `CartController` → `clsOrder` (business) → `clsOrdersData` (ADO.NET) → `SP_CreateNewPayment` (SQL Server) → Stripe checkout → verified webhook → `clsCheckoutService` atomically creates enrollments.

---

## 🛠 Tech Stack

| Technology | Purpose |
|---|---|
| **.NET 10 / ASP.NET Core Web API** | REST API host (`net10.0`) |
| **C#** | Implementation language |
| **Microsoft.Data.SqlClient** | Raw ADO.NET data access (no ORM) |
| **SQL Server + T-SQL** | Relational storage, constraints, stored procedures |
| **JWT Bearer (HS256)** | Stateless authentication (30-min access tokens) |
| **BCrypt.Net-Next** | Password hashing (work factor 12) + refresh-token hashing |
| **ASP.NET Core Rate Limiting** | Built-in fixed-window limiter (login/refresh/registration) |
| **Swashbuckle / OpenAPI** | Swagger UI (Development only) |
| **QuestPDF + QRCoder** | A4 certificate PDF generation |
| **Stripe REST (raw HttpClient)** | Hosted checkout + HMAC-verified webhooks (no SDK) |
| **DotNetEnv** | `.env`-based configuration (secrets never in source) |

---

## ✨ Features

### 👨‍🎓 Student Experience
- Register / Login with secure JWT sessions (access + rotating refresh tokens)
- Browse published courses, lessons, and bundles
- Shopping cart with **discount codes** (expiry + usage limits)
- Stripe hosted checkout with success/cancellation flow
- **12-month enrollments** created atomically on validated payment
- Lesson video metadata + **progress tracking** (per-lesson completion)
- Course-completion percentage and **printable certificate**

### 👨‍🏫 Instructor & Admin Experience
- Full course builder: create courses, lessons, publishing workflow
- Manage users, grant/revoke **Instructor** and **Admin** roles
- Discount-code management (SuperAdmin)
- **Audit log** viewer — track administrative actions
- **System log** viewer — structured error/monitoring log

### ⚙️ Cross-Cutting
- Centralized exception middleware → RFC `problem+json` responses
- Resource-based authorization (owners, instructors, enrolled users, admins)
- Role-based access (Admin / Instructor / Student / SuperAdmin)
- Soft-delete for users and products; full audit trail

---

## 🔐 Security Implementation

Security is a first-class concern, not an afterthought:

| Control | Implementation |
|---|---|
| **Password hashing** | BCrypt (work factor 12) via `BCrypt.Net-Next` — never stored in plaintext |
| **JWT authentication** | HS256-signed access tokens, validated issuer/audience/lifetime/signing key (`Program.cs`) |
| **Refresh-token rotation** | Hashing refresh tokens with BCrypt, 3-day expiry, rotation on each refresh, revocation on logout (`clsUser`) |
| **Rate limiting** | Fixed-window limiting per IP: **5 req/min** on login & refresh, **3 req/min** on registration — 429 `problem+json` responses |
| **Resource-based authorization** | Custom `IAuthorizationHandler`s: `InstructorOwnership`, `UserOwnerOrAdmin`, `UserOwnerOnly`, `IsUserEnrolledOrAdmin` |
| **Defense against SQL injection** | 100% parameterized ADO.NET commands / stored procedures |
| **Stripe webhook HMAC** | Request signatures verified with HMAC-SHA256 + `whsec_` secret before any side effect |
| **Secret management** | Zero secrets in source — `.env` (gitignored) + environment variables; `appsettings.json` holds no connection strings |
| **Centralized error handling** | `ExceptionMiddleware` maps exceptions → proper HTTP statuses, prevents stack-trace leakage |
| **Soft deletes** | Users/products are deactivated, not destroyed — auditability preserved |

---

## 🗄 Database Design

> The full schema lives in **`EduCore.sql`** — clean enough to read line-by-line.

### Core tables

| Table | Purpose |
|---|---|
| `Users` / `Roles` / `UserRoles` | Users, roles (4 seeded), M:N role assignment |
| `Products` | Polymorphic sellable base row (`ProductType`: Lesson / Course / Bundle / CourseLesson) |
| `Courses` / `Bundles` / `Lessons` | 1:1 product extensions + content |
| `CoursesInstructors` | Course ↔ instructor ownership (M:N) |
| `BundlesItems` | Bundle contents |
| `Orders` / `OrderItems` | Order state machine + product snapshots (`PriceAtPurchase`) |
| `Payments` | Idempotency-keyed payment lifecycle, `FinalPrice` computed |
| `Enrollments` | Access grant with `ExpireAt = now + 12 months`, unique per `(UserId, ProductId)` |
| `Progress` | Per-lesson completion (upsert) |
| `DiscountCodes` | Rate (1–100), expiry, usage limits |
| `AuditLogs` / `SystemLogs` | Administrative actions + system events/errors |

### Database engineering highlights

- **Native constraints** — `CHECK` on payment statuses, `UNIQUE` filtered indexes (one pending order per user), `FinalPrice >= 0`
- **Stored procedures** — `SP_AddNewItemToOrder`, `SP_CreateNewPayment`, `SP_CreateEnrollmentsFromPaidOrder`
- **Server-side price computation** — prices and discounts computed in T-SQL so clients can't inject totals
- **Seed data** — demo instructors/products with hashed credentials (`docs/database/seed-demo-courses.sql`)

---

## 🔌 API Surface

14 controllers, all reachable under `/api/*` (full inventory: `docs/api/endpoint-inventory.md`).

| Area | Highlights | Auth |
|---|---|---|
| **Auth** | `POST /auth/login` · `/auth/refresh` · `/auth/logout` | Anonymous (rate-limited) |
| **Users** | List, get, update, change password, promote to instructor | Owner / Admin |
| **Courses** | Full CRUD, lesson management, publish/unpublish | Instructor ownership / Admin |
| **Lessons** | Full CRUD + publish/unpublish | Instructor ownership |
| **Bundles** | CRUD + publish/unpublish | Admin |
| **Catalog** | `GET /courses`, `GET /lessons`, `GET /bundles` | Public (published only) |
| **Enrollments** | `GET /enrollments/my` | Student |
| **Progress** | mark lesson complete, course %, certificate | Enrolled user / Admin |
| **Cart & Orders** | Create order, add/remove items, checkout complete | Student |
| **Payments** | Create payment, Stripe checkout session, status | Student |
| **Webhooks** | `POST /webhooks/stripe` | HMAC-verified |
| **Discounts** | Create/validate/delete codes | SuperAdmin (validate = public) |
| **Audit / Logs** | `GET /audit` · `GET /logs` | Admin |

> Swagger UI with JWT "Authorize" is available at `/swagger` in Development.

---

## 📁 Project Structure

```
EduCore/
├── EduCore.slnx                    .NET solution (4 projects)
├── EduCore.sql                     Full DB schema + views + stored procedures + seed
│
├── Common/                         ── Cross-cutting layer ──
│   ├── DTOs/                       Request/response contracts
│   ├── Enums/                      enRoles, enOrderStatus, enPaymentStatus, ...
│   ├── Exceptions/                 Typed exceptions mapped to HTTP statuses
│   ├── Utils/                      Email / password / URL / price validators
│   └── ViewModels/
│
├── EduCoreAPI/                     ── PRESENTATION LAYER ──
│   ├── Controllers/                14 REST controllers
│   ├── Authorization/              Custom policy handlers (ownership / enrollment)
│   ├── Middlewares/                ExceptionMiddleware (problem+json)
│   ├── Program.cs                  JWT, rate limiting, CORS, Swagger, pipeline
│   └── .env.example                Env template (secrets live here, not source)
│
├── EduCore_BusinessLayer/          ── BUSINESS LAYER ──
│   ├── clsUser.cs                  BCrypt, refresh-token rotation
│   ├── clsOrder.cs / clsPayment.cs / clsEnrollment.cs ...
│   ├── clsCheckoutService.cs       Transactional payment-success completion
│   ├── clsStripeGateway.cs         Raw Stripe REST + webhook HMAC verification
│   └── clsProgress.cs              Progress rules + QuestPDF certificates
│
├── EduCore_DataAccess/             ── DATA ACCESS LAYER ──
│   ├── clsDataAccessSettings.cs    Connection string from env (never appsettings)
│   ├── cls*Data.cs                 Parameterized ADO.NET + stored procedures
│   └── clsGeneralData.cs           Shared ExecuteTransaction helper
│
├── EduCore.IntegrationTests/       xUnit test project (Testcontainers-ready)
├── frontend/                       Next.js app — will be added in a later phase
└── docs/
    ├── api/endpoint-inventory.md   Every endpoint + auth level + known issues
    ├── architecture/               Actual architecture notes
    ├── database/                   Schema docs + demo seed + catalog reset
    ├── security/                   Security audit write-up
    ├── decisions/                  ADRs (0001-discovery-phase)
    ├── domain/                     Lifecycle models
    └── testing/                    Test strategy
```

---

## 🚀 Getting Started

### Prerequisites

- **.NET 10 SDK**
- **SQL Server** (LocalDB, Express, Developer, or any reachable instance)
- *(Optional)* [Stripe CLI](https://stripe.com/docs/stripe-cli) for local payment testing

### 1. Database (once)

```bash
sqlcmd -S . -d EduCore -i EduCore.sql
```

`EduCore.sql` creates the database objects — all tables, views, and stored procedures — and seeds base roles.

### 2. Backend

```bash
cd EduCoreAPI
copy .env.example .env        # then fill in real values
dotnet run
```

`.env` needs:
```ini
JWT_SECRET_KEY=<long random string, ≥64 chars>
DB_CONNECTION=Server=.;Database=EduCore;User Id=...;Password=...;TrustServerCertificate=True
STRIPE_SECRET_KEY=sk_test_...        # optional for checkout testing
STRIPE_WEBHOOK_SECRET=whsec_...      # optional
FRONTEND_URL=http://localhost:3000
```

Generate a JWT secret quickly:
```powershell
[Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Maximum 256 }))
```

- HTTP: `http://localhost:5087` · HTTPS: `https://localhost:7009`
- Swagger: `https://localhost:7009/swagger` (Development only)

---

## 💳 Stripe Payments (End-to-End)

The full purchase loop is live, end-to-end:

```
cart → create payment → Stripe hosted checkout → redirect → webhook (HMAC-verified)
     → transactional completion → enrollments created atomically
```

| File | Role |
|---|---|
| `EduCore_BusinessLayer/clsStripeGateway.cs` | Raw Stripe REST via `HttpClient` + webhook HMAC verification (**no SDK** — provider-switch point) |
| `EduCoreAPI/Controllers/PaymentsController.cs` | `POST /api/payments/{id}/stripe-checkout-session` → `{ url }` |
| `EduCoreAPI/Controllers/StripeWebhookController.cs` | Verified webhook → runs the transactional completion chain |

Test locally with the Stripe CLI:

```bash
stripe listen --forward-to localhost:5087/api/webhooks/stripe
stripe trigger checkout.session.completed        # or use test card 4242 4242 4242 4242
```

---

## 🚀 Production Deployment

> The code is production-hardened (fail-fast secrets, forwarded headers, HSTS,
> env-driven CORS, admin-only payment hooks, config test suite). Before launch
> the operator must provide real secrets/hosting — follow the
> **[Deployment Checklist](./docs/deployment/PRODUCTION_CHECKLIST.md)**.

Key config: the backend reads `EduCoreAPI/.env` (see `.env.example`). Expose the API
behind a TLS proxy and allow your real origin via `CORS_ALLOWED_ORIGINS`.

---

## 🧭 Documentation

| Doc | What it covers |
|---|---|
| [`docs/api/endpoint-inventory.md`](./docs/api/endpoint-inventory.md) | Authoritative endpoint + auth matrix |
| [`docs/domain/lifecycles.md`](./docs/domain/lifecycles.md) | Domain model: users, products, orders, payments, enrollments, certificates |
| [`docs/security/audit-2026-08-22.md`](./docs/security/audit-2026-08-22.md) | Security audit & findings |
| [`docs/decisions/0001-discovery-phase.md`](./docs/decisions/0001-discovery-phase.md) | Architecture decisions (ADRs) |
| [`docs/deployment/PRODUCTION_CHECKLIST.md`](./docs/deployment/PRODUCTION_CHECKLIST.md) | Everything needed before going live |
| [`docs/database/schema.md`](./docs/database/schema.md) | Database schema documentation |

---

## 🛠 Troubleshooting

| Symptom | Fix |
|---|---|
| Backend starts then dies with `JWT_SECRET_KEY` error | `.env` missing beside `EduCoreAPI.csproj`, var name typo, or the secret is under 32 bytes (API now fails fast by design) |
| SQL errors referencing `SP_*` not found | Re-run `EduCore.sql` against your database |
| `GET /api/bundles` or `GET /api/lessons` returns 500 "Invalid column name" | Database out of sync with `EduCore.sql` (or an old binaries draft queryed a `Products.IsDeleted` column — fixed in code). Re-run `EduCore.sql` (idempotent; `CREATE OR ALTER VIEW vwLessonsWithOutCourses` refreshes the stale view), then restart the API |
| Catalog endpoints return empty lists | Expected with no published data — run `docs/database/seed-demo-courses.sql` |

---

## 📄 License

@(Licensed for showcase/portfolio purposes)

---

<div align="center">

**Built with discipline, designed with care** — from requirements and T-SQL to a secure, layered .NET API.

_Questions, feedback, or opportunities? Let's talk._

</div>