using EduCore_BusinessLayer;
using EduCoreAPI.Helpers.Dtos.ResponeDto;

namespace EduCoreAPI.Helpers.Mappers
{
    public class CourseMapper
    {
        public static CourseResponse ToCourseRespone(clsCourse course)
        {
            return new CourseResponse
            {
                Id = course.Id,
                ProductId = course.ProductId,
                Name = course.Name,
                CreatedAt = course.CreatedAt,
                BasePrice = course.BasePrice,
                CoverImageUrl = course.CoverImageUrl,
                CreatedByUser = course.CreatedByUser.Name,
                IsPublished = course.IsPublished,
                Summary = course.Summary,
                ThumbnailUrl = course.ThumbnailUrl,
                UpdatedAt = course.UpdatedAt,
            };

        }
    }
}
