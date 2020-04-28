using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Quartz.Impl;
using Quartz.Impl.Matchers;
using Quartz.Impl.Triggers;
using Quartz.NET.Web.Constant;
using Quartz.NET.Web.Enum;
using Quartz.NET.Web.Models;
using Quartz.NET.Web.Utility;

namespace Quartz.NET.Web.Extensions {
    public static class QuartzNetExtension {
        private static List<TaskOptions> _taskList = new List<TaskOptions>();

        /// <summary>
        ///     初始化作业
        /// </summary>
        /// <param name="applicationBuilder"></param>
        /// <param name="env"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseQuartz(this IApplicationBuilder applicationBuilder,
            IHostingEnvironment env) {
            var services = applicationBuilder.ApplicationServices;

            var schedulerFactory = services.GetService<ISchedulerFactory>();

            var path = FileQuartz.CreateQuartzRootPath(env);
            var jobConfig = FileHelper.ReadFile(path + QuartzFileInfo.JobConfigFileName);
            if (string.IsNullOrEmpty(jobConfig)) {
                FileHelper.WriteFile(FileQuartz.LogPath, "start.txt",
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss},没有默认配置任务\r\n", true);
                return applicationBuilder;
            }

            var errorCount = 0;
            var errorMsg = "";
            TaskOptions options = null;
            try {
                _taskList = JsonConvert.DeserializeObject<List<TaskOptions>>(jobConfig);
                _taskList.ForEach(x => {
                    options = x;
                    var result = x.AddJob(schedulerFactory, true).Result;
                });
            } catch (Exception ex) {
                errorCount = +1;
                errorMsg += $"作业:{options?.TaskName},异常：{ex.Message}";
            }

            var content = $"成功:{_taskList.Count - errorCount}个,失败{errorCount}个,异常：{errorMsg}\r\n";
            FileQuartz.WriteStartLog(content);


            return applicationBuilder;
        }

        /// <summary>
        ///     获取所有的作业
        /// </summary>
        /// <param name="schedulerFactory"></param>
        /// <returns></returns>
        public static async Task<List<TaskOptions>> GetJobs(this ISchedulerFactory schedulerFactory) {
            var list = new List<TaskOptions>();
            try {
                var scheduler = await schedulerFactory.GetScheduler();
                var groups = await scheduler.GetJobGroupNames();
                foreach (var groupName in groups)
                foreach (var jobKey in await scheduler.GetJobKeys(GroupMatcher<JobKey>.GroupEquals(groupName))) {
                    var taskOptions = _taskList
                        .FirstOrDefault(x => x.GroupName == jobKey.Group && x.TaskName == jobKey.Name);
                    if (taskOptions == null)
                        continue;

                    var triggers = await scheduler.GetTriggersOfJob(jobKey);
                    foreach (var trigger in triggers) {
                        var dateTimeOffset = trigger.GetPreviousFireTimeUtc();
                        if (dateTimeOffset != null) {
                            taskOptions.LastRunTime = Convert.ToDateTime(dateTimeOffset.ToString());
                        } else {
                            var runLog = FileQuartz.GetJobRunLog(taskOptions.TaskName, taskOptions.GroupName, 1, 2);
                            if (runLog.Count <= 0) continue;
                            DateTime.TryParse(runLog[0].BeginDate, out var lastRunTime);
                            taskOptions.LastRunTime = lastRunTime;
                        }
                    }

                    list.Add(taskOptions);
                }
            } catch (Exception ex) {
                FileQuartz.WriteStartLog("获取作业异常：" + ex.Message + ex.StackTrace);
            }

            return list;
        }

        /// <summary>
        ///     添加作业
        /// </summary>
        /// <param name="taskOptions"></param>
        /// <param name="schedulerFactory"></param>
        /// <param name="init">是否初始化,否=需要重新生成配置文件，是=不重新生成配置文件</param>
        /// <returns></returns>
        public static async Task<object> AddJob(this TaskOptions taskOptions, ISchedulerFactory schedulerFactory,
            bool init = false) {
            try {
                var validExpression = taskOptions.Interval.IsValidExpression();
                if (!validExpression.Item1)
                    return new {status = false, msg = validExpression.Item2};

                var result = taskOptions.Exists(init);
                if (!result.Item1)
                    return result.Item2;
                if (!init) {
                    _taskList.Add(taskOptions);
                    FileQuartz.WriteJobConfig(_taskList);
                }

                var job = JobBuilder.Create<HttpResultful>()
                    .WithIdentity(taskOptions.TaskName, taskOptions.GroupName)
                    .Build();
                var trigger = TriggerBuilder.Create()
                    .WithIdentity(taskOptions.TaskName, taskOptions.GroupName)
                    .StartNow().WithDescription(taskOptions.Describe)
                    .WithCronSchedule(taskOptions.Interval)
                    .Build();
                var scheduler = await schedulerFactory.GetScheduler();
                await scheduler.ScheduleJob(job, trigger);
                if (taskOptions.Status == (int) TriggerState.Normal) {
                    await scheduler.Start();
                } else {
                    await schedulerFactory.Pause(taskOptions);
                    FileQuartz.WriteStartLog(
                        $"作业:{taskOptions.TaskName},分组:{taskOptions.GroupName},新建时未启动原因,状态为:{taskOptions.Status}");
                }

                if (!init)
                    FileQuartz.WriteJobAction(JobAction.新增, taskOptions.TaskName, taskOptions.GroupName);
            } catch (Exception ex) {
                return new {status = false, msg = ex.Message};
            }

            return new {status = true};
        }

        /// <summary>
        ///     移除作业
        /// </summary>
        /// <param name="schedulerFactory"></param>
        /// <param name="taskOptions"></param>
        /// <returns></returns>
        public static Task<object> Remove(this ISchedulerFactory schedulerFactory, TaskOptions taskOptions) {
            return schedulerFactory.TriggerAction(taskOptions.TaskName, taskOptions.GroupName, JobAction.删除,
                taskOptions);
        }

        /// <summary>
        ///     更新作业
        /// </summary>
        /// <param name="schedulerFactory"></param>
        /// <param name="taskOptions"></param>
        /// <returns></returns>
        public static Task<object> Update(this ISchedulerFactory schedulerFactory, TaskOptions taskOptions) {
            return schedulerFactory.TriggerAction(taskOptions.TaskName, taskOptions.GroupName, JobAction.修改,
                taskOptions);
        }

        /// <summary>
        ///     暂停作业
        /// </summary>
        /// <param name="schedulerFactory"></param>
        /// <param name="taskOptions"></param>
        /// <returns></returns>
        public static Task<object> Pause(this ISchedulerFactory schedulerFactory, TaskOptions taskOptions) {
            return schedulerFactory.TriggerAction(taskOptions.TaskName, taskOptions.GroupName, JobAction.暂停,
                taskOptions);
        }

        /// <summary>
        ///     启动作业
        /// </summary>
        /// <param name="schedulerFactory"></param>
        /// <param name="taskOptions"></param>
        /// <returns></returns>
        public static Task<object> Start(this ISchedulerFactory schedulerFactory, TaskOptions taskOptions) {
            return schedulerFactory.TriggerAction(taskOptions.TaskName, taskOptions.GroupName, JobAction.开启,
                taskOptions);
        }

        /// <summary>
        ///     立即执行一次作业
        /// </summary>
        /// <param name="schedulerFactory"></param>
        /// <param name="taskOptions"></param>
        /// <returns></returns>
        public static Task<object> Run(this ISchedulerFactory schedulerFactory, TaskOptions taskOptions) {
            return schedulerFactory.TriggerAction(taskOptions.TaskName, taskOptions.GroupName, JobAction.立即执行,
                taskOptions);
        }

        public static object ModifyTaskEntity(this TaskOptions taskOptions, ISchedulerFactory schedulerFactory,
            JobAction action) {
            TaskOptions options = null;
            object result = null;
            switch (action) {
                case JobAction.删除:
                    for (var i = 0; i < _taskList.Count; i++) {
                        options = _taskList[i];
                        if (options.TaskName == taskOptions.TaskName && options.GroupName == taskOptions.GroupName)
                            _taskList.RemoveAt(i);
                    }

                    break;
                case JobAction.修改:
                    options = _taskList
                        .FirstOrDefault(x => x.TaskName == taskOptions.TaskName && x.GroupName == taskOptions.GroupName);
                    //移除以前的配置
                    if (options != null) _taskList.Remove(options);

                    //生成任务并添加新配置
                    result = taskOptions.AddJob(schedulerFactory).GetAwaiter().GetResult();
                    break;
                case JobAction.暂停:
                case JobAction.开启:
                case JobAction.停止:
                case JobAction.立即执行:
                    options = _taskList
                        .FirstOrDefault(x => x.TaskName == taskOptions.TaskName && x.GroupName == taskOptions.GroupName);
                    if (action == JobAction.暂停)
                        options.Status = (int) TriggerState.Paused;
                    else if (action == JobAction.停止)
                        options.Status = (int) action;
                    else
                        options.Status = (int) TriggerState.Normal;
                    break;
            }

            //生成配置文件
            FileQuartz.WriteJobConfig(_taskList);
            FileQuartz.WriteJobAction(action, taskOptions.TaskName, taskOptions.GroupName,
                "操作对象：" + JsonConvert.SerializeObject(taskOptions));
            return result;
        }

        /// <summary>
        ///     触发新增、删除、修改、暂停、启用、立即执行事件
        /// </summary>
        /// <param name="schedulerFactory"></param>
        /// <param name="taskName"></param>
        /// <param name="groupName"></param>
        /// <param name="action"></param>
        /// <param name="taskOptions"></param>
        /// <returns></returns>
        public static async Task<object> TriggerAction(this ISchedulerFactory schedulerFactory, string taskName,
            string groupName, JobAction action, TaskOptions taskOptions = null) {
            var errorMsg = "";
            try {
                var scheduler = await schedulerFactory.GetScheduler();
                var jobKeys = scheduler.GetJobKeys(GroupMatcher<JobKey>.GroupEquals(groupName)).Result.ToList();
                if (!jobKeys.Any()) {
                    errorMsg = $"未找到分组[{groupName}]";
                    return new {status = false, msg = errorMsg};
                }

                var jobKey = jobKeys
                    .FirstOrDefault(s => scheduler.GetTriggersOfJob(s).Result.Any(x => (x as CronTriggerImpl)?.Name == taskName));
                if (jobKey == null) {
                    errorMsg = $"未找到触发器[{taskName}]";
                    return new {status = false, msg = errorMsg};
                }

                var triggers = await scheduler.GetTriggersOfJob(jobKey);
                var trigger = triggers?.Where(x => (x as CronTriggerImpl)?.Name == taskName).FirstOrDefault();

                if (trigger == null) {
                    errorMsg = $"未找到触发器[{taskName}]";
                    return new {status = false, msg = errorMsg};
                }

                object result = null;
                switch (action) {
                    case JobAction.删除:
                    case JobAction.修改:
                        await scheduler.PauseTrigger(trigger.Key);
                        await scheduler.UnscheduleJob(trigger.Key); // 移除触发器
                        await scheduler.DeleteJob(trigger.JobKey);
                        result = taskOptions.ModifyTaskEntity(schedulerFactory, action);
                        break;
                    case JobAction.暂停:
                    case JobAction.停止:
                    case JobAction.开启:
                        result = taskOptions.ModifyTaskEntity(schedulerFactory, action);
                        if (action == JobAction.暂停)
                            await scheduler.PauseTrigger(trigger.Key);
                        else if (action == JobAction.开启)
                            await scheduler.ResumeTrigger(trigger.Key);
                        //   await scheduler.RescheduleJob(trigger.Key, trigger);
                        else
                            await scheduler.Shutdown();
                        break;
                    case JobAction.立即执行:
                        await scheduler.TriggerJob(jobKey);
                        break;
                }

                return result ?? new {status = true, msg = $"作业{action.ToString()}成功"};
            } catch (Exception ex) {
                errorMsg = ex.Message;
                return new {status = false, msg = ex.Message};
            } finally {
                FileQuartz.WriteJobAction(action, taskName, groupName, errorMsg);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="context"></param>
        /// 通过作业上下文获取作业对应的配置参数
        /// <returns></returns>
        public static TaskOptions GetTaskOptions(this IJobExecutionContext context) {
            var trigger = (context as JobExecutionContextImpl)?.Trigger as AbstractTrigger;
            var taskOptions = _taskList
                .FirstOrDefault(x => trigger != null && (x.TaskName == trigger.Name && x.GroupName == trigger.Group));
            return taskOptions ?? _taskList
                .FirstOrDefault(x => trigger != null && (x.TaskName == trigger.JobName && x.GroupName == trigger.JobGroup));
        }

        /// <summary>
        ///     作业是否存在
        /// </summary>
        /// <param name="taskOptions"></param>
        /// <param name="init">初始化的不需要判断</param>
        /// <returns></returns>
        public static (bool, object) Exists(this TaskOptions taskOptions, bool init) {
            if (!init && _taskList.Any(x => x.TaskName == taskOptions.TaskName && x.GroupName == taskOptions.GroupName))
                return (false,
                    new {
                        status = false,
                        msg = $"作业:{taskOptions.TaskName},分组：{taskOptions.GroupName}已经存在"
                    });
            return (true, null);
        }

        public static (bool, string) IsValidExpression(this string cronExpression) {
            try {
                var trigger = new CronTriggerImpl {CronExpressionString = cronExpression};
                var date = trigger.ComputeFirstFireTimeUtc(null);
                return (date != null, date == null ? $"请确认表达式{cronExpression}是否正确!" : "");
            } catch (Exception e) {
                return (false, $"请确认表达式{cronExpression}是否正确!{e.Message}");
            }
        }
    }
}