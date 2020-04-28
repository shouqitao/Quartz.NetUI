using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Quartz.NET.Web.Extensions;

namespace Quartz.NET.Web.Controllers {
    public class HomeController : Controller {
        public HomeController(IConfiguration configuration, IMemoryCache memoryCache) {
            Configuration = configuration;
            MemoryCache = memoryCache;
        }

        private IConfiguration Configuration { get; }
        private IMemoryCache MemoryCache { get; }

        [AllowAnonymous]
        public IActionResult Index() {
            if (!string.IsNullOrEmpty(HttpContext.Request("ReturnUrl")))
                return new ContentResult {
                    ContentType = "text/html",
                    Content =
                        "<script language='javaScript' type='text/javaScript'> window.parent.location.href = '/Home/Index';</script>"
                };
            var msg = MemoryCache.Get("msg")?.ToString();
            if (msg == null) return View();
            ViewBag.msg = msg;
            MemoryCache.Remove("msg");

            return View();
        }

        /// <summary>
        ///     登陆
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        [AllowAnonymous]
        public async Task<IActionResult> ValidateAuthor(string token) {
            if (token == null) throw new ArgumentNullException(nameof(token));
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            MemoryCache.Remove("msg");
            token = Configuration["token"];
            if (!string.IsNullOrEmpty(token)) {
                var claimIdentity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
                claimIdentity.AddClaim(new Claim(ClaimTypes.NameIdentifier, token));
                await HttpContext.SignInAsync(new ClaimsPrincipal(claimIdentity), new AuthenticationProperties {
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(60)
                });
            } else {
                MemoryCache.Set("msg", string.IsNullOrEmpty(token) ? "请填写token" : "token不正确");
            }

            return new RedirectResult("/");
        }

        /// <summary>
        ///     退出
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        public async Task<IActionResult> SignOut() {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return new RedirectResult("/");
        }

        public IActionResult Guide() {
            return View("~/Views/Home/Guide.cshtml");
        }

        public IActionResult Help() {
            return View("~/Views/Home/Help.cshtml");
        }
    }
}