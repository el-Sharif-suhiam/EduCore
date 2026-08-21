using Common.Dtos;
using Common.Enums;
using Common.Exceptions;
using Common.Utils;
using Common.ViewModels;
using EduCore_DataAccess;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
namespace EduCore_BusinessLayer
{
    public class clsLesson
    {
        enMode _Mode;
        clsProduct _Product;
        DtoLessons _LessonsData;
        public int Id => _LessonsData.Id;
        public string Name => _Product.Name;
        public int ProductId => _LessonsData.ProductId;
        public DateTime CreatedAt => _Product.CreatedAt;
        public DateTime UpdatedAt => _Product.UpdatedAt;
        public decimal BasePrice => _Product.BasePrice;
        public clsUser CreatedByUser => _Product.CreatedByUser;
        public string? ThumbnailUrl => _Product.ThumbnailUrl;
        public bool IsPublished => _Product.IsPublished;
        public string? Summary => _Product.Summary;

        public string Title => _LessonsData.Title;

        public string? VideoUrl => _LessonsData.VideoUrl;

        public string? BodyText => _LessonsData.BodyText;

        public bool IsDeleted => _LessonsData.IsDeleted;

        public DateTime? DeletedAt => _LessonsData.DeletedAt;

        public int? DeletedById => _LessonsData.DeletedById;

        public int InstructorId => _LessonsData.InstructorId;
        public int? CourseId => _LessonsData.CourseId;
        
        public clsLesson()
        {
            _LessonsData = new DtoLessons();
            _Product = new clsProduct();
            _Product.SetProductType(enProductType.Lesson);
            _Mode = enMode.Add;

        }

        private clsLesson(DtoLessons lesson)
        {
            _Product = new clsProduct();
            _LessonsData = lesson;
            _Mode = enMode.Update;
        }

        // setters

        // product setter 
        public void SetName(string name)
        {
            _Product.SetName(name);
        }

        public void SetBasePrice(decimal basePrice)
        {
            _Product.SetBasePrice(basePrice);
        }
        public void SetThumbnailUrl(string thumbnailUrl)
        {
            _Product.SetThumbnailUrl(thumbnailUrl);
        }

        public async Task AssignCreatedByUserAsync(int AdminId)
        {
            await _Product.SetCreatedByUser(AdminId);
        }

        public void SetSummary(string summary)
        {
            _Product.SetSummary(summary);
        }

        // Lesson setters 

        public void SetTitle(string title)
        {
            _LessonsData.Title = clsValidation.ValidateString(title, "Title", 150);
        }

        public void SetVideoUrl(string videoUrl)
        {
            _LessonsData.VideoUrl = clsValidation.ValidateUrl(videoUrl, "Video Url");
        }

        public void SetBodyText(string bodyText)
        {
            _LessonsData.BodyText = clsValidation.ValidateString(bodyText, "Body Text");
        }

        public async Task SetInstructorToLesson(int? instructorId)
        {

            bool result = await clsUsersRoles.IsUserInstructorOrSuperAdmin((int)instructorId);
            if (!result)
                throw new NotFoundException("This user may not exist or it's don't have the permission");

            _LessonsData.InstructorId = clsValidation.ValidatePositiveInt((int)instructorId, "instructor id");
        }
        public async Task SetCourseId(int? courseId)
        {
            if (courseId is null)
                _LessonsData.CourseId = null;
            else
            {
                bool result = await clsCourse.IsCourseExist((int)courseId);
                if (!result)
                    throw new NotFoundException("There is no course with id");

                _LessonsData.CourseId = clsValidation.ValidatePositiveInt((int)courseId, "course Id");
            }
        }

        private void _ValidateForAdd()
        {
            if (_LessonsData == null || _Product == null)
                throw new ValidationException("Lesson data is missing");

            if (CreatedByUser.Id <= 0)
                throw new ValidationException("CreatedByUser is required");

            if (string.IsNullOrWhiteSpace(Name))
                throw new ValidationException("Name is required");
            if (string.IsNullOrEmpty(Title))
                throw new ValidationException("Title is missing"); 
            if (BasePrice < 0)
                throw new ValidationException("Base price is required");
        }

        private void _ValidateForUpdate()
        {
            if (Id <= 0)
                throw new ValidationException("Invalid Lesson id");
            if (_Product.Id <= 0)
                throw new ValidationException("Invalid product id");

            if (string.IsNullOrWhiteSpace(Name))
                throw new ValidationException("Name is required");
            if (string.IsNullOrEmpty(Title))
                throw new ValidationException("Title is missing");

            if (BasePrice < 0)
                throw new ValidationException("Base price is required");
        }
       
        private static async Task<clsLesson> InternalFind(DtoLessons dtoLesson)
        {
            if (dtoLesson == null)
                throw new NotFoundException("There is no lesson with this Id");

            clsLesson lesson = new clsLesson(dtoLesson);
            clsProduct product = await clsProduct.Find(lesson.ProductId);
            if (product == null)
                throw new NotFoundException("Invalid product Id");
            lesson._Product = product;
            return lesson;
        }
        public static async Task<clsLesson> Find(int lessonId, bool includeDeleted = false)
        {
            if (lessonId <= 0)
                throw new ValidationException("Lesson id is not valid");

            DtoLessons dtoLesson = includeDeleted ? await clsLessonsData.GetLessonByIdIncludeDeleted(lessonId) : await clsLessonsData.GetLessonById(lessonId);
            return await InternalFind(dtoLesson);
        }
        public static async Task<clsLesson> FindbyProductId(int productId)
        {
            if (productId <= 0)
                throw new ValidationException("Lesson id is not valid");

            DtoLessons dtoLesson =  await clsLessonsData.GetLessonByProductId(productId);
            return await InternalFind(dtoLesson);
        }

        public static async Task<clsLesson> FindWithCourseId(int lessonId, int courseId, bool includeDeleted = false)
        {
            DtoLessons dtoLesson = await clsLessonsData.GetLessonWithCourseById(lessonId, courseId);
            return await InternalFind(dtoLesson);
        }




        async Task<bool> _AddLesson()
        {
            return await clsGeneralData.ExecuteTransaction(async (conn, tx) =>
            {

                int productId = await clsProductsData.AddProduct(_Product.ToDto(), conn, tx);


                if (productId <= 0)
                    throw new ConflictException("Product creation failed");

                _LessonsData.ProductId = productId;
                int lessonId = await clsLessonsData.AddLesson(_LessonsData, conn, tx);

                if (lessonId <= 0)
                    throw new ConflictException("Lesson creation failed");

                _LessonsData.Id = lessonId;

                string auditMessage = string.Empty;
                if (CourseId != null)
                    auditMessage = $"Created a new lesson associated with course id: { CourseId}";
                else
                    auditMessage = "Created independent lesson";


                await clsAudit.LogAsync(
                                CreatedByUser.Id,
                                enAuditActionType.CreateLesson,
                                "Lesson",
                                lessonId,
                                auditMessage,
                                null,null,
                                conn,tx);
                _Mode = enMode.Update;
                return true;
            });


        }

        async Task<bool> _UpdateLesson()
        {
            return await clsGeneralData.ExecuteTransaction(async(conn, tx) =>
            {
                DtoProduct oldProduct = await clsProductsData.GetProductById(_LessonsData.ProductId);

                if (oldProduct == null)
                    throw new NotFoundException("Product not found");

                bool productChanged = clsCompare.IsProductChanged(oldProduct,_Product.ToDto());

                if (productChanged)
                {
                    bool updatedProduct = await clsProductsData.UpdateProduct(_Product.ToDto(), conn, tx);

                    if (!updatedProduct)
                        throw new ConflictException("Failed to update product");
                }

                bool updatedLesson = await clsLessonsData.UpdateLesson(_LessonsData, conn, tx);

                if (!updatedLesson)
                    throw new ConflictException("Failed to update lesson");


                string auditMessage = "Update independent lesson";

                if (CourseId == null)
                    auditMessage = $"Update a lesson associated with course id: {CourseId}";


                await clsAudit.LogAsync(
                                CreatedByUser.Id,
                                enAuditActionType.UpdateLesson,
                                "Lesson",
                                _LessonsData.Id,
                                auditMessage,
                                null, null,
                                conn, tx);
                return true;
            });
        }

        public async Task<bool> Save()
        {
            switch (_Mode)
            {
                case enMode.Add:
                    _ValidateForAdd();
                    return await _AddLesson();

                case enMode.Update:
                    _ValidateForUpdate();
                    return await _UpdateLesson();

                default:
                    return false;
            }
        }

        private async Task<bool> ControlDelete(int adminId,bool UnDelete = false)
        {
            if (!(await clsUsersRoles.IsUserInstructorOrSuperAdmin(adminId)))
                throw new ConflictException("this user is not permitted to delete course");

            if (await _Product.Unpublish())
                return UnDelete ? await clsLessonsData.UnDeleteLesson(Id,adminId) : await clsLessonsData.DeleteLesson(Id, adminId);
            return false;
        }

        public async Task<bool> Delete(int adminId)
        {
            bool result = await ControlDelete(adminId);
            if (result)
            {

                string auditMessage = "Delete independent lesson";

                if (CourseId == null)
                    auditMessage = $"Delete alesson associated with course id: {CourseId}";


                await clsAudit.LogAsync(
                                CreatedByUser.Id,
                                enAuditActionType.CreateLesson,
                                "Lesson",
                                Id,
                                auditMessage);
            }

            return result;
        }

        public async Task<bool> UnDelete(int adminId)
        {
            bool result = await ControlDelete(adminId, true);
            if (result)
            {

                string auditMessage = "UnDelete independent lesson";

                if (CourseId == null)
                    auditMessage = $"UnDelete alesson associated with course id: {CourseId}";


                await clsAudit.LogAsync(
                                CreatedByUser.Id,
                                enAuditActionType.UnDeleteLesson,
                                "Lesson",
                                Id,
                                auditMessage);
            }

            return result;
        }
        public static async Task<List<LessonsWithOutCoursesViewModel>> GetIndependntLessons(int pageNumber,int pageSize, string searchText = "")
        {
            return await clsLessonsData.GetAllLessonsWithOutCourses(pageNumber, pageSize,searchText);
        }

        public static async Task<List<DtoLessons>> GetAllLessons(int pageNumber, int pageSize,bool includeDeleted = false)
        {
            if (includeDeleted)
                return await clsLessonsData.GetAllLessonsIncludeDeleted(pageNumber, pageSize);
            else
                return await clsLessonsData.GetAllLessons(pageNumber, pageSize);
        }

        public static async Task<List<LessonsByCourseViewModel>> GetLessonsByCourse(int courseId)
        {
            
            if (!(await clsCourse.IsCourseExist(courseId)))
                throw new NotFoundException("Course not found or has been deleted");
            return await clsLessonsData.GetLessonsByCourse(courseId);
        }

    }
}
