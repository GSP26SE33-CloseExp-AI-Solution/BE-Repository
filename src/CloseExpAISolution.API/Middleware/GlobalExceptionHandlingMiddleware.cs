using System.Text.Json;
using CloseExpAISolution.Application.DTOs.Response;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CloseExpAISolution.API.Middleware;

public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger)
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
        catch (Exception ex)
        {
            var (statusCode, message, code) = MapException(ex);
            _logger.LogError(ex, "Unhandled exception. Path={Path}, Status={Status}, Code={Code}",
                context.Request.Path, statusCode, code);

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";
            var payload = ApiResponse<object>.ErrorResponse(message, [code]);
            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            await context.Response.WriteAsync(json);
        }
    }

    private static (int Status, string Message, string Code) MapException(Exception ex)
    {
        if (ex is KeyNotFoundException)
            return (StatusCodes.Status404NotFound, ex.Message, "not_found");

        if (ex is UnauthorizedAccessException)
            return (StatusCodes.Status403Forbidden, ex.Message, "forbidden");

        if (ex is InvalidOperationException invalidOp &&
            invalidOp.Message.Contains("R2Storage:BucketName is required", StringComparison.OrdinalIgnoreCase))
        {
            return (StatusCodes.Status503ServiceUnavailable,
                "R2 storage is not configured. Image-upload workflow is temporarily unavailable.",
                "config_missing_r2_bucket");
        }

        if (ex is DbUpdateException dbEx && dbEx.InnerException is PostgresException pgEx)
        {
            if (pgEx.SqlState == PostgresErrorCodes.ForeignKeyViolation &&
                string.Equals(pgEx.ConstraintName, "FK_StockLots_UnitOfMeasures_UnitId", StringComparison.Ordinal))
            {
                return (StatusCodes.Status409Conflict,
                    "Cannot create StockLot because UnitOfMeasure is not configured/valid.",
                    "lot_unit_invalid");
            }
        }

        if (ex.Message.Contains("Cannot write DateTime with Kind=Local", StringComparison.OrdinalIgnoreCase))
        {
            return (StatusCodes.Status400BadRequest,
                "Datetime must be UTC (or include timezone offset) for this endpoint.",
                "invalid_datetime_utc_required");
        }

        if (ex is InvalidOperationException)
            return (StatusCodes.Status409Conflict, ex.Message, "workflow_conflict");

        if (ex is ArgumentException)
            return (StatusCodes.Status400BadRequest, ex.Message, "invalid_argument");

        return (StatusCodes.Status500InternalServerError, "An unexpected server error occurred.", "internal_error");
    }
}

