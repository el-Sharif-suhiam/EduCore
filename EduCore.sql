	CREATE DATABASE EduCore;
	use EduCore;
	CREATE TABLE Users (
	Id INT PRIMARY KEY IDENTITY(1,1) NOT NULL,
	Name NVARCHAR(150) NOT NULL,
	BirthDate Date NOT NULL,
	Email NVARCHAR(254) NOT NULL UNIQUE,
	PasswordHash NVARCHAR(255) NOT NULL,
	RefreshTokenHash NVARCHAR(255) NULL,
	RefreshTokenExpiresAt DATETIME2 Null,
	RefreshTokenRevokedAt DATETIME2 NULL,
	IsActive BIT DEFAULT 1,
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
	BasePrice SMALLMONEY NOT NULL,
	CreatedByAdmin Int Not NULL,
	ThumbnailUrl NVARCHAR(500),
	Summary nvarchar(300),
	IsPublished bit DEFAULT 0,
	FOREIGN KEY (CreatedByAdmin) REFERENCES Users(Id)
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
	FOREIGN KEY (ProductId) REFERENCES Products(Id),
	FOREIGN KEY (DeletedById) REFERENCES Users(Id),
	FOREIGN KEY (InstructorId) REFERENCES Users(Id)
	)
	

	CREATE TABLE CoursesLessons(
	CourseId int Not Null,
	LessonId Int Not Null,
	PRIMARY KEY (CourseId,LessonId),
	CONSTRAINT FK_CoursesL FOREIGN KEY (CourseId) REFERENCES Courses(Id),
	CONSTRAINT FK_LessonL FOREIGN KEY (LessonId) REFERENCES Lessons(Id),
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
	TotalPrice SmallMoney,
	Status varchar(50),
	CreatedAt DATETIME DEFAULT GETDATE(),
	FOREIGN KEY (UserId) REFERENCES Users(Id),

	)

	CREATE TABLE OrderItems(
	Id INT PRIMARY KEY IDENTITY(1,1) NOT NULL,
	OrderId INT NOT NULL,
	ProductId INT NOT NULL,
	PriceAtPurchase SmallMoney,
	FOREIGN KEY (ProductId) REFERENCES Products(Id),
	FOREIGN KEY (OrderId) REFERENCES Orders(Id),
	UNIQUE (OrderId, ProductId)
	)

	CREATE TABLE Payments (
	Id INT PRIMARY KEY IDENTITY(1,1) NOT NULL,
	OrderId Int NOT NULL,
	PaidAt DATE DEFAULT GETDATE(),
	Price smallmoney not null,
	DiscountId SmallInt NULL,
	DiscountPrice smallmoney NULL,
	PaymentMethod NVARCHAR(50),
	Status VARCHAR(50),
	TransactionId NVARCHAR(200),
	PayedPrice AS (Price - ISNULL(DiscountPrice,0)) PERSISTED,
	FOREIGN KEY (OrderId) REFERENCES Orders(Id),
	FOREIGN KEY (DiscountId) REFERENCES DiscountCodes(Id)
	)

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
    SELECT L.Id,P.Name, 
	P.Summary,
	P.BasePrice,
	P.CreatedAt, 
	P.ThumbnailUrl, 
	P.IsPublished,
	U.Id AS InstructorId,
    U.Name AS InstructorName
    FROM Lessons L
    JOIN Products P ON P.Id = L.ProductId
    LEFT JOIN CoursesLessons CL ON L.Id = CL.LessonId
    JOIN Users U ON L.InstructorId = U.Id
    WHERE CL.CourseId IS NULL AND IsDeleted = 0;


	CREATE VIEW vwLessonsWithCourses 
	AS 
	SELECT L.Id,P.Name, 
	P.Summary,
	P.BasePrice,
	P.CreatedAt, 
	P.ThumbnailUrl, 
	P.IsPublished,
	CL.CourseId,
	U.Id AS InstructorId,
    U.Name AS InstructorName
    FROM Lessons L
    JOIN Products P ON P.Id = L.ProductId
    LEFT JOIN CoursesLessons CL ON L.Id = CL.LessonId
    JOIN Users U ON L.InstructorId = U.Id
    WHERE CL.CourseId IS NOT NULL AND IsDeleted = 0;



	CREATE UNIQUE INDEX UX_Orders_User_Pending
	ON Orders(UserId)
	WHERE Status = 'Pending';