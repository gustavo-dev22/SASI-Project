using Microsoft.Extensions.Hosting;

namespace SASI.Middleware
{
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly bool _esProduccion;

        public SecurityHeadersMiddleware(RequestDelegate next, IHostEnvironment environment)
        {
            _next = next;
            _esProduccion = !environment.IsDevelopment();
        }

        public async Task InvokeAsync(HttpContext context)
        {
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            context.Response.Headers["Permissions-Policy"] = _esProduccion
                ? "camera=(), microphone=(), geolocation=(), payment=(), usb=(), unload=()"
                : "camera=(), microphone=(), geolocation=(), payment=(), usb=(), unload=(self)";

            context.Response.Headers["Content-Security-Policy"] = _esProduccion
                ? "default-src 'self'; " +
                  "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
                  "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
                  "img-src 'self' data: https://cdn.jsdelivr.net; " +
                  "font-src 'self' data: https://cdn.jsdelivr.net; " +
                  "connect-src 'self' https://cdn.jsdelivr.net; " +
                  "worker-src 'self' blob:; " +
                  "frame-ancestors 'self'"
                : "default-src 'self'; " +
                  "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net http://localhost:*; " +
                  "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net http://localhost:*; " +
                  "img-src 'self' data: https://cdn.jsdelivr.net; " +
                  "font-src 'self' data: https://cdn.jsdelivr.net; " +
                  "connect-src 'self' https://cdn.jsdelivr.net http://localhost:* ws://localhost:*; " +
                  "worker-src 'self' blob:; " +
                  "frame-ancestors 'self'";

            await _next(context);
        }
    }
}
