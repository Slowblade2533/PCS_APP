using Microsoft.AspNetCore.Diagnostics;
using System.Net;

namespace PCS_API.Handlers;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IWebHostEnvironment env) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException)
        {
            logger.LogInformation("การขอดึงข้อมูลถูกยกเลิกโดยผู้ใช้ (Request was cancelled by the client)");
            httpContext.Response.StatusCode = 499;
            return true;
        }

        logger.LogError(exception, "เกิดข้อผิดพลาดที่ไม่ได้จัดการ {Message}", exception.Message);

        var statusCode = HttpStatusCode.InternalServerError;
        var message = "เกิดข้อผิดพลาดภายในระบบ กรุณาลองใหม่อีกครั้งในภายหลัง";
        var isDev = env.IsDevelopment();

        if (exception is KeyNotFoundException)
        {
            statusCode = HttpStatusCode.NotFound;
            message = isDev || !exception.Message.Contains('/') && !exception.Message.Contains('\\') ? exception.Message : "ไม่พบข้อมูลที่ระบุ";
        }
        else if (exception is ArgumentException)
        {
            statusCode = HttpStatusCode.BadRequest;
            message = isDev || !exception.Message.Contains('/') && !exception.Message.Contains('\\') ? exception.Message : "ข้อมูลไม่ถูกต้อง";
        }
        else if (exception is InvalidOperationException)
        {
            statusCode = HttpStatusCode.Conflict;
            message = isDev || !exception.Message.Contains('/') && !exception.Message.Contains('\\') ? exception.Message : "ไม่สามารถดำเนินรายการได้";
        }

        httpContext.Response.StatusCode = (int)statusCode;
        httpContext.Response.ContentType = "application/json";

        var errorResponse = new
        {
            statusCode = (int)statusCode,
            message,
            detailed = isDev ? exception.ToString() : null
        };

        await httpContext.Response.WriteAsJsonAsync(errorResponse, cancellationToken);

        return true;
    }
}
