using Microsoft.AspNetCore.Http;

namespace Quartz.NET.Web.Utility {
    public static class HttpContext {
        private static IHttpContextAccessor _accessor;

        public static Microsoft.AspNetCore.Http.HttpContext Current {
            get { return _accessor.HttpContext; }
        }

        internal static void Configure(IHttpContextAccessor accessor) {
            _accessor = accessor;
        }
    }
}