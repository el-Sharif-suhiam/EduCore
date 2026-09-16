# EduCore — Frontend Work Log

> Chronological log of frontend work sessions. Newest entries at top.
> One entry per session: date, goal, what was done, decisions, next steps.

---

## 2026-09-17 — Session 13: standalone-lessons admin console

**Goal:** complete the "lessons and everything about them" half of the directive — the one
missing surface was management of standalone (independent) lessons.

**Backend (additive):** `GET /api/lessons` gained the same `includeUnpublished` pattern as courses
— public callers keep a published-only feed; authenticated Admin/SuperAdmin passing
`?includeUnpublished=true` see drafts too. DAL/BL pass-throughs (query predicate
`@IncludeUnpublished = 1 OR v.IsPublished = 1`), no new endpoints needed. 0 build errors.

**Frontend:**
- `lib/admin.ts`: `AdminLessonSummary`, `getLessonsPaged` (sends includeUnpublished=true),
  `LessonInput`, `createStandaloneLesson` / `updateStandaloneLesson` / `publishLesson` /
  `unpublishLesson` / `deleteLesson` / `restoreLesson` fetchers.
- NEW `/admin/lessons` console (Admin/SuperAdmin only, mirrors the page-level role gate): debounced
  search + load-more paging; title links out to the public `/lessons/{id}` page; price in mono;
  Published/Draft badge; Publish/Hide quick toggle; Edit modal (lazy detail fetch via
  `GET /api/lessons/{id}`, full form incl. video/body/thumbnail); New-lesson modal; delete with a
  confirm modal that is honest about soft-delete semantics.
- admin-shell: "Lessons" nav entry + command-palette action (PlayCircle icon, Admin/SuperAdmin).
- React 19 compliance: edit-modal loading state derived at mount (keyed remount per id), no sync
  setState in effects.

**Tests executed:** `dotnet build` 0 errors; `npm run lint` 0; `npm run build` passes — 28 routes
(incl. /admin/lessons).

**Unresolved:** deleted lessons leave the admin feed (SQL view keeps IsDeleted=0 and there is no
deleted-list endpoint yet), so the restore action stays API-only for now — documented.

---

## 2026-09-16 — Session 12: courses+lessons close-out with a performance pass

**Goal:** Owner directive — finish courses and lessons "and everything about them", professional
UX, and no bandwidth waste: fewer requests, lazy loading, nothing fetched that isn't seen.

**Modifications (backend — additive/minimal):**
- Public course feed is now **published-only**; `GET /api/courses` (anon) hides drafts while
  Admin/SuperAdmin may pass `?includeUnpublished=true` (the admin console now sends it).
  DAL `GetAllCoursesWithInstructorViewModelInternal` gained an `@IncludeUnpublished` predicate.
- `GET /api/courses/{id}` now returns 404 for drafts unless the caller is Admin/SuperAdmin or the
  owner instructor (`IsInstructorOwnProduct`). Draft URLs no longer leak to the public.
- NEW `GET /api/lessons/{id}/info` (anon): sanitized `LessonPublicInfoViewModel` (no video/body —
  content stays enrolled/owner/admin-gated) served by a tiny view query.

**Modifications (frontend — performance & UX):**
- NEW `lib/use-in-view.ts` (IntersectionObserver hook): Bundles + Standalone-lessons sections no
  longer fire on page load — they fetch only when scrolled near the viewport (480px rootMargin),
  with stable-height skeletons. Empty/unreachable sections vanish quietly.
- NEW `lib/courses.ts` session cache (`cached()`): any identical catalog GET within 60s shares one
  promise — collapses StrictMode double-fetches, re-mounts, and palette/re-open duplicates. Failed
  responses are evicted, never cached.
- NEW `/lessons/[id]` public detail page (RSC): metadata, hero, instructor, purchase rail
  (`AddToCartButton` via ProductId) + "unlocks after purchase" note; `notFound()` on bad ids.
  LessonCard titles now link here.
- admin-shell: CommandPalette split into a `next/dynamic` chunk (ssr:false) — /admin first paint no
  longer carries its JS.

**Tests executed:** `dotnet build` 0 errors; `npm run lint` 0; `npm run build` passes (27 routes,
incl. /lessons/[id]).

**Unresolved:** none for this scope. Storefront image handling still plain `<img>` + native lazy
(next/image needs remotePatterns for user-defined URLs). Batch pushed to origin/main.

---

**Goal:** Owner confirmed the earlier gap report — `GET /api/lessons` already listed standalone
lessons, but (a) the list hid `ProductId` so they could never be added to a cart, and (b) the
frontend had no surface for them. Fix both, guided: "add ProductId from DAL to endpoint, then add a UI".

**Modifications (backend — additive):**
- `EduCore.sql`: `vwLessonsWithOutCourses` now exposes `P.Id AS ProductId`.
- `LessonsWithOutCoursesViewModel` + `ProductId`.
- `clsLessonsData.GetAllLessonsWithOutCourses`: replaced `SELECT * FROM view` with an explicit
  column list (idempotent to view redefinition), reads ProductId, filters **published only**
  (`v.IsPublished = 1` — public feed shouldn't advertise drafts like the course list does), newest
  first (`CreatedAt DESC`). BL + endpoint unchanged (pass-through).

**Modifications (frontend):**
- lib/courses.ts: `LessonSummary` type + client `getLessons(page, pageSize)` (public endpoint).
- NEW `components/shared/lesson-card.tsx` (thumbnail/placeholder, "Standalone lesson" chip,
  title, summary, instructor, price rail via `AddToCartButton` using ProductId).
- /courses: `LessonsSection` beneath Bundles — first 9 + Load more, hidden when the feed is
  empty or unreachable (same quiet-chrome rule as bundles).

**Tests executed:** `npm run lint` 0; `npm run build` passes; `dotnet build` 0 errors.

**Unresolved:** No lesson *detail* page — lesson body/video stays gated behind
`GET /api/lessons/{id}` (enrolled/owner/admin only), and enrolled playback already lives at
/learn/lessons/[id]. Purchase funnel: catalog card → cart → paid → appears in /learn.

---

**Goal:** Close the remaining admin gaps from Session 9's review (orders/payments dashboards,
course publish state in admin list, user re-activation). Certificates deferred by owner
("اترك موضوع الشهادات لاحقا").

**Modifications (backend — additive only):**
- `CourseWithInstructorViewModel` + `IsPublished`; course list SELECT/reader/mapping updated.
- Users: `clsUsersData.ActivateUser(id)` + `clsUser.ReactivateUser(id, actionByUserId)` with
  `enAuditActionType.UserReactivated` (appended as 31 — no renumber). UsersController gains
  `includeInactive` (bool?, default false) on students/admins/instructors feeds and
  `POST /api/users/{id}/activate` (Admin,SuperAdmin).
- NEW `Common/ViewModels/CommerceAdminViewModels.cs` (PaymentAdminViewModel, OrderAdminViewModel).
- NEW paged feeds backed by joins (newest first, LIKE search on buyer name/email):
  `clsOrderData.GetAllOrdersView`, `clsPaymentData.GetAllPaymentsView` + BL pass-throughs
  (clsOrder/clsPayment) + `GET /api/orders` and `GET /api/payments` (Admin,SuperAdmin, PageRequest).

**Modifications (frontend):**
- CourseSummary + `isPublished`; /admin/courses has Draft/Published badge + quick Publish/Hide
  toggle (toast feedback); keep Manage → builder.
- lib/admin: `getUsers(..., includeInactive)`, `activateUser(id)`; /admin/people shows
  deactivated accounts with a Reactivate button; deactivate copy updated.
- NEW /admin/orders + /admin/payments (paged tables + detail modal + debounced search),
  status label maps; admin-shell nav entries (Orders, Payments) for Admin/SuperAdmin.
- AUDIT_ACTION_LABELS + 31 "Reactivated user".

**Tests executed:** `dotnet build` 0 errors (0 warnings after repass); `npm run lint` 0;
`npm run build` passes — 26 routes (incl. /admin/orders, /admin/payments).

**Unresolved:** None blocking. IntegrationTests project has no discoverable tests (needs live DB).

---

**Goal:** Close the remaining frontend work from the approved P1/P2/P3 plan: give admin real
bundle management (the biggest known backend gap — no unpublished visibility), add the public
bundle detail page, and land the polish list.

**P2 — Bundles:**
- Backend (minimal additive): `BundleViewModel.IsPublished` + `GetAllBundlesView(includeUnpublished)`
  + `GET api/bundles/all` [Instructor,SuperAdmin]. Nothing pre-existing changed behavior.
- lib/admin.ts bundle fetchers; `/admin/bundles` list + NewBundleModal (create → router.push
  into builder); `/admin/bundles/[id]` builder with publish toggle, identity edit, contents
  editor (course pool via getCoursesPaged, add/remove with confirms, toast everywhere).
- Public `/bundles/[id]` as an RSC: cover, badges, contents list deep-linking to `/courses/[id]`,
  sticky price rail with AddToCartButton; not-found + graceful contents-unavailable states.
- BundleCard title now links to the detail page; the lazy "What's inside" modal stays.

**P1 — Product fixes:**
- Global not-found/error/loading boundaries (error page uses Link, not `<a>` — lint enforced).
- Landing "View all" `#courses` → `/courses`.
- `/register` honors `?next=` (Suspense + useSearchParams restructure, mirrors login).
- `/admin/people` rewritten: load-more (20/page, dedupe by id), 300ms debounced server-side
  search, server-side tab filtering, ARIA tablist w/ roving tabindex + arrow keys.
- `/learn` load-more enrollments (24/page) + progress fetched for every course (no 8-cap).

**P3 — Infra & a11y:**
- ui/textarea + ui/select (zero-dep), now used by the course builder.
- ui/toast + ToastProvider in root layout; feedback wired into bundle builder, course builder
  (publish/save/instructors), discounts, people (with per-action success labels).
- Modal focus trap + restore-focus-to-trigger (Esc/overlay close kept).
- admin-shell: Bundles nav entry; palette actions moved to router.push.
- .env.example gains NEXT_PUBLIC_SITE_URL.

**Verification:** dotnet build 0 errors (138 pre-existing warnings) · lint 0 · `npm run build`
passes with 24 routes.

**Notes:** learn page initial fetch moved to a `.then` pattern to satisfy the strict
`react-hooks/set-state-in-effect` rule (same shape as the progress fetcher).

---

**Goal:** Close every remaining frontend gap so the product is testable end-to-end.

**Profile & settings:**
- lib/profile.ts + /profile page (details + password change).
- Gotcha honored: refresh-token flow is keyed to the STORED email — changing email
  signs the user out for a clean re-login instead of silently breaking refresh.
- UserMenu "Profile" enabled (was a disabled placeholder since M2).

**Certificates (honest M14 handling):**
- Backend IssueCertificate returns bare Ok() — no PDF exists. So the certificate is
  RENDERED in-app from real progress data at /learn/certificate/[courseId] with
  browser print-to-PDF; issuance endpoint still called (audit trail). Entry points:
  completed library cards on /learn. Gated on 100% progress with a kind not-yet state.

**Bundles:**
- Public getBundles/getBundleItems fetchers; BundleCard ("See what's inside" lazy
  modal, AddToCartButton via Products.Id); bundles section under the course grid.

**M6 polish:**
- AppHeader gained the missing mobile nav panel (hamburger, aria-expanded,
  auth-filtered links) matching SiteHeader behavior.

**Incident note:** PowerShell Add-Content wrote non-UTF8 bytes into three files
(lib/learning.ts, lib/courses.ts, courses/page.tsx) → "failed to convert rope into
string" build failures. Repaired by rewriting affected regions/files via file tools;
all three verified as strict-UTF8 before rebuild. LESSON: never append source files
via shell here-strings on this machine.

**Verification:** strict UTF-8 check on repaired files · lint 0 · build passes ·
20 routes (added /profile, /learn/certificate/[courseId]).

**Test script for owner:** register → browse catalog/bundles → add to cart → Stripe
test card → /learn dashboard → complete lessons → view/print certificate → profile
edits + password change → admin console (seed instructors: any @educore.demo demo
account, password Demo1234!) → Ctrl+K palette → audit/logs feeds live.

---

## 2026-08-26 — Session 7: milestone 5 (admin console) + study-notes design language

**Goal:** Finish the remaining frontend — the M5 admin/instructor console — plus the
"Study Notes" visual identity pass (landing/auth/shared) and the Gemini image prompt pack.

**Design language (session 7a):**
- New signature devices: `Mark` (amber highlighter stroke) + `MarginNote` (Caveat
  handwritten annotations, new next/font). CSS utilities in globals.css:
  `paper-grain` (feTurbulence data-URI), `ruled-paper`, `index-card`, `sticky-note`.
- Hero rework: node-graph SVG replaced by `learning-path.tsx` — hand-drawn trail that
  draws itself on mount through 5 milestone doodles with handwritten labels. Mouse
  parallax + scroll choreography preserved; reduced-motion = static drawn path.
- Landing touches: index cards, handwritten step numbers, dashed journey rail,
  rotated certificate stamp (SVG textPath), highlighter nav hover, ruled footer strip.
- Auth pages: split-screen `AuthBrandPanel` (ruled paper, quote, static trail).
- Assets: custom `app/icon.svg` favicon (default favicon.ico removed),
  `app/opengraph-image.tsx` via ImageResponse (Satori quirks: no z-index, no inline
  svg → data-URI img), `metadataBase` added to layout metadata.
- `docs/design/gemini-prompts.md`: on-brand prompt pack (6 covers + auth art +
  texture) w/ palette hexes and SQL to attach covers via Products.ThumbnailUrl.

**Admin console (session 7b):**
- Contracts extracted from controllers (endpoint-inventory cross-checked):
  paged lists are plain arrays (no totals), enums arrive as NUMBERS (no
  JsonStringEnumConverter → label maps in lib/admin.ts), GET /api/courses has no
  isPublished flag (publish state only from detail endpoint), bundle item add takes
  a RAW int body, discount CreatedById comes from JWT.
- lib/admin.ts typed fetchers for users/courses/lessons/course-instructors/
  discounts/audit/logs.
- Shell: sidebar w/ one sliding active pill (motion layoutId), mobile drawer,
  sticky topbar, hand-rolled Ctrl/Cmd+K command palette (no cmdk dep — hard rule
  keeps deps minimal; navigation actions).
- Pages: dashboard (honest quick links + live audit feed — NO fake stat numbers
  since backend exposes no aggregates), courses table (search/load-more/new-draft),
  course builder (identity form + publish toggle + curriculum CRUD + instructor
  assignment for Admin+; lesson edit loads full detail via GET /api/lessons/{id}
  because the list omits body/video), people tabs (deactivate/promote/demote with
  confirm modals — deactivation has NO reverse endpoint), discounts (SuperAdmin,
  create/edit/delete), audit + logs (paged, row-detail modals).
- UserMenu gains "Console" entry for Admin/SuperAdmin/Instructor.

**React 19 lint compliance:** react-hooks/set-state-in-effect hit 6 spots → fixed by
async-only effect bodies, derived invalid-id render branch, handler-based search/page
reset, drawer close via onClick (not effect), palette reset via keyed remount.

**Deferred (documented gaps, need backend first):**
- Orders/payments tables — no list endpoints exist (only by-id/cart).
- Bundles management — no admin list incl. unpublished; publish state invisible.
- Course list lacks isPublished (status shows only inside builder).

**Verification:** lint 0 problems · production build passes · all 8 admin routes compile
(16 routes total).

---

## 2026-08-25 — Session 6: milestone 4 (student learning app)

**Goal:** The learner side: dashboard ("Continue Learning") + lesson player with Focus Mode.

**Backend (L9):**
- EnrollmentViewModel adds ProductName/ProductTypeId/Summary/ThumbnailUrl AND CourseId/LessonId
  deep-link ids (LEFT JOIN Courses/Lessons on ProductId) — required because progress endpoints
  take Courses.Id while enrollments only carry Products.Id.
- New EnrollmentsController GET api/enrollments/my ([Authorize], current user only).
- Gotcha hit: PageRequest lives in namespace EduCoreAPI.Helpers.Dtos.RequestDto and
  clsApiValidators in EduCoreAPI.Helpers (folder≠namespace rule from PROJECT_CONTEXT).

**Frontend:**
- lib/learning.ts typed layer. AppHeader shell added to /courses + /cart (previously these
  pages had NO navigation — coherence fix per UiUxDesign §17).
- /learn: Continue-Learning hero picks highest-% incomplete course, Resume → first
  incomplete lesson; library grid w/ progress bars & Completed badge; progress fetched
  lazily (≤8 courses); empty/error states; auth gate ?next=/learn.
- Lesson player: optimistic complete/undo w/ rollback, prev/next (router.push — loader is
  keyed by lessonId so remount-data works), curriculum rail w/ completion checks,
  YouTube/Vimeo/native-video embed helper (no SDKs), Focus Mode (Esc exits),
  locked-lesson friendly state.

**Verification:** backend build 0 errors · lint 0 · build passes · smoke test all routes 200.

**Next candidates:** bundles deep-linking (needs bundle→items page or endpoint clarity),
certificates UI (PDF endpoint exists), admin app M5, mobile UX pass.

---

## 2026-08-25 — Session 5: milestone 3.5 (real Stripe, end-to-end)

**Goal:** Replace the payment stubs with the real Stripe hosted-checkout loop.

**Backend (raw REST — no NuGet SDK, owner decision):**
- NEW `clsStripeGateway.cs`: checkout-session creation (form-encoded REST,
  Idempotency-Key derived from the payment's unique key → retries reuse the same
  session) + manual webhook HMAC-SHA256 verification (constant-time compare,
  ±300 s timestamp tolerance). Banner comments mark it as THE provider file.
- `PaymentsController` + POST {id}/stripe-checkout-session (ownership-checked,
  pending-only; success/cancel URLs composed from FRONTEND_URL env).
- NEW `StripeWebhookController` [AllowAnonymous]: signature-verified; completed/
  async_payment_succeeded → clsCheckoutService.CompletePaymentAsync(order.UserId,…);
  expired/async_payment_failed handled; replays no-op. Payment id arrives via
  client_reference_id/metadata set server-side — never from the browser. This
  closes audit finding H1; checkOut-succeed now marked DEPRECATED in code.
- `.env.example`: STRIPE_SECRET_KEY / STRIPE_WEBHOOK_SECRET / FRONTEND_URL.

**Frontend:**
- `payments.ts`: real startCheckoutSession (window.location.href = hosted URL);
  initPayment sends paymentMethod:"Stripe"; banners updated to "live".
- `/cart`: Suspense-wrapped useSearchParams return-trip handler — success polls
  waitForPaymentSuccess then refreshes cart; cancelled/failed/expired/timeout
  banners; URL cleaned after read.

**Verification:** backend `dotnet build` 0 errors · frontend lint 0 · build passes ·
routes smoke-tested. Live-loop testing requires real sk_test_/whsec_ keys +
Stripe CLI (documented in README §5) — NOT executed this session (no keys on machine).

**Next:** M4 student learning app (dashboard "Continue Learning", lesson player),
and/or M-enrollments backend endpoint (L9) which My-Learning depends on.

---

## 2026-08-25 — Session 4: milestone 3 (cart + checkout attempt)

**Goal:** Full cart experience against existing endpoints; checkout attempt honoring
payments-stub policy.

**Owner decision this session:**
- Course detail/catalog could not add-to-cart: cart API requires **Products.Id** but course
  responses only exposed Courses.Id. Owner approved the tiny backend addition:
  `ProductId` added to `CourseWithInstructorViewModel`, `clsCoursesData` SQL+reader,
  `CourseResponse`, `CourseMapper`. No DB/schema changes; solution builds 0 errors.

**Done:**
- lib/orders.ts typed fetchers; courses.ts types carry productId.
- CartProvider (load-on-auth, optimistic remove + rollback, render-time clear on sign-out,
  compiler-friendly memo deps). Mounted in root layout inside AuthProvider.
- Header: live cart icon + count badge; sign-in prompt variant when logged out.
- AddToCartButton component on course detail (idle/adding/added/error).
- /cart page: auth gate with ?next=/cart, items w/ remove, totals, discount code field,
  checkout → real POST /api/payments then stub gateway catch → info panel
  ("payment record #N created; gateway not connected yet").

**Verification:** lint 0 · build passes (/cart route added) · smoke test all pages 200.

**Next:** M4 student learning app (dashboard/lesson player) and/or backend Stripe endpoint
decision to complete the purchase loop end-to-end.

---

## 2026-08-25 — Session 3: milestone 2 (auth + catalog + course detail)

**Goal:** Real authentication end-to-end and the course catalog.

**Contracts confirmed from backend source (not assumed):**
- `POST /api/auth/login {email,password}` → `{accessToken, refreshToken}` (JWT 30 min; roles array claim).
- `POST /api/auth/refresh {email, refreshToken}` → rotated tokens. `POST /api/auth/logout` revokes.
- `POST /api/users/students {name,email,birthDate,password}` → 201 `{id,name,email,birthDate}`.
- `GET /api/users/{id}` → UserResponse incl. display name (used for header avatar/name).
- `GET /api/courses/{id}` → CourseResponse (`name`, not `title`). `GET /api/courses/{id}/lessons` →
  LessonsByCourseViewModel[] (has `instructorName`).

**Done:**
- jwt.ts decoder; AuthProvider (restore/silent-refresh/profile/login/register/logout) mounted in root layout.
- Real login & register pages replacing placeholders; ApiError messages surfaced inline.
- Header: shadcn dropdown-menu based UserMenu; auth-state-aware buttons; mobile menu adapts.
- /courses catalog: debounced search + Load more (API has no totals), skeletons, empty/error states,
  stale-response guard via requestId ref.
- /courses/[id] RSC detail page: curriculum with lock icons, sticky price rail with disabled
  "Add to cart — coming soon" + M3 marker comment; notFound() on 404/unreachable.
- CourseCard now links to detail (stretched link).

**React 19 lint learnings (apply in future sessions):**
- `react-hooks/set-state-in-effect` forbids synchronous setState in effects — even traced through
  called async functions. Patterns used: keyed-child remount for search resets; all setState after
  awaits; one justified eslint-disable where the rule false-positives on fetch-on-mount.

**Verification:** lint 0 problems · build passes (6 routes) · smoke test:
/ 200, /courses 200, /login 200, /register 200, /courses/123 → 404 (graceful while API offline).

**Next:** M3 cart/checkout (needs owner decision on backend Stripe endpoint timing).

---

## 2026-08-25 — Session 2: milestone 1 executed (scaffold + design DNA + landing)

**Goal:** Execute the approved milestone-1 plan end to end.

**Environment discovered:**
- Node 24.19 / npm 11.17 / .NET 10 SDK.
- create-next-app installed **Next.js 16.3.3 (Turbopack default)** + React 19.2 +
  Tailwind v4 — newer than typical training data; consulted bundled docs at
  `frontend/node_modules/next/dist/docs/` per AGENTS.md before coding
  (rewrites unchanged; `"use client"` unchanged; Turbopack is the default build).
- shadcn CLI 4.x: new flags (`init -b radix -p nova`); components use `radix-ui`
  package; Tailwind v4 CSS-variable theming.

**Done:**
- Tracking docs created (CONTEXT/TODO/LOG) + root README rewritten for full stack.
- Scaffold in `frontend/`; deps: motion, next-themes, shadcn init + button/input/card/badge/skeleton.
- Dev proxy via `next.config.ts` rewrites `/api/:path*` → `http://localhost:5087` (CORS-free dev,
  HTTP port dodges self-signed TLS server-side). `.env.example` added; gitignore exception `!.env.example`.
- `src/lib/api.ts`: native fetch client (axios banned), JWT attach, one-shot refresh on 401,
  RFC7807 → ApiError. `src/lib/courses.ts`: server/client course fetchers typed from backend VMs.
- Design DNA "Ink & Amber" in globals.css (semantic tokens, light+dark, Fraunces display font,
  ease tokens as utilities). ThemeProvider + CSS-only ThemeToggle.
- Landing page composed: Hero (layered mouse parallax w/ springs + scroll choreography),
  WhatIs, Journey (scroll-drawn rail), FeaturedCourses (REAL GET /api/courses with graceful
  offline empty-state), HowItWorks, OutcomesCta, header/footer, placeholder login/register.
- `src/lib/payments.ts` payment stubs with big STRIPE CHANGE HERE banners documenting target
  flow and exact backend files to change later.

**Verification:**
- `npm run lint` → 0 problems (fixed unused-var + set-state-in-effect findings).
- `npm run build` → success (6 routes).
- Production-server smoke test → HTTP 200; hero + sections present;
  featured-courses fallback renders when API offline.

**Decisions:**
- Course covers rendered with plain `<img>` (user-defined external URLs; next/image remotePatterns
  unknowable ahead) with eslint-disable + rationale comment.
- FeaturedCourses = RSC fetching SERVER_API_BASE_URL directly (Node fetch needs absolute URL);
  browser traffic keeps using the same-origin proxy.
- ThemeToggle uses `.dark:` CSS visibility instead of mounted-state (React 19 lint rule).

**Next:** Milestone 2 — real auth pages + auth context + catalog/course pages (owner approval needed).

---

## 2026-08-25 — Session 1: kickoff + tracking docs

**Goal:** Start frontend per UiUxDesign.md. Milestone 1 = scaffold + Design DNA + landing page.
Payments = stubs only with STRIPE CHANGE HERE banners.

**Decisions made (with owner):**
- Stack: Next.js (App Router) + TypeScript + Tailwind CSS + Motion + shadcn/ui.
  Owner explicitly chose this over a leaner Vite+React option.
- Native fetch only — axios banned (owner rule).
- Frontend lives in `frontend/` inside this repo.
- Payments: frontend STUBS only in milestone 1 (no backend changes); real wiring later.
- Dev CORS avoided via Next.js rewrites proxy (`/api/*` → Kestrel), no backend edits.
- Landing featured courses consume real anonymous endpoint `GET /api/courses`.

**Done:**
- Created docs/frontend/FRONTEND_CONTEXT.md, FRONTEND_TODO.md, FRONTEND_LOG.md.
- Rewrote root README.md with full backend+frontend setup.

**Next:** scaffold Next.js app → done in Session 2.

---
