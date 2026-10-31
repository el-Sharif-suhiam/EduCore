# EduCore — Database Structure (summary)

For the full reference, see `docs/system-guide/02-database.md`.

Database: `EduCore` (SQL Server). Script `EduCore.sql` is idempotent. Integration
tests apply it to a disposable `EduCoreTests` database.

## Notes on previously reported drift (corrected)

- The junction table is `CoursesLessons` (not `CourseLessons`).
- `BundlesItems` has a composite primary key `(BundleId, CourseId)`.
- `DiscountCodes.TotalUserNumber` **is** incremented when an applicable payment
  succeeds (`clsDiscountCodesData.IncrementUsage`).
- `Products` has no soft-delete column.
- `Users.IsActive` is login eligibility; distinct from soft deletion (courses/lessons).
- Indexes now include `IX_Orders_UserId`, `IX_DiscountCodes_DiscountCode`,
  `IX_Enrollments_ExpireAt`, `IX_Audits_UserId_DoneAt`, `IX_Logs_CreatedAt`.

## Tables

See `docs/system-guide/02-database.md` for the complete column-level reference.

## Stored procedures

`SP_AddNewItemToOrder`, `SP_RemoveItemFromOrder`, `SP_CreateNewPayment`,
`SP_CreateEnrollmentsFromPaidOrder`, `SP_IssueCertificate`. See system guide
chapter 02 for details.