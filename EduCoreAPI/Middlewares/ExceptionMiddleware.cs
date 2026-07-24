
using Common.Enums;
using Common.Exceptions;
using EduCore_BusinessLayer;
using System.ComponentModel.DataAnnotations;

namespace EduCore_API.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (ValidationException ex)
            {
                await _RegistLogs(context, ex.Message, "Validtion", ex.StackTrace ,enLogType.Warning);
                await _WriteResponse(context, 400, ex.Message);
            }
            catch (ConflictException ex)
            {
                await _RegistLogs(context, ex.Message, "Conflict",ex.StackTrace , enLogType.Warning);

                await _WriteResponse(context, 409, ex.Message);
            }
            catch (NotFoundException ex)
            {
                await _RegistLogs(context, ex.Message, "NotFound", ex.StackTrace, enLogType.Warning);
                await _WriteResponse(context, 404, ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                await _RegistLogs(context, ex.Message, "Unauthorized",ex.StackTrace ,enLogType.Warning);
                await _WriteResponse(context,401,ex.Message);
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception");
                await _RegistLogs(context, ex.Message, "Unhandled exception", ex.StackTrace ,enLogType.Error);
                await _WriteResponse(context, 500, "Internal server error");
            }

        }

        private static async Task _RegistLogs(HttpContext context, string message,  string exceptionSource, string? stackTrace , enLogType type)
        {
            string? ipAddress =
           context.Connection.RemoteIpAddress?.ToString();

            string? userAgent =
                context.Request.Headers.UserAgent.ToString();

            string requestPath =
                context.Request.Path;
            await clsLog.AddAsync(
            type,
            message,
            source: $"ExceptionMiddleware: {exceptionSource}",
            stackTrace,
            ipAddress,
            userAgent,
            requestPath);
        }

        private static Task _WriteResponse(HttpContext context, int statusCode, string message)
        {
          
            context.Response.StatusCode = statusCode;
            return context.Response.WriteAsJsonAsync(message);
            //return context.Response.WriteAsJsonAsync(new
            //{
            //    StatusCode = statusCode,
            //    Message = message
            //});
        }
    }
}