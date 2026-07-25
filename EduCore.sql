	
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
	CreatedAt DATETIME Default GETDATE() NOT NULL
	)

	
	CREATE TABLE Roles(
	RoleId TINYINT PRIMARY KEY IDENTITY(1,1),
	Name NVARCHAR(50) NOT NULL UNIQUE
	)

	CREATE TABLE UserRoles (
	UserId Int NOT NULL,
	RoleId TINYINT NOT NULL
	PRIMARY KEY (UserId, RoleId),
	CONSTRAINT FK_User FOREIGN KEY (UserId) REFERENCES Users(Id),
    CONSTRAINT FK_Role FOREIGN KEY (RoleId) REFERENCES Roles(RoleId)
	)

	CREATE TABLE Products(
	Id Int PRIMARY KEY IDENTITY(1,1),
	ProductType tinyint,
	Name NVARCHAR(120) NOT NULL,
	CreatedAt DATE DEFAULT CAST(GETDATE() AS DATE),
	UpdatedAt DATE,
	BasePrice DECIMAL(9,2) NOT NULL,
	CreatedByUser Int Not NULL,
	ThumbnailUrl NVARCHAR(500),
	Summary nvarchar(300),
	IsPublished bit DEFAULT 0,
	FOREIGN KEY (CreatedByUser) REFERENCES Users(Id)
	)

	CREATE TABLE Courses (
	Id int PRIMARY KEY IDENTITY(1,1),
	ProductId Int NOT NULL,
	CoverImageUrl nvarchar(350),
	IsDeleted BIT DEFAULT 0,
	DeletedAt DATE NULL,
	DeletedById INT NUll ,
	FOREIGN KEY (ProductId) REFERENCES Products(Id)
	)
	

	CREATE TABLE CoursesInstructors (
	CourseId int NOT NULL,
	InstructorId INT NOT NULL,
	PRIMARY KEY (CourseId,InstructorId),
	CONSTRAINT FK_Course FOREIGN KEY (CourseId) REFERENCES Courses(Id),
    CONSTRAINT FK_Instructor FOREIGN KEY (InstructorId) REFERENCES Users(Id)
	)

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


	CREATE TABLE Bundles (
	Id smallInt PRIMARY KEY IDENTITY(1,1),
	ProductId Int Not Null,
	FOREIGN KEY (ProductId) REFERENCES Products(Id)
	)	

	CREATE TABLE BundlesItems(
	BundleId smallint Not Null,
	CourseId Int Not Null,
	PRIMARY KEY (BundleId,CourseId),
	CONSTRAINT FK_BundleB FOREIGN KEY (BundleId) REFERENCES Bundles(Id),
	CONSTRAINT FK_CourseB FOREIGN KEY (CourseId) REFERENCES Courses(Id),
	)


	CREATE TABLE Progress (
	UserId INT NOT NULL,
	LessonId int NOT NULL,
	CompletedDate Date DEFAULT GETDATE(),
	IsComplete Bit NULL,
	PRIMARY KEY (UserId,LessonId),
	CONSTRAINT FK_UserProgress FOREIGN KEY (UserId) REFERENCES Users(Id),
    CONSTRAINT FK_LessonProgress FOREIGN KEY (LessonId) REFERENCES Lessons(Id)
	)

	CREATE TABLE DiscountCodes (
	Id smallInt PRIMARY KEY IDENTITY(1,1),
	DiscountCode NVARCHAR(200) NOT NULL,
	DiscountRate DECIMAL(5,2),
	CreatedById Int NOT NULL,
	ExpireAt Date NULL,
	AllowedUseNumber smallint ,
	TotalUserNumber SmallInt 
	)
	
	CREATE TABLE Orders(
	Id INT PRIMARY KEY IDENTITY(1,1) NOT NULL,
	UserId INT NOT NULL,
	TotalPrice DECIMAL(9,2),
	Status varchar(50),
	CreatedAt DATETIME DEFAULT GETDATE(),
	FOREIGN KEY (UserId) REFERENCES Users(Id),
	-- add user id index here
	)

	CREATE TABLE OrderItems(
	Id INT PRIMARY KEY IDENTITY(1,1) NOT NULL,
	OrderId INT NOT NULL,
	ProductId INT NOT NULL,
	PriceAtPurchase DECIMAL(9,2),
	FOREIGN KEY (ProductId) REFERENCES Products(Id),
	FOREIGN KEY (OrderId) REFERENCES Orders(Id),
	UNIQUE (OrderId, ProductId)
	)
	CREATE UNIQUE INDEX UX_Order_Product
    ON OrderItems(OrderId, ProductId);

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


CREATE UNIQUE INDEX UX_Payments_IdempotencyKey
ON Payments(IdempotencyKey);

CREATE UNIQUE INDEX UX_Payments_TransactionId
ON Payments(TransactionId)
WHERE TransactionId IS NOT NULL;
	
	CREATE TABLE Enrollments (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UserId INT NOT NULL,
    ProductId INT NOT NULL,

    EnrolledAt DATETIME DEFAULT GETDATE(),
    ExpireAt DATE NULL,

    PaymentId INT NOT NULL,

    FOREIGN KEY (ProductId) REFERENCES Products(Id),
    FOREIGN KEY (UserId) REFERENCES Users(Id),
    FOREIGN KEY (PaymentId) REFERENCES Payments(Id),

    CONSTRAINT UQ_User_Product UNIQUE (UserId, ProductId)
	)

    
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


	CREATE INDEX idx_user_lesson 
	ON Progress (UserId, LessonId);

	CREATE INDEX idx_course_Instructor
	ON CoursesInstructors(CourseId,InstructorId);


	INSERT INTO Roles(Name)
	VALUES('Admin');

	 
	INSERT INTO Roles(Name)
	VALUES('Instructor');

	
	INSERT INTO Roles(Name)
	VALUES('Student');
	
	INSERT INTO Roles 
	VALUES('SuperAdmin');

	CREATE VIEW vwLessonsWithOutCourses 
	AS 
    SELECT L.Id,L.Title, 
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


	CREATE VIEW vwLessonsWithCourses 
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



	CREATE UNIQUE INDEX UX_Orders_User_Pending
	ON Orders(UserId)
	WHERE Status = 'Pending';


	------ ######### begining of SP ###### ------
	
CREATE PROCEDURE SP_AddNewItemToOrder
   @OrderId INT,
   @ProductId INT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @PriceAtPurchase DECIMAL(9,2);

        SELECT @PriceAtPurchase = BasePrice
        FROM Products
        WHERE Id = @ProductId AND IsPublished = 1;

        INSERT INTO OrderItems(OrderId, ProductId, PriceAtPurchase)
        VALUES (@OrderId, @ProductId, @PriceAtPurchase);

        DECLARE @OrderItemId INT = SCOPE_IDENTITY();

        DECLARE @NewTotal DECIMAL(9,2);

        SELECT @NewTotal = ISNULL(SUM(PriceAtPurchase),0)
        FROM OrderItems
        WHERE OrderId = @OrderId;

        UPDATE Orders
		SET TotalPrice = @NewTotal,
		Status = CASE 
                WHEN @NewTotal = 0 THEN 'Empty'
                ELSE 'Pending'
             END
		WHERE Id = @OrderId;

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


CREATE PROCEDURE SP_RemoveItemFromOrder
    @OrderId INT,
    @ProductId INT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DELETE FROM OrderItems
        WHERE OrderId = @OrderId
          AND ProductId = @ProductId;

        DECLARE @NewTotal DECIMAL(9,2);

        SELECT @NewTotal = ISNULL(SUM(PriceAtPurchase),0)
        FROM OrderItems
        WHERE OrderId = @OrderId;

        UPDATE Orders
        SET TotalPrice = @NewTotal,
            Status = CASE 
                        WHEN @NewTotal = 0 THEN 'Empty'
                        ELSE 'Pending'
                     END
        WHERE Id = @OrderId;

        COMMIT;

        SELECT @NewTotal AS TotalPrice;

    END TRY
    BEGIN CATCH
        ROLLBACK;
        THROW;
    END CATCH
END





CREATE PROCEDURE SP_CreateNewPayment
    @OrderId INT,
    @IdempotencyKey VARCHAR(255),
    @DiscountId SMALLINT = NULL,
    @paymentMethod NVARCHAR(60)
AS
BEGIN
    SET NOCOUNT ON;

        -- =========================
        -- 2. جلب بيانات الطلب
        -- =========================
        DECLARE @TotalPrice DECIMAL(9,2);
        DECLARE @OrderStatus VARCHAR(50);

        SELECT 
            @TotalPrice = TotalPrice,
            @OrderStatus = Status
        FROM Orders
        WHERE Id = @OrderId;

        IF @TotalPrice IS NULL
            THROW 50001, 'Order not found', 1;

        IF @OrderStatus <> 'Pending'
            THROW 50002, 'Order is not valid for payment', 1;

        -- =========================
        -- 3. حساب الخصم
        -- =========================
        DECLARE @DiscountPrice DECIMAL(9,2) = 0;

        IF @DiscountId IS NOT NULL AND @DiscountId > 0
        BEGIN
            DECLARE 
                @ExpireAt DATETIME,
                @DiscountRate DECIMAL(9,2),
                @AllowedUseNumber INT,
                @UsedCount INT;

            SELECT 
                @ExpireAt = ExpireAt,
                @DiscountRate = DiscountRate,
                @AllowedUseNumber = AllowedUseNumber,
                @UsedCount = TotalUserNumber
            FROM DiscountCodes
            WHERE Id = @DiscountId;

            -- انتهاء الصلاحية
            IF @ExpireAt IS NOT NULL AND @ExpireAt <= SYSUTCDATETIME()
                THROW 50003, 'Discount expired', 1;

            -- حد الاستخدام
            IF @AllowedUseNumber <> 0 AND @UsedCount >= @AllowedUseNumber
                THROW 50004, 'Discount limit reached', 1;

            SET @DiscountPrice = (@TotalPrice * @DiscountRate) / 100;
        END

        -- =========================
        -- 4. إدخال الدفع
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

END


CREATE PROCEDURE SP_CreateEnrollmentsFromPaidOrder
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

        -- 1) إرجاع كل عناصر الطلب
        SELECT 
            OI.Id,
            OI.OrderId,
            OI.ProductId,
            OI.PriceAtPurchase
        FROM OrderItems OI
        WHERE OI.OrderId = @OrderId;

        -- 2) إنشاء enrollments لكل منتج في الطلب
        INSERT INTO Enrollments
            (UserId, ProductId, EnrolledAt, ExpireAt, PaymentId)
        SELECT DISTINCT
            @UserId,
            OI.ProductId,
            SYSUTCDATETIME(),
            @ExpireAt,
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

    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END