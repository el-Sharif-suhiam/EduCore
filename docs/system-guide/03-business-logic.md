# Business Rules and Domain Behavior

Navigation: [Overview](00-overview.md) · [Architecture](01-architecture.md) · [Database](02-database.md) · [Workflows](04-workflows.md) · [API](05-api-reference.md) · [Security](06-security.md) · [Testing](07-testing-and-deployment.md) · [Glossary](08-glossary.md)

## Users and roles

Roles: Student, Instructor, Admin, SuperAdmin. Users have `IsActive`. Deactivation blocks login? Access checks rely on auth.

## Products and types

ProductType tinyint distinguishes course/bundle/lesson. Products have BasePrice, IsPublished. Purchase requires IsPublished=1 (checked in SP_AddNewItemToOrder).

## Courses and lessons

Courses and Lessons link to Products. Courses/Lessons use IsDeleted. Standalone lessons are buyable products. Course-to-lesson assignment via CoursesLessons.

## Bundles

Bundle product aggregates courses via BundlesItems. Bundle content display filters published bundle, non-deleted courses, published course products.

## Prices and discounts

DiscountCodes have rate, allowed uses, expiry. Usage counts succeeded + pending payments. Expired if ExpireAt < today (UTC date).

## Orders and order items

Orders: Pending/Empty/Completed/Cancelled. Only Pending/Empty editable. Item mutations lock order row first. Pending payment pins order (no item changes).

## Payment lifecycle

Payment: Pending → Succeeded/Failed/Expired. Completion requires explicit Stripe payment_status="paid". Idempotency key ensures one payment per key.

## Enrollment creation/renewal

SP_CreateEnrollmentsFromPaidOrder creates/renews enrollments per order products. Expired enrollments renewed on re-purchase.

## Course/lesson access

IsUserEnrolled: direct enrollment on product OR bundle enrollment whose course is non-deleted and course product published, respecting expiry.

## Progress tracking

Upsert progress per (UserId, LessonId). CompletedDate set when IsComplete=1.

## Certificate generation

Certificates issued per user/course; generated via QuestPDF.

## Publication/deletion/visibility

Publication controls public visibility; drafts 404 to non-owners. Deletion soft (IsDeleted). Users deactivated via IsActive.

## Key distinctions

- Product vs course: product is sellable wrapper; course holds content.
- Purchase vs enrollment: payment completion creates enrollments.
- Publication vs access: unpublished content hidden publicly but previously purchased direct access may remain.
- Pending vs completed payment.
```