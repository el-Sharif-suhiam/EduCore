# EduCore — Project Context (Agent Context File)

> Primary context file for AI agents working on EduCore. Describes the ACTUAL implementation.
> Last updated: 2026-08-22 (discovery/audit phase).

## Purpose
EduCore is a backend for an online-learning platform: instructors publish courses/lessons/bundles
(products), students buy them through cart → payment → enrollment, then track progress and earn
PDF certificates. Admins manage users, roles, discounts, audits and logs.

## Technology Stack
- .NET 10 (`net10.0`), ASP.NET Core Web API (controllers), C#
- SQL Server via raw ADO.NET (`Microsoft.Data.SqlClient`) — NO EF Core, no migrations; schema lives in `EduCore.sql`
- Auth: JWT Bearer (HS256) + hashed refresh tokens (BCrypt, work factor 12)
- Passwords: BCrypt.Net-Next
- PDF certificates: QuestPDF + QRCoder (ZXing referenced but effectively unused)
- Env config: DotNetEnv (`.env` loaded in `Program.cs`)
- Swagger via Swashbuckle (Development only)

## Solution Layout
```
EduCore.slnx
├── EduCoreAPI/            Web API: controllers, authorization handlers, mappers, request/response models, middleware
├── EduCore_BusinessLayer/ cls* business classes (Active-Record style), checkout service, certificate generation
├── EduCore_DataAccess/    cls*Data static data-access classes (ADO.NET + stored procedures)
├── Common/                DTOs, enums, exceptions, validation utils, view models (shared)
└── Dtos/                  ORPHANED duplicate of Common/Dtos — not in slnx, referenced by nothing (dead project)
```
NOTE: folder structure ≠ namespaces. `EduCoreAPI/Helpers/Models/RequestModels/*.cs` declare
`namespace EduCoreAPI.Helpers.Dtos.RequestDto`; `ResponeModels` declare `...Dtos.ResponeDto`.

## Architecture (actual)
- Layered: API → BusinessLayer → DataAccess → Common. Dependency direction is respected.
- "Active Record" pattern: `clsUser`, `clsCourse`, `clsLesson`, `clsBundle`, `clsOrder`, `clsPayment`,
  `clsDiscountCode`, `clsProgress` wrap a DTO, expose `Set*` validators, static `Find()`, and `Save()`
  switching on `enMode {Add, Update}`.
- Business + data classes are STATIC or instantiated directly — NO dependency injection of services.
  Only authorization handlers are registered in DI. Consequence: nothing is unit-testable without a seam.
- Transactions: `clsGeneralData.ExecuteTransaction(conn, tx)` helper; used for user creation,
  product+course/lesson/bundle creation, checkout. Many multi-step ops are NOT transactional.
- Request flow: HTTP → Controller → static cls* → static cls*Data → SqlConnection/SP → DTO → mapper → anonymous-object response.
- Errors: `ExceptionMiddleware` maps `ValidationException`→400, `NotFoundException`→404,
  `ConflictException`→409, `UnauthorizedAccessException`→401, else 500; logs every exception to `Logs` table.

## Domain Model
- `Users` —`UserRoles`→ `Roles` (Admin=1, Instructor=2, Student=3, SuperAdmin=4)
- `Products` (ProductType: Lesson=1, Course=2, Bundle=3) — base table for anything sellable
  (Name, BasePrice, CreatedByUser, IsPublished, ThumbnailUrl, Summary)
- `Courses` → ProductId (+CoverImageUrl, soft delete); `Lessons` → ProductId (+Title, VideoUrl, BodyText,
  InstructorId, CourseId nullable = independent vs course lesson, soft delete)
- `CoursesInstructors` (M:N), `Bundles` → ProductId, `BundlesItems` (bundle→courses)
- `Orders` (Status: Pending/Completed/Cancelled/Empty; filtered unique index: one Pending per user)
  → `OrderItems` (ProductId, PriceAtPurchase)
- `Payments` (Status: Pending/Succeeded/Failed/Expired/Cancelled; IdempotencyKey unique;
  FinalPrice = Price - DiscountPrice computed column) → `DiscountCodes`
- `Enrollments` (UserId, ProductId, PaymentId, ExpireAt; unique UserId+ProductId)
- `Progress` (UserId, LessonId, IsComplete), `Audits`, `Logs`
- Course↔lesson link is DUAL in code: `Lessons.CourseId` column (used by controllers/views) AND a
  `CourseLessons` join table referenced by `clsProgressData`/`clsCourseLessonData` (table was missing
  from `EduCore.sql`; added during fixes).

## Key Lifecycles
- Auth: register (anonymous, Student role auto-assigned) → login (JWT 30 min + refresh 3 days, rotated) → logout revokes.
- Purchase: create cart (order) → add/remove items (SP maintains TotalPrice + Status) →
  create payment (optional discount code) → mark succeeded (transaction: payment + enrollments via SP) → enrollments per product (12-month expiry, `clsGeneralRules.DefaultExpireDateByMonths`).
- Content: instructor creates product (unpublished by default) → publish → students can buy.
  Soft delete for courses/lessons; bundles have no delete.
- Progress: mark lesson complete → course progress % → certificate PDF (QuestPDF + QR) when 100%.

## Authentication / Authorization
- JWT: issuer `EduCoreApi`, audience `EduCoreApiUsers`, HS256, secret from env `JWT_SECRET_KEY` (.env).
- Roles in JWT claims (all user roles). Role checks via `[Authorize(Roles=...)]`.
- Custom policies (resource-based, in `EduCoreAPI/Authorization/`):
  - `UserOwnerOrAdmin` (resource: int userId) — orders, user profile
  - `UserOwnerOnly` (resource: int userId) — profile/password update
  - `InstructorOwnership` (resource: `ProductAccessResource`) — course/lesson/bundle mutation
  - `IsUserEnrolledOrAdmin` (resource: int productId) — lesson content access
- Rate limiting: `AuthLimiter` (5 req/min/IP) on login + refresh only.

## Conventions
- `cls` prefix + static members for business/data classes; `Dto*` for transfer objects; `en*` for enums.
- Validation is imperative in business layer `Set*` methods (`clsValidation` utils); request records have NO DataAnnotations.
- Audit everything: `clsAudit.LogAsync(userId, actionType, entityType, entityId, description, ip, userAgent[, conn, tx])`.
- Exceptions as control flow: throw `NotFoundException`/`ConflictException`/`ValidationException`; middleware converts.
- Pagination: `PageRequest(PageNumber, PageSize)` + `clsApiValidators.ValidatePaging`; SQL Server OFFSET/FETCH.
- Typos are canonical in code (`Respone`, `Assigan`, `Complated`) — do not rename casually (API surface).

## Important Dependencies
BCrypt.Net-Next 4.1.0, DotNetEnv 3.2.0, Microsoft.Data.SqlClient 7.0.1, QuestPDF 2026.6.0,
QRCoder 1.8.0, ZXing.Net 0.16.11, Swashbuckle.AspNetCore 10.1.7, Microsoft.AspNetCore.Authentication.JwtBearer 10.0.8.

## Database Access
- Connection string: env var `DB_CONNECTION` (loaded from `.env` via DotNetEnv) — see `clsDataAccessSettings`.
- Stored procedures: `SP_AddNewItemToOrder`, `SP_RemoveItemFromOrder`, `SP_CreateNewPayment`, `SP_CreateEnrollmentsFromPaidOrder`.
- Views: `vwLessonsWithOutCourses`, `vwLessonsWithCourses`.

## Known Technical Debt / Issues (see docs/agent/ENGINEERING_TODO.md for full list)
- No DI for services → untestable; orphaned `Dtos/` project; folder≠namespace mismatch.
- Mixed GETDATE()/SYSUTCDATETIME(); `Products.CreatedAt` is DATE (no time).
- Pagination has no total counts; `PageSize` was uncapped (now capped).
- Certificate: no persistence table, no verification endpoint, writes PDFs to disk per request.
- Large commented-out code blocks (old clsPayment copy, controller endpoints).

## Things Future Agents MUST NOT Break
- JWT issuer/audience/secret contract (`EduCoreApi` / `EduCoreApiUsers` / env `JWT_SECRET_KEY`).
- DB column/table names (code uses raw SQL everywhere; renames = runtime failures).
- SP names and parameter names (`SP_*` called by name from ADO.NET).
- Role names in `Roles` table must match `enRoles` and JWT role claims exactly.
- `Payments.IdempotencyKey` unique index and `FinalPrice` computed column.
- Filtered unique index `UX_Orders_User_Pending` (one pending order per user).
- BCrypt hashing of passwords AND refresh tokens (do not switch algorithms without migration).
- Public anonymous endpoints: catalog GETs (courses/lessons/bundles lists), register, login, email-exists.

## Build & Run
- `dotnet build EduCore.slnx` (verified: builds with warnings, 0 errors as of 2026-08-22).
- Requires SQL Server with `EduCore.sql` applied and `.env` in `EduCoreAPI/` (see `.env.example`).
- No tests exist yet (see docs/testing/strategy.md).
