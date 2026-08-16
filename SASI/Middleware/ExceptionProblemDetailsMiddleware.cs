using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SASI.Middleware
{
    public class ExceptionProblemDetailsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionProblemDetailsMiddleware> _logger;

        public ExceptionProblemDetailsMiddleware(RequestDelegate next, ILogger<ExceptionProblemDetailsMiddleware> logger)
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
                if (context.Response.HasStarted)
                {
                    throw;
                }

                _logger.LogError(ex, "Excepción no controlada en {Path}", context.Request.Path);

                context.Response.Clear();

                var esApi = context.GetEndpoint()?.Metadata
                    .Any(m => m is ApiControllerAttribute) ?? false;

                if (esApi)
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    context.Response.ContentType = "application/problem+json";

                    await context.Response.WriteAsJsonAsync(new ProblemDetails
                    {
                        Status = StatusCodes.Status500InternalServerError,
                        Title = "Ocurrió un error interno",
                        Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
                        Instance = context.Request.Path
                    });
                }
                else
                {
                    context.Response.StatusCode = StatusCodes.Status302Found;
                    context.Response.Redirect("/SASI/Home/Error");
                }
            }
        }
    }
}
