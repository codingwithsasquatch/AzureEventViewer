using System;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using HeyRed.Mime;

namespace AzureEventViewer
{
    public static class StaticFileServer
    {
        const string staticFilesFolder = "www";
        static string defaultPage = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DEFAULT_PAGE")) ?
            "index.html" : Environment.GetEnvironmentVariable("DEFAULT_PAGE");

        [FunctionName("StaticFileServer")]
        public static HttpResponseMessage Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "")]HttpRequestMessage req, TraceWriter log)
        {
            try
            {
                var filePath = GetFilePath(req, log);
                var response = new HttpResponseMessage(HttpStatusCode.OK);
                var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                response.Content = new StreamContent(stream);
                response.Content.Headers.ContentType = new MediaTypeHeaderValue(GetMimeType(filePath));
                return response;
            }
            catch
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }
        }

        private static string GetScriptPath()
        {
            if (Environment.GetEnvironmentVariable("isLocal") == "1")
            {
                return Path.GetFullPath(@".");
            }
            else
            {
                return Path.Combine(System.Environment.GetEnvironmentVariable("HOME", EnvironmentVariableTarget.Process), @"site\wwwroot\www");
            }
        }

        private static string GetFilePath(HttpRequestMessage req, TraceWriter log)
        {
            var pathValue = req.GetQueryNameValuePairs()
                .FirstOrDefault(q => string.Compare(q.Key, "file", true) == 0)
                .Value;

            var path = pathValue ?? "";

            var staticFilesPath = Path.GetFullPath(Path.Combine(GetScriptPath(), staticFilesFolder));
            var fullPath = Path.GetFullPath(Path.Combine(staticFilesPath, path));

            if (!IsInDirectory(staticFilesPath, fullPath))
            {
                throw new ArgumentException("Invalid path");
            }

            var isDirectory = Directory.Exists(fullPath);
            if (isDirectory)
            {
                fullPath = Path.Combine(fullPath, defaultPage);
            }

            return fullPath;
        }

        private static bool IsInDirectory(string parentPath, string childPath)
        {
            var parent = new DirectoryInfo(parentPath);
            var child = new DirectoryInfo(childPath);

            var dir = child;
            do
            {
                if (dir.FullName == parent.FullName)
                {
                    return true;
                }
                dir = dir.Parent;
            } while (dir != null);

            return false;
        }

        private static string GetMimeType(string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            return MimeTypesMap.GetMimeType(fileInfo.Name);
        }
    }
}