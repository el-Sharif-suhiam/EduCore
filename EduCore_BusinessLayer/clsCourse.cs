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
    public class clsCourse
    {
        enMode _Mode;
        clsProduct _Product;
        DtoCourse _CourseData;
        public int Id => _CourseData.Id;
        public string Name => _Product.Name;
        public int ProductId => _CourseData.ProductId;
        public DateTime CreatedAt => _Product.CreatedAt;
        public DateTime UpdatedAt => _Product.UpdatedAt;
        public decimal BasePrice => _Product.BasePrice;
        public clsUser CreatedByUser=> _Product.CreatedByUser;
        public string? ThumbnailUrl => _Product.ThumbnailUrl;
        public bool IsPublished => _Product.IsPublished;
        public string? Summary => _Product.Summary;
        public string? CoverImageUrl => _CourseData.CoverImageUrl;
        public bool IsDeleted => _CourseData.IsDeleted;
        public DateTime? DeletedAt => _CourseData.DeletedAt;
        public int? DeletedById => _CourseData.DeletedById;


        public clsCourse()
        {
            _CourseData = new DtoCourse();
            _Product = new clsProduct();
            _Product.SetProductType(enProductType.Course);
            _Mode = enMode.Add;
        }

        private clsCourse(DtoCourse course)
        {
            _Product = new clsProduct();
            _CourseData = course;
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

        public async Task<bool> PublishCourse()
            => await _Product.Publish();

        public async Task<bool> UnPublishCourse()
            => await _Product.Unpublish();

        // Course setters 
        public void SetCoverImageUrl(string? url)
        {
           _CourseData.CoverImageUrl = clsValidation.ValidateUrl(url, "cover image Url");
        }

        static async Task<clsCourse> InternalFind(DtoCourse dtoCourse)
        {

            if (dtoCourse is null)
                throw new NotFoundException("There is no course with this Id");

            clsCourse course = new clsCourse(dtoCourse);
            clsProduct product = await clsProduct.Find(dtoCourse.ProductId);

            if (product is null)
                throw new NotFoundException("There is no product for this Id");


            course._Product = product;
            return course;
        }
        public static async Task<clsCourse> Find(int courseId, bool includeDeleted = false)
        {
            if (courseId <= 0)
                throw new ValidationException("course id is not valid");

            DtoCourse dtoCourse = includeDeleted ? await clsCoursesData.GetCourseByIdIncludeDeleted(courseId) : await clsCoursesData.GetCourseById(courseId);

           return await InternalFind(dtoCourse);
        }

        public static async Task<clsCourse> FindByProductId(int productId)
        {
            if (productId <= 0)
                throw new ValidationException("course id is not valid");

            DtoCourse dtoCourse = await clsCoursesData.GetCoursebyProductId(productId);
            return await InternalFind(dtoCourse);

        }
        private void _ValidateForAdd()
        {
            if (_CourseData == null || _Product == null)
                throw new Exception("Course data is missing");

            if (CreatedByUser.Id <= 0)
                throw new Exception("CreatedByUser is required");

            if (string.IsNullOrWhiteSpace(Name))
                throw new Exception("Name is required");

            if (BasePrice <= 0)
                throw new Exception("Base price is required");
        }

        private void _ValidateForUpdate()
        {
            if (_CourseData == null || _Product == null)
                throw new Exception("Course data is missing");
        
            if (Id <= 0)
                throw new Exception("Invalid course id");
            if (_Product.Id <= 0)
                throw new Exception("Invalid product id");

            if (string.IsNullOrWhiteSpace(Name))
                throw new Exception("Name is required");

            if (BasePrice <= 0)
                throw new Exception("Base price is required");
        }

        async Task<bool> _AddCourse()
        {
            return await clsGeneralData.ExecuteTransaction(async (conn, tx) =>
            {

                int productId = await clsProductsData.AddProduct(_Product.ToDto(),conn,tx);


                if (productId <= 0)
                    throw new Exception("Product creation failed");
                
                _Product.SetId(productId);
                _CourseData.ProductId = productId;
                int courseId = await clsCoursesData.AddCourse(_CourseData,conn,tx);

                if (courseId <= 0)
                    throw new Exception("Course creation failed");

                _CourseData.Id = courseId;


                string auditMessage = "Create course";


                await clsAudit.LogAsync(
                                CreatedByUser.Id,
                                enAuditActionType.CreateCourse,
                                "Course",
                                Id,
                                auditMessage);
                

                _Mode = enMode.Update;
                return true;
            });

        }

        async Task<bool> _UpdateCourse()
        {
            return await clsGeneralData.ExecuteTransaction(async (conn, tx) =>
            {
                DtoProduct oldProduct = await clsProductsData.GetProductById(_CourseData.ProductId);

                if (oldProduct == null)
                    throw new Exception("Product not found");

                bool productChanged = clsCompare.IsProductChanged(oldProduct,_Product.ToDto());

                if (productChanged)
                {
                    bool updatedProduct = await clsProductsData.UpdateProduct(_Product.ToDto(), conn, tx);

                    if (!updatedProduct)
                        throw new Exception("Failed to update product");
                }

                bool updatedCourse = await clsCoursesData.UpdateCourse(_CourseData, conn, tx);

                if (!updatedCourse)
                    throw new Exception("Failed to update course");



                string auditMessage = "Update course";


                await clsAudit.LogAsync(
                                CreatedByUser.Id,
                                enAuditActionType.UpdateCourse,
                                "Course",
                                Id,
                                auditMessage);
                return true;
            });
        }
        
        public async Task<bool> Save()
        {
            switch (_Mode)
            {
                case enMode.Add:
                    _ValidateForAdd();
                    return await _AddCourse();

                case enMode.Update:
                    _ValidateForUpdate();
                    return await  _UpdateCourse();

                default:
                    return false;
            }
        }

        private async Task<bool> ControlDelete(int adminId,bool UnDelete = false)
        {
            if (!(await clsUsersRoles.IsUserAdmin(adminId)))
                throw new ConflictException("this user is not permitted to delete course");

            return await clsGeneralData.ExecuteTransaction(async (conn, tx) =>
            {
                if (UnDelete)
                {
                    return await clsCoursesData.UnDelete(Id, adminId, conn, tx);
                }

                bool unpublished = await _Product.Unpublish(conn, tx);

                if (!unpublished)
                    return false;

                return await clsCoursesData.DeleteCourse(Id, adminId, conn, tx);
            });

            
        }

        public async Task<bool> UnDelete(int adminId)
        {


            bool result = await ControlDelete(adminId,true);


            if (result)
            {
                string auditMessage = "UnDelete course";


                await clsAudit.LogAsync(
                                CreatedByUser.Id,
                                enAuditActionType.UnDeleteCourse,
                                "Course",
                                Id,
                                auditMessage);
            }
            return result;
        }
        public async Task<bool> Delete(int adminId)
        {
            bool result = await ControlDelete(adminId);

            if (result)
            {
                string auditMessage = "Delete course";

                await clsAudit.LogAsync(
                                CreatedByUser.Id,
                                enAuditActionType.DeleteCourse,
                                "Course",
                                Id,
                                auditMessage);
            }
            return result;
        }
        public static async Task<List<DtoCourse>> GetAllCourses(int pageNumber,int pageSize, bool evenDeletedIncluded = false)
        {
            if (evenDeletedIncluded)
                return await clsCoursesData.GetAllCoursesIncludeDeleted(pageNumber, pageSize);
            else
                return await clsCoursesData.GetAllCourses(pageNumber, pageSize);
        }

        public static async Task<List<CourseWithInstructorViewModel>> GetAllCoursesWithInstructors(int pageNumber,int pageSize, bool evenDeletedIncluded = false, string searchText = "")
        {
            if (!evenDeletedIncluded)
                return await clsCoursesData.GetAllCoursesWithInstructorViewModel(pageNumber, pageSize,searchText);
            else
                return await clsCoursesData.GetAllCoursesWithInstructorViewModelWithDeleted(pageNumber, pageSize,searchText);
        }

        public static async Task<bool> IsCourseExist(int courseId) {
            return await clsCoursesData.CourseExists(courseId);
        }

        public async Task<bool> AssignLessonToCourse(int lessonId)
        {
            return await clsCoursesData.AssignLessonToCourse(Id, lessonId);
        }
    }
}
