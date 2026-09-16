# EduCore — Frontend Context (Agent Context File)

> Primary context file for agents working on the **EduCore frontend**.
> Design direction: `/UiUxDesign.md` (READ IT before changing visual work).
> Backend context: `/docs/agent/PROJECT_CONTEXT.md`.
> Last updated: 2026-08-25 (milestone 1 — scaffold + design DNA + landing).

## Purpose
Build the web frontend for the EduCore LMS: public landing/marketing site, student
learning app (browse → cart → checkout → learn → progress → certificates), and admin
app. The backend is an existing .NET 10 API in this same repo — **never modify backend
code from frontend tasks** unless a task explicitly says so.

## Technology Stack (locked decisions)
| Concern | Choice | Notes |
|---|---|---|
| Framework | Next.js (App Router) + TypeScript | lives in `frontend/` |
| Styling | Tailwind CSS + hand-written design tokens | tokens are CSS custom properties mapped to Tailwind theme |
| UI primitives | shadcn/ui | copied INTO repo (`src/components/ui/`) — fully ours to restyle; never fight it with overrides |
| Motion | `motion` (Framer Motion successor) | landing only; student/admin apps stay restrained per UiUxDesign §12 |
| Themes | `next-themes` | class-based light/dark toggle |
| HTTP | **native `fetch` ONLY** — via `src/lib/api.ts` wrapper | **HARD RULE: never install or use axios** (owner decision) |
| State | React Context + hooks | no Redux/Zustand unless a future task says so |
| Payments | Stripe Checkout hosted page = plain redirect | no Stripe.js SDK on frontend |

### Hard rules
1. **Native fetch, never axios** — all HTTP goes through `src/lib/api.ts`.
2. **Design DNA first** — no page-specific random colors/spacings/durations; use tokens.
3. **Performance is a design requirement** (UiUxDesign §32): animate only
   `transform`/`opacity`, throttle pointer handlers with rAF, lazy-load below-fold media,
   respect `prefers-reduced-motion`.
4. Landing may be spectacular; student/admin must be calm and fast (UiUxDesign §2, §12).
5. Big banner comments mark every payment-gateway touch point:
   `// ====== PAYMENT GATEWAY: STRIPE — CHANGE HERE ======`

## Folder Map (current)
```
frontend/
├── next.config.ts          # dev proxy: /api/* → http://localhost:5087/api/* (no CORS changes needed)
├── .env.example            # NEXT_PUBLIC_API_BASE_URL (empty = use proxy), SERVER_API_BASE_URL
├── src/
│   ├── app/                # routes: / (landing), /login, /register, /courses, /courses/[id],
│   │                       #         /cart  — /learn/* (student) + /admin/* later milestones
│   ├── components/
│   │   ├── ui/             # shadcn primitives (button, input, card, badge, skeleton, label,
│   │   │                   #            dropdown-menu…)
│   │   ├── landing/        # hero + landing sections (signature motion lives here)
│   │   └── shared/         # logo, theme-toggle/toggle-provider, container, site-header/footer,
│   │                       #            user-menu, course-card, add-to-cart-button
│   ├── lib/
│   │   ├── api.ts          # native-fetch client: base URL, JWT attach, 401→refresh once, ApiError
│   │   ├── auth-context.tsx# AuthProvider/useAuth (login/register/logout, profile fetch)
│   │   ├── cart-context.tsx# CartProvider/useCart (pending order, add/remove/count badge)
│   │   ├── jwt.ts          # client-side JWT claims decode
│   │   ├── courses.ts      # catalog/detail/lessons fetchers + types (server & client variants)
│   │   ├── orders.ts       # cart/order fetchers + types
│   │   ├── payments.ts     # ⚠ payment gateway stubs w/ STRIPE CHANGE HERE banners
│   │   └── motion.ts       # motion tokens (JS twins of CSS easings)
│   └── styles/globals.css  # DESIGN TOKENS ("Ink & Amber"): colors light/dark, type scale,
│                           # radius, elevation, motion easings as Tailwind utilities
```

## Backend API ↔ Frontend map (what we build against)
Base URL (dev): same-origin `/api` through Next rewrite → .NET Kestrel `https://localhost:7009`.
Full inventory: `docs/api/endpoint-inventory.md`.

| Area | Endpoints | Frontend usage |
|---|---|---|
| Auth | `POST /api/auth/login`, `/refresh`, `/logout`; `POST /api/users/students` (register) | login/register pages, auth context |
| Catalog (anon) | `GET /api/courses`, `/api/courses/{id}`, `/api/lessons`, `/api/bundles` (+`/{id}/items`) | landing featured courses, catalog pages |
| Cart/Orders | `POST /api/orders`, `GET /api/orders/cart/{userId}`, `POST/DELETE …/items/…` | cart page + header badge |
| Payments | `POST /api/payments` (returns Id/FinalPrice/IdempotencyKey) then gateway step | `src/lib/payments.ts` — see Payment flow below |
| Progress | `GET/PUT /api/progress/lessons/{id}…`, certificates PDF | student lesson player (later milestone) |

Auth contract: JWT access ~30 min + rotating refresh (~3 days). Tokens kept client-side;
`api.ts` retries once via `/refresh` on 401. Backend errors are RFC7807
(`application/problem+json`) — wrapper converts them into thrown `ApiError { status, title, detail }`.

## Cart & checkout (LIVE since M3.5)
- Cart = the user's pending Order. `POST /api/orders` creates-or-returns it; items are
  **Products.Id**s (`POST /api/orders/{id}/items/{productId}`). Courses expose `productId`
  (owner-approved backend addition, M3).
- Checkout: `initPayment` → `startCheckoutSession` → browser redirected to Stripe hosted
  page → returns to `/cart?checkout=success|cancelled&paymentId=N` → poll status.
- SOURCE OF TRUTH = backend webhook (`StripeWebhookController`), which runs the
  transactional completion chain (enrollments). The browser can never declare success.

## Payment gateway map (STRIPE — CHANGE HERE touch points)
- `frontend/src/lib/payments.ts` — the ONLY frontend file that knows about payments.
- `EduCore_BusinessLayer/clsStripeGateway.cs` — the ONLY backend file talking to Stripe REST.
- `EduCoreAPI/Controllers/StripeWebhookController.cs` — webhook application logic.
- `EduCoreAPI/.env`: STRIPE_SECRET_KEY, STRIPE_WEBHOOK_SECRET, FRONTEND_URL.
- Local testing: `stripe listen --forward-to localhost:5087/api/webhooks/stripe`
  then pay with card 4242 4242 4242 4242 on the hosted page.

## Payment flow (current state: STUBS)
Backend today has NO Stripe endpoints yet. Current self-service success endpoint exists but is
flagged Critical in security audit — do NOT rely on it in new UI beyond the stub.

Target flow (what stubs document):
1. `POST /api/payments` (order id, optional discount code, idempotency key) → payment Id + FinalPrice
2. **STRIPE CHANGE HERE:** call future `POST /api/payments/{id}/stripe-checkout-session`
3. **STRIPE CHANGE HERE:** `window.location.href = sessionUrl` → Stripe hosted page
4. Success URL back into app → poll `GET /api/payments/{id}` until Succeeded
5. Webhook (backend, later milestone) is the real source of truth

All of steps 2–5 live in ONE file: `src/lib/payments.ts`. Switching provider = edit that file only.

## Conventions
- Components: PascalCase files, one default export, props typed with `type X = {...}`.
- Client vs server components: landing hero/motion parts are `"use client"`; static sections stay server components for performance.
- shadcn components are restyled via token classes — do not hardcode hex values anywhere.
- Motion variants import duration/easing from `lib/motion.ts` (single source of truth).
- Commit style: short imperative subject.

## Environment
- Dev: Node ≥ 20, npm. `.env.local` (gitignored) ← copy of `.env.example`.
- `NEXT_PUBLIC_API_BASE_URL` empty ⇒ requests go to same-origin `/api/*` and Next rewrites
  proxy them to the .NET API (dev-friendly, CORS-free).
- Production: set full origin (e.g. `https://api.educore.example`) and configure backend CORS.

## Things Future Agents MUST NOT Break
- `src/lib/api.ts` refresh-once-on-401 semantics (avoid infinite refresh loops).
- Token names in `globals.css` (components reference them by name).
- The "fetch only" rule.
- UiUxDesign.md prohibitions: generic AI-gradient look, glassmorphism-everywhere, over-animation.
- Backend contract quirks documented in `docs/agent/PROJECT_CONTEXT.md`
  (canonical typos like `Respone`, role names, paging shape without totals).

## Build & Run (quick)
```bash
cd frontend
npm install
copy .env.example .env.local   # Windows (or cp)
npm run dev                    # http://localhost:3000
```
Backend must run separately (see root README.md) for live data.
