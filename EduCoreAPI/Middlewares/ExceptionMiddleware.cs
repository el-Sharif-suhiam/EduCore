
using Common.Enums;
using Common.Exceptions;
using EduCore_BusinessLayer;
using Microsoft.AspNetCore.Mvc;
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
                await _WriteResponse(context, StatusCodes.Status400BadRequest, "Bad Request", ex.Message);
            }
            catch (ConflictException ex)
            {
                await _RegistLogs(context, ex.Message, "Conflict",ex.StackTrace , enLogType.Warning);
                await _WriteResponse(context, StatusCodes.Status409Conflict, "Conflict", ex.Message);
            }
            catch (NotFoundException ex)
            {
                await _RegistLogs(context, ex.Message, "NotFound", ex.StackTrace, enLogType.Warning);
                await _WriteResponse(context, StatusCodes.Status404NotFound, "Not Found", ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                await _RegistLogs(context, ex.Message, "Unauthorized",ex.StackTrace ,enLogType.Warning);
                await _WriteResponse(context, StatusCodes.Status401Unauthorized, "Unauthorized", ex.Message);
            }
            catch (ForbiddenException ex)
            {
                await _RegistLogs(context, ex.Message, "Forbidden", ex.StackTrace, enLogType.Warning);
                await _WriteResponse(context, StatusCodes.Status403Forbidden, "Forbidden", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception");
                await _RegistLogs(context, ex.Message, "Unhandled exception", ex.StackTrace ,enLogType.Error);
                await _WriteResponse(context, StatusCodes.Status500InternalServerError, "Internal Server Error", "An unexpected error occurred.");
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

        private static Task _WriteResponse(HttpContext context, int statusCode, string title, string detail)
        {
            if (context.Response.HasStarted)
                return Task.CompletedTask;

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Type = $"https://httpstatuses.io/{statusCode}",
                Instance = context.Request.Path
            };

            return context.Response.WriteAsJsonAsync(problem);
        }
    }
}