using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Quartz.NET.WindowsService {
    public partial class Service1 : ServiceBase {
        public Service1() {
            InitializeComponent();
        }

        protected override void OnStart(string[] args) {
            Task.Run(Start);
        }

        private static void Start() {
            var path = AppDomain.CurrentDomain.BaseDirectory + "\\log\\";
            //默认1分钟调用一次
            var interval = 1;
            var message = "";
            var url = "";
            while (true)
                try {
                    ConfigurationManager.RefreshSection("appSettings");
                    url = ConfigurationManager.AppSettings["url"];
                    interval = Convert.ToInt32(ConfigurationManager.AppSettings["interval"]);

                    var request = (HttpWebRequest) WebRequest.Create(url);
                    using (var response = (HttpWebResponse) request.GetResponse()) {
                        var responseStream = response.GetResponseStream();
                        var streamReader = new StreamReader(responseStream, Encoding.UTF8);
                        message = streamReader.ReadToEnd();
                    }
                } catch (Exception ex) {
                    message = url + ",interval:[" + interval + "]," + ex.Message;
                } finally {
                    try {
                        message = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "__" + message + "\r\n";
                        WriteFile(path, DateTime.Now.ToString("yyyy-MM-dd") + ".txt", message, true);
                    } catch {
                        // ignored
                    }

                    Thread.Sleep(new TimeSpan(0, interval, 0));
                }
        }

        private static void WriteFile(string path, string fileName, string content, bool appendToLast = false) {
            if (!Directory.Exists(path)) //如果不存在就创建file文件夹
                Directory.CreateDirectory(path);

            using (var stream = File.Open(path + fileName, FileMode.OpenOrCreate, FileAccess.Write)) {
                var by = Encoding.Default.GetBytes(content);
                if (appendToLast)
                    stream.Position = stream.Length;
                else
                    stream.SetLength(0);
                stream.Write(by, 0, by.Length);
            }
        }

        protected override void OnStop() { }
    }
}