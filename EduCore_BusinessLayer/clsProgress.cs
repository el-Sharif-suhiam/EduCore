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
            clsProgress progress = new clsProgress(userId, lessonId);
            progress.SetComplete(true);
            return await progress.Save();
        }

        public static async Task<bool> MarkAsIncomplete(int userId, int lessonId)
        {
            clsProgress progress = new clsProgress(userId, lessonId);
            progress.SetComplete(false);
            return await progress.Save();
        }

        public static async Task<CourseProgressViewModel> GetCourseProgress(int userId, int courseId)
        {
            if (!(await clsCoursesData.CourseExists(courseId)))
                throw new NotFoundException("Course not found");

            return await clsProgressData.GetCourseProgress(userId, courseId);
        }

        public static async Task<bool> IsCourseComplated(int userId, int courseId)
        {
            CourseProgressViewModel courseProgress = await GetCourseProgress(userId, courseId);
            return courseProgress.ProgressPercentage == 100;
        }

        //private static void SavePdf(CertificateDocument document,string fileName)
        //{
        //    try
        //    {
        //        string folder = Path.Combine(AppContext.BaseDirectory, "certificates");
        //        Directory.CreateDirectory(folder);

        //        string pdfFile = Path.Combine(folder, $"{fileName}.pdf");

        //        document.GeneratePdf(pdfFile);
        //        ////this will get the current project directory folder.
        //        //string currentDir = System.IO.Directory.GetCurrentDirectory();
        //        //string DirFullName = currentDir + "\\certificates";
        //        //bool isDirExist = Directory.Exists(DirFullName);

        //        ////incase the username is empty, delete the file
        //        //if (!isDirExist)
        //        //    Directory.CreateDirectory(DirFullName);

        //        //document.GeneratePdf(DirFullName+ $"\\{fileName}.pdf");
                
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception($"An error occurred: {ex.Message}");
        //    }
        //}

        private static void SavePdf(CertificateDocument document, string certificateCode)
        {
            string currentDir = Directory.GetCurrentDirectory();

            string dir = Path.Combine(currentDir, "certificates");

            Directory.CreateDirectory(dir);

            string pdfPath = Path.Combine(dir, $"{certificateCode}.pdf");

            document.GeneratePdf(pdfPath);
        }
        private static byte[] CreateQrImage(string content)
        {
            string currentDir = Directory.GetCurrentDirectory();

            string qrDirectory = Path.Combine(currentDir, "certificates", "qr");

            Directory.CreateDirectory(qrDirectory);

            string filePath = Path.Combine(qrDirectory, $"{Guid.NewGuid()}.png");

            using QRCodeGenerator generator = new();

            using QRCodeData data =
                generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);

            PngByteQRCode qr = new(data);

            byte[] bytes = qr.GetGraphic(20);
            return bytes;
            //File.WriteAllBytes(filePath, bytes);

            //return filePath;
        }
        public static async Task IssueCertificate(int userId, int courseId,string certificateEndPoint)
        {
            bool isComplated = await IsCourseComplated(userId, courseId);
            if (!isComplated)
                throw new ConflictException("Can't issue a certificate because user doesn't complate the course");

            clsCourse course = await clsCourse.Find(courseId);

            clsUser student = await clsUser.Find(userId);

            Guid certificateGuid = Guid.NewGuid();

            string certificateUrl =
                $"{certificateEndPoint.TrimEnd('/')}/{certificateGuid}";

            byte[] qrImageBytes = CreateQrImage(certificateUrl);

            CertificateDocument document =
                new CertificateDocument(
                    student.Name,
                    course.Name,
                    DateTime.UtcNow.ToString("yyyy-MM-dd"),
                    certificateGuid.ToString(),
                    "",
                    "",
                    "",
                    qrImageBytes);

            SavePdf(document, certificateGuid.ToString());


            //clsCourse course = await clsCourse.Find(courseId);
            //clsUser strudentData = await clsUser.Find(userId);
            //var InstructorData = await clsCoursesInstructors.GetAllCourseInstructor(courseId);
            //Guid certificateGuid = Guid.NewGuid();
            //string logoPath = "";
            //string QrPath = certificateEndPoint + certificateGuid.ToString();
            //CertificateDocument certificate = new CertificateDocument(strudentData.Name, course.Name, DateTime.UtcNow.ToString(), certificateGuid.ToString(), logoPath, "", "", QrPath);

            //SavePdf(certificate, certificateGuid.ToString());


        }
        //////////////////////////////////////////////////////// تذكير بكتابة منطق لكتابة الشهادة
        /// اول شيء كلاس يراجع هل اكمل الكورس 
        /// بعدها جدول فيه الشهادات بالتاريخ واسم الشهادة ب uuid 
        /// وكلاس لانشاء ملف pdf فيه بيانات الشهادة
    }
}