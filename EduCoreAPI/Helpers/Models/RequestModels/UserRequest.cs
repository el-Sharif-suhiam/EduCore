using System;
using System.Collections.Generic;
using System.Text;

namespace EduCoreAPI.Helpers.Dtos.RequestDto
{
    public record UserRequest
    (
        string? Name,
        DateTime? BirthDate,
        string? Email,
        string? Password

    );

}
