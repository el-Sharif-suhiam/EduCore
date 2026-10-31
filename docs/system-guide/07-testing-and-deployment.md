# Testing and Deployment

Navigation: [Overview](00-overview.md) · [Architecture](01-architecture.md) · [Database](02-database.md) · [Business Logic](03-business-logic.md) · [Workflows](04-workflows.md) · [API](05-api-reference.md) · [Security](06-security.md) · [Glossary](08-glossary.md)

## Build

`dotnet build EduCore.slnx`

## Tests

`dotnet test EduCore.IntegrationTests/EduCore.IntegrationTests.csproj`

Set `EDUCORE_REQUIRE_DB=1` in CI to fail if SQL unavailable. Tests use isolated `EduCoreTests` DB.

## Environment

- `DB_CONNECTION` (set by fixture)
- `JWT_SECRET_KEY`
- `STRIPE_WEBHOOK_SECRET`
- `EDUCORE_TEST_DB_CONNECTION` (optional)

## SQL Server

Local default instance `Server=.` with integrated security. CI provides service container.

## Deployment

Prerequisites: .NET 10 runtime, SQL Server, env vars. Apply `EduCore.sql` to production DB manually (not via tests).
```