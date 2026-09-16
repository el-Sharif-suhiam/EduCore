using Common.ViewModels;
using EduCore_BusinessLayer;

namespace EduCoreAPI.Helpers.Mappers
{
    public class bundleMapper
    {
        public static BundleViewModel ToBundleResponse(clsBundle bundle)
        {
            return new BundleViewModel
            {
                Id = bundle.Id,
                BasePrice = bundle.BasePrice,
                CreatedAt = bundle.CreatedAt,
                Name = bundle.Name,
                ProductId = bundle.ProductId,
                Summary = bundle.Summary,
                ThumbnailUrl = bundle.ThumbnailUrl,
                IsPublished = bundle.IsPublished,
            };
        }
    }
}
