/* ============================================================
   EduCore — reset-catalog.sql
   ============================================================
   Purpose: remove ALL seeded / demo / test catalog content from
   the database (courses, lessons, bundles and every connection
   row), leaving accounts, roles, orders and payments untouched.

   Use it when you want a clean slate before importing real
   courses (or before re-running docs/database/seed-demo-courses.sql
   to regenerate the demo catalog).

   * Safe to run repeatedly (idempotent — nothing to delete → 0).
   * Wrapped in a single transaction: any failure rolls back.
   * Runs with SET NOCOUNT ON; prints row counts at the end.

   NOTE: this also removes commerce rows (OrderItems / Enrollments)
   that point at the removed products, so the database stays
   referentially consistent.
   ============================================================ */

SET NOCOUNT ON;

BEGIN TRY
    BEGIN TRAN;

    DECLARE @CourseProducts TABLE (ProductId INT PRIMARY KEY);
    DECLARE @LessonProducts TABLE (ProductId INT PRIMARY KEY);
    DECLARE @BundleProducts TABLE (ProductId INT PRIMARY KEY);

    -- the three catalog product families currently in the database
    INSERT INTO @CourseProducts SELECT ProductId FROM Courses;
    INSERT INTO @LessonProducts SELECT ProductId FROM Lessons;
    INSERT INTO @BundleProducts SELECT ProductId FROM Bundles;

    -- learner progress tied to lessons being removed
    DELETE P FROM Progress P
        WHERE P.LessonId IN (SELECT Id FROM Lessons);

    -- curriculum links
    DELETE FROM CoursesLessons;
    DELETE FROM CoursesInstructors;

    -- bundle-to-course links
    DELETE FROM BundlesItems;

    -- commerce rows pointing at removed products (keep consistency)
    DELETE OI FROM OrderItems OI
        WHERE OI.ProductId IN (SELECT ProductId FROM @CourseProducts)
           OR OI.ProductId IN (SELECT ProductId FROM @LessonProducts)
           OR OI.ProductId IN (SELECT ProductId FROM @BundleProducts);

    DELETE E FROM Enrollments E
        WHERE E.ProductId IN (SELECT ProductId FROM @CourseProducts)
           OR E.ProductId IN (SELECT ProductId FROM @LessonProducts)
           OR E.ProductId IN (SELECT ProductId FROM @BundleProducts);

    -- leaf rows
    DELETE FROM Lessons;
    DELETE FROM Courses;
    DELETE FROM Bundles;

    -- the products behind all of the above
    DELETE FROM Products
        WHERE Id IN (SELECT ProductId FROM @CourseProducts)
           OR Id IN (SELECT ProductId FROM @LessonProducts)
           OR Id IN (SELECT ProductId FROM @BundleProducts);

    -- orphaned catalog products (type Course/Lesson/Bundle with no
    -- linked row — e.g. test leftovers). Catalog products are always
    -- meant to be linked, so unreferenced ones are garbage.
    DELETE FROM Products
        WHERE ProductType IN (1, 2, 3)
          AND Id NOT IN (SELECT ProductId FROM Courses)
          AND Id NOT IN (SELECT ProductId FROM Lessons)
          AND Id NOT IN (SELECT ProductId FROM Bundles);

    COMMIT;

    SELECT N'reset complete' AS Result,
           (SELECT COUNT(*) FROM Courses)  AS Courses,
           (SELECT COUNT(*) FROM Lessons)  AS Lessons,
           (SELECT COUNT(*) FROM Bundles)  AS Bundles,
           (SELECT COUNT(*) FROM Products) AS Products;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH;