# ADR 0001 — Discovery Phase & Fix Roadmap

- **Date:** 2026-08-22
- **Status:** Accepted (owner-approved workflow per INITIAL_AUDIT_PROMPT.md)

## Context
EduCore backend audited end-to-end (architecture, domain, API, security, performance, testing).
10 critical, 9 high, 17 medium, 10 low findings recorded in `docs/agent/ENGINEERING_TODO.md`.

## Decision
1. No code changes during discovery; baseline committed first (`ac27ee3`).
2. Document actual implementation under `docs/` before fixing.
3. Fix in phases, one commit per major step:
   - Phase 0 — config/runtime landmines (connection string to env, payment data bugs C2/C3,
     CourseLessons schema C8, route mismatches M3, BundleExists hardcoded id).
   - Phase 1 — security (bundle auth C4, payment ownership C5/H1, enrollment checks H2,
     audit IP H3, rate-limit/message fixes).
   - Phase 2 — commerce correctness (create-cart C9, order state machine H5, unpublished-product
     guard H6, idempotency H7, discount usage C10, publish endpoints H4).
   - Phase 3 — consistency (error envelope, response contracts, audit actor/messages, PageSize cap,
     dead code removal, indexes, idempotent SQL script).
   - Phase 4 — tests (seam + test projects per docs/testing/strategy.md).
4. Workflow per session: DISCOVER → DOCUMENT → ANALYZE → PLAN → ASK APPROVAL → IMPLEMENT → TEST → DOCUMENT.

## Consequences
- API behavior will change where security requires it (bundle/payment endpoints gain auth).
- DB script gains objects (CourseLessons, indexes); existing databases need the additions applied.
- Secrets must be rotated outside the repo.
