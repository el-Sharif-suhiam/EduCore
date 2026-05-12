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
        public DateTime CreatedAt => _Product.CreatedAt;
        public DateTime UpdatedAt => _Product.UpdatedAt;
        public decimal BasePrice => _Product.BasePrice;
        public clsUser CreatedByAdmin => _Product.CreatedByAdmin;
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
            clsProduct product = clsProduct.Find(course.ProductId);
            if (product == null)
                throw new NotFoundException("There is no product for this Id");
            _Product = product;
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

        public void SetCreatedByAdmin(int AdminId)
        {
            _Product.SetCreatedByAdmin(AdminId);
        }

        public void SetSummary(string summary)
        {
            _Product.SetSummary(summary);
        }

        // Course setters 
        public void SetCoverImageUrl(string? url)
        {
           _CourseData.CoverImageUrl = clsValidation.ValidateUrl(url, "cover image Url");
        }

        public static clsCourse Find(int courseId, bool includeDeleted = false)
        {
            if (courseId <= 0)
                throw new ValidationException("course id is not valid");
            DtoCourse dtoCourse = includeDeleted ? clsCoursesData.GetCourseByIdIncludeDeleted(courseId) : clsCoursesData.GetCourseById(courseId);
            if (dtoCourse == null)
                throw new NotFoundException("There is no course with this Id");

            clsCourse course = new clsCourse(dtoCourse);
            return course;
        }

        private void _ValidateForAdd()
        {
            if (_CourseData == null || _Product == null)
                throw new Exception("Course data is missing");

            if (CreatedByAdmin.Id <= 0)
                throw new Exception("CreatedByAdmin is required");

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

        bool _AddCourse()
        {
            return clsGeneralData.ExecuteTransaction((conn, tx) =>
            {

                int productId = clsProductsData.AddProduct(_Product.ToDto(),conn,tx);


                if (productId <= 0)
                    throw new Exception("Product creation failed");

                _CourseData.ProductId = productId;
                int courseId = clsCoursesData.AddCourse(_CourseData,conn,tx);

                if (courseId <= 0)
                    throw new Exception("Course creation failed");


                _Mode = enMode.Update;
                return true;
            });

        }

        bool _UpdateCourse()
        {
            return clsGeneralData.ExecuteTransaction((conn, tx) =>
            {
                DtoProduct oldProduct = clsProductsData.GetProductById(_CourseData.ProductId);

                if (oldProduct == null)
                    throw new Exception("Product not found");

                bool productChanged = clsCompare.IsProductChanged(oldProduct,_Product.ToDto());

                if (productChanged)
                {
                    bool updatedProduct = clsProductsData.UpdateProduct(_Product.ToDto(), conn, tx);

                    if (!updatedProduct)
                        throw new Exception("Failed to update product");
                }

                bool updatedCourse = clsCoursesData.UpdateCourse(_CourseData, conn, tx);

                if (!updatedCourse)
                    throw new Exception("Failed to update course");

                return true;
            });
        }
        
        public bool Save()
        {
            switch (_Mode)
            {
                case enMode.Add:
                    _ValidateForAdd();
                    return _AddCourse();

                case enMode.Update:
                    _ValidateForUpdate();
                    return _UpdateCourse();

                default:
                    return false;
            }
        }

        private bool ControlDelete(int adminId,bool UnDelete = false)
        {
            if (!clsUsersRoles.IsUserAdmin(adminId))
                throw new ConflictException("this user is not permitted to delete course");

            if (_Product.Unpublish())
            {
                return UnDelete ? clsCoursesData.UnDelete(Id,adminId) : clsCoursesData.DeleteCourse(Id, adminId);
            }
            return false;
        }

        public bool UnDelete(int adminId)
        {
            return ControlDelete(adminId,true);
        }
        public bool Delete(int adminId)
        {
            return ControlDelete(adminId);
        }
        public static List<DtoCourse> GetAllCourses(int pageNumber,int pageSize, bool evenDeletedIncluded = false)
        {
            if (evenDeletedIncluded)
                return clsCoursesData.GetAllCoursesIncludeDeleted(pageNumber, pageSize);
            else
                return clsCoursesData.GetAllCourses(pageNumber, pageSize);
        }

        public static List<CourseWithInstructorViewModel> GetAllCoursesWithInstructors(int pageNumber,int pageSize)
        {
                return clsCoursesData.GetAllCoursesWithInstructorViewModel(pageNumber, pageSize);
        }

        public static bool IsCourseExist(int courseId) {
            return clsCoursesData.CourseExists(courseId);
        }
    }
}
