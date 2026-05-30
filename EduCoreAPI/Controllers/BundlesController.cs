using Common.Dtos;
using Common.Exceptions;
using Common.ViewModels;
using EduCore_BusinessLayer;
using EduCoreAPI.Helpers.Dtos.RequestDto;
using EduCoreAPI.Helpers.Mappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EduCoreAPI.Controllers
{
    [Route("api/bundles")]
    [ApiController]
    public class BundlesController : ControllerBase
    {
        // =========================
        // GET: All Bundles
        // =========================
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<List<BundleViewModel>>> GetAllBundles()
        {
            var bundles = await clsBundle.GetBundlesView();
            return Ok(bundles);
        }

        // =========================
        // GET: Bundle With Items
        // =========================
        [AllowAnonymous]
        [HttpGet("{id:int}/items")]
        public async Task<ActionResult<BundleItemsViewModel>> GetBundleItems([FromRoute] short id)
        {
            var bundle = await clsBundle.GetBundleItems(id);

            if (bundle is null)
                throw new NotFoundException("Bundle not found");

            return Ok(bundle);
        }

        // =========================
        // GET: by id
        // =========================
        [AllowAnonymous]
        [HttpGet("{id:int}")]
        public async Task<ActionResult> GetBundleById([FromRoute] int id)
        {
            clsBundle bundle = await clsBundle.Find(id);
           
            return Ok(bundleMapper.ToBundleResponse(bundle));
        }

        // =========================
        // GET: Bundle Exists
        // =========================
        [AllowAnonymous]
        [HttpGet("{id:int}/exists")]
        public async Task<ActionResult> BundleExists([FromRoute] int id)
        {
            var exists = await clsBundle.IsBundleExist(id);
            return Ok(new { exists });
        }

        // =========================
        // POST: Create Bundle
        // =========================
        [Authorize(Roles = "Instructor,Admin,SuperAdmin")]
        [HttpPost]
        public async Task<ActionResult> CreateBundle([FromBody] BundleRequest request)
        {
            var newBundle = new clsBundle();

            newBundle.SetName(request.Name);
            newBundle.SetBasePrice(request.BasePrice);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int authenticatedStudentId = int.Parse(userId);

            await newBundle.AssignCreatedByUserAsync(authenticatedStudentId);

            if (request.Summary is not null)
                newBundle.SetSummary(request.Summary);

            if (request.ThumbnailUrl is not null)
                newBundle.SetThumbnailUrl(request.ThumbnailUrl);

            bool result = await newBundle.Save();

            if (!result)
                throw new ConflictException("Failed to create bundle");

            return CreatedAtAction(
                nameof(GetBundleById),
                new { id = newBundle.Id },
                new
                {
                    newBundle.Id,
                    newBundle.Name,
                    newBundle.BasePrice
                });
        }

        // =========================
        // PUT: Update Bundle
        // =========================
        [HttpPut("{id:int}")]
        public async Task<ActionResult> UpdateBundle(
            [FromRoute] int id,
            [FromBody] BundleRequest request)
        {
            clsBundle bundle = await clsBundle.Find(id);

            if (request.Name is not null)
                bundle.SetName(request.Name);

            if (request.BasePrice > 0)
                bundle.SetBasePrice(request.BasePrice);

            if (request.Summary is not null)
                bundle.SetSummary(request.Summary);

            if (request.ThumbnailUrl is not null)
                bundle.SetThumbnailUrl(request.ThumbnailUrl);

            bool result = await bundle.Save();

            if (!result)
                throw new ConflictException("Failed to update bundle");

            return Ok(bundleMapper.ToBundleResponse(bundle));
        }

        // =========================
        // DELETE: Bundle
        //// =========================
        //[HttpDelete("{id:int}")]
        //public async Task<ActionResult> DeleteBundle([FromRoute] int id)
        //{
        //    clsBundle bundle = await clsBundle.Find(id);

        //    bool result = await bundle.();

        //    if (!result)
        //        throw new ConflictException("Failed to delete bundle");

        //    return Ok(bundleMapper.ToBundleResponse(bundle));
        //}

        // =========================
        // POST: Add Item To Bundle
        // =========================
        [HttpPost("{id:int}/items")]
        public async Task<ActionResult> AddItemToBundle(
            [FromRoute] int id,
            [FromBody] int courseId)
        {
            if (!await clsBundle.IsBundleExist(id))
                throw new NotFoundException("Bundle not found");

            bool result = await clsBundle.AddItemToBundle(new DtoBundleItem
            {
                BundleId = id,
                CourseId = courseId
            });

            if (!result)
                throw new ConflictException("Failed to add item to bundle");

            return Ok(new { success = result });
        }

        // =========================
        // DELETE: Remove Item From Bundle
        // =========================
        [HttpDelete("{id:int}/items/{courseId:int}")]
        public async Task<ActionResult> DeleteItemFromBundle(
            [FromRoute] int id,
            [FromRoute] int courseId)
        {
            if (!await clsBundle.IsBundleExist(id))
                throw new NotFoundException("Bundle not found");

            bool result = await clsBundle.DeleteItemFromBundle(new DtoBundleItem
            {
                BundleId = id,
                CourseId = courseId
            });

            if (!result)
                throw new ConflictException("Failed to remove item from bundle");

            return Ok(new { success = result });
        }
    }
}