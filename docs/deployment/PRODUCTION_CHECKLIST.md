# EduCore — Production Deployment Checklist

Everything below must be decided/provided by the **infra/secrets owner**
before pointing real traffic at the API. The application code is already
hardened (fail-fast config, forwarded headers, HSTS, env-driven CORS,
admin-only checkout hook, automated config tests). This checklist is the
remaining **operational** work.

---

## 1. Required secrets & config (backend)

Copy `EduCoreAPI/.env.example` → the production host's env (`.env` file
next to the published DLL, or platform env vars — the code reads
`Environment.GetEnvironmentVariable` after `DotNetEnv.Env.Load()`).

| Variable | Required? | Notes |
|---|---|---|
| `JWT_SECRET_KEY` | **yes** | ≥ 32 bytes (HS256). API **refuses to start** without it. Rotate before first prod deploy. |
| `DB_CONNECTION` | **yes** | SQL Server connection string. |
| `STRIPE_SECRET_KEY` | yes (for checkout) | `sk_live_…` in production. |
| `STRIPE_WEBHOOK_SECRET` | yes (for webhook) | `whsec_…`; see §4. |
| `FRONTEND_URL` | yes | e.g. `https://app.your-domain.com` — used for Stripe success/cancel redirects. |
| `CORS_ALLOWED_ORIGINS` | yes | Comma-separated real frontend origins. Defaults are localhost-only. |
| `JWT_ISSUER` / `JWT_AUDIENCE` | optional | Only override if tokens must interop with an external system. |

> Do **not** copy `EduCoreAPI/.env` (gitignored) into the host — generate
> fresh values.

## 2. Required config (frontend)

| Variable | Value |
|---|---|
| `NEXT_PUBLIC_API_BASE_URL` | Full API origin, e.g. `https://api.your-domain.com` (browser calls it directly; CORS must allow it). |
| `SERVER_API_BASE_URL` | Same API origin, reachable from the Next.js server. |
| `NEXT_PUBLIC_SITE_URL` | Canonical site URL (used for `metadataBase`). |
| `API_PROXY_TARGET` | Only if serving `/api/*` through the Next.js proxy in production — normally set it to the same API origin. |

Run production build with `npm run build` then `npm run start`.

## 3. Hosting / TLS / proxy

- TLS **must** terminate in front of Kestrel (nginx / IIS / caddy / LB). The
  API's `UseHttpsRedirection` + `UseHsts` then behave correctly, and the
  proxy must set `X-Forwarded-Proto: https` and `X-Forwarded-For`.
- `UseForwardedHeaders` is enabled so the **rate limiter keys on the real
  client IP** behind the proxy. Verify the proxy passes `X-Forwarded-For`.
- If the proxy is a multi-hop chain, restrict `ForwardedHeadersOptions.KnownNetworks/Proxies`
  (see `Program.cs`) — by default all proxies are trusted.
- Swagger is enabled **only** in `Development`. Ensure the host runs with
  `ASPNETCORE_ENVIRONMENT=Production`.
- QuestPDF: `LicenseType.Community` is set in code — community licence
  limits apply to commercial use; re-check for production scale.

## 4. Stripe webhook

- Create a live webhook endpoint → `https://api.your-domain.com/api/webhooks/stripe`.
- Subscribe to event `checkout.session.completed` (and optionally
  `checkout.session.expired`).
- Put the signing secret in `STRIPE_WEBHOOK_SECRET`. The webhook verifies
  the HMAC signature and rejects unsigned requests (returns 400) — no valid
  signature, no transaction.

## 5. Payment rails security

- `PUT /api/payments/{id}/checkOut-succeed` is **Admin/SuperAdmin only**
  (audit finding H1). Students can never self-declare success; the webhook
  is the source of truth. Keep it only if manual ops need it; otherwise
  remove the endpoint entirely.
- The frontend never calls this endpoint — safe to leave off.

## 6. Database

- Run `EduCore.sql` once (idempotent) against the **production** database.
- Use a dedicated SQL login (`DB_CONNECTION`), not `sa`.
- Back up before first deploy and on a schedule; the DB holds orders,
  payments and enrollment state.

## 7. Verification before launch

Run on the production build:

```powershell
# Backend config tests (fast, no DB)
dotnet test EduCore.IntegrationTests\EduCore.IntegrationTests.csproj

# Backend compile
dotnet build EduCoreAPI\EduCoreAPI.csproj

# Frontend lint + prod build
cd frontend; npm run lint; npm run build
```

Smoke test checklist (manual):

- [ ] Anonymous request to `GET /api/courses` works; to `/api/users` → 401.
- [ ] Login without settings `JWT_SECRET_KEY` → API fails to start (not silently).
- [ ] 6 rapid logins from one IP → HTTP 429 with `application/problem+json`.
- [ ] CORS: request from the production origin includes `Access-Control-Allow-Origin`;
      request from an unknown origin is blocked.
- [ ] Browsing site → `Strict-Transport-Security` header present (after first
      HTTPS request).
- [ ] Webhook with a forged signature → 400.
- [ ] One end-to-end purchase with Stripe test/live keys in the prod env.

## 8. Rotations & monitoring

- Rotate `JWT_SECRET_KEY` on a schedule (= all existing sessions invalid).
- Watch `/api/payments` payment states and the audit log for anomalies
  (self-serve checkout attempts should never appear in payment history as
  succeeded without a webhook).
- Unbounded growth: paging ends at `pageSize` cap of 100 (server-enforced).

---

## Leave the checklist blank/annotate as you complete each item
Annotate this file (dates, owners) during the actual deployment so the next
operator knows what was already verified.