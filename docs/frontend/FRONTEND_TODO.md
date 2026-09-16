# EduCore — Frontend TODO

> Live checklist. Status: `[ ]` pending · `[~]` in progress · `[x]` done · `[-]` cancelled.
> Update this file at the END of every work session (with FRONTEND_LOG.md).

## Milestone 1 — Scaffold + Design DNA + Landing (COMPLETE ✅)

### Phase 0 — Tracking docs
- [x] docs/frontend/FRONTEND_CONTEXT.md
- [x] docs/frontend/FRONTEND_TODO.md
- [x] docs/frontend/FRONTEND_LOG.md
- [x] Root README.md rewrite (backend + frontend run guide)

### Phase 1 — Scaffold
- [x] create-next-app → `frontend/` (TypeScript, Tailwind v4, App Router, ESLint, src dir)
- [x] Install deps: `motion`, `next-themes`; init shadcn/ui (Radix base, Nova preset)
- [x] shadcn components: button, input, card, badge, skeleton
- [x] `next.config.ts`: `/api/:path*` rewrite → `http://localhost:5087/api/:path*`
      (HTTP port avoids self-signed-TLS failures server-side; override via API_PROXY_TARGET)
- [x] `.env.example` + `.gitignore` exception for it (`!.env.example`)
- [x] `src/lib/api.ts` native-fetch client (JWT attach, 401→refresh once, ApiError from problem+json)
- [x] Clean scaffold boilerplate (page/layout/fonts replaced)

### Phase 2 — Design DNA (UiUxDesign §4–5, §32–36)
- [x] globals.css tokens: "Ink & Amber" identity — semantic colors light+dark,
      Fraunces display font + Geist body (next/font), radius/elevation scales,
      motion easings as Tailwind utilities (`ease-natural|emerge|spring`)
- [x] Theme provider (next-themes, class strategy) + CSS-driven ThemeToggle (no hydration flash)
- [x] Core components restyled to DNA: Button/Input/Card/Badge/Skeleton (shadcn),
      Container, Logo (journey-nodes mark), SiteHeader (scroll-aware), SiteFooter, CourseCard
- [x] prefers-reduced-motion global handling (CSS) + per-component checks (JS)

### Phase 3 — Landing page (UiUxDesign §6–11)
- [x] Site header + footer
- [x] Hero: layered composition — atmosphere / knowledge-network / cursor glow /
      typography; mouse parallax via springs + scroll choreography via useScroll
- [x] Section: What is EduCore (#what-is)
- [x] Section: Learning Journey preview with scroll-drawn line (#journey)
- [x] Section: Featured courses ← REAL data via GET /api/courses (#courses);
      graceful skeleton empty-state when backend offline
- [x] Section: How learning works (#how-it-works)
- [x] Section: Outcomes / certificates + CTA (#outcomes)
- [x] Placeholder /login and /register pages (so header links don't dead-end)

### Phase 4 — Payment groundwork (stubs only)
- [x] `src/lib/payments.ts` stubs with `====== PAYMENT GATEWAY: STRIPE — CHANGE HERE ======`
      banners documenting the full target flow + every backend file to change
- [x] Cart/checkout UI intentionally deferred to Milestone 3 (documented)

### Phase 5 — Verify & close
- [x] `npm run lint` → 0 problems
- [x] `npm run build` → passes (Next 16.3.3 / Turbopack)
- [x] Runtime smoke test: production server returns 200; hero renders;
      featured-courses fallback works when backend is offline
- [x] Update this file + FRONTEND_LOG.md

## Milestone 2 — Auth + catalog + course detail (COMPLETE ✅)

### Contracts extracted from backend
- [x] login `{email,password}` → `{accessToken,refreshToken}`; refresh rotates; logout revokes
- [x] register student `{name,email,birthDate,password}` → 201; auto-login after
- [x] GET /api/courses/{id} → CourseResponse shape; GET /api/courses/{id}/lessons → LessonsByCourseViewModel[]

### Implementation
- [x] `src/lib/jwt.ts` client-side JWT claims decoder (nameid/email/role/exp)
- [x] `src/lib/auth-context.tsx` AuthProvider: restore-on-mount, silent refresh if expired,
      profile fetch for display name, login/register/logout
- [x] Login page (real): error surfacing from problem+json, ?next= redirect support
- [x] Register page (real): validation, auto sign-in after account creation
- [x] Header: UserMenu dropdown (initial avatar, profile disabled until M4, destructive sign-out);
      auth buttons swap by state; mobile menu adapts
- [x] /courses catalog page: debounced search, skeleton loading, empty states,
      Load-more pagination (no totals in API), stale-response guard
- [x] /courses/[id] course page (RSC): cover/hero, instructors, curriculum list with lock icons,
      sticky price rail w/ disabled "Add to cart" (M3 marker comment), generateMetadata,
      notFound() on missing/unreachable course
- [x] CourseCard titles link to detail pages (stretched-link pattern)

### React 19 lint compliance
- [x] Catalog uses keyed-child remount pattern instead of sync setState in effects
- [x] AuthProvider effect updates state only after awaits

## Milestone 3 — Cart + checkout attempt (COMPLETE ✅)

### Backend addition (owner-approved, tiny)
- [x] `ProductId` exposed on course endpoints: CourseWithInstructorViewModel,
      clsCoursesData SQL+reader, CourseResponse, CourseMapper (~4 lines, no DB changes).
      Reason: cart API requires Products.Id; courses previously only exposed Courses.Id.
- [x] Backend solution builds with 0 errors after change.

### Contracts confirmed
- [x] POST /api/orders → creates-or-returns pending cart (OrderResponse incl. items[])
- [x] GET /api/orders/cart/{userId} → pending cart (404 if none — treated as empty)
- [x] POST /api/orders/{id}/items/{productId} / DELETE …/items/{productId} → updated order
- [x] ItemId in add-item = **Products.Id** (SP validates publishability)

### Implementation
- [x] `src/lib/orders.ts` typed fetchers (createOrGetCart/getCart/addItem/removeItem)
- [x] `src/lib/courses.ts` types now carry productId
- [x] `src/lib/cart-context.tsx` CartProvider: load-on-auth, optimistic remove w/ rollback,
      render-time clear on sign-out, header badge count
- [x] Header: live cart button + count badge (links /cart; sign-in prompt when logged out)
- [x] AddToCartButton on course detail (idle/adding/added→In-your-cart/error states)
- [x] `/cart` page: item list w/ remove, totals rail, discount code field, auth gate
      (?next=/cart), checkout attempt → REAL initPayment then stub gateway →
      "payment record created, gateway not connected" info panel

## Milestone 3.5 — Real Stripe integration (COMPLETE ✅)

### Backend (raw REST, NO SDK — owner decision)
- [x] `EduCore_BusinessLayer/clsStripeGateway.cs`: CreateCheckoutSessionAsync
      (form-encoded, Idempotency-Key from Payments.IdempotencyKey) +
      VerifyWebhookSignature (HMAC-SHA256, constant-time, 5-min tolerance)
- [x] `PaymentsController`: POST /api/payments/{id}/stripe-checkout-session
      (auth + ownership, pending-only, success/cancel → FRONTEND_URL/cart?checkout=…)
- [x] `StripeWebhookController` ([AllowAnonymous], signature-verified):
      session.completed/async_payment_succeeded → clsCheckoutService.CompletePaymentAsync;
      expired → MarkAsExpired; async_payment_failed → MarkAsFailed; replays are no-ops
- [x] checkOut-succeed endpoint marked DEPRECATED (audit H1 closed by webhook design)
- [x] .env.example: STRIPE_SECRET_KEY / STRIPE_WEBHOOK_SECRET / FRONTEND_URL

### Frontend
- [x] payments.ts stub replaced with REAL startCheckoutSession (redirect to hosted page);
      banners updated to "live — change here to switch provider"
- [x] initPayment now sends paymentMethod:"Stripe"
- [x] /cart return-trip handler: ?checkout=success|cancelled&paymentId → poll status,
      refresh cart, status banner (processing/succeeded/failed/expired/timeout/cancelled)

## Milestone 4 — Student learning app (COMPLETE ✅)

### Backend (L9 closed — owner-approved pattern, additive)
- [x] `Common/ViewModels/EnrollmentViewModel.cs`: product info + deep-link ids
      (CourseId/LessonId resolved via LEFT JOINs — progress API takes Courses.Id
      while enrollments carry Products.Id)
- [x] `clsEnrollmentsData.GetUserEnrollmentViewModels` + `clsEnrollment.GetUserEnrollments`
- [x] NEW `EnrollmentsController`: GET api/enrollments/my ([Authorize], current-user only)

### Frontend
- [x] `lib/learning.ts`: enrollments / course-progress / lesson-detail fetchers,
      complete+incomplete mutations
- [x] `AppHeader` shared shell header; added to /courses and /cart (navigation coherence)
- [x] `/learn` dashboard: welcome, Continue-Learning hero (highest incomplete %,
      resume → first incomplete lesson), library grid with progress bars + Completed badges,
      empty state, lazy per-course progress loading (max 8), auth gate
- [x] `/learn/lessons/[id]` player: video embed (YouTube/Vimeo/native), body text,
      optimistic complete/undo with rollback, prev/next via router.push,
      curriculum rail with checks + active state, thin progress line,
      locked-lesson state (403/404), Focus Mode (hides rail+header, Esc exits)

## Milestone 5 — Admin console (COMPLETE ✅)

### Design language pass ("Study Notes") — landing/auth/shared
- [x] `Mark` highlighter component + `MarginNote` handwritten annotations (Caveat font)
- [x] globals.css utilities: paper-grain / ruled-paper / index-card / sticky-note
- [x] Hero: knowledge-network SVG → self-drawing `learning-path.tsx` (milestone doodles,
      handwritten labels; parallax + scroll choreography preserved; reduced-motion static)
- [x] Landing touches: index cards, handwritten step numbers, dashed journey rail,
      certificate stamp, nav highlighter hover, footer ruled strip
- [x] Auth split-screen via AuthBrandPanel (login + register)
- [x] Brand assets: app/icon.svg favicon + opengraph-image.tsx (ImageResponse) + metadataBase
- [x] docs/design/gemini-prompts.md — Gemini prompt pack w/ cover-attachment SQL

### Console
- [x] lib/admin.ts — typed fetchers (users, courses, lessons, instructors, discounts, audit, logs)
- [x] Enum label maps (wire values are NUMERIC — no JsonStringEnumConverter)
- [x] AdminShell: sidebar (sliding active pill), mobile drawer, topbar
- [x] Command palette Ctrl/Cmd+K (hand-rolled, no cmdk dependency)
- [x] /admin layout: role gate (Admin/SuperAdmin full · Instructor courses-only)
- [x] Dashboard: quick links + live recent-audit feed (no fake stats — no aggregate endpoints)
- [x] /admin/courses: search, load-more paging, create-draft modal
- [x] /admin/courses/[id]: builder — identity save-in-place, publish/unpublish toggle,
      curriculum add/edit (full detail fetched per lesson), instructor assign/remove (Admin+)
- [x] /admin/people: students/instructors/admins tabs, deactivate/promote/demote with confirms
- [x] /admin/discounts: SuperAdmin CRUD on valid codes
- [x] /admin/audit + /admin/logs: paged tables with row-detail modals
- [x] UserMenu "Console" entry for privileged roles

### React 19 compliance
- [x] set-state-in-effect fixed across 6 files (async-only bodies, derived branches,
      keyed remounts, handler resets)

### Known gaps (backend-dependent — do NOT fake in UI)
- Orders/payments admin tables: NO list endpoints exist (by-id and cart only)
- Bundles management: no unpublished-list or publish-state visibility
- Course list carries no isPublished (status visible only inside the builder)
- User deactivation has no reverse endpoint (confirm modal warns)

## Milestone 6 — Finish-the-product pass (COMPLETE ✅)

### Profile & settings
- [x] lib/profile.ts (updateProfile / changePassword)
- [x] /profile: details form + password change; email change → clean re-login
      (refresh flow is keyed to stored email); UserMenu "Profile" enabled

### Certificates
- [x] lib/learning.ts issueCertificate (marks issuance via GET …/certificate)
- [x] /learn/certificate/[courseId]: gated on 100% progress, renders the document
      from real data (name/course/date/stamp), Print → Save-as-PDF; honest about the
      no-file backend gap (M14) — no fake downloads
- [x] /learn library completed cards expose "Certificate" entry

### Bundles (catalog completion)
- [x] getBundles/getBundleItems public fetchers
- [x] BundleCard with "See what's inside" modal (lazy items fetch) + AddToCartButton
- [x] /courses: bundles section beneath course grid (hidden when API empty)

### Responsive/a11y polish (M6)
- [x] AppHeader mobile nav panel (hamburger, auth-filtered links, aria-expanded)
- [x] Register "Get started" hidden on tiny screens (already in header CTA row)

## Backlog (needs backend decisions — do not start without owner)
- Certificate verification endpoint + shareable URLs (M14)
- Independent lesson products can't be sold from UI (no ProductId exposed)

## Milestone 6.5 — Admin bundles console + P1/P2/P3 close-out (session 9)

### Backend (minimal, additive — needed so admin sees real publish state)
- [x] `BundleViewModel.IsPublished` (Common)
- [x] `clsBundlesData/clsBundle.GetAllBundlesView(includeUnpublished)` gated
- [x] `GET api/bundles/all` (Instructor,SuperAdmin) — admin unpublished-list visibility (closes M5 gap)

### P2 — Bundle management console
- [x] lib/admin.ts bundle fetchers (list/create/update/publish/unpublish/detail/items add+remove)
- [x] /admin/bundles: list table + NewBundleModal (create → jump into builder)
- [x] /admin/bundles/[id]: builder — publish toggle, identity card, contents editor
      (course pool via getCoursesPaged, add/remove with confirms), not-found/unreachable states
- [x] Public /bundles/[id] detail page (RSC): sticky price rail + AddToCartButton,
      contents list deep-linking to course pages, cover/meta, honest empty states
- [x] BundleCard title links to the detail page (catalog keeps "What's inside" modal)

### P1 — product-level fixes
- [x] Global not-found / error (Link fix) / loading boundaries
- [x] Landing "View all" → /courses (was #courses)
- [x] /register honors ?next= (RegisterPage+Suspense restructure)
- [x] /admin/people: load-more paging, debounced search, server-side tab filtering,
      ARIA tablist w/ roving tabindex + arrow keys
- [x] /learn: load-more enrollments (24/page), progress fetched for ALL courses (no cap)

### P3 — infra & a11y
- [x] ui/textarea + ui/select primitives (zero-dep style), used in course builder
- [x] ui/toast (hand-rolled useToast) + ToastProvider in root layout; wired into
      bundle builder, course builder (publish/save/lessons/instructors), discounts, people
- [x] Modal focus trap + focus restore to trigger on close
- [x] admin-shell Bundles nav entry + command-palette router.push (was location.assign)
- [x] .env.example: NEXT_PUBLIC_SITE_URL documented
- [x] lint 0 · tsc clean · production build passes (24 routes)

## Milestone 7 — Admin commerce dashboards + publish/reactivate (session 10)

### Backend (minimal, additive)
- [x] `CourseWithInstructorViewModel.IsPublished` (course list shows publish state)
- [x] `POST /api/users/{id}/activate` + `ActivateUser` DAL/BL + `UserReactivated` audit action;
      `includeInactive` param on students/admins/instructors filters
- [x] `PaymentAdminViewModel` + `OrderAdminViewModel` (Common/ViewModels)
- [x] `GetAllOrdersView` / `GetAllPaymentsView` paged feeds (join Users, LIKE search) + BL pass-throughs
- [x] `GET api/orders` + `GET api/payments` (Admin,SuperAdmin)

### Frontend
- [x] /admin/courses: Published/Draft badge + quick Publish/Hide toggle (toast feedback)
- [x] lib/admin: getUsers includeInactive + activateUser; /admin/people Reactivate button for
      deactivated rows (showDeactivated included), deactivate copy updated
- [x] /admin/orders: paged table + detail modal + debounced search
- [x] /admin/payments: paged table + detail modal + debounced search
- [x] admin-shell Orders + Payments nav entries (Admin/SuperAdmin); AUDIT_ACTION_LABELS + 31
- [x] lint 0 · tsc clean · production build passes (26 routes)

## Milestone 8 — Standalone lessons purchase path (session 11)

### Backend (additive)
- [x] `vwLessonsWithOutCourses` + `P.Id AS ProductId` (EduCore.sql)
- [x] `LessonsWithOutCoursesViewModel.ProductId`
- [x] `GetAllLessonsWithOutCourses`: explicit cols + ProductId, published-only filter, newest first

### Frontend
- [x] lib/courses.ts: `LessonSummary` + `getLessons()` client fetcher
- [x] NEW lesson-card (thumbnail, instructor, price, `AddToCartButton` via ProductId)
- [x] /courses `LessonsSection` (first 9 + Load more; hidden when empty/unreachable)

## Milestone 9 — Courses+lessons correctness & performance pass (session 12)

### Backend (additive/minimal)
- [x] Public `GET /api/courses` published-only; `?includeUnpublished=true` for Admin/SuperAdmin
- [x] `GET /api/courses/{id}` 404 for drafts unless admin/owner instructor
- [x] `GET /api/lessons/{id}/info` (anon) — sanitized public lesson cover sheet (no video/body)
- [x] admin `getCoursesPaged` sends includeUnpublished

### Frontend
- [x] useInView hook — Bundles/Lessons sections lazy-fetch at scroll (skeleton placeholders)
- [x] Session GET cache + single-flight dedupe in lib/courses.ts (60s TTL, cache-misses evicted)
- [x] NEW /lessons/[id] RSC detail page (metadata, hero, purchase rail, unlocks-note)
- [x] LessonCard titles → /lessons/[id]
- [x] admin-shell CommandPalette via next/dynamic (smaller shell bundle)
- [x] lint 0 · build passes (27 routes)

## Milestone 10 — Standalone-lessons admin console (session 13)

### Backend (additive)
- [x] `GET /api/lessons` `?includeUnpublished=true` (Admin/SuperAdmin only) — drafts visible in the console

### Frontend
- [x] lib/admin: `AdminLessonSummary`, `getLessonsPaged`, lesson CRUD/publish/delete fetchers
- [x] NEW `/admin/lessons` console: search + load-more, publish/hide toggle, create modal,
      edit modal (lazy detail fetch), soft-delete with confirm
- [x] admin-shell Lessons nav entry + palette action (Admin/SuperAdmin)
- [x] lint 0 · build passes (28 routes)

## Milestone 11 — Instructor ownership scope (session 14)

### Backend (additive)
- [x] Course & lesson feeds `?includeUnpublished=true` honour Instructors scoped to their own products
- [x] Course paging corrected: filters moved into the paging CTE (accurate offsets)

### Frontend
- [x] `/admin/lessons` opened to Instructors (own lessons); nav role list updated
- [x] Instructor `/admin/courses` shows own drafts (no change needed — feed now scopes)
- [x] lint 0 · build passes

## Backlog (needs backend decisions — do not start without owner)
- Certificate verification endpoint + shareable URLs (M14)
- Deleted-lesson restore UI (needs a deleted-list feed; restore endpoint already exists)

## Known backend gaps affecting frontend (do NOT fix silently)
- ~~Courses don't expose ProductId~~ FIXED in M3 (owner-approved backend addition).
- ~~Drafts leak to public storefront~~ FIXED in M9 (published-only feed + draft 404 for anon).
- ~~No Stripe checkout-session endpoint yet~~ LIVE since M3.5.
- ~~No admin orders/payments feeds~~ FIXED in M7 (owner-approved additive endpoints).
- Missing `GET current-user enrollments/payments` endpoints (backend issue L9) — "My Learning" needs it.
- Pagination responses have no totals — avoid building "page X of Y" UI.
