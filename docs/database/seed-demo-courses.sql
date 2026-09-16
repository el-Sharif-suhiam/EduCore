/* ============================================================
   EduCore — demo seed: multi-subject published courses
   ============================================================
   Purpose: populate the anonymous catalog (landing "Featured
   courses" + /courses) with published courses across several
   subjects, so the product reads as a GENERAL LMS.

   * Idempotent — safe to run repeatedly; existing rows skipped.
   * Run AFTER EduCore.sql (tables + roles must exist).
   * All demo instructor accounts share the password:

         Demo1234!

     stored as BCrypt work-factor 12 — the same hashing scheme
     used by the API (clsUser.cs). They can sign in normally.
   ============================================================ */

USE EduCore;
GO

SET NOCOUNT ON;

------------------------------------------------------------
-- All scalar variables declared exactly once (batch scope)
------------------------------------------------------------
DECLARE @PwdHash NVARCHAR(255) =
    N'$2a$12$mIJQ9kOvAxCdC4utgmky3Ob2MZeNn8UkT4TZ9.sXDW10YQwsdy632'; -- Demo1234!

-- instructor loop
DECLARE @RoleIdInstructor TINYINT;
DECLARE @demoIdx INT = 0;
DECLARE @UserId INT;
DECLARE @demoName NVARCHAR(150);
DECLARE @demoBirth DATE;
DECLARE @demoEmail NVARCHAR(254);

-- course/lesson loop
DECLARE @courseIdx INT = 0;
DECLARE @courseCount INT;
DECLARE @ProductName NVARCHAR(120);
DECLARE @Price DECIMAL(9,2);
DECLARE @Summary NVARCHAR(300);
DECLARE @InsEmail NVARCHAR(254);
DECLARE @CreatorId INT;
DECLARE @ProductId INT;
DECLARE @CourseId INT;
DECLARE @lessonIdx INT;
DECLARE @lessonCount INT;
DECLARE @lTitle NVARCHAR(120);
DECLARE @lBody NVARCHAR(MAX);
DECLARE @LessonProductId INT;
DECLARE @LessonId INT;
DECLARE @PublishedCourses INT;

------------------------------------------------------------
-- 1) Demo instructors (+ Instructor role membership)
------------------------------------------------------------
SET @RoleIdInstructor = (SELECT RoleId FROM dbo.Roles WHERE Name = N'Instructor');

IF OBJECT_ID('tempdb..#Instructors') IS NOT NULL DROP TABLE #Instructors;
CREATE TABLE #Instructors (
    Id INT IDENTITY(1,1),
    Name NVARCHAR(150),
    BirthDate DATE,
    Email NVARCHAR(254)
);

INSERT INTO #Instructors (Name, BirthDate, Email) VALUES
(N'Sofia Marin',   '1988-04-12', 'sofia.marin@educore.demo'),
(N'Daniel Okafor', '1982-09-03', 'daniel.okafor@educore.demo'),
(N'Lucia Herrera', '1990-01-27', 'lucia.herrera@educore.demo'),
(N'Elias Brandt',  '1985-11-18', 'elias.brandt@educore.demo'),
(N'Maya Chen',     '1993-06-08', 'maya.chen@educore.demo'),
(N'Amara Diallo',  '1991-02-21', 'amara.diallo@educore.demo');

WHILE @demoIdx < (SELECT COUNT(*) FROM #Instructors)
BEGIN
    SET @demoIdx += 1;
    SELECT @demoName = Name, @demoBirth = BirthDate, @demoEmail = Email
    FROM #Instructors WHERE Id = @demoIdx;

    IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = @demoEmail)
        INSERT INTO dbo.Users (Name, BirthDate, Email, PasswordHash)
        VALUES (@demoName, @demoBirth, @demoEmail, @PwdHash);

    SET @UserId = (SELECT Id FROM dbo.Users WHERE Email = @demoEmail);

    IF @RoleIdInstructor IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM dbo.UserRoles
                       WHERE UserId = @UserId AND RoleId = @RoleIdInstructor)
        INSERT INTO dbo.UserRoles (UserId, RoleId) VALUES (@UserId, @RoleIdInstructor);
END

DROP TABLE #Instructors;

------------------------------------------------------------
-- 2) Seed data definitions
--    enProductType: Lesson=1, Course=2, Bundle=3
------------------------------------------------------------
IF OBJECT_ID('tempdb..#CourseSeed') IS NOT NULL DROP TABLE #CourseSeed;
CREATE TABLE #CourseSeed (
    Id INT IDENTITY(1,1),
    Name NVARCHAR(120),
    Price DECIMAL(9,2),
    Summary NVARCHAR(300),
    InstructorEmail NVARCHAR(254)
);

INSERT INTO #CourseSeed (Name, Price, Summary, InstructorEmail) VALUES
(N'Photography Fundamentals', 49.00,
 N'Move off auto mode with confidence. Composition, light, and exposure taught through short field exercises — so your first hundred photos teach you something every time.',
 N'sofia.marin@educore.demo'),

(N'Personal Finance Essentials', 39.00,
 N'Budgets, emergency funds, and compound interest explained plainly. Build a personal money system you can actually keep running after the course ends.',
 N'daniel.okafor@educore.demo'),

(N'Conversational Spanish: Beginner', 59.00,
 N'Start speaking from the first lesson. Everyday dialogues, listening practice, and gentle grammar — designed for real conversations, not textbook drills.',
 N'lucia.herrera@educore.demo'),

(N'Music Theory I: Reading & Rhythm', 45.00,
 N'Read standard notation and feel rhythm with certainty. From the staff and intervals to meters — with ear-training woven into every lesson.',
 N'elias.brandt@educore.demo'),

(N'Graphic Design Basics', 55.00,
 N'Typography, color, and layout fundamentals for non-designers. Learn to make clean, deliberate visuals — and to explain why they work.',
 N'maya.chen@educore.demo'),

(N'Creative Writing Workshop', 35.00,
 N'A workshop for first drafts. Weekly prompts, honest feedback criteria, and revision techniques that turn rough pages into finished stories.',
 N'amara.diallo@educore.demo');

-- 3–4 lessons per course (Products type=1 + Lessons + join row)
IF OBJECT_ID('tempdb..#LessonSeed') IS NOT NULL DROP TABLE #LessonSeed;
CREATE TABLE #LessonSeed (
    Id INT IDENTITY(1,1),
    CourseName NVARCHAR(120),
    Pos INT,
    Title NVARCHAR(120),
    Body NVARCHAR(MAX)
);

INSERT INTO #LessonSeed (CourseName, Pos, Title, Body) VALUES
(N'Photography Fundamentals', 1, N'Seeing light',
 N'Hard vs. soft light, direction, and time of day. A short observation exercise trains your eye before you touch a single setting.'),
(N'Photography Fundamentals', 2, N'Composition basics',
 N'Rule of thirds, framing, and leading lines — why they work, and when to break them on purpose.'),
(N'Photography Fundamentals', 3, N'The exposure triangle',
 N'Aperture, shutter speed, and ISO as one connected system, with practical recipes for common scenes.'),
(N'Photography Fundamentals', 4, N'Your first photo walk',
 N'A guided assignment that combines everything so far, plus how to review your own shots honestly.'),

(N'Personal Finance Essentials', 1, N'Where your money goes',
 N'Tracking without judgment: a one-week audit that reveals your real spending patterns.'),
(N'Personal Finance Essentials', 2, N'Building a budget that sticks',
 N'Simple frameworks compared, and how to choose one that matches your personality.'),
(N'Personal Finance Essentials', 3, N'Emergency funds & debt',
 N'How much buffer is enough, and a calm order of operations for paying debt down.'),
(N'Personal Finance Essentials', 4, N'Compounding, explained',
 N'Why time beats timing — with plain-number examples you can redo yourself.'),

(N'Conversational Spanish: Beginner', 1, N'Greetings & introductions',
 N'Say who you are and ask others about themselves, with pronunciation modeled line by line.'),
(N'Conversational Spanish: Beginner', 2, N'Ordering food & asking directions',
 N'The two situations every traveler meets first — practiced through short dialogues.'),
(N'Conversational Spanish: Beginner', 3, N'Talking about your day',
 N'Present-tense verbs for daily routines, learned in context instead of tables.'),
(N'Conversational Spanish: Beginner', 4, N'Small talk & stories',
 N'Connect sentences into simple past-tense stories about your weekend.'),

(N'Music Theory I: Reading & Rhythm', 1, N'The staff & notes',
 N'Clefs, note names, and how written pitch maps to the keyboard and the voice.'),
(N'Music Theory I: Reading & Rhythm', 2, N'Intervals & scales',
 N'Distance hearing and major-scale construction, with ear-training drills.'),
(N'Music Theory I: Reading & Rhythm', 3, N'Rhythm & meter',
 N'Note values, time signatures, and counting systems that keep you steady.'),
(N'Music Theory I: Reading & Rhythm', 4, N'Reading your first melody',
 N'Combine pitch and rhythm to sight-read a simple tune from start to finish.'),

(N'Graphic Design Basics', 1, N'What makes design good?',
 N'A working definition of good design, and a checklist for critiquing anything visual.'),
(N'Graphic Design Basics', 2, N'Working with type',
 N'Choosing and pairing fonts, sizing for hierarchy, and the mistakes everyone makes first.'),
(N'Graphic Design Basics', 3, N'Color with intent',
 N'A practical palette-building process — no color theory degree required.'),
(N'Graphic Design Basics', 4, N'Layout & hierarchy',
 N'Grids, spacing, and alignment: making the important thing feel important.'),

(N'Creative Writing Workshop', 1, N'Finding your premise',
 N'Turn vague ideas into a premise with stakes — the sentence everything else serves.'),
(N'Creative Writing Workshop', 2, N'Character wants & obstacles',
 N'Characters move when they want something. Build desire and friction that generate plot.'),
(N'Creative Writing Workshop', 3, N'Scene structure',
 N'Goal, conflict, turn: a repeatable shape for scenes that earn their place.'),
(N'Creative Writing Workshop', 4, N'Revising with purpose',
 N'A pass-by-pass revision method that improves drafts without flattening them.');

------------------------------------------------------------
-- 3) Insert courses + lessons (skip any course already present)
------------------------------------------------------------
SET @courseCount = (SELECT COUNT(*) FROM #CourseSeed);

WHILE @courseIdx < @courseCount
BEGIN
    SET @courseIdx += 1;
    SELECT @ProductName = Name, @Price = Price, @Summary = Summary, @InsEmail = InstructorEmail
    FROM #CourseSeed WHERE Id = @courseIdx;

    IF EXISTS (SELECT 1 FROM dbo.Products WHERE Name = @ProductName)
        CONTINUE; -- already seeded

    SET @CreatorId = (SELECT Id FROM dbo.Users WHERE Email = @InsEmail);

    INSERT INTO dbo.Products (ProductType, Name, BasePrice, CreatedByUser, Summary, IsPublished)
    VALUES (2 /* Course */, @ProductName, @Price, @CreatorId, @Summary, 1);
    SET @ProductId = SCOPE_IDENTITY();

    INSERT INTO dbo.Courses (ProductId) VALUES (@ProductId);
    SET @CourseId = SCOPE_IDENTITY();

    INSERT INTO dbo.CoursesInstructors (CourseId, InstructorId)
    VALUES (@CourseId, @CreatorId);

    -- lessons for this course, in order
    SET @lessonIdx = 0;
    SET @lessonCount = (SELECT COUNT(*) FROM #LessonSeed WHERE CourseName = @ProductName);

    WHILE @lessonIdx < @lessonCount
    BEGIN
        SET @lessonIdx += 1;
        SELECT @lTitle = Title, @lBody = Body
        FROM #LessonSeed
        WHERE CourseName = @ProductName AND Pos = @lessonIdx;

        INSERT INTO dbo.Products (ProductType, Name, BasePrice, CreatedByUser, Summary, IsPublished)
        VALUES (1 /* Lesson */, @lTitle, 0, @CreatorId, NULL, 1);
        SET @LessonProductId = SCOPE_IDENTITY();

        INSERT INTO dbo.Lessons (ProductId, Title, BodyText, InstructorId, CourseId)
        VALUES (@LessonProductId, @lTitle, @lBody, @CreatorId, @CourseId);
        SET @LessonId = SCOPE_IDENTITY();

        INSERT INTO dbo.CoursesLessons (CourseId, LessonId)
        VALUES (@CourseId, @LessonId);
    END

    PRINT N'Seeded course: ' + @ProductName;
END

SET @PublishedCourses =
    (SELECT COUNT(*) FROM dbo.Products WHERE ProductType = 2 AND IsPublished = 1);
PRINT N'Demo seed complete. Published courses now in catalog: '
    + CAST(@PublishedCourses AS NVARCHAR(10));

DROP TABLE IF EXISTS #CourseSeed;
DROP TABLE IF EXISTS #LessonSeed;
GO
