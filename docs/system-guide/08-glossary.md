# Domain Glossary

Navigation: [Overview](00-overview.md) · [Architecture](01-architecture.md) · [Database](02-database.md) · [Business Logic](03-business-logic.md) · [Workflows](04-workflows.md) · [API](05-api-reference.md) · [Security](06-security.md) · [Testing](07-testing-and-deployment.md)

- **Product**: sellable wrapper for course/lesson/bundle.
- **Course**: educational content entity linked to a product.
- **Lesson**: content unit; may be standalone product or in course.
- **Bundle**: product grouping courses.
- **Order**: purchase container with items and status.
- **Order item**: product in an order with price snapshot.
- **Payment**: financial transaction for an order.
- **Discount code**: percentage discount with usage limit/expiry.
- **Enrollment**: access grant to a product.
- **Progress**: per-user per-lesson completion.
- **Certificate**: issued proof of completion.
- **Publication**: IsPublished controls public visibility.
- **Soft deletion**: IsDeleted for courses/lessons (not products).
- **Activation**: Users.IsActive.
- **Pending payment**: not yet completed.
- **Completed payment**: Succeeded.
```