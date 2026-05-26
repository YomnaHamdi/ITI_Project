using System.Net;
using System.Text.Json;

namespace storybuild.API.Middleware
{
    public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
                await WriteErrorResponseAsync(context, ex);
            }
        }

        private static async Task WriteErrorResponseAsync(HttpContext context, Exception ex)
        {
            context.Response.ContentType = "application/json";

            var (statusCode, message) = ex switch
            {
                InvalidOperationException => (HttpStatusCode.UnprocessableEntity, ex.Message),
                ArgumentException => (HttpStatusCode.BadRequest, ex.Message),
                _ => (HttpStatusCode.InternalServerError, "حدث خطأ أثناء إنشاء القصة. يرجى المحاولة مرة أخرى.")
            };

            context.Response.StatusCode = (int)statusCode;

            var body = JsonSerializer.Serialize(new
            {
                error = message,
                statusCode = (int)statusCode
            });

            await context.Response.WriteAsync(body);
        }
    }
}
