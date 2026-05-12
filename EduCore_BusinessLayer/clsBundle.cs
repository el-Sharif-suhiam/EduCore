using Common.Dtos;
using Common.Enums;
using Common.Exceptions;
using Common.Utils;
using Common.ViewModels;
using EduCore_DataAccess;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EduCore_BusinessLayer
{
    public class clsBundle
    {
        enMode _Mode;
        clsProduct _Product;
        DtoBundle _BundleData;

        public int Id => _BundleData.Id;
        public string Name => _Product.Name;
        public DateTime CreatedAt => _Product.CreatedAt;
        public DateTime UpdatedAt => _Product.UpdatedAt;
        public decimal BasePrice => _Product.BasePrice;
        public clsUser CreatedByAdmin => _Product.CreatedByAdmin;
        public string? ThumbnailUrl => _Product.ThumbnailUrl;
        public bool IsPublished => _Product.IsPublished;
        public string? Summary => _Product.Summary;


        public clsBundle()
        {
            _BundleData = new DtoBundle();
            _Product = new clsProduct();
            _Product.SetProductType(enProductType.Bundle);
            _Mode = enMode.Add;
        }

        private clsBundle(DtoBundle bundle)
        {
            clsProduct product = clsProduct.Find(bundle.ProductId);
            if (product is null)
                throw new NotFoundException("There is no product for this Id");

            _Product = product;
            _BundleData = bundle;
            _Mode = enMode.Update;
        }


        public void SetName(string name)
            => _Product.SetName(name);

        public void SetBasePrice(decimal basePrice)
            => _Product.SetBasePrice(basePrice);

        public void SetThumbnailUrl(string thumbnailUrl)
            => _Product.SetThumbnailUrl(thumbnailUrl);

        public void SetCreatedByAdmin(int adminId)
            => _Product.SetCreatedByAdmin(adminId);

        public void SetSummary(string? summary)
            => _Product.SetSummary(summary);


        public static clsBundle Find(int bundleId)
        {
            if (bundleId <= 0)
                throw new ValidationException("Bundle id is not valid");

            DtoBundle dtoBundle = clsBundlesData.GetBundleById(bundleId);

            if (dtoBundle is null)
                throw new NotFoundException("There is no bundle with this Id");

            return new clsBundle(dtoBundle);
        }


        private void _ValidateForAdd()
        {
            if (_BundleData is null || _Product is null)
                throw new Exception("Bundle data is missing");

            if (CreatedByAdmin.Id <= 0)
                throw new Exception("CreatedByAdmin is required");

            if (string.IsNullOrWhiteSpace(Name))
                throw new Exception("Name is required");

            if (BasePrice <= 0)
                throw new Exception("Base price is required");
        }

        private void _ValidateForUpdate()
        {
            if (_BundleData is null || _Product is null)
                throw new Exception("Bundle data is missing");

            if (Id <= 0)
                throw new Exception("Invalid bundle id");

            if (_Product.Id <= 0)
                throw new Exception("Invalid product id");

            if (string.IsNullOrWhiteSpace(Name))
                throw new Exception("Name is required");

            if (BasePrice <= 0)
                throw new Exception("Base price is required");
        }


        private bool _AddBundle()
        {
            return clsGeneralData.ExecuteTransaction((conn, tx) =>
            {
                int productId = clsProductsData.AddProduct(_Product.ToDto(), conn, tx);

                if (productId <= 0)
                    throw new Exception("Product creation failed");

                _BundleData.ProductId = productId;
                int bundleId = clsBundlesData.AddBundle(_BundleData, conn, tx);

                if (bundleId <= 0)
                    throw new Exception("Bundle creation failed");

                _Mode = enMode.Update;
                return true;
            });
        }

        private bool _UpdateBundle()
        {
            return clsGeneralData.ExecuteTransaction((conn, tx) =>
            {
                DtoProduct oldProduct = clsProductsData.GetProductById(_BundleData.ProductId);

                if (oldProduct is null)
                    throw new Exception("Product not found");

                bool productChanged = clsCompare.IsProductChanged(oldProduct, _Product.ToDto());

                if (productChanged)
                {
                    bool updatedProduct = clsProductsData.UpdateProduct(_Product.ToDto(), conn, tx);

                    if (!updatedProduct)
                        throw new Exception("Failed to update product");
                }

                bool updatedBundle = clsBundlesData.UpdateBundle(_BundleData, conn, tx);

                if (!updatedBundle)
                    throw new Exception("Failed to update bundle");

                return true;
            });
        }

        public bool Save()
        {
            switch (_Mode)
            {
                case enMode.Add:
                    _ValidateForAdd();
                    return _AddBundle();

                case enMode.Update:
                    _ValidateForUpdate();
                    return _UpdateBundle();

                default:
                    return false;
            }
        }


       

        public static List<DtoBundle> GetAllBundles()
            => clsBundlesData.GetAllBundles();

        public static BundleItemsViewModel GetBundleItems(short bundleId)
            => clsBundleItemsData.GetBundleWithCourses(bundleId);
        
        public static bool IsBundleExist(int bundleId)
            => clsBundlesData.BundleExists(bundleId);

        public static List<BundleViewModel> GetBundlesView()
            => clsBundlesData.GetAllBundlesView();

        public static bool AddItemToBundle(DtoBundleItem item)
            => clsBundleItemsData.AddItemToBundle(item);

        public static bool DeleteItemFromBundle(DtoBundleItem item)
            => clsBundleItemsData.DeleteItemFromBundle(item);
    }
}