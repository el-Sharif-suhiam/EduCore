using Common.Dtos;
using Common.Enums;
using Common.Exceptions;
using Common.Utils;
using EduCore_DataAccess;
using System.ComponentModel.DataAnnotations;

namespace EduCore_BusinessLayer
{
    public static class clsLog
    {
        public static async Task<int> AddAsync(
            enLogType logType,
            string message,
            string? source = null,
            string? stackTrace = null,
            string? ipAddress = null,
            string? userAgent = null,
            string? requestPath = null)
        {
            if (string.IsNullOrWhiteSpace(message))
                throw new ValidationException("Log message is required.");

            message = clsValidation.ValidateString(message, "Message", 4000);



            if (!string.IsNullOrWhiteSpace(stackTrace))
                stackTrace = stackTrace ?? "";

            if (!string.IsNullOrWhiteSpace(source))
                source = clsValidation.ValidateString(source, "Source", 200);

            if (!string.IsNullOrWhiteSpace(requestPath))
                requestPath = clsValidation.ValidateString(requestPath, "RequestPath", 300);

            DtoLog log = new DtoLog
            {
                LogType = logType,
                Message = message,
                Source = source,
                StackTrace = stackTrace,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                RequestPath = requestPath
            };

            int id = await clsLogsData.AddLogAsync(log);

            if (id <= 0)
                throw new ConflictException("Failed to add log.");

            return id;
        }

        public static async Task<DtoLog> FindAsync(int id)
        {
            if (id <= 0)
                throw new ValidationException("Log id is not valid.");

            DtoLog? log = await clsLogsData.GetLogByIdAsync(id);

            if (log is null)
                throw new NotFoundException("Log not found.");

            return log;
        }

        public static Task<List<DtoLog>> GetAllAsync(int pageNumber, int pageSize)
        {
            return clsLogsData.GetAllLogsAsync(pageNumber, pageSize);
        }

        public static Task<List<DtoLog>> GetByTypeAsync(enLogType logType, int pageNumber, int pageSize)
        {
            return clsLogsData.GetLogsByTypeAsync(logType, pageNumber, pageSize);
        }
    }
}