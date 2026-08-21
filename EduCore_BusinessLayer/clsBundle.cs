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
        public int ProductId => _BundleData.ProductId;
        public DateTime CreatedAt => _Product.CreatedAt;
        public DateTime UpdatedAt => _Product.UpdatedAt;
        public decimal BasePrice => _Product.BasePrice;
        public clsUser CreatedByUser => _Product.CreatedByUser;
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
          
            _BundleData = bundle;
            _Mode = enMode.Update;
        }


        public void SetName(string name)
            => _Product.SetName(name);

        public void SetBasePrice(decimal basePrice)
            => _Product.SetBasePrice(basePrice);

        public void SetThumbnailUrl(string thumbnailUrl)
            => _Product.SetThumbnailUrl(thumbnailUrl);

        public async Task AssignCreatedByUserAsync(int AdminId)
        {
            await _Product.SetCreatedByUser(AdminId);
        }

        public void SetSummary(string? summary)
            => _Product.SetSummary(summary);

        public async Task<bool> PublishBundle()
            => await _Product.Publish();

        public async Task<bool> UnPublishBundle()
            => await _Product.Unpublish();
        public static async Task<clsBundle> Find(int bundleId)
        {
            if (bundleId <= 0)
                throw new ValidationException("Bundle id is not valid");

            DtoBundle dtoBundle = await clsBundlesData.GetBundleById(bundleId);

            if (dtoBundle is null)
                throw new NotFoundException("There is no bundle with this Id");
            clsBundle bundle = new clsBundle(dtoBundle);
            clsProduct product = await clsProduct.Find(bundle.ProductId);
            if (product is null)
                throw new NotFoundException("There is no product for this Id");

            bundle._Product = product;
            return bundle;
        }



        private void _ValidateForAdd()
        {
            if (_BundleData is null || _Product is null)
                throw new Exception("Bundle data is missing");

            if (CreatedByUser.Id <= 0)
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


        private async Task<bool> _AddBundle()
        {
            return await clsGeneralData.ExecuteTransaction(async(conn, tx) =>
            {
                int productId = await clsProductsData.AddProduct(_Product.ToDto(), conn, tx);

                if (productId <= 0)
                    throw new Exception("Product creation failed");

                _BundleData.ProductId = productId;
                _Product.SetId(productId);
                int bundleId = await clsBundlesData.AddBundle(_BundleData, conn, tx);

                if (bundleId <= 0)
                    throw new Exception("Bundle creation failed");
                _BundleData.Id = bundleId;

                string auditMessage = "Create Bundle";


                await clsAudit.LogAsync(
                                CreatedByUser.Id,
                                enAuditActionType.CreateBundle,
                                "Bundle",
                                Id,
                                auditMessage);
                
                _Mode = enMode.Update;
                return true;
            });
        }

        private async Task<bool> _UpdateBundle()
        {
            return await clsGeneralData.ExecuteTransaction(async(conn, tx) =>
            {
                DtoProduct oldProduct = await clsProductsData.GetProductById(_BundleData.ProductId);

                if (oldProduct is null)
                    throw new Exception("Product not found");

                bool productChanged = clsCompare.IsProductChanged(oldProduct, _Product.ToDto());

                if (productChanged)
                {
                    bool updatedProduct = await clsProductsData.UpdateProduct(_Product.ToDto(), conn, tx);

                    if (!updatedProduct)
                        throw new Exception("Failed to update product");
                }

                bool updatedBundle = await clsBundlesData.UpdateBundle(_BundleData, conn, tx);

                if (!updatedBundle)
                    throw new Exception("Failed to update bundle");

                string auditMessage = "Update Bundle";


                await clsAudit.LogAsync(
                                CreatedByUser.Id,
                                enAuditActionType.UpdateBundle,
                                "Bundle",
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
                    return await _AddBundle();

                case enMode.Update:
                    _ValidateForUpdate();
                    return await _UpdateBundle();

                default:
                    return false;
            }
        }


       

        public static async Task<List<DtoBundle>> GetAllBundles()
            => await clsBundlesData.GetAllBundles();

        public static async Task<BundleItemsViewModel> GetBundleItems(short bundleId)
            => await clsBundleItemsData.GetBundleWithCourses(bundleId);
        
        public static async Task<bool> IsBundleExist(int bundleId)
            => await clsBundlesData.BundleExists(bundleId);

        public static async Task<List<BundleViewModel>> GetBundlesView()
            => await clsBundlesData.GetAllBundlesView();

        public static async Task<bool> AddItemToBundle(DtoBundleItem item)
            => await clsBundleItemsData.AddItemToBundle(item);

        public static async Task<bool> DeleteItemFromBundle(DtoBundleItem item)
            => await clsBundleItemsData.DeleteItemFromBundle(item);
    }
}