using Common.Dtos;
using Common.Enums;
using Common.Exceptions;
using Common.Utils;
using EduCore_DataAccess;
using Microsoft.Data.SqlClient;

namespace EduCore_BusinessLayer
{
    public static class clsAudit
    {
        public static async Task<int> LogAsync(
     int userId,
     enAuditActionType actionType,
     string entityType,
     int entityId,
     string description,
     string? ipAddress = null,
     string? userAgent = null,
     SqlConnection? conn = null,
     SqlTransaction? tx = null)
        {
            clsValidation.ValidatePositiveInt(userId, "UserId");

            description = clsValidation.ValidateString(
                description,
                "Description",
                300);

            DtoAudit audit = new DtoAudit
            {
                UserId = userId,
                ActionType = actionType,
                EntityType = entityType,
                EntityId = entityId,
                Description = description,
                IpAddress = ipAddress,
                UserAgent = userAgent
            };

            int id = await clsAuditsData.AddAuditAsync(
                audit,
                conn,
                tx);

            if (id <= 0)
                throw new ConflictException("Failed to add audit log.");

            return id;
        }
        public static async Task<DtoAudit> FindAsync(int id)
        {
            clsValidation.ValidatePositiveInt(id, "Id");

            DtoAudit? audit = await clsAuditsData.GetAuditByIdAsync(id);

            if (audit == null)
                throw new NotFoundException("Audit not found.");

            return audit;
        }

        public static Task<List<DtoAudit>> GetAllAsync(int pageNumber, int pageSize)
        {
            return clsAuditsData.GetAllAuditsAsync(pageNumber, pageSize);
        }

        public static Task<List<DtoAudit>> GetByUserAsync(int userId, int pageNumber, int pageSize)
        {
            clsValidation.ValidatePositiveInt(userId, "UserId");
            return clsAuditsData.GetAuditsByUserAsync(userId, pageNumber, pageSize);
        }
    }
}