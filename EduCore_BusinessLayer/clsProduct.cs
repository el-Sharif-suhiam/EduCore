using Common.Dtos;
using Common.Enums;
using Common.Exceptions;
using Common.Utils;
using EduCore_DataAccess;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace EduCore_BusinessLayer
{
    public class clsProduct
    {
        private DtoProduct _productData;
        private enMode _Mode;

        public int Id => _productData.Id;
        public enProductType ProductType => (enProductType)_productData.ProductType;
        public string Name => _productData.Name;
        public DateTime CreatedAt => _productData.CreatedAt;
        public DateTime UpdatedAt => _productData.UpdatedAt;
        public decimal BasePrice => _productData.BasePrice;
        public clsUser CreatedByAdmin { get; private set; }
        public string? ThumbnailUrl => _productData.ThumbnailUrl;
        public bool IsPublished => _productData.IsPublished;
        public string? Summary => _productData.Summary;

        public clsProduct()
        {
            _productData = new DtoProduct();
            _Mode = enMode.Add;
        }

        private clsProduct(DtoProduct product)
        {
            _productData = product;
            _Mode = enMode.Update;

            CreatedByAdmin = clsUser.Find(product.CreatedByAdmin);
        }

        public static clsProduct FromDto(DtoProduct dto)
        {
            if (dto == null)
                return null;

            return new clsProduct(dto);
        }

        public DtoProduct ToDto()
        {
            return new DtoProduct
            {
                Id = _productData.Id,
                ProductType = _productData.ProductType,
                Name = _productData.Name,
                CreatedAt = _productData.CreatedAt,
                UpdatedAt = _productData.UpdatedAt,
                BasePrice = _productData.BasePrice,
                CreatedByAdmin = _productData.CreatedByAdmin,
                ThumbnailUrl = _productData.ThumbnailUrl,
                Summary = _productData.Summary,
                IsPublished = _productData.IsPublished
            };
        }

        public void SetName(string name)
        {
            _productData.Name = clsValidation.ValidateString(name,"name", 150);
        }

        public void SetProductType(enProductType productType)
        {
            _productData.ProductType = (byte)productType;
        }

        public void SetBasePrice(decimal price)
        {

            _productData.BasePrice = clsValidation.ValidatePrice(price);
        }

        public void SetThumbnailUrl(string? url)
        {
            _productData.ThumbnailUrl = clsValidation.ValidateUrl(url, "thumbnail Url");
        }

        public void SetCreatedByAdmin(int adminId)
        { 
            if (clsUsersRoles.IsUserAdmin(clsValidation.ValidatePositiveInt(adminId, "Admin Id")))
            {
                _productData.CreatedByAdmin = adminId;
                CreatedByAdmin = clsUser.Find(adminId);
            }
            
        }

        public void SetSummary(string summary)
        {
            _productData.Summary = clsValidation.ValidateString(summary, "Summary");
        }

        public bool Publish()
        {
            if (_productData.Id <= 0)
                throw new ValidationException("Invalid Id number");



            if (clsProductsData.PublishProduct(_productData.Id))
            {
                _productData.IsPublished = true;
                _productData.UpdatedAt = DateTime.UtcNow;
                return true;
            }
            else
                throw new ConflictException("something wrong happend when try to pulish the product");

        }

        public bool Unpublish()
        {
            if (_productData.Id <= 0)
                throw new ValidationException("Invalid Id number");



            if (clsProductsData.UnPublishProduct(_productData.Id))
            {
                _productData.IsPublished = false;
                _productData.UpdatedAt = DateTime.UtcNow;
                return true;
            }
            else
                throw new ConflictException("something wrong happend with unpulishing the product");
            
        }

        private void ValidateForAdd()
        {
            if (_productData == null)
                throw new ValidationException("Product data is missing");

            if (_productData.CreatedByAdmin <= 0)
                throw new ValidationException("CreatedByAdmin is required");

            if (string.IsNullOrWhiteSpace(_productData.Name))
                throw new ValidationException("Name is required");

            if (_productData.ProductType == 0)
                throw new ValidationException("Product type is required");

            if (_productData.BasePrice <= 0)
                throw new ValidationException("Base price is required");
        }

        private void ValidateForUpdate()
        {
            if (_productData == null)
                throw new ValidationException("Product data is missing");

            if (_productData.Id <= 0)
                throw new ValidationException("Invalid product id");

            if (string.IsNullOrWhiteSpace(_productData.Name))
                throw new ValidationException("Name is required");

            if (_productData.BasePrice <= 0)
                throw new ValidationException("Base price is required");
        }

        private bool _AddProduct()
        {
            int productId = clsProductsData.AddProduct(_productData);

            if (productId > 0)
            {
                _productData.Id = productId;
                _Mode = enMode.Update;
                return true;
            }

            return false;
        }

        private bool _UpdateProduct()
        {
            _productData.UpdatedAt = DateTime.UtcNow;
            return clsProductsData.UpdateProduct(_productData);
        }

        public bool Save()
        {
            switch (_Mode)
            {
                case enMode.Add:
                    ValidateForAdd();
                    return _AddProduct();

                case enMode.Update:
                    ValidateForUpdate();
                    return _UpdateProduct();

                default:
                    return false;
            }
        }

        public static clsProduct Find(int productId)
        {
            if (productId <= 0)
                throw new ValidationException("Product id is not valid");
           
            DtoProduct dto = clsProductsData.GetProductById(productId);
            if (dto == null)
                throw new NotFoundException("There is no product with this Id");
            return new clsProduct(dto);
        }
    }
}