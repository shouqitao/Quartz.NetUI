using System.ComponentModel;
using System.Configuration.Install;

namespace Quartz.NET.WindowsService {
    [RunInstaller(true)]
    public partial class ProjectInstaller : Installer {
        public ProjectInstaller() {
            InitializeComponent();
        }
    }
}