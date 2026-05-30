using BCrypt.Net;
using Common.Dtos;
using Common.ViewModels;
using EduCore_DataAccess;
using Common.Utils;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Common.Enums;
using Common;
using System.ComponentModel.DataAnnotations;
using Common.Exceptions;
namespace EduCore_BusinessLayer
{
    enum enMode { Add, Update };

    public class clsUser
    {
         DtoUser _userData;

        

        enMode _Mode;
        
        public clsUser()
        {
            _userData = new DtoUser();
            _Mode = enMode.Add;
        }
        private clsUser(DtoUser user) { 
            _userData = user;
            _Mode = enMode.Update;
        }

        // getters 

        public int Id  => _userData.Id;
        public string Email => _userData.Email;
        public string Name => _userData.Name;
        public DateTime CreatedAt => _userData.CreatedAt;
        public DateTime BirthDate => _userData.BirthDate;
        public DateTime? RefreshTokenExpiresAt => _userData.RefreshTokenExpiresAt;
        public DateTime? RefreshTokenRevokedAt => _userData.RefreshTokenRevokedAt;
        public List<string> Roles => _userData.Roles;

        // setters 


        public void SetEmail(string email)
            {
                
        
                _userData.Email = clsValidation.ValidateEmail(email);
            }
        
         public void SetName(string name)
         {
            string validName = clsValidation.ValidateString(name, "name");
             // اسم قوي لكن مرن: أحرف Unicode + مسافات + شرطة + apostrophe + نقطة
             if (!Regex.IsMatch(validName, @"^[\p{L}\p{M}][\p{L}\p{M}\s'\-\.]{1,99}$"))
                 throw new Exception("Invalid name format");
        
             _userData.Name = validName;
         }
        
         public void SetBirthDate(DateTime date)
         {
            
             _userData.BirthDate = clsValidation.ValidateBirthDate(date);
         }
        
         public void SetPassword(string password)
         {
            string validPassword = clsValidation.ValidatePassword(password);
             _userData.PasswordHash = BCrypt.Net.BCrypt.HashPassword(validPassword, workFactor: 12);
         }
        
         public bool VerifyPassword(string password)
         {
             if (string.IsNullOrWhiteSpace(_userData.PasswordHash))
                 return false;
        
             return BCrypt.Net.BCrypt.Verify(password, _userData.PasswordHash);
         }
         public void SetRefreshToken(string refreshToken)
         {
             if (string.IsNullOrWhiteSpace(refreshToken))
                 throw new ValidationException("Refresh token is required");
        
             if (refreshToken.Length < 32)
                 throw new ValidationException("Refresh token is too short");
        
             _userData.RefreshTokenHash = BCrypt.Net.BCrypt.HashPassword(refreshToken, workFactor: 12);
         }
        
         public bool VerifyRefreshToken(string refreshToken)
         {
             if (string.IsNullOrWhiteSpace(_userData.RefreshTokenHash))
                 return false;
        
             return BCrypt.Net.BCrypt.Verify(refreshToken, _userData.RefreshTokenHash);
         }
        
         public void SetRefreshRevokedAt(DateTime? date)
         {
            if (date == null)
            {
                _userData.RefreshTokenRevokedAt = null;
            }
            else
            {
                if (date > DateTime.UtcNow.AddMinutes(1))
                    throw new ValidationException("Invalid revoked date");

                _userData.RefreshTokenRevokedAt = DateTime.SpecifyKind((DateTime)date, DateTimeKind.Utc);
            }
         }
        
         public void SetRefreshExpiredAt(DateTime date)
         {
             if (date <= DateTime.UtcNow)
                 throw new ValidationException("Expiration must be in the future");
        
             _userData.RefreshTokenExpiresAt = DateTime.SpecifyKind(date, DateTimeKind.Utc);
         }
    

        
        public static async Task<clsUser> Find(int userId)
        {
            DtoUser DtoUser = await clsUsersData.GetUserById(userId);

            if (DtoUser == null)
                throw new NotFoundException("There is no user with this id");

            clsUser user = new clsUser(DtoUser);
            return user;            
        }

        public static async Task<clsUser> Find(string email)
        {
            DtoUser dtoUser = await clsUsersData.GetUserByEmail(email);
            if (dtoUser == null)
                throw new UnauthorizedAccessException("Invalid credentials");

            clsUser user = new clsUser(dtoUser);
            return user;
        }

        private void ValidateForAdd()
        {
            if (_userData == null)
                throw new ValidationException("User data is missing");

            if (string.IsNullOrWhiteSpace(_userData.Name))
                throw new ValidationException("Name is required");

            if (string.IsNullOrWhiteSpace(_userData.Email))
                throw new ValidationException("Email is required");

            if (_userData.Email.Length > 254)
                throw new ValidationException("Email is too long");

            if (_userData.BirthDate == null)
                throw new ValidationException("BirthDate is required");

            if (string.IsNullOrWhiteSpace(_userData.PasswordHash))
                throw new ValidationException("Password is required");

            if (string.IsNullOrWhiteSpace(_userData.RefreshTokenHash))
                throw new ValidationException("Refresh token is required");

            if (_userData.RefreshTokenExpiresAt == null)
                throw new ValidationException("Refresh token expiration is required");

        }


        async Task<bool> _AddUser(string? ip, string? userAgent)
        {
            return await clsGeneralData.ExecuteTransaction(async(conn, tx) =>
            {
                int userId = await clsUsersData.AddUser(_userData, conn, tx);

                if (userId <= 0)
                    throw new ConflictException("User creation failed");

                _userData.Id = userId;


                bool roleAdded = await clsUserRolesData.AddRoleToUser(userId,enRoles.Student, conn, tx);

                if (!roleAdded)
                    throw new ConflictException("Failed to assign role");

                await clsAudit.LogAsync(userId,
                                        enAuditActionType.CreateUser, 
                                        "User",
                                        userId,
                                        $"New user created: {_userData.Email}",ip,userAgent,
                                        conn,
                                        tx);
                _Mode = enMode.Update;
                return true;
            });
        }
        async Task<bool> _UpdateUser(string? ip, string? userAgent)
        {

            bool result = await clsUsersData.UpdateUser(_userData);

            if (!result)
                throw new ConflictException("Update failed");

            await clsAudit.LogAsync(
                Id,
                enAuditActionType.UpdateUser,
                "User",
                _userData.Id,
                $"User {_userData.Id} updated",ip, userAgent);
            return result;
        }

        public async Task<bool> Save(string? ipAddress, string? userAgent)
        {
            switch (_Mode)
            {
                case enMode.Add:
                    ValidateForAdd();
                    if (await clsUsersData.IsEmailExist(_userData.Email))
                        throw new ConflictException("Email already exists");
                    return await _AddUser(ipAddress,userAgent);
                case enMode.Update:
                    return await _UpdateUser(ipAddress,userAgent);
                default: return false;
            }
        }

        public static async Task<bool> IsEmailExist( string email)
        {
            return await clsUsersData.IsEmailExist(email);
        }

        public static async Task<bool> DeleteUser(int id, int actionByUserId)
        {
            bool result = await clsUsersData.DeactivateUser(id);

            if (!result)
                throw new ConflictException("Failed to delete user");

            await clsAudit.LogAsync(
                actionByUserId,
                enAuditActionType.DeleteUser,
                "User",
                id,
                $"User {id} was deleted/deactivated");

            return result;
        }

        public static async Task<List<UsersViewModel>> GetAllStudents(int pageNumber,int pageSize, bool IncludeNonActive = false)
        {
            if (IncludeNonActive)
                return await clsUsersData.GetAllUsersIncludeNonActive(pageNumber, pageSize, enRoles.Student);
            else
                return await clsUsersData.GetAllUsers(pageNumber, pageSize, enRoles.Student);
        }

        public static async Task<List<UsersViewModel>> GetAllInstructor(int pageNumber, int pageSize, bool IncludeNonActive = false)
        {
            if (IncludeNonActive)
                return await clsUsersData.GetAllUsersIncludeNonActive(pageNumber, pageSize, enRoles.Instructor);
            else
                return await clsUsersData.GetAllUsers(pageNumber, pageSize, enRoles.Instructor);
        }

        public static async Task<List<UsersViewModel>> GetAllAdmin(int pageNumber, int pageSize, bool IncludeNonActive = false)
        {
            if (IncludeNonActive)
                return await clsUsersData.GetAllUsersIncludeNonActive(pageNumber, pageSize, enRoles.Admin);
            else
                return await clsUsersData.GetAllUsers(pageNumber, pageSize, enRoles.Admin);
        }

    }
}
