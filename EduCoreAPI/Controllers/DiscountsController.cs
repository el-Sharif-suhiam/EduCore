using Common.Dtos;
using Common.Exceptions;
using EduCore_BusinessLayer;
using EduCoreAPI.Helpers.Mappers;
using EduCoreAPI.Helpers.Models.RequestModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EduCoreAPI.Controllers
{
    [Route("api/discount-codes")]
    [ApiController]
    public class DiscountCodesController : ControllerBase
    {
        // =========================
        // GET: Valid Discount Codes
        // =========================
        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpGet("valid")]
        public async Task<ActionResult<List<DtoDiscountCode>>> GetValidDiscountCodes()
        {
            var codes = await clsDiscountCode.GetValidDiscountCodes();
            return Ok(codes);
        }

        // =========================
        // GET: by id
        // =========================
        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpGet("{id:int}")]
        public async Task<ActionResult> GetDiscountCodeById([FromRoute] short id)
        {
            clsDiscountCode code = await clsDiscountCode.Find(id);
            return Ok(DiscountCodeMapper.ToDiscountCodeResponse(code));
        }

        // =========================
        // GET: by code
        // =========================
        [Authorize]
        [HttpGet]
        public async Task<ActionResult> GetDiscountCodeByCode([FromQuery] string discountCode)
        {
            clsDiscountCode code = await clsDiscountCode.Find(discountCode);
            return Ok(DiscountCodeMapper.ToDiscountCodeResponse(code));
        }



        // =========================
        // GET: Is Code Valid
        // =========================
        [Authorize]
        [HttpGet("{id:int}/is-valid")]
        public async Task<ActionResult> IsCodeValid([FromRoute] short id)
        {
            var isValid = await clsDiscountCode.IsCodeValid(id);
            return Ok(new { isValid });
        }

        // =========================
        // POST: Create Discount Code
        // =========================
        [Authorize(Roles = "SuperAdmin")]
        [HttpPost]
        public async Task<ActionResult> CreateDiscountCode([FromBody] DiscountCodeRequest request)
        {
            var newCode = new clsDiscountCode();

            newCode.SetCode(request.DiscountCode);
            newCode.SetDiscountRate(request.DiscountRate);
            newCode.SetExpireAt(request.ExpireAt);
            newCode.SetAllowedUseNumber(request.AllowedUseNumber);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int adminId = int.Parse(userId);


            
            await newCode.AssiganCreatedByIdAsync(adminId);

            bool result = await newCode.Save();

            if (!result)
                throw new ConflictException("Failed to create discount code");

            return CreatedAtAction(
                nameof(GetDiscountCodeById),
                new { id = newCode.Id },
                new
                {
                    newCode.Id,
                    newCode.Code,
                    newCode.DiscountRate,
                    newCode.ExpireAt,
                    newCode.IsUnlimited
                });
        }

        // =========================
        // PUT: Update Discount Code
        // =========================
        [Authorize(Roles = "SuperAdmin")]
        [HttpPut("{id:int}")]
        public async Task<ActionResult> UpdateDiscountCode(
            [FromRoute] short id,
            [FromBody] DiscountCodeRequest request)
        {
            clsDiscountCode code = await clsDiscountCode.Find(id);

            if (request.DiscountCode is not null)
                code.SetCode(request.DiscountCode);

            if (request.DiscountRate is not null)
                code.SetDiscountRate(request.DiscountRate);

            if (request.ExpireAt is not null)
                code.SetExpireAt(request.ExpireAt);

            if (request.AllowedUseNumber is not null)
                code.SetAllowedUseNumber(request.AllowedUseNumber);

            bool result = await code.Save();

            if (!result)
                throw new ConflictException("Failed to update discount code");

            return Ok(new
            {
                code.Id,
                code.Code,
                code.DiscountRate,
                code.ExpireAt,
                code.AllowedUseNumber,
                code.IsUnlimited,
                code.IsValid
            });
        }

        // =========================
        // DELETE: Discount Code
        // =========================
        [Authorize(Roles = "SuperAdmin")]
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> DeleteDiscountCode([FromRoute] short id)
        {
            clsDiscountCode code = await clsDiscountCode.Find(id);

            bool result = await code.Delete();

            if (!result)
                throw new ConflictException("Failed to delete discount code");

            return Ok(new { success = result });
        }
    }
}