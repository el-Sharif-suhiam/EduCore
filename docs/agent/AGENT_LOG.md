# EduCore — Agent Work Log

Persistent project memory. Every agent session appends an entry. Do not rewrite history.

---

## 2026-08-22 — Discovery, Audit & Planning (session 1)

**Objective:** Full repository understanding, documentation, architecture/security/API/performance
audit per `INITIAL_AUDIT_PROMPT.md`. No code changes in this phase.

**Files inspected:** Entire repository — all 5 projects (~120 source files), `EduCore.sql`,
`Program.cs`, appsettings, `.env`, `.gitignore`, git history/state.

**Findings (summary — full detail in docs/security/audit-2026-08-22.md and ENGINEERING_TODO.md):**
- CRITICAL: enrollment check inverted (`ExpireAt < now`); checkout broken (double `OpenAsync` on open
  connection); `GetPaymentByIdAsync` parses Status from wrong column; bundle mutation endpoints
  unauthenticated; payment endpoints lack ownership checks; hardcoded `sa` connection string in source;
  `.env` secrets committed; `CourseLessons` table missing from schema script; no API endpoint to create
  an order/cart; discount usage counter never incremented.
- HIGH: self-service payment success without gateway/ownership; progress/certificate without enrollment
  checks; audit logs store Connection.Id instead of client IP; courses/lessons cannot be published via API.
- MEDIUM/LOW: inconsistent response contracts, wrong status codes, inverted audit messages, wrong audit
  actor on updates, route/param mismatches in CoursesController, dead `Dtos/` project, 126 build warnings,
  missing indexes, GETDATE vs SYSUTCDATETIME mix, unbounded PageSize.

**Decisions:**
- Follow DISCOVER → DOCUMENT → ANALYZE → PLAN → ASK APPROVAL → IMPLEMENT → TEST → DOCUMENT workflow.
- Fix order approved by owner: baseline commit → docs → Phase 0 (config/runtime landmines) →
  Phase 1 (security) → Phase 2 (commerce correctness) → Phase 3 (consistency) → Phase 4 (tests).
- Commit per major step.

**Modifications:** None (read-only phase). Baseline commit `ac27ee3` created including pre-existing WIP.

**Tests executed:** `dotnet build EduCore.slnx` → 0 errors, 126 warnings.

**Unresolved issues:** Everything in ENGINEERING_TODO.md.

**Next recommended actions:** Create docs tree, then Phase 0 fixes (connection string to config,
payment data bugs, CourseLessons table, route mismatches).

---

## 2026-08-22 — Documentation & Phase 0 fixes (session 1, continued)

**Objective:** Write persistent documentation (audit prompt §9–12), then fix Phase 0 items.

**Modifications:** docs/ tree created; see commits after baseline for per-step details.
