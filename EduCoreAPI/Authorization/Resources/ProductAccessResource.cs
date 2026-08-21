using Common.Enums;

namespace EduCoreAPI.Authorization.Resources
{
    public class ProductAccessResource
    {
        public int LessonId { get; set; } = 0;
        public int CourseId { get; set; } = 0;
        public int BundleId { get; set; } = 0;
        public enProductType Type { get; set; }

    }
}
