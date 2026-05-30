using EduCore_BusinessLayer;
using EduCoreAPI.Helpers.Dtos.ResponeDto;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduCoreAPI.Helpers.Mappers
{
    public class userMapper
    {
        public static UserResponse ToUserRespone(clsUser user)
        {
            return new UserResponse
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                BirthDate = user.BirthDate,
                CreatedDate = user.CreatedAt,
                Roles = user.Roles,

            };
        }
    }
}
