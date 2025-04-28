using Microsoft.EntityFrameworkCore;
using System.Net;

public class ExceptionMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogger<ExceptionMiddleware> logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        this.next = next;
        this.logger = logger;
    }

    public async Task Invoke(HttpContext httpContext)
    {
        try
        {
            await next(httpContext);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception at {Path} {Method}",
                httpContext.Request.Path, httpContext.Request.Method);

            await HandleException(httpContext, ex);
        }
    }

    public async Task HandleException(HttpContext httpContext, Exception ex)
    {
        var response = httpContext.Response;
        response.ContentType = "application/json";

        response.StatusCode = ex switch
        {
            ArgumentNullException => (int)HttpStatusCode.BadRequest,
            ArgumentException => (int)HttpStatusCode.BadRequest,
            KeyNotFoundException => (int)HttpStatusCode.NotFound,
            InvalidOperationException => (int)HttpStatusCode.Conflict,
            DbUpdateException => (int)HttpStatusCode.InternalServerError,
            UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
            _ => (int)HttpStatusCode.InternalServerError
        };

        var errorResponse = new
        {
            statusCode = response.StatusCode,
            message = ex.Message,
            path = httpContext.Request.Path,
            traceId = httpContext.TraceIdentifier
        };

        await response.WriteAsJsonAsync(errorResponse);
    }
}
