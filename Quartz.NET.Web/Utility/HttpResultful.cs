using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Quartz.Impl;
using Quartz.Impl.Triggers;
using Quartz.NET.Web.Extensions;

namespace Quartz.NET.Web.Utility {
    public class HttpResultful : IJob {
        public Task Execute(IJobExecutionContext context) {
            var dateTime = DateTime.Now;
            var taskOptions = context.GetTaskOptions();
            var httpMessage = "";
            var trigger = (context as JobExecutionContextImpl).Trigger as AbstractTrigger;
            if (taskOptions == null) {
                FileHelper.WriteFile(FileQuartz.LogPath + trigger.Group, $"{trigger.Name}.txt", "未到找作业或可能被移除", true);
                return Task.CompletedTask;
            }

            if (string.IsNullOrEmpty(taskOptions.ApiUrl) || taskOptions.ApiUrl == "/") {
                FileHelper.WriteFile(FileQuartz.LogPath + trigger.Group, $"{trigger.Name}.txt", "未配置url", true);
                return Task.CompletedTask;
            }

            try {
                var header = new Dictionary<string, string>();
                if (!string.IsNullOrEmpty(taskOptions.AuthKey)
                    && !string.IsNullOrEmpty(taskOptions.AuthValue))
                    header.Add(taskOptions.AuthKey.Trim(), taskOptions.AuthValue.Trim());

                if (taskOptions.RequestType?.ToLower() == "get")
                    httpMessage = HttpManager.HttpGetAsync(taskOptions.ApiUrl, header).Result;
                else
                    httpMessage = HttpManager.HttpPostAsync(taskOptions.ApiUrl, null, null, 60, header).Result;
            } catch (Exception ex) {
                httpMessage = ex.Message;
            }

            try {
                var logContent =
                    $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}_{dateTime.ToString("yyyy-MM-dd HH:mm:ss")}_{(string.IsNullOrEmpty(httpMessage) ? "OK" : httpMessage)}\r\n";
                FileHelper.WriteFile(FileQuartz.LogPath + taskOptions.GroupName + "\\", $"{taskOptions.TaskName}.txt",
                    logContent, true);
            } catch (Exception) { }

            Console.Out.WriteLineAsync(trigger.FullName + " " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:sss") + " " +
                                       httpMessage);
            return Task.CompletedTask;
        }
    }
}