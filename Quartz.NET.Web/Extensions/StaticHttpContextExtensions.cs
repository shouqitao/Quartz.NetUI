using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using HttpContext = Quartz.NET.Web.Utility.HttpContext;

namespace Quartz.NET.Web.Extensions {
    public static class StaticHttpContextExtensions {
        public static IApplicationBuilder UseStaticHttpContext(this IApplicationBuilder app) {
            var httpContextAccessor = app.ApplicationServices.GetRequiredService<IHttpContextAccessor>();
            HttpContext.Configure(httpContextAccessor);
            return app;
        }
    }
}