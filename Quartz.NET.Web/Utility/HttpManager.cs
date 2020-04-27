using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Quartz.NET.Web.Utility {
    public class HttpManager {
        public static string GetUserIP(IHttpContextAccessor httpContextAccessor) {
            var Request = httpContextAccessor.HttpContext.Request;
            string realIP = null;
            string forwarded = null;
            var remoteIpAddress = httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString();
            if (Request.Headers.ContainsKey("X-Real-IP")) {
                realIP = Request.Headers["X-Real-IP"].ToString();
                if (realIP != remoteIpAddress) remoteIpAddress = realIP;
            }

            if (Request.Headers.ContainsKey("X-Forwarded-For")) {
                forwarded = Request.Headers["X-Forwarded-For"].ToString();
                if (forwarded != remoteIpAddress) remoteIpAddress = forwarded;
            }

            return remoteIpAddress;
        }

        public static Task<string> HttpPostAsync(string url, string postData = null, string contentType = null,
            int timeOut = 30, Dictionary<string, string> headers = null) {
            var request = (HttpWebRequest) WebRequest.Create(url);
            request.Method = "POST";
            if (!string.IsNullOrEmpty(contentType)) request.ContentType = contentType;
            if (headers != null)
                foreach (var header in headers)
                    request.Headers[header.Key] = header.Value;

            try {
                var bytes = Encoding.UTF8.GetBytes(postData ?? "");
                using (var sendStream = request.GetRequestStream()) {
                    sendStream.Write(bytes, 0, bytes.Length);
                }

                using (var response = (HttpWebResponse) request.GetResponse()) {
                    var responseStream = response.GetResponseStream();
                    var streamReader = new StreamReader(responseStream, Encoding.UTF8);
                    return streamReader.ReadToEndAsync();
                }
            } catch (Exception ex) {
                return Task.FromResult(ex.Message);
            }
        }

        public static Task<string> HttpGetAsync(string url, Dictionary<string, string> headers = null) {
            try {
                var request = (HttpWebRequest) WebRequest.Create(url);
                if (headers != null)
                    foreach (var header in headers)
                        request.Headers[header.Key] = header.Value;
                using (var response = (HttpWebResponse) request.GetResponse()) {
                    var responseStream = response.GetResponseStream();
                    var streamReader = new StreamReader(responseStream, Encoding.UTF8);
                    return streamReader.ReadToEndAsync();
                }
            } catch (Exception ex) {
                return Task.FromResult(ex.Message);
            }
        }
    }
}