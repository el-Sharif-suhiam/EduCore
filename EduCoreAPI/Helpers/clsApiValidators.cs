using EduCoreAPI.Helpers.Dtos.RequestDto;
using System.ComponentModel.DataAnnotations;

namespace EduCoreAPI.Helpers
{
    public class clsApiValidators
    {
        public static void ValidatePaging(PageRequest pageRequest)
        {
            if (pageRequest.PageNumber < 1)
                throw new ValidationException("PageNumber must be >= 1");

            if (pageRequest.PageSize < 5)
                throw new ValidationException("PageSize must be >= 5");
        }
    }
}
