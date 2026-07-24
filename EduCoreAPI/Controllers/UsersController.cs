using Common.Exceptions;
using Common.Utils;
using Common.ViewModels;
using EduCore_BusinessLayer;
using EduCore_DataAccess;
using EduCoreAPI.Helpers;
using EduCoreAPI.Helpers.Dtos.RequestDto;
using EduCoreAPI.Helpers.Mappers;
using EduCoreAPI.Helpers.Models.RequestModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
namespace EduCoreAPI.Controllers
{
    [Route("api/users")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        // =========================
        // GET: Students
        // =========================
        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpGet("students")]
        public async Task<ActionResult<List<UsersViewModel>>> GetStudents(
            [FromQuery] PageRequest pageRequest,string? search)
        {
             clsApiValidators.ValidatePaging(pageRequest);

            var users = await clsUser.GetAllStudents(
                pageRequest.PageNumber,
                pageRequest.PageSize,false,search);

            return Ok(users);
        }

        // =========================
        // GET: Admins
        // =========================
        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpGet("admins")]
        public async Task<ActionResult<List<UsersViewModel>>> GetAdmins([FromQuery] PageRequest pageRequest,string? search)
        {
            clsApiValidators.ValidatePaging(pageRequest);

            var users = await clsUser.GetAllAdmin(
                pageRequest.PageNumber,
                pageRequest.PageSize,false,search);

            return Ok(users);
        }

        // =========================
        // GET: Instructors
        // =========================
        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpGet("instructors")]
        public async Task<ActionResult<List<UsersViewModel>>> GetInstructors(
            [FromQuery] PageRequest pageRequest, string? search)
        {
            clsApiValidators.ValidatePaging(pageRequest);

            var users = await clsUser.GetAllInstructor(
                pageRequest.PageNumber,
                pageRequest.PageSize,false,search);

            return Ok(users);
        }

        // =========================
        // GET: by id
        // =========================
        [Authorize]
        [HttpGet("{id:int}")]
        public async Task<ActionResult> GetUserById([FromRoute] int id, [FromServices] IAuthorizationService authorizationService)
        {


            var authResult = await authorizationService.AuthorizeAsync(
                User,
                id,
                "UserOwnerOrAdmin");

            if (!authResult.Succeeded)
                return Forbid(); // 403

            clsUser user = await clsUser.Find(id);

            return Ok(userMapper.ToUserRespone(user));

        }

        // =========================
        // GET: by email
        // =========================
        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpGet("by-email")]
        public async Task<ActionResult> GetByEmail([FromQuery] string email)
        {
            clsUser user = await clsUser.Find(email);

            return Ok(userMapper.ToUserRespone(user));
        }

        // =========================
        // POST: create student
        // =========================
        [AllowAnonymous]
        [HttpPost("students")]
        public async Task<ActionResult> CreateStudent([FromBody] UserRequest request)
        {
            var newUser = new clsUser();

            newUser.SetName(request.Name);
            newUser.SetEmail(request.Email);
            newUser.SetBirthDate((DateTime)request.BirthDate);
            newUser.SetPassword(request.Password);

            newUser.SetRefreshToken(Guid.NewGuid().ToString());
            newUser.SetRefreshExpiredAt(DateTime.UtcNow.AddHours(5));
            string ip = HttpContext.Connection.Id.ToString();
            string userAgent = HttpContext.Request.Headers.UserAgent.ToString();
            var result = await newUser.Save(ip,userAgent);

            if (!result)
                throw new ConflictException("Failed to create user");

            return CreatedAtAction(
                nameof(GetUserById),
                new { id = newUser.Id },
                new
                {
                    newUser.Id,
                    newUser.Name,
                    newUser.Email,
                    newUser.BirthDate
                });
        }

        // =========================
        // DELETE user
        // =========================
        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete([FromRoute] int id)
        {
            var user = await clsUser.Find(id);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int actionbyId = int.Parse(userId);


            var result = await clsUser.DeleteUser(id,actionbyId);

            if (!result)
                throw new ConflictException("Failed to delete user");

            return Ok(userMapper.ToUserRespone(user));
        }

        // =========================
        // CHECK EMAIL
        // =========================
        [AllowAnonymous]
        [HttpGet("email-exists")]
        public async Task<ActionResult> EmailExists([FromQuery] string email)
        {
            var exists = await clsUser.IsEmailExist(email);

            return Ok(new
            {
                exists
            });
        }

        // =========================
        // PUT: Update User
        // =========================
        [Authorize]
        [HttpPut("{id:int}", Name = "UpdateUser")]
        public async Task<ActionResult> UpdateUser([FromRoute]int id,[FromBody] UserUpdateRequest request, [FromServices] IAuthorizationService authorizationService)
        {
            var authResult = await authorizationService.AuthorizeAsync(
                User,
                id,
                "UserOwnerOnly");

            if (!authResult.Succeeded)
                return Forbid(); // 403

            clsUser user = await clsUser.Find(id);
            if(!string.IsNullOrEmpty(request.Name))
                user.SetName(request.Name);
            if(!string.IsNullOrEmpty(request.Email))
                user.SetEmail(request.Email);

            if (DateTime.TryParse(request.BirthDate,out DateTime newDate))
                user.SetBirthDate(newDate);

            string ip = HttpContext.Connection.Id.ToString();
            string userAgent = HttpContext.Request.Headers.UserAgent.ToString();
            bool result = await user.Save(ip,userAgent);
            if (!result)
                throw new ConflictException("updating the user is faild");

            return Ok(userMapper.ToUserRespone(user));
        }

        // =========================
        // PUT: Update User Password
        // =========================
        [Authorize]
        [HttpPut("{id:int}/password")]
        public async Task<ActionResult> UpdatePassword([FromRoute]int id,[FromBody] UpdatePasswordRequest updatePassword, 
            [FromServices] IAuthorizationService authorizationService)
        {
            var authResult = await authorizationService.AuthorizeAsync(
                User,
                id,
                "UserOwnerOnly");

            if (!authResult.Succeeded)
                return Forbid(); // 403

            clsUser user = await clsUser.Find(id);
            bool checkPassword = user.VerifyPassword(updatePassword.oldPassword);

            if (!checkPassword)
                throw new ConflictException("Your current password is not correct!");

            user.SetPassword(updatePassword.newPassword);
            string ip = HttpContext.Connection.Id.ToString();
            string userAgent = HttpContext.Request.Headers.UserAgent.ToString();
            bool result = await user.Save(ip,userAgent);

            if (!result)
                throw new ConflictException("Password updating is faild");

            return Ok(new { success = result });
        }



        // Get /users/me
        // Put /users/me

        // =========================
        // PUT: Update User Password
        // =========================

        //[HttpGet("/me")]
        //public async Task<ActionResult> Use


        //

        // =========================
        // POST: Change User Role To Instructor
        // =========================
        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpPost("toInstructor/{id:int}")]
        public async Task<ActionResult> AssaignUserAsInstructor([FromRoute] int id)
        {
           clsUser user = await clsUser.Find(id);


            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int actionbyId = int.Parse(userId);


            bool result = await clsUsersRoles.RegistInstructor(user.Id, actionbyId);
           if(!result)
                throw new ConflictException("something went wrong in resign the user as instructor");

            return Ok(new { success = result });
        }
        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpPost("toInstructor/{id:int}/remove")]
        public async Task<ActionResult> RemoveInstructor([FromRoute] int id) {


            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int actionbyId = int.Parse(userId);


            bool result = await clsUsersRoles.RemoveInstructor(id,actionbyId);
            if (!result)
                throw new ConflictException("something went wrong in while deleting instructor");

            return Ok(new { success = result });
        }

        // =========================
        // POST: Change User Role To Admin
        // =========================
        [Authorize(Roles = "SuperAdmin")]
        [HttpPost("toAdmin/{id:int}")]
        public async Task<ActionResult> AssaignUserAsAdmin([FromRoute] int id)
        {
            clsUser user = await clsUser.Find(id);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int actionbyId = int.Parse(userId);


            bool result = await clsUsersRoles.AddAdmin(user.Id, actionbyId);
            if (!result)
                throw new ConflictException("something went wrong in resign the user as admin");

            return Ok(new { success = result });
        }


        [Authorize(Roles = "SuperAdmin")]
        [HttpPost("toAdmin/{id:int}/remove")]
        public async Task<ActionResult> RemoveAdmin([FromRoute] int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int actionbyId = int.Parse(userId);

            bool result = await clsUsersRoles.RemoveAdmin(id, actionbyId);
            if (!result)
                throw new ConflictException("something went wrong in while deleting admin");

            return Ok(new { success = result });
        }

    }
}