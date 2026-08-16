using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SASI.Filters
{
    public class AutoValidateAntiforgeryFilter : IAsyncAuthorizationFilter
    {
        private readonly IAntiforgery _antiforgery;

        public AutoValidateAntiforgeryFilter(IAntiforgery antiforgery)
        {
            _antiforgery = antiforgery;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var metadata = context.ActionDescriptor.EndpointMetadata;

            if (metadata.Any(m => m is ApiControllerAttribute)) return;
            if (metadata.Any(m => m is IgnoreAntiforgeryTokenAttribute)) return;

            var method = context.HttpContext.Request.Method;
            if (HttpMethods.IsPost(method) || HttpMethods.IsPut(method) || HttpMethods.IsDelete(method) || HttpMethods.IsPatch(method))
            {
                try
                {
                    await _antiforgery.ValidateRequestAsync(context.HttpContext);
                }
                catch (AntiforgeryValidationException)
                {
                    context.Result = new BadRequestResult();
                }
            }
        }
    }
}
