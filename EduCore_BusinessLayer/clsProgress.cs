using Common.Dtos;
using Common.Exceptions;
using Common.ViewModels;
using EduCore_DataAccess;
using System;
using System.ComponentModel.DataAnnotations;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ZXing;
using ZXing.QrCode;
using ZXing.Rendering;
using QRCoder;
namespace EduCore_BusinessLayer
{

    public class CertificateDocument : IDocument
    {
        private readonly string _studentName;
        private readonly string _courseTitle;
        private readonly string _dateText;
        private readonly string _certificateCode;
        private readonly string _logoPath;
        private readonly string _signaturePath;
        private readonly string _watermarkPath;
        private readonly byte[] _qrPath;

        public CertificateDocument(
            string studentName,
            string courseTitle,
            string dateText,
            string certificateCode,
            string logoPath,
            string signaturePath,
            string watermarkPath,
            byte[] qrPath)
        {
            _studentName = studentName;
            _courseTitle = courseTitle;
            _dateText = dateText;
            _certificateCode = certificateCode;
            _logoPath = logoPath;
            _signaturePath = signaturePath;
            _watermarkPath = watermarkPath;
            _qrPath = qrPath;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(18);
                page.PageColor("#F7F1E4");
                page.DefaultTextStyle(x => x.FontFamily("Arial"));

                page.Content()
                    .Extend()
                    .AlignCenter()
                    .AlignMiddle()
                    .Layers(layers =>
                    {
                        if (!string.IsNullOrWhiteSpace(_watermarkPath) && File.Exists(_watermarkPath))
                        {
                            //layers.Layer()
                            //    .AlignCenter()
                            //    .AlignMiddle()
                            //    .Opacity(0.06f)
                            //    .Image(_watermarkPath)
                            //    .FitArea();
                        }

                        layers.PrimaryLayer()
                            .PaddingHorizontal(36)
                            .PaddingVertical(26)
                            .Border(3)
                            .BorderColor("#C9A227")
                            .Padding(8)
                            .Border(1)
                            .BorderColor("#1F4E79")
                            .Padding(24)
                            .Column(column =>
                            {
                                column.Spacing(16);

                                if (!string.IsNullOrWhiteSpace(_logoPath) && File.Exists(_logoPath))
                                {
                                    column.Item()
                                        .AlignCenter()
                                        .Height(52)
                                        .Image(_logoPath)
                                        .FitHeight();
                                }

                                column.Item().AlignCenter().Column(title =>
                                {
                                    title.Item()
                                        .Text("CERTIFICATE OF COMPLETION")
                                        .FontSize(30)
                                        .Bold()
                                        .FontColor("#1F4E79")
                                        .AlignCenter();

                                    title.Item()
                                        .PaddingTop(4)
                                        .Text("شهادة إتمام كورس")
                                        .FontSize(18)
                                        .Bold()
                                        .FontColor("#1F4E79")
                                        .AlignCenter();
                                });

                                column.Item()
                                    .PaddingTop(8)
                                    .AlignCenter()
                                    .Column(nameBlock =>
                                    {
                                        nameBlock.Item()
                                            .Text("THIS CERTIFICATE IS PROUDLY AWARDED TO")
                                            .FontSize(14)
                                            .FontColor("#4A4A4A")
                                            .AlignCenter();

                                        nameBlock.Item()
                                            .PaddingTop(6)
                                            .Text("تمنح هذه الشهادة بفخر")
                                            .FontSize(18)
                                            .AlignCenter();

                                        nameBlock.Item()
                                            .PaddingTop(12)
                                            .Text(_studentName)
                                            .FontSize(36)
                                            .Bold()
                                            .FontColor("#1F4E79")
                                            .AlignCenter();

                                        nameBlock.Item()
                                            .PaddingTop(8)
                                            .AlignCenter()
                                            .LineHorizontal(2)
                                            .LineColor("#C9A227");
                                    });

                                column.Item()
                                    .PaddingTop(2)
                                    .AlignCenter()
                                    .Column(course =>
                                    {
                                        course.Item()
                                            .Text("In recognition of successfully completing the course titled:")
                                            .FontSize(12)
                                            .FontColor("#4A4A4A")
                                            .AlignCenter();

                                        course.Item()
                                            .Text("تقديرًا لإتمامه بنجاح الدورة التدريبية بعنوان:")
                                            .FontSize(16)
                                            .AlignCenter();

                                        course.Item()
                                            .PaddingTop(8)
                                            .Text(_courseTitle)
                                            .FontSize(24)
                                            .Bold()
                                            .FontColor("#1F4E79")
                                            .AlignCenter();
                                    });

                                column.Item()
                                    .PaddingTop(18)
                                    .LineHorizontal(1)
                                    .LineColor("#D7C7A1");

                                column.Item()
                                    .PaddingTop(14)
                                    .Row(row =>
                                    {
                                        row.RelativeItem()
                                            .Column(left =>
                                            {
                                                left.Item()
                                                    .Text("مدير المنصة")
                                                    .FontSize(14)
                                                    .Bold()
                                                    .AlignLeft();

                                                if (!string.IsNullOrWhiteSpace(_signaturePath) && File.Exists(_signaturePath))
                                                {
                                                    left.Item()
                                                        .PaddingTop(10)
                                                        .Height(48)
                                                        .Image(_signaturePath)
                                                        .FitHeight();
                                                }

                                                left.Item()
                                                    .PaddingTop(8)
                                                    .Text("م.الشريف سحيم")
                                                    .FontSize(16)
                                                    .Bold()
                                                    .AlignLeft();
                                            });

                                        row.RelativeItem()
                                            .Column(center =>
                                            {
                                                center.Item()
                                                    .Text("التاريخ")
                                                    .FontSize(14)
                                                    .Bold()
                                                    .AlignCenter();

                                                center.Item()
                                                    .PaddingTop(4)
                                                    .Text(_dateText)
                                                    .FontSize(16)
                                                    .Bold()
                                                    .AlignCenter();

                                                center.Item()
                                                    .PaddingTop(10)
                                                    .LineHorizontal(1)
                                                    .LineColor("#B08D57");
                                            });

                                        row.RelativeItem()
                                            .AlignRight()
                                            .Column(right =>
                                            {
                                                right.Item().Element(container =>
                                                {
                                                    container
                                                        .AlignCenter()
                                                        .Width(70)
                                                        .Height(70)
                                                        .OffsetY(-20)
                                                        .Image(_qrPath)
                                                        .FitArea();
                                                });
                                                //right.Item()
                                                //.Width(70)
                                                //.Height(70)
                                                //.OffsetY(-20)
                                                //.AlignCenter()
                                                //.Image(_qrPath);
                                                
                                              
                                                right.Item()
                                                    .Text("Certificate No.")
                                                    .FontSize(11)
                                                    .Bold()
                                                    .AlignCenter();

                                                right.Item()
                                                    .PaddingTop(4)
                                                    .Text(_certificateCode)
                                                    .FontSize(10)
                                                    .AlignCenter();
                                            });
                                    });
                            });
                    });
            
            });
        }



    }
    public class clsProgress
    {
        private DtoProgress _ProgressData;

        public int UserId => _ProgressData.UserId;
        public int LessonId => _ProgressData.LessonId;
        public bool? IsComplete => _ProgressData.IsComplete;
        public DateTime? CompletedDate => _ProgressData.CompletedDate;

        
        public clsProgress(int userId, int lessonId)
        {
            if (userId <= 0)
                throw new ValidationException("User id is not valid");
            if (lessonId <= 0)
                throw new ValidationException("Lesson id is not valid");

            _ProgressData = new DtoProgress
            {
                UserId = userId,
                LessonId = lessonId,
            };
        }

        private clsProgress(DtoProgress progress)
        {
            _ProgressData = progress;
        }


        public void SetComplete(bool isComplete)
        {
            _ProgressData.IsComplete = isComplete;
        }


        public static async Task<clsProgress> Find(int userId, int lessonId)
        {
            if (userId <= 0)
                throw new ValidationException("User id is not valid");
            if (lessonId <= 0)
                throw new ValidationException("Lesson id is not valid");

            DtoProgress dto = await clsProgressData.GetProgress(userId, lessonId);

            if (dto is null)
                throw new NotFoundException("No progress found for this user and lesson");

            return new clsProgress(dto);
        }


        public async Task<bool> Save()
        {
            return await clsProgressData.UpsertProgress(_ProgressData);
        }


        public static async Task<bool> MarkAsComplete(int userId, int lessonId)
        {
            await EnsureEnrolledForLesson(userId, lessonId);

            clsProgress progress = new clsProgress(userId, lessonId);
            progress.SetComplete(true);
            return await progress.Save();
        }

        public static async Task<bool> MarkAsIncomplete(int userId, int lessonId)
        {
            await EnsureEnrolledForLesson(userId, lessonId);

            clsProgress progress = new clsProgress(userId, lessonId);
            progress.SetComplete(false);
            return await progress.Save();
        }

        public static async Task<CourseProgressViewModel> GetCourseProgress(int userId, int courseId)
        {
            if (!(await clsCoursesData.CourseExists(courseId)))
                throw new NotFoundException("Course not found");

            await EnsureEnrolledForCourse(userId, courseId);

            return await clsProgressData.GetCourseProgress(userId, courseId);
        }

        public static async Task EnsureEnrolledForLesson(int userId, int lessonId)
        {
            // Access lookup must work for course lessons too — GetLessonById
            // only returns independent lessons (CourseId IS NULL).
            DtoLessons? lesson = await clsLessonsData.GetLessonByIdAny(lessonId);

            if (lesson is null)
                throw new NotFoundException("There is no lesson with this Id");

            int productId;

            if (lesson.CourseId.HasValue)
            {
                clsCourse course = await clsCourse.Find(lesson.CourseId.Value);
                productId = course.ProductId;
            }
            else
            {
                productId = lesson.ProductId;
            }

            // IsUserEnrolled also recognizes bundle purchases covering the
            // course, so bundle buyers can study its lessons.
            if (!await clsEnrollment.IsUserEnrolled(userId, productId))
                throw new ForbiddenException("You are not enrolled in this product");
        }

        public static async Task EnsureEnrolledForCourse(int userId, int courseId)
        {
            clsCourse course = await clsCourse.Find(courseId);

            if (!await clsEnrollment.IsUserEnrolled(userId, course.ProductId))
                throw new ForbiddenException("You are not enrolled in this course");
        }

        public static async Task<bool> IsCourseComplated(int userId, int courseId)
        {
            CourseProgressViewModel courseProgress = await GetCourseProgress(userId, courseId);
            return courseProgress.ProgressPercentage == 100;
        }

        // ------------------------------------------------------------
        // Certificate storage: configurable via CERTIFICATES_STORAGE_PATH
        // so deployments can point at a persistent volume. Defaults to a
        // 'certificates' folder under the current working directory.
        // Only the certificate CODE is stored/returned to clients — file
        // paths never leave the server, and file names are always derived
        // from a validated DB code (no user-controlled path segments).
        // ------------------------------------------------------------
        public static string CertificatesDirectory =>
            Environment.GetEnvironmentVariable("CERTIFICATES_STORAGE_PATH")
            ?? Path.Combine(Directory.GetCurrentDirectory(), "certificates");

        private static string PdfPathFor(string certificateCode)
            => Path.Combine(CertificatesDirectory, $"{certificateCode}.pdf");

        private static void SavePdf(CertificateDocument document, string certificateCode)
        {
            Directory.CreateDirectory(CertificatesDirectory);

            string pdfPath = PdfPathFor(certificateCode);

            document.GeneratePdf(pdfPath);
        }
        private static byte[] CreateQrImage(string content)
        {
            using QRCodeGenerator generator = new();

            using QRCodeData data =
                generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);

            PngByteQRCode qr = new(data);

            byte[] bytes = qr.GetGraphic(20);
            return bytes;
        }
        /// <summary>
        /// Full certificate lifecycle (idempotent):
        ///   1. eligibility — enrollment + 100% completion (also re-verified
        ///      inside SP_IssueCertificate, server-side);
        ///   2. one certificate row per (user, course) — repeated calls return
        ///      the SAME certificate code;
        ///   3. PDF generated once per code into the configured storage path;
        ///   4. the code is verifiable via GET /api/certificates/{code}.
        /// Throws ConflictException when the course is not completed and
        /// surfaces storage failures as InvalidOperationException (500).
        /// </summary>
        public static async Task<DtoCertificate> IssueCertificate(int userId, int courseId)
        {
            bool isCompleted = await IsCourseComplated(userId, courseId);
            if (!isCompleted)
                throw new ConflictException("Can't issue a certificate because the user hasn't completed the course");

            DtoCertificate? certificate =
                await clsCertificatesData.IssueCertificate(userId, courseId);

            if (certificate is null)
                throw new ConflictException("Failed to issue certificate");

            string pdfPath = PdfPathFor(certificate.CertificateCode);

            if (!File.Exists(pdfPath))
            {
                clsCourse course = await clsCourse.Find(courseId);
                clsUser student = await clsUser.Find(userId);

                string frontendBase =
                    Environment.GetEnvironmentVariable("FRONTEND_URL")?.TrimEnd('/');

                // QR content must be a MEANINGFUL, verifiable URL. The public
                // verification endpoint resolves the code back to real data.
                string verificationUrl = string.IsNullOrWhiteSpace(frontendBase)
                    ? $"api/certificates/{certificate.CertificateCode}"
                    : $"{frontendBase}/verify/{certificate.CertificateCode}";

                byte[] qrImageBytes = CreateQrImage(verificationUrl);

                CertificateDocument document = new CertificateDocument(
                    student.Name,
                    course.Name,
                    certificate.IssuedAt.ToString("yyyy-MM-dd"),
                    certificate.CertificateCode,
                    "",
                    "",
                    "",
                    qrImageBytes);

                SavePdf(document, certificate.CertificateCode);
            }

            return certificate;
        }

        /// <summary>
        /// Retrieves a certificate by its code through the Data Access layer.
        /// The API layer must resolve certificates via this method — never
        /// by calling clsCertificatesData directly.
        /// </summary>
        public static async Task<DtoCertificate?> GetCertificateByCode(string certificateCode)
        {
            return await clsCertificatesData.GetByCode(certificateCode);
        }
    }
}