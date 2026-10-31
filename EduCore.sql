
-----------------------------------------------------------
-- 1) Create Database
-----------------------------------------------------------
IF DB_ID('EduCore') IS NULL
BEGIN
    CREATE DATABASE EduCore;
END
GO

USE EduCore;
GO

-----------------------------------------------------------
-- 2) Tables (idempotent: created only when missing)
-----------------------------------------------------------

IF OBJECT_ID('dbo.Users', 'U') IS NULL
BEGIN
	CREATE TABLE Users (
	Id INT PRIMARY KEY IDENTITY(1,1) NOT NULL,
	Name NVARCHAR(150) NOT NULL,
	BirthDate Date NOT NULL,
	Email NVARCHAR(254) NOT NULL UNIQUE,
	PasswordHash NVARCHAR(255) NOT NULL,
	RefreshTokenHash NVARCHAR(255) NULL,
	RefreshTokenExpiresAt DATETIME2 Null,
	RefreshTokenRevokedAt DATETIME2 NULL,
	IsActive BIT NOT NULL CONSTRAINT DF_User_IsActive DEFAULT (1),
	CreatedAt DATETIME2 Default SYSUTCDATETIME() NOT NULL
	)
END
GO

IF OBJECT_ID('dbo.Roles', 'U') IS NULL
BEGIN
	CREATE TABLE Roles(
	RoleId TINYINT PRIMARY KEY IDENTITY(1,1),
	Name NVARCHAR(50) NOT NULL UNIQUE
	)
END
GO

IF OBJECT_ID('dbo.UserRoles', 'U') IS NULL
BEGIN
	CREATE TABLE UserRoles (
	UserId Int NOT NULL,
	RoleId TINYINT NOT NULL
	PRIMARY KEY (UserId, RoleId),
	CONSTRAINT FK_User FOREIGN KEY (UserId) REFERENCES Users(Id),
    CONSTRAINT FK_Role FOREIGN KEY (RoleId) REFERENCES Roles(RoleId)
	)
END
GO

IF OBJECT_ID('dbo.Products', 'U') IS NULL
BEGIN
	CREATE TABLE Products(
	Id Int PRIMARY KEY IDENTITY(1,1),
	ProductType tinyint,
	Name NVARCHAR(120) NOT NULL,
	CreatedAt DATETIME2 DEFAULT SYSUTCDATETIME(),
	UpdatedAt DATETIME2,
	BasePrice DECIMAL(9,2) NOT NULL,
	CreatedByUser Int Not NULL,
	ThumbnailUrl NVARCHAR(500),
	Summary nvarchar(300),
	IsPublished bit DEFAULT 0,
	FOREIGN KEY (CreatedByUser) REFERENCES Users(Id)
	)
END
GO

IF OBJECT_ID('dbo.Courses', 'U') IS NULL
BEGIN
	CREATE TABLE Courses (
	Id int PRIMARY KEY IDENTITY(1,1),
	ProductId Int NOT NULL,
	CoverImageUrl nvarchar(350),
	IsDeleted BIT DEFAULT 0,
	DeletedAt DATE NULL,
	DeletedById INT NUll ,
	FOREIGN KEY (ProductId) REFERENCES Products(Id)
	)
END
GO

IF OBJECT_ID('dbo.CoursesInstructors', 'U') IS NULL
BEGIN
	CREATE TABLE CoursesInstructors (
	CourseId int NOT NULL,
	InstructorId INT NOT NULL,
	PRIMARY KEY (CourseId,InstructorId),
	CONSTRAINT FK_Course FOREIGN KEY (CourseId) REFERENCES Courses(Id),
    CONSTRAINT FK_Instructor FOREIGN KEY (InstructorId) REFERENCES Users(Id)
	)
END
GO

IF OBJECT_ID('dbo.Lessons', 'U') IS NULL
BEGIN
	CREATE TABLE Lessons (
	Id int PRIMARY KEY IDENTITY(1,1),
	ProductId int NOT NULL,
	Title NVARCHAR(150) NOT NULL,
	VideoUrl NVARCHAR(500),
	BodyText NVARCHAR(MAX),
	IsDeleted BIT DEFAULT 0,
	DeletedAt DATE NULL,
	DeletedById INT NUll ,
	InstructorId int,
    CourseId int NULL,
    FOREIGN KEY (CourseId) REFERENCES Courses(Id),
	FOREIGN KEY (ProductId) REFERENCES Products(Id),
	FOREIGN KEY (DeletedById) REFERENCES Users(Id),
	FOREIGN KEY (InstructorId) REFERENCES Users(Id)
	)
END
GO

IF OBJECT_ID('dbo.Bundles', 'U') IS NULL
BEGIN
	CREATE TABLE Bundles (
	Id smallInt PRIMARY KEY IDENTITY(1,1),
	ProductId Int Not Null,
	FOREIGN KEY (ProductId) REFERENCES Products(Id)
	)
END
GO

IF OBJECT_ID('dbo.CoursesLessons', 'U') IS NULL
BEGIN
	CREATE TABLE CoursesLessons (
	CourseId int NOT NULL,
	LessonId int NOT NULL,
	PRIMARY KEY (CourseId, LessonId),
	CONSTRAINT FK_CL_Course FOREIGN KEY (CourseId) REFERENCES Courses(Id),
	CONSTRAINT FK_CL_Lesson FOREIGN KEY (LessonId) REFERENCES Lessons(Id)
	)
END
GO

IF OBJECT_ID('dbo.BundlesItems', 'U') IS NULL
BEGIN
	CREATE TABLE BundlesItems(
	BundleId smallint Not Null,
	CourseId Int Not Null,
	PRIMARY KEY (BundleId,CourseId),
	CONSTRAINT FK_BundleB FOREIGN KEY (BundleId) REFERENCES Bundles(Id),
	CONSTRAINT FK_CourseB FOREIGN KEY (CourseId) REFERENCES Courses(Id),
	)
END
GO

IF OBJECT_ID('dbo.Progress', 'U') IS NULL
BEGIN
	CREATE TABLE Progress (
	UserId INT NOT NULL,
	LessonId int NOT NULL,
	CompletedDate Date DEFAULT SYSUTCDATETIME(),
	IsComplete Bit NULL,
	PRIMARY KEY (UserId,LessonId),
	CONSTRAINT FK_UserProgress FOREIGN KEY (UserId) REFERENCES Users(Id),
    CONSTRAINT FK_LessonProgress FOREIGN KEY (LessonId) REFERENCES Lessons(Id)
	)
END
GO

IF OBJECT_ID('dbo.DiscountCodes', 'U') IS NULL
BEGIN
	CREATE TABLE DiscountCodes (
	Id smallInt PRIMARY KEY IDENTITY(1,1),
	DiscountCode NVARCHAR(200) NOT NULL,
	DiscountRate DECIMAL(5,2),
	CreatedById Int NOT NULL,
	ExpireAt Date NULL,
	AllowedUseNumber smallint ,
	TotalUserNumber SmallInt 
	)
END
GO

IF OBJECT_ID('dbo.Orders', 'U') IS NULL
BEGIN
	CREATE TABLE Orders(
	Id INT PRIMARY KEY IDENTITY(1,1) NOT NULL,
	UserId INT NOT NULL,
	TotalPrice DECIMAL(9,2),
	Status varchar(50),
	CreatedAt DATETIME2 DEFAULT SYSUTCDATETIME(),
	FOREIGN KEY (UserId) REFERENCES Users(Id)
	)
END
GO

IF OBJECT_ID('dbo.OrderItems', 'U') IS NULL
BEGIN
	CREATE TABLE OrderItems(
	Id INT PRIMARY KEY IDENTITY(1,1) NOT NULL,
	OrderId INT NOT NULL,
	ProductId INT NOT NULL,
	PriceAtPurchase DECIMAL(9,2),
	FOREIGN KEY (ProductId) REFERENCES Products(Id),
	FOREIGN KEY (OrderId) REFERENCES Orders(Id),
	UNIQUE (OrderId, ProductId)
	)
END
GO

IF OBJECT_ID('dbo.Payments', 'U') IS NULL
BEGIN
	CREATE TABLE Payments
	(
		Id INT PRIMARY KEY IDENTITY(1,1),

		OrderId INT NOT NULL,

		CreatedAt DATETIME2 NOT NULL
		DEFAULT SYSUTCDATETIME(),

		PaidAt DATETIME2 NULL,

		Price DECIMAL(18,2) NOT NULL,

		DiscountId SMALLINT NULL,

		DiscountPrice DECIMAL(18,2) NULL,

		PaymentMethod NVARCHAR(50) NOT NULL,

		Status VARCHAR(50) NOT NULL
		DEFAULT 'Pending',

		TransactionId NVARCHAR(200) NULL,

		IdempotencyKey VARCHAR(255) NOT NULL,

		FinalPrice AS
		(
			Price - ISNULL(DiscountPrice,0)
		) PERSISTED,

		FOREIGN KEY (OrderId)
		REFERENCES Orders(Id),

		FOREIGN KEY (DiscountId)
		REFERENCES DiscountCodes(Id),

		CHECK (Price >= 0),

		CHECK (DiscountPrice >= 0),

		CHECK (Price >= ISNULL(DiscountPrice,0)),

		CHECK (
			Status IN
			('Pending','Succeeded','Failed','Expired','Cancelled')
		)
	);
END
GO

IF OBJECT_ID('dbo.Enrollments', 'U') IS NULL
BEGIN
	CREATE TABLE Enrollments (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UserId INT NOT NULL,
    ProductId INT NOT NULL,

    EnrolledAt DATETIME2 DEFAULT SYSUTCDATETIME(),
    ExpireAt DATE NULL,

    PaymentId INT NOT NULL,

    FOREIGN KEY (ProductId) REFERENCES Products(Id),
    FOREIGN KEY (UserId) REFERENCES Users(Id),
    FOREIGN KEY (PaymentId) REFERENCES Payments(Id),

    CONSTRAINT UQ_User_Product UNIQUE (UserId, ProductId)
	)
END
GO

IF OBJECT_ID('dbo.Audits', 'U') IS NULL
BEGIN
    CREATE TABLE Audits (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UserId INT NOT NULL,
    ActionType NVARCHAR(100) NOT NULL,
    EntityType VARCHAR(50) NOT NULL,
    EntityId INT NOT NULL,
    Description NVARCHAR(300) NOT NULL,
    DoneAt DATETIME2 DEFAULT SYSUTCDATETIME(),
    IpAddress VARCHAR(45) NULL,
    UserAgent NVARCHAR(500) NULL,
    FOREIGN KEY (UserId) REFERENCES Users(Id)
	);
END
GO

IF OBJECT_ID('dbo.Certificates', 'U') IS NULL
BEGIN
    CREATE TABLE Certificates (
    Id INT PRIMARY KEY IDENTITY(1,1),
    CertificateCode NVARCHAR(64) NOT NULL UNIQUE,
    UserId INT NOT NULL,
    CourseId INT NOT NULL,
    IssuedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Certificate_User FOREIGN KEY (UserId) REFERENCES Users(Id),
    CONSTRAINT FK_Certificate_Course FOREIGN KEY (CourseId) REFERENCES Courses(Id),
    CONSTRAINT UQ_Certificate_User_Course UNIQUE (UserId, CourseId)
    );
END
GO

IF OBJECT_ID('dbo.Logs', 'U') IS NULL
BEGIN
    CREATE TABLE Logs(
    Id INT PRIMARY KEY IDENTITY(1,1),
    LogType NVARCHAR(30) NOT NULL,
    Message NVARCHAR(MAX) NOT NULL,
    Source NVARCHAR(200) NULL,
    StackTrace NVARCHAR(MAX),
    IpAddress VARCHAR(45) NULL,
    UserAgent NVARCHAR(500) NULL,
    RequestPath NVARCHAR(300) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    );
END
GO

-----------------------------------------------------------
-- 3) Indexes (idempotent)
-----------------------------------------------------------

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_user_lesson' AND object_id = OBJECT_ID('dbo.Progress'))
	CREATE INDEX idx_user_lesson 
	ON Progress (UserId, LessonId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_course_Instructor' AND object_id = OBJECT_ID('dbo.CoursesInstructors'))
	CREATE INDEX idx_course_Instructor
	ON CoursesInstructors(CourseId,InstructorId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Orders_User_Pending' AND object_id = OBJECT_ID('dbo.Orders'))
	CREATE UNIQUE INDEX UX_Orders_User_Pending
	ON Orders(UserId)
	WHERE Status = 'Pending';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Orders_UserId' AND object_id = OBJECT_ID('dbo.Orders'))
	CREATE INDEX IX_Orders_UserId
	ON Orders(UserId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Payments_IdempotencyKey' AND object_id = OBJECT_ID('dbo.Payments'))
	CREATE UNIQUE INDEX UX_Payments_IdempotencyKey
	ON Payments(IdempotencyKey);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Payments_TransactionId' AND object_id = OBJECT_ID('dbo.Payments'))
	CREATE UNIQUE INDEX UX_Payments_TransactionId
	ON Payments(TransactionId)
	WHERE TransactionId IS NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Payments_OrderId' AND object_id = OBJECT_ID('dbo.Payments'))
	CREATE INDEX IX_Payments_OrderId
	ON Payments(OrderId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DiscountCodes_DiscountCode' AND object_id = OBJECT_ID('dbo.DiscountCodes'))
	CREATE INDEX IX_DiscountCodes_DiscountCode
	ON DiscountCodes(DiscountCode);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Enrollments_ExpireAt' AND object_id = OBJECT_ID('dbo.Enrollments'))
	CREATE INDEX IX_Enrollments_ExpireAt
	ON Enrollments(ExpireAt);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Audits_UserId_DoneAt' AND object_id = OBJECT_ID('dbo.Audits'))
	CREATE INDEX IX_Audits_UserId_DoneAt
	ON Audits(UserId, DoneAt);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Logs_CreatedAt' AND object_id = OBJECT_ID('dbo.Logs'))
	CREATE INDEX IX_Logs_CreatedAt
	ON Logs(CreatedAt);
GO

-----------------------------------------------------------
-- 4) Seed Roles (idempotent)
-----------------------------------------------------------

IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = 'Admin')
	INSERT INTO Roles(Name) VALUES('Admin');

IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = 'Instructor')
	INSERT INTO Roles(Name) VALUES('Instructor');

IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = 'Student')
	INSERT INTO Roles(Name) VALUES('Student');

IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = 'SuperAdmin')
	INSERT INTO Roles(Name) VALUES('SuperAdmin');
GO

-----------------------------------------------------------
-- 5) Views
-----------------------------------------------------------

CREATE OR ALTER VIEW vwLessonsWithOutCourses 
AS 
    SELECT L.Id,L.Title,
	P.Id AS ProductId,
	P.Summary,
	P.BasePrice,
	P.CreatedAt, 
	P.ThumbnailUrl, 
	P.IsPublished,
	U.Id AS InstructorId,
    U.Name AS InstructorName
    FROM Lessons L
    JOIN Products P ON P.Id = L.ProductId
    JOIN Users U ON L.InstructorId = U.Id
    WHERE L.CourseId IS NULL AND IsDeleted = 0;
GO

CREATE OR ALTER VIEW vwLessonsWithCourses 
AS 
	SELECT L.Id,L.Title, 
	P.Summary,
	P.BasePrice,
	P.CreatedAt, 
	P.ThumbnailUrl, 
	P.IsPublished,
	L.CourseId,
	U.Id AS InstructorId,
    U.Name AS InstructorName
    FROM Lessons L
    JOIN Products P ON P.Id = L.ProductId
    JOIN Users U ON L.InstructorId = U.Id
    WHERE L.CourseId IS NOT NULL AND IsDeleted = 0;
GO

-----------------------------------------------------------
-- 6) Stored Procedures
-----------------------------------------------------------

CREATE OR ALTER PROCEDURE SP_AddNewItemToOrder
   @OrderId INT,
   @ProductId INT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Serialize against SP_CreateNewPayment / SP_RemoveItemFromOrder:
        -- the order-row lock (UPDLOCK) is taken FIRST so item mutations
        -- and payment creation can never interleave on the same order.
        DECLARE @OrderStatus VARCHAR(50);

        SELECT @OrderStatus = Status
        FROM Orders WITH (UPDLOCK, ROWLOCK)
        WHERE Id = @OrderId;

        -- State machine guard: only editable orders accept new items.
        -- Completed/Cancelled are final; they must never be modified
        -- or silently flipped back to Pending by this procedure.
        IF @OrderStatus IS NULL OR @OrderStatus NOT IN ('Pending', 'Empty')
            THROW 50011, 'Order is not editable in its current state', 1;

        -- A live payment pins the order total: mutating items under a
        -- pending payment would let the buyer pay a stale price.
        IF EXISTS (
            SELECT 1 FROM Payments
            WHERE OrderId = @OrderId AND Status = 'Pending'
        )
            THROW 50012, 'Order has a pending payment', 1;

        DECLARE @PriceAtPurchase DECIMAL(9,2);

        SELECT @PriceAtPurchase = BasePrice
        FROM Products
        WHERE Id = @ProductId AND IsPublished = 1;

        IF @PriceAtPurchase IS NULL
            THROW 50010, 'Product is not available for purchase', 1;

        INSERT INTO OrderItems(OrderId, ProductId, PriceAtPurchase)
        VALUES (@OrderId, @ProductId, @PriceAtPurchase);

        DECLARE @OrderItemId INT = SCOPE_IDENTITY();

        DECLARE @NewTotal DECIMAL(9,2);

        SELECT @NewTotal = ISNULL(SUM(PriceAtPurchase),0)
        FROM OrderItems
        WHERE OrderId = @OrderId;

        -- Conditional update: the editable-state check is repeated in
        -- the WHERE clause so a concurrent Complete/Cancel that commits
        -- first cannot be overwritten. Rowcount 0 => state changed
        -- underneath us => abort.
        UPDATE Orders
		SET TotalPrice = @NewTotal,
		    Status = CASE
                WHEN @NewTotal = 0 THEN 'Empty'
                ELSE 'Pending'
             END
		WHERE Id = @OrderId
		  AND Status IN ('Pending', 'Empty');

        IF @@ROWCOUNT = 0
            THROW 50011, 'Order is not editable in its current state', 1;

        COMMIT;

        SELECT 
            @OrderItemId AS OrderItemId,
            @NewTotal AS TotalPrice;

    END TRY
    BEGIN CATCH
        ROLLBACK;
        THROW;
    END CATCH
END
GO

CREATE OR ALTER PROCEDURE SP_RemoveItemFromOrder
    @OrderId INT,
    @ProductId INT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Same serialization point as SP_AddNewItemToOrder: lock the
        -- order row first, then validate state.
        DECLARE @OrderStatus VARCHAR(50);

        SELECT @OrderStatus = Status
        FROM Orders WITH (UPDLOCK, ROWLOCK)
        WHERE Id = @OrderId;

        -- State machine guard: only editable orders accept removals.
        IF @OrderStatus IS NULL OR @OrderStatus NOT IN ('Pending', 'Empty')
            THROW 50011, 'Order is not editable in its current state', 1;

        -- Items cannot be removed while a payment is pending on them.
        IF EXISTS (
            SELECT 1 FROM Payments
            WHERE OrderId = @OrderId AND Status = 'Pending'
        )
            THROW 50012, 'Order has a pending payment', 1;

        DELETE FROM OrderItems
        WHERE OrderId = @OrderId
          AND ProductId = @ProductId;

        DECLARE @NewTotal DECIMAL(9,2);

        SELECT @NewTotal = ISNULL(SUM(PriceAtPurchase),0)
        FROM OrderItems
        WHERE OrderId = @OrderId;

        -- Conditional update with the same state guard as the add path
        -- (see SP_AddNewItemToOrder); a completed/cancelled order is
        -- never modified and never reset to Pending/Empty.
        UPDATE Orders
        SET TotalPrice = @NewTotal,
            Status = CASE 
                        WHEN @NewTotal = 0 THEN 'Empty'
                        ELSE 'Pending'
                     END
        WHERE Id = @OrderId
          AND Status IN ('Pending', 'Empty');

        IF @@ROWCOUNT = 0
            THROW 50011, 'Order is not editable in its current state', 1;

        COMMIT;

        SELECT @NewTotal AS TotalPrice;

    END TRY
    BEGIN CATCH
        ROLLBACK;
        THROW;
    END CATCH
END
GO

CREATE OR ALTER PROCEDURE SP_CreateNewPayment
    @OrderId INT,
    @IdempotencyKey VARCHAR(255),
    @DiscountId SMALLINT = NULL,
    @paymentMethod NVARCHAR(60)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- =========================
        -- 1. Lock the order row FIRST (same serialization point as
        --    SP_AddNewItemToOrder / SP_RemoveItemFromOrder): item
        --    mutations and payment creation can never interleave.
        -- =========================
        DECLARE @TotalPrice DECIMAL(9,2);
        DECLARE @OrderStatus VARCHAR(50);

        SELECT
            @TotalPrice = TotalPrice,
            @OrderStatus = Status
        FROM Orders WITH (UPDLOCK, ROWLOCK)
        WHERE Id = @OrderId;

        IF @TotalPrice IS NULL
            THROW 50001, 'Order not found', 1;

        IF @OrderStatus <> 'Pending'
            THROW 50002, 'Order is not valid for payment', 1;

        -- =========================
        -- 2. Idempotency: same key + same order returns the existing
        --    payment; same key + a different order is a client error.
        --    HOLDLOCK keeps the check-then-insert atomic against a
        --    concurrent call with the same key (the unique index
        --    UX_Payments_IdempotencyKey is the last line of defense).
        -- =========================
        DECLARE @ExistingPaymentId INT;
        DECLARE @ExistingFinalPrice DECIMAL(18,2);
        DECLARE @ExistingOrderId INT;

        SELECT
            @ExistingPaymentId = Id,
            @ExistingFinalPrice = FinalPrice,
            @ExistingOrderId = OrderId
        FROM Payments WITH (HOLDLOCK, UPDLOCK, ROWLOCK)
        WHERE IdempotencyKey = @IdempotencyKey;

        IF @ExistingPaymentId IS NOT NULL
        BEGIN
            IF @ExistingOrderId <> @OrderId
                THROW 50005, 'Idempotency key already used for another order', 1;

            COMMIT;

            SELECT
                @ExistingPaymentId AS PaymentId,
                @ExistingFinalPrice AS FinalPrice;
            RETURN;
        END

        -- =========================
        -- 3. At most one live (Pending) payment per order: an older
        --    Pending payment must be expired/failed before a new one
        --    is created, otherwise the discount usage and the order
        --    total could be double-counted.
        -- =========================
        IF EXISTS (
            SELECT 1 FROM Payments
            WHERE OrderId = @OrderId AND Status = 'Pending'
        )
            THROW 50006, 'A pending payment already exists for this order', 1;

        -- =========================
        -- 4. حساب الخصم
        -- =========================
        DECLARE @DiscountPrice DECIMAL(9,2) = 0;

        IF @DiscountId IS NOT NULL AND @DiscountId > 0
        BEGIN
            DECLARE
                @ExpireAt DATE,
                @DiscountRate DECIMAL(5,2),
                @AllowedUseNumber INT,
                @UsedCount INT;

            -- Lock the discount row for the duration of the transaction so
            -- concurrent reservations cannot both observe the final slot.
            IF NOT EXISTS (
                SELECT 1 FROM DiscountCodes WITH (UPDLOCK, HOLDLOCK, ROWLOCK) WHERE Id = @DiscountId
            )
                THROW 50007, 'Discount not found', 1;

            SELECT
                @ExpireAt = ExpireAt,
                @DiscountRate = DiscountRate,
                @AllowedUseNumber = AllowedUseNumber,
                @UsedCount = ISNULL(TotalUserNumber, 0)
            FROM DiscountCodes WITH (UPDLOCK, HOLDLOCK, ROWLOCK)
            WHERE Id = @DiscountId;

            -- Rate sanity (mirrors clsDiscountCode.SetDiscountRate):
            -- a rate must fall in (0, 100] when present.
            IF @DiscountRate IS NOT NULL AND (@DiscountRate <= 0 OR @DiscountRate > 100)
                THROW 50008, 'Discount rate is invalid', 1;

            -- ExpireAt is a DATE column: the code stays valid through
            -- the END of its last day (UTC). It is expired only when
            -- its date is strictly before today.
            IF @ExpireAt IS NOT NULL AND @ExpireAt < CAST(SYSUTCDATETIME() AS DATE)
                THROW 50003, 'Discount expired', 1;

            -- حد الاستخدام (NULL أو 0 = غير محدود).
            -- TotalUserNumber counts SUCCEEDED payments; Pending payments
            -- reserve a slot so concurrent checkouts cannot over-consume.
            SELECT @UsedCount = @UsedCount + COUNT(*)
            FROM Payments
            WHERE DiscountId = @DiscountId AND Status = 'Pending';

            IF ISNULL(@AllowedUseNumber, 0) <> 0 AND @UsedCount >= @AllowedUseNumber
                THROW 50004, 'Discount limit reached', 1;

            SET @DiscountPrice = ISNULL((@TotalPrice * @DiscountRate) / 100, 0);
        END

        -- =========================
        -- 5. إدخال الدفع
        -- =========================
        INSERT INTO Payments
        (
            OrderId,
            Price,
            DiscountId,
            DiscountPrice,
            PaymentMethod,
            Status,
            TransactionId,
            IdempotencyKey
        )
        OUTPUT
            INSERTED.Id AS PaymentId,
            INSERTED.FinalPrice
        VALUES
        (
            @OrderId,
            @TotalPrice,
            @DiscountId,
            @DiscountPrice,
            @paymentMethod,
            'Pending',
            NULL,
            @IdempotencyKey
        );

        COMMIT;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK;
        THROW;
    END CATCH
END
GO

CREATE OR ALTER PROCEDURE SP_CreateEnrollmentsFromPaidOrder
    @OrderId INT,
    @PaymentId INT,
    @ExpireAt DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY

        DECLARE @UserId INT;
        DECLARE @PaymentStatus VARCHAR(50);

        -- جلب صاحب الطلب
        SELECT @UserId = O.UserId
        FROM Orders O
        WHERE O.Id = @OrderId;

        IF @UserId IS NULL
            THROW 50001, 'Order not found', 1;

        -- التحقق من أن الدفع ناجح وينتمي لنفس الطلب
        SELECT @PaymentStatus = P.Status
        FROM Payments P
        WHERE P.Id = @PaymentId
          AND P.OrderId = @OrderId;

        IF @PaymentStatus IS NULL
            THROW 50002, 'Payment not found for this order', 1;

        IF @PaymentStatus <> 'Succeeded'
            THROW 50003, 'Payment is not succeeded', 1;

        -- 1) تجديد الاشتراكات المنتهية: إعادة شراء نفس المنتج تمدد
        --    صلاحية الاشتراك المنتهي بدل الفشل بسبب قيد التفرد
        --    UQ_User_Product. ExpireAt عمود DATE: الاشتراك منتهٍ فقط
        --    إذا كان تاريخ انتهائه قبل تاريخ اليوم (UTC) — يبقى صالحاً
        --    حتى نهاية آخر يوم له.
        UPDATE E
        SET E.ExpireAt  = CAST(@ExpireAt AS DATE),
            E.PaymentId = @PaymentId
        FROM Enrollments E
        JOIN OrderItems OI ON OI.ProductId = E.ProductId
        WHERE OI.OrderId = @OrderId
          AND E.UserId = @UserId
          AND E.ExpireAt IS NOT NULL
          AND E.ExpireAt < CAST(SYSUTCDATETIME() AS DATE);

        DECLARE @Renewed INT = @@ROWCOUNT;

        -- 2) إنشاء enrollments جديدة للمنتجات غير المشتركة سابقاً
        INSERT INTO Enrollments
            (UserId, ProductId, EnrolledAt, ExpireAt, PaymentId)
        SELECT DISTINCT
            @UserId,
            OI.ProductId,
            SYSUTCDATETIME(),
            CAST(@ExpireAt AS DATE),
            @PaymentId
        FROM OrderItems OI
        WHERE OI.OrderId = @OrderId
          AND NOT EXISTS
          (
              SELECT 1
              FROM Enrollments E
              WHERE E.UserId = @UserId
                AND E.ProductId = OI.ProductId
          );

        DECLARE @Inserted INT = @@ROWCOUNT;

        -- Row counts are suppressed by SET NOCOUNT ON, so clients cannot
        -- rely on ExecuteNonQuery(); expose the affected count explicitly.
        -- 0 is a valid outcome (every product in the order already has an
        -- active enrollment) and must NOT be treated as a failure — the
        -- payment succeeded, so the order completion chain must proceed.
        SELECT @Inserted + @Renewed AS AffectedEnrollments;

    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

-----------------------------------------------------------
-- 7) Certificates (idempotent issuance + verification)
-----------------------------------------------------------

CREATE OR ALTER PROCEDURE SP_IssueCertificate
    @UserId INT,
    @CourseId INT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        DECLARE @ProductId INT;

        SELECT @ProductId = ProductId
        FROM Courses
        WHERE Id = @CourseId;

        IF @ProductId IS NULL
            THROW 50020, 'Course not found', 1;

        -- Authorization is enforced server-side: only an active
        -- enrollment for this exact course product may earn a certificate.
        IF NOT EXISTS (
            SELECT 1 FROM Enrollments E
            WHERE E.UserId = @UserId
              AND E.ProductId = @ProductId
              AND (E.ExpireAt IS NULL
                   OR E.ExpireAt >= CAST(SYSUTCDATETIME() AS DATE))
        )
            THROW 50021, 'User is not enrolled in this course', 1;

        -- Only a fully completed course is eligible. Deleted lessons do
        -- not count toward the total; a zero-lesson course is never
        -- completable.
        DECLARE @TotalLessons INT;
        DECLARE @CompletedLessons INT;

        SELECT @TotalLessons = COUNT(*)
        FROM CoursesLessons cl
        JOIN Lessons l ON l.Id = cl.LessonId AND l.IsDeleted = 0
        WHERE cl.CourseId = @CourseId;

        SELECT @CompletedLessons = COUNT(*)
        FROM CoursesLessons cl
        JOIN Lessons l ON l.Id = cl.LessonId AND l.IsDeleted = 0
        JOIN Progress p ON p.LessonId = l.Id
        WHERE cl.CourseId = @CourseId
          AND p.UserId = @UserId
          AND p.IsComplete = 1;

        IF @TotalLessons = 0 OR @CompletedLessons < @TotalLessons
            THROW 50022, 'Course is not fully completed', 1;

        -- Idempotent issuance: one certificate per (user, course).
        -- Repeated calls return the existing code instead of creating
        -- duplicates or regenerating the PDF under a new identity.
        IF EXISTS (
            SELECT 1 FROM Certificates
            WHERE UserId = @UserId AND CourseId = @CourseId
        )
        BEGIN
            SELECT CertificateCode, IssuedAt
            FROM Certificates
            WHERE UserId = @UserId AND CourseId = @CourseId;
        END
        ELSE
        BEGIN
            DECLARE @Code NVARCHAR(64) = LOWER(REPLACE(NEWID(), '-', ''));

            BEGIN TRY
                INSERT INTO Certificates (CertificateCode, UserId, CourseId)
                VALUES (@Code, @UserId, @CourseId);
            END TRY
            BEGIN CATCH
                -- Lost a race against a concurrent issuance: the unique
                -- constraint UQ_Certificate_User_Course rejected the
                -- duplicate. Fall through and return the winner's row.
                IF ERROR_NUMBER() NOT IN (2627, 2601)
                    THROW;
            END CATCH

            SELECT CertificateCode, IssuedAt
            FROM Certificates
            WHERE UserId = @UserId AND CourseId = @CourseId;
        END

    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO
