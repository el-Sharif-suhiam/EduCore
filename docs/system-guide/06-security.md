# Security Model

Navigation: [Overview](00-overview.md) · [Architecture](01-architecture.md) · [Database](02-database.md) · [Business Logic](03-business-logic.md) · [Workflows](04-workflows.md) · [API](05-api-reference.md) · [Testing](07-testing-and-deployment.md) · [Glossary](08-glossary.md)

- Auth: JWT HS256, refresh rotation.
- Passwords: BCrypt work factor 12.
- Authorization: roles + resource-based handlers (ownership, enrollment, admin).
- SQL injection: parameterized queries/stored procs.
- Payments: Stripe webhook HMAC verification; payment_status must be "paid"; server-side payment id.
- Secrets: env vars only.
- Rate limiting on auth endpoints.
- Soft delete/deactivation preserves auditability.
```