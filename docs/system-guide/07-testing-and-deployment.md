# EduCore — Testing and Deployment

## 1. Prerequisites

- .NET 10 SDK.
- SQL Server (local or remote) with a login able to create databases.
- (Optional) Stripe account + CLI for webhook testing.

## 2. Build

```
dotnet build EduCore.slnx -c Release
```

## 3. Database Setup

Run `EduCore.sql` against your SQL Server instance to create the `EduCore`
database, schema, indexes, seed roles, views, and stored procedures. The script
is idempotent.

## 4. Configuration

Environment variables (from `.env.example` / configuration):

| Variable | Purpose |
|---|---|
| `DB_CONNECTION` | ADO.NET connection string to the application database. |
| `JWT_SECRET_KEY` | HMAC signing key for JWTs (required). |
| `JWT_ISSUER` | JWT issuer. |
| `JWT_AUDIENCE` | JWT audience. |
| `JWT_EXPIRES_MINUTES` | Access token lifetime. |
| `STRIPE_SECRET_KEY` | Stripe secret key. |
| `STRIPE_WEBHOOK_SECRET` | Stripe webhook signing secret. |
| `CERTIFICATES_STORAGE_PATH` | Directory for certificate PDFs. |
| `FRONTEND_URL` | Base URL for QR verification links. |
| `EDUCORE_TEST_DB_CONNECTION` | Connection string for the test database server (CI). |
| `EDUCORE_REQUIRE_DB` | When set, missing SQL Server fails the run. |

## 5. JWT Setup

Set `JWT_SECRET_KEY` to a long random string (>= 32 chars). Configure issuer/
audience consistently between API and clients.

## 6. Stripe Setup and Webhook Testing

- Set `STRIPE_SECRET_KEY` and `STRIPE_WEBHOOK_SECRET`.
- Use the Stripe CLI to forward webhooks:
  `stripe listen --forward-to http://localhost:5000/api/webhooks/stripe`.
- Trigger test events with `stripe trigger payment_intent.succeeded`.

## 7. Certificate Storage

Set `CERTIFICATES_STORAGE_PATH` to a writable directory. Defaults to
`./certificates`.

## 8. API Startup

```
cd EduCoreAPI
dotnet run
```

Swagger UI available at `/swagger` in Development.

## 9. Integration Tests

- Isolated database `EduCoreTests` is created/dropped by `DatabaseFixture`.
- The real `EduCore` database is never touched.
- Set `EDUCORE_TEST_DB_CONNECTION` for CI service containers.
- Set `EDUCORE_REQUIRE_DB=1` in CI so missing SQL Server fails the run.
- Run:
  ```
  dotnet test EduCore.IntegrationTests/EduCore.IntegrationTests.csproj -c Release
  ```

## 10. Test Categories

- Unit-ish: env config tests.
- SQL integration: order state, payment state, bundle publication, course-lesson
  consistency, progress, concurrency.
- Skipped tests are reported; `EDUCORE_REQUIRE_DB` ensures DB tests execute.

## 11. CI Workflow

`.github/workflows` defines the build + test pipeline. It runs the Release
build and the integration tests against a SQL Server service container with
`EDUCORE_REQUIRE_DB=1`.<|end|><|start|>write to=assistant<|channel|>analysis<|message|>Wrote file successfully: docs/system-guide/07-testing-and-deployment.md