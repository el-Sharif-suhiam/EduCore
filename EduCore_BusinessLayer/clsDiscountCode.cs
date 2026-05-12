using Common.Dtos;
using Common.Enums;
using Common.Exceptions;
using Common.Utils;
using EduCore_DataAccess;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EduCore_BusinessLayer
{
    public class clsDiscountCode
    {
        enMode _Mode;
        DtoDiscountCode _DiscountData;

        public short Id => _DiscountData.Id;
        public string Code => _DiscountData.DiscountCode;
        public decimal? DiscountRate => _DiscountData.DiscountRate;
        public int CreatedById => _DiscountData.CreatedById;
        public DateTime? ExpireAt => _DiscountData.ExpireAt;
        public short? AllowedUseNumber => _DiscountData.AllowedUseNumber;
        public short? TotalUsedNumber => _DiscountData.TotalUsedNumber;

        // كود غير محدود الاستخدام إذا كان AllowedUseNumber = 0
        public bool IsUnlimited => AllowedUseNumber == 0;

        public bool IsExpired => ExpireAt.HasValue && ExpireAt.Value < DateTime.Now;

        public bool IsValid =>
            !IsExpired &&
            (IsUnlimited || (AllowedUseNumber > TotalUsedNumber));


        public clsDiscountCode()
        {
            _DiscountData = new DtoDiscountCode();
            _Mode = enMode.Add;
        }

        private clsDiscountCode(DtoDiscountCode dto)
        {
            _DiscountData = dto;
            _Mode = enMode.Update;
        }


        public void SetCode(string code)
        {
            _DiscountData.DiscountCode = clsValidation.ValidateString(code, "DiscountCode");
        }

        public void SetDiscountRate(decimal? rate)
        {
            if (rate.HasValue && (rate <= 0 || rate > 100))
                throw new ValidationException("Discount rate must be between 1 and 100");

            _DiscountData.DiscountRate = rate;
        }

        public void SetCreatedById(int adminId)
        {
            if (!clsUsersRoles.IsUserAdmin(adminId))
                throw new NotFoundException("This user is not an Admin!");

            _DiscountData.CreatedById = adminId;
        }

        public void SetExpireAt(DateTime? expireAt)
        {
            if (expireAt.HasValue && expireAt.Value <= DateTime.Now)
                throw new ValidationException("Expiry date must be in the future");

            _DiscountData.ExpireAt = expireAt;
        }

        public void SetAllowedUseNumber(short? allowedUse)
        {
            if (allowedUse.HasValue && allowedUse < 0)
                throw new ValidationException("Allowed use number cannot be negative");

            // 0 = unlimited
            _DiscountData.AllowedUseNumber = allowedUse;
        }


        public static clsDiscountCode Find(short id)
        {
            if (id <= 0)
                throw new ValidationException("Discount code id is not valid");

            DtoDiscountCode dto = clsDiscountCodesData.GetDiscountCodeById(id);

            if (dto is null)
                throw new NotFoundException("No discount code found with this id");

            return new clsDiscountCode(dto);
        }


        private void _ValidateForAdd()
        {
            if (_DiscountData is null)
                throw new Exception("Discount code data is missing");

            if (string.IsNullOrWhiteSpace(Code))
                throw new ValidationException("Discount code is required");

            if (CreatedById <= 0)
                throw new ValidationException("CreatedById is required");

            if (DiscountRate.HasValue && (DiscountRate <= 0 || DiscountRate > 100))
                throw new ValidationException("Discount rate must be between 1 and 100");

            if (ExpireAt.HasValue && ExpireAt.Value <= DateTime.Now)
                throw new ValidationException("Expiry date must be in the future");

            if (AllowedUseNumber.HasValue && AllowedUseNumber < 0)
                throw new ValidationException("Allowed use number cannot be negative");
        }

        private void _ValidateForUpdate()
        {
            if (Id <= 0)
                throw new ValidationException("Invalid discount code id");

            _ValidateForAdd();
        }


        private bool _Add()
        {
            short newId = clsDiscountCodesData.AddDiscountCode(_DiscountData);

            if (newId <= 0)
                throw new Exception("Failed to add discount code");

            _DiscountData.Id = newId;
            _Mode = enMode.Update;
            return true;
        }

        private bool _Update()
        {
            if (!clsDiscountCodesData.UpdateDiscountCode(_DiscountData))
                throw new Exception("Failed to update discount code");

            return true;
        }

        public bool Save()
        {
            switch (_Mode)
            {
                case enMode.Add:
                    _ValidateForAdd();
                    return _Add();

                case enMode.Update:
                    _ValidateForUpdate();
                    return _Update();

                default:
                    return false;
            }
        }


        public bool Delete()
        {
            return clsDiscountCodesData.DeleteDiscountCode(Id);
        }


        public static List<DtoDiscountCode> GetValidDiscountCodes()
            => clsDiscountCodesData.GetValidDiscountCodes();

        public static bool IsCodeValid(short id)
        {
            DtoDiscountCode dto = clsDiscountCodesData.GetDiscountCodeById(id);

            if (dto is null)
                return false;

            return new clsDiscountCode(dto).IsValid;
        }
    }
}