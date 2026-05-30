using System;
using System.Collections.Generic;
using System.Text;

namespace EduCoreAPI.Helpers.Dtos.RequestDto
{
    public record PageRequest
    (
        int PageNumber,
        int PageSize
    );
}
