using System.ServiceProcess;

namespace Quartz.NET.WindowsService {
    internal static class Program {
        /// <summary>
        ///     应用程序的主入口点。
        /// </summary>
        private static void Main() {
            var servicesToRun = new ServiceBase[] {
                new Service1()
            };
            ServiceBase.Run(servicesToRun);
        }
    }
}