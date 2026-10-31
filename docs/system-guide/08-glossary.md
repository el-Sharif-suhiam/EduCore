# EduCore — Glossary

- **Product**: The sellable unit (lesson, course, or bundle). Carries price,
  name, publication flag.
- **Course**: An educational container; owns lessons via `CoursesLessons`.
- **Lesson**: A single learning unit; standalone or course-attached.
- **Course Lesson**: A lesson row linked to a course (`Lessons.CourseId` +
  `CoursesLessons` junction).
- **Bundle**: A product grouping courses.
- **Bundle Item**: A row in `BundlesItems` linking a bundle to a course.
- **Order**: A user's cart; one pending per user.
- **Payment**: A payment attempt against an order.
- **Purchase**: The business act of paying; results in enrollments.
- **Enrollment**: Grants a user access to a product until `ExpireAt`.
- **Pending Payment**: A payment awaiting completion; reserves a discount slot.
- **Succeeded Payment**: A completed payment; increments discount usage and
  creates enrollments.
- **Discount Reservation**: A pending payment counting against a discount's
  usage limit.
- **Discount Usage**: `DiscountCodes.TotalUserNumber`; incremented on payment
  success.
- **Publication**: `Products.IsPublished`; controls public catalog visibility.
- **Soft Deletion**: `IsDeleted` on Courses/Lessons; hides content and
  blocks access.
- **User Deactivation**: `Users.IsActive = 0`; blocks login.
- **Progress**: Per (user, lesson) completion record.
- **Course Completion**: All non-deleted lessons of a course marked complete.
- **Certificate Issuance**: Creating a certificate row (idempotent).
- **Certificate Verification**: Public lookup of a certificate by code.
- **Idempotency Key**: Unique key preventing duplicate payment creation.
- **Refresh Token Rotation**: Replacing a refresh token on use and revoking
  the old one.