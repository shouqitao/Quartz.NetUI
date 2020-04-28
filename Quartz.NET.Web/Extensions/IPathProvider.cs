using Microsoft.AspNetCore.Hosting;

namespace Quartz.NET.Web.Extensions {
    public interface IPathProvider {
        string MapPath(string path);
        string MapPath(string path, bool rootPath);
        IHostingEnvironment GetHostingEnvironment();
    }
}