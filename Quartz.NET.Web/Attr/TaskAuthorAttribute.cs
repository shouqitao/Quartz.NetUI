using System;

namespace Quartz.NET.Web.Attr {
    public class TaskAuthorAttribute : Attribute {
        public string Name { get; set; }
        public string Role { get; set; }
    }
}