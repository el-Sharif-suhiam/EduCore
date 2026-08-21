# EduCore — Full Project Understanding, Documentation & Audit

You are now the primary engineering agent responsible for understanding and auditing the entire EduCore backend project.

## IMPORTANT: DO NOT MODIFY THE CODE YET

This is the discovery and planning phase.

Do NOT:

* modify source code
* delete files
* rename files
* refactor code
* change architecture
* fix bugs
* install unnecessary dependencies
* change database structure
* change API contracts

Your first responsibility is to deeply understand the existing project.

---

# 1. Understand the Entire Repository

Inspect the entire repository systematically.

Study:

* project structure
* solution/project files
* source code
* controllers
* services/business logic
* repositories/data access
* DTOs/ViewModels
* entities/models
* database configuration
* authentication/authorization
* middleware
* validation
* exception handling
* logging
* configuration
* dependency injection
* API contracts
* relationships between entities
* migrations if present
* tests if present
* documentation
* configuration files

Do not assume that the architecture specification is perfectly synchronized with the implementation.

The actual code is the source of truth.

Compare the implementation against the documented architecture and identify differences.

---

# 2. Build a Mental Model of EduCore

Understand the system as a complete backend rather than inspecting files independently.

Determine:

* What EduCore does
* Main business domains
* Main entities
* Entity relationships
* Main user roles
* Authentication flow
* Authorization/RBAC
* Course lifecycle
* Lesson lifecycle
* Bundle lifecycle
* Order lifecycle
* Payment lifecycle
* Enrollment/progress lifecycle
* Discount lifecycle
* Audit/logging behavior
* Dependencies between domains

Create a clear internal model of how a request travels through the application:

HTTP Request
→ Controller
→ Business/Application logic
→ Data access
→ Database
→ Response

Also identify all important variations of this flow.

---

# 3. Analyze the Architecture

Determine the actual architecture currently implemented.

Evaluate:

* separation of concerns
* dependency direction
* controller responsibilities
* business logic placement
* data access responsibilities
* DTO usage
* entity usage
* validation boundaries
* transaction handling
* exception handling
* authentication
* authorization
* dependency injection
* async/sync usage
* database access patterns
* API consistency
* naming consistency

Do not change anything yet.

For every architectural problem, explain:

1. What the problem is
2. Where it exists
3. Why it is a problem
4. Severity
5. Recommended solution

---

# 4. Analyze the Domain and Business Logic

Pay special attention to business rules.

Verify whether the implementation correctly handles:

* users and roles
* instructors
* courses
* lessons
* bundles
* orders
* order items
* payments
* discount codes
* enrollments
* progress
* auditing
* logging

Look for:

* missing validation
* incorrect assumptions
* impossible states
* inconsistent status transitions
* authorization gaps
* ownership problems
* duplicated business rules
* incorrect calculations
* race conditions
* transaction problems
* idempotency problems
* null handling problems
* incorrect entity relationships
* orphaned records
* inconsistent deletion behavior

Do not fix them yet.

---

# 5. Test Strategy

Determine how the project can realistically be tested.

Identify:

* existing tests
* missing test coverage
* critical business flows requiring tests
* integration-test opportunities
* unit-test opportunities
* API endpoint coverage
* authorization scenarios
* invalid input scenarios
* edge cases

Create a proposed testing strategy for the project.

If the project has insufficient infrastructure for testing, explain what should be added.

---

# 6. API Audit

Inspect every API endpoint.

For each endpoint determine:

* purpose
* HTTP method
* route
* request DTO
* response DTO
* validation
* authorization
* business rules
* error behavior
* database interaction
* possible edge cases

Compare the implementation with the existing API documentation.

Identify:

* undocumented endpoints
* documented but missing endpoints
* inconsistent response models
* inconsistent naming
* incorrect status codes
* missing validation
* security problems
* redundant endpoints

Do not modify the API yet.

---

# 7. Security Audit

Perform a defensive application-security review.

Look for:

* authentication weaknesses
* authorization weaknesses
* IDOR/resource ownership issues
* privilege escalation
* insecure password handling
* sensitive data exposure
* unsafe input handling
* missing validation
* insecure configuration
* secrets accidentally committed
* improper error disclosure
* unsafe logging
* missing rate limiting where appropriate
* insecure file handling if applicable
* transaction/payment manipulation possibilities

Do not exploit anything destructively.

Only identify and document issues.

---

# 8. Performance Review

Inspect the project for likely performance problems.

Look for:

* unnecessary database queries
* N+1 queries
* inefficient LINQ/database operations
* unnecessary data loading
* missing indexes where obvious
* excessive serialization
* duplicated queries
* inefficient pagination
* unnecessary synchronous blocking
* unnecessary allocations
* poor async usage

Classify findings by impact.

---

# 9. Create Persistent Project Documentation

After understanding the repository, create/update a dedicated project documentation directory.

Use something like:

docs/
├── architecture/
├── api/
├── domain/
├── database/
├── security/
├── testing/
├── decisions/
└── agent/

The exact structure may be adapted to the existing repository.

The documentation must describe the ACTUAL implementation, not an idealized architecture.

---

# 10. Create an Agent Context File

Create:

docs/agent/PROJECT_CONTEXT.md

This file is extremely important.

It should become the primary context file for future AI agents working on EduCore.

Include:

* project purpose
* technology stack
* architecture
* folder structure
* domain model
* entity relationships
* API structure
* authentication
* authorization
* important business rules
* database structure
* conventions
* coding conventions
* important dependencies
* known technical debt
* known bugs
* known architectural issues
* testing strategy
* important decisions
* things future agents MUST NOT break

Keep it factual and concise enough to load quickly.

---

# 11. Create an Agent Work Log

Create:

docs/agent/AGENT_LOG.md

This file will be used as persistent project memory.

Every future agent session must append to this file.

For each session record:

* date
* objective
* files inspected
* findings
* decisions
* modifications
* tests executed
* test results
* unresolved issues
* next recommended actions

Do not rewrite previous history.

Append new entries chronologically.

---

# 12. Create an Engineering TODO

Create:

docs/agent/ENGINEERING_TODO.md

Organize findings into:

## Critical

Security, data integrity, serious logic errors.

## High

Important architectural or functional problems.

## Medium

Maintainability/performance/consistency issues.

## Low

Minor improvements and cleanup.

Each item should contain:

* issue
* location
* explanation
* recommended solution
* dependencies
* status

---

# 13. Important Rule About Changes

During this discovery phase:

DO NOT fix anything.

Instead, produce a detailed implementation plan.

After the analysis is complete, report:

### Architecture Health

Overall assessment.

### Critical Findings

Problems that should be fixed first.

### Security Findings

### Logic Findings

### API Findings

### Database Findings

### Performance Findings

### Testing Gaps

### Documentation Created

### Recommended Fix Order

Provide a prioritized roadmap.

---

# 14. Approval Workflow

From this point onward, follow this workflow:

DISCOVER
→ DOCUMENT
→ ANALYZE
→ PLAN
→ ASK FOR APPROVAL
→ IMPLEMENT
→ TEST
→ DOCUMENT

Never silently make large architectural changes.

For any significant change, explain what you intend to change and why, then wait for approval.

For small, clearly safe corrections, you may group them together and request approval before implementation.

---

# 15. Mock/Fallback Data

The frontend is not currently the primary goal.

However, where useful for testing API behavior, create controlled mock/test data or test fixtures.

Do NOT introduce fake production behavior into the real business logic.

Mocks must be clearly separated from production code.

---

# Final Requirement

Do not judge the project only by whether it compiles.

Understand the business logic and architecture deeply.

Your goal is to eventually make EduCore:

* correct
* secure
* maintainable
* testable
* documented
* architecturally coherent
* production-quality

But for THIS PHASE, your only job is:

**Understand → Document → Audit → Plan.**

Do not modify implementation code until I explicitly approve the proposed changes.
