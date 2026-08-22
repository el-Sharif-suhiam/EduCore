using Common.Dtos;
using Common.Enums;
using Common.Exceptions;
using Common.Utils;
using EduCore_DataAccess;
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

        public bool IsUnlimited => AllowedUseNumber is null or 0;

        public bool IsExpired => ExpireAt.HasValue && ExpireAt.Value < DateTime.UtcNow;

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
            _DiscountData.DiscountCode =
                clsValidation.ValidateString(code, "DiscountCode");
        }

        public void SetDiscountRate(decimal? rate)
        {
            if (rate.HasValue && (rate <= 0 || rate > 100))
                throw new ValidationException("Discount rate must be between 1 and 100");

            _DiscountData.DiscountRate = rate;
        }

        public async Task AssiganCreatedByIdAsync(int adminId)
        {
            if (!(await clsUsersRoles.IsUserAdmin(adminId)))
                throw new NotFoundException("This user is not an Admin!");

            _DiscountData.CreatedById = adminId;
        }

        public void SetExpireAt(DateTime? expireAt)
        {
            if (expireAt.HasValue && expireAt.Value <= DateTime.UtcNow)
                throw new ValidationException("Expiry date must be in the future");

            _DiscountData.ExpireAt = expireAt;
        }

        public void SetAllowedUseNumber(short? allowedUse)
        {
            if (allowedUse.HasValue && allowedUse < 0)
                throw new ValidationException("Allowed use number cannot be negative");

            _DiscountData.AllowedUseNumber = allowedUse;
        }



        public static async Task<clsDiscountCode> Find(short id)
        {
            if (id <= 0)
                throw new ValidationException("Discount code id is not valid");

            DtoDiscountCode dto = await clsDiscountCodesData.GetDiscountCodeById(id);

            if (dto is null)
                throw new NotFoundException("No discount code found with this id");

            return new clsDiscountCode(dto);
        }

        public static async Task<clsDiscountCode> Find(string code)
        {

            string validString = clsValidation.ValidateString(code, "Dicount code");
            DtoDiscountCode dto = await clsDiscountCodesData.GetDiscountCodeByCode(validString);

            if (dto is null)
                throw new NotFoundException("No discount code found with this id");

            return new clsDiscountCode(dto);
        }
        private async Task<bool> _Add()
        {
            short newId =
                await clsDiscountCodesData.AddDiscountCode(_DiscountData);

            if (newId <= 0)
                throw new Exception("Failed to add discount code");

            _DiscountData.Id = newId;
            _Mode = enMode.Update;

            await clsAudit.LogAsync(
                _DiscountData.CreatedById,
                enAuditActionType.CreateDiscount,
                "DiscountCode",
                newId,
                $"Created discount code '{Code}'");

            return newId > 0;
        }

        private async Task<bool> _Update()
        {
            bool result = await clsDiscountCodesData.UpdateDiscountCode(_DiscountData);
            if (!result)
                throw new Exception("Failed to update discount code");

            await clsAudit.LogAsync(
                _DiscountData.CreatedById,
                enAuditActionType.UpdateDiscount,
                "DiscountCode",
                Id,
                $"Updated discount code '{Code}'");

            return result;
        }


        public async Task<bool> Save()
        {
            switch (_Mode)
            {
                case enMode.Add:
                    return await _Add();

                case enMode.Update:
                    return await _Update();

                default:
                    return false;
            }
        }

        public async Task<bool> Delete()
        {
            bool result =
                await clsDiscountCodesData.DeleteDiscountCode(Id);

            if (!result)
                return false;

            await clsAudit.LogAsync(
                CreatedById,
                enAuditActionType.DeleteDiscount,
                "DiscountCode",
                Id,
                $"Deleted discount code '{Code}'");

            return result;
        }

        public static async Task<List<DtoDiscountCode>> GetValidDiscountCodes() 
            => await clsDiscountCodesData.GetValidDiscountCodes(); 
        
        public static async Task<bool> IsCodeValid(short id) { 
            DtoDiscountCode dto = await clsDiscountCodesData.GetDiscountCodeById(id);
            if (dto is null) 
                return false;
            
            return new clsDiscountCode(dto).IsValid; 
        }
        // باقي الكود بدون تغيير...
    }
}

