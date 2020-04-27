using System.Runtime.InteropServices;

namespace Quartz.NET.Web.Extensions {
    public static class ConvertPath {
        public static bool Windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public static string ReplacePath(this string path) {
            if (string.IsNullOrEmpty(path))
                return "";
            return Windows ? path.Replace("/", "\\") : path.Replace("\\", "/");
        }
    }
}