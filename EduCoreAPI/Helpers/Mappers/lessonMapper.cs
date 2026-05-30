using EduCore_BusinessLayer;
using EduCoreAPI.Helpers.Dtos.ResponeDto;
using EduCoreAPI.Helpers.Dtos;
using EduCoreAPI.Helpers.Dtos.RequestDto;
namespace EduCoreAPI.Helpers.Mappers
{
    public class lessonMapper
    {
        public static LessonResponse ToLessonRespone(clsLesson lesson)
        {
            return new LessonResponse
            {
              Id = lesson.Id,
              Name = lesson.Name,
              BasePrice = lesson.BasePrice,
              Summary = lesson.Summary,
              BodyText = lesson.BodyText,
              CreatedAt = lesson.CreatedAt,
              ThumbnailUrl = lesson.ThumbnailUrl,
              Title = lesson.Title,
              VideoUrl = lesson.VideoUrl,
              InstructorId = lesson.InstructorId,
              CourseId = lesson.CourseId,
              DeletedAt = lesson.DeletedAt,
              IsDeleted = lesson.IsDeleted,
              UpdatedAt = lesson.UpdatedAt,
              IsPublished = lesson.IsPublished
            };
        }
    }
}
