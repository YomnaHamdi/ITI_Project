using System.Net;
using System.Text.Json;

namespace storybuild.API.Middleware
{
    public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            try { await next(context); }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled: {Method} {Path}", context.Request.Method, context.Request.Path);
                await WriteErrorAsync(context, ex);
            }
        }

        private static async Task WriteErrorAsync(HttpContext ctx, Exception ex)
        {
            ctx.Response.ContentType = "application/json";
            var (code, msg) = ex switch
            {
                InvalidOperationException => (HttpStatusCode.UnprocessableEntity, ex.Message),
                ArgumentException => (HttpStatusCode.BadRequest, ex.Message),
                TimeoutException => (HttpStatusCode.GatewayTimeout, "انتهت مهلة إنشاء الصورة. حاول مرة أخرى."),
                _ => (HttpStatusCode.InternalServerError, "حدث خطأ في الخادم.")
            };
            ctx.Response.StatusCode = (int)code;
            await ctx.Response.WriteAsync(JsonSerializer.Serialize(new { error = msg, statusCode = (int)code }));
        }
    }

}
