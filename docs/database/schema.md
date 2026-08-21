# EduCore — Database Structure (from EduCore.sql + code usage, 2026-08-22)

Database: `EduCore` (SQL Server). Script `EduCore.sql` is NOT idempotent (re-run fails).

## Tables
| Table | Key columns | Notes |
|---|---|---|
| Users | Id, Name, BirthDate, Email UNIQUE, PasswordHash, RefreshTokenHash, RefreshTokenExpiresAt, RefreshTokenRevokedAt, IsActive (def 1), CreatedAt | soft-deactivate via IsActive |
| Roles | RoleId TINYINT, Name UNIQUE | seeded Admin/Instructor/Student/SuperAdmin |
| UserRoles | (UserId, RoleId) PK | M:N |
| Products | Id, ProductType tinyint, Name, CreatedAt DATE, UpdatedAt DATE, BasePrice DECIMAL(9,2), CreatedByUser→Users, ThumbnailUrl, Summary, IsPublished def 0 | base for all sellables; DATE loses time |
| Courses | Id, ProductId→Products, CoverImageUrl, IsDeleted/DeletedAt/DeletedById | soft delete |
| CoursesInstructors | (CourseId, InstructorId) PK | |
| Lessons | Id, ProductId→Products, Title, VideoUrl, BodyText MAX, IsDeleted..., InstructorId→Users, CourseId→Courses NULL | independent vs course lesson |
| Bundles | Id SMALLINT, ProductId→Products | smallint capacity cliff |
| BundlesItems | (BundleId, CourseId) PK | |
| Progress | (UserId, LessonId) PK, CompletedDate, IsComplete BIT NULL | |
| DiscountCodes | Id SMALLINT, DiscountCode, DiscountRate DECIMAL(5,2), CreatedById, ExpireAt, AllowedUseNumber, TotalUserNumber | usage counter never incremented (C10) |
| Orders | Id, UserId→Users, TotalPrice, Status varchar(50), CreatedAt | no CHECK on Status; filtered unique index UX_Orders_User_Pending WHERE Status='Pending' |
| OrderItems | Id, OrderId→Orders, ProductId→Products, PriceAtPurchase, UNIQUE(OrderId,ProductId) (+dup index UX_Order_Product) | NULL price possible via SP bug (H6) |
| Payments | Id, OrderId→Orders, CreatedAt, PaidAt, Price, DiscountId→DiscountCodes, DiscountPrice, PaymentMethod, Status CHECK IN (Pending,Succeeded,Failed,Expired,Cancelled), TransactionId, IdempotencyKey UNIQUE, FinalPrice = Price−ISNULL(DiscountPrice,0) PERSISTED | CHECKs: Price≥0, DiscountPrice≥0, Price≥DiscountPrice; unique filtered index on TransactionId |
| Enrollments | Id, UserId, ProductId, EnrolledAt, ExpireAt, PaymentId→Payments, UNIQUE(UserId,ProductId) | |
| Audits | Id, UserId→Users, ActionType, EntityType, EntityId, Description, DoneAt, IpAddress, UserAgent | |
| Logs | Id, LogType, Message, Source, StackTrace, IpAddress, UserAgent, RequestPath, CreatedAt | |
| CourseLessons | (CourseId, LessonId) | **was missing from script (C8); added during fixes** |

## Views
- `vwLessonsWithOutCourses` — independent lessons + product + instructor (IsDeleted=0).
- `vwLessonsWithCourses` — course lessons + product + instructor (IsDeleted=0).

## Stored procedures
- `SP_AddNewItemToOrder(@OrderId,@ProductId)` — tx; prices only published products (NULL otherwise — bug H6); recomputes total; Status Empty/Pending; returns OrderItemId+TotalPrice.
- `SP_RemoveItemFromOrder(@OrderId,@ProductId)` — tx; delete + recompute; returns TotalPrice.
- `SP_CreateNewPayment(@OrderId,@IdempotencyKey,@DiscountId,@paymentMethod)` — validates order Pending; discount expiry/usage checks (no lock, race); inserts Pending payment; OUTPUT PaymentId+FinalPrice. Does NOT increment usage (C10).
- `SP_CreateEnrollmentsFromPaidOrder(@OrderId,@PaymentId,@ExpireAt)` — validates payment Succeeded for order; SELECTs items (result set before INSERT — confuses ExecuteNonQuery); inserts missing enrollments; 0 rows if all exist → caller treats as failure.

## Indexes
- UX_Order_Product (OrderItems), UX_Payments_IdempotencyKey, UX_Payments_TransactionId (filtered),
  UX_Orders_User_Pending (filtered), idx_user_lesson (Progress — duplicates PK), idx_course_Instructor.
- **Missing:** Orders.UserId (script has literal TODO comment), DiscountCodes.DiscountCode,
  Enrollments.ExpireAt, Audits.UserId/DoneAt, Logs.CreatedAt.

## Known drift / issues
- `CourseLessons` vs `CoursesLessons` naming inconsistency in code (C8).
- GETDATE() vs SYSUTCDATETIME() mixed.
- Orders.Status free text; code writes 'Empty' not documented anywhere in DB.
- Bundles/DiscountCodes smallint ids.
