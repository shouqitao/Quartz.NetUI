﻿using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace Quartz.NET.Web.Extensions {
    public class PathProvider : IPathProvider {
        private readonly IHostingEnvironment _hostingEnvironment;

        public PathProvider(IHostingEnvironment environment) {
            _hostingEnvironment = environment;
        }

        public IHostingEnvironment GetHostingEnvironment() {
            return _hostingEnvironment;
        }

        public string MapPath(string path) {
            return MapPath(path, false);
        }

        /// <summary>
        /// </summary>
        /// <param name="path"></param>
        /// <param name="rootPath">获取wwwroot路径</param>
        /// <returns></returns>
        public string MapPath(string path, bool rootPath) {
            return Path.Combine(rootPath ? _hostingEnvironment.WebRootPath : _hostingEnvironment.ContentRootPath, path);
        }
    }
}