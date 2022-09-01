using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace WinService
{
    public partial class Service1 : ServiceBase
    {
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Rll.RllApp.RllMain();
            Rll.RllApp.SendNOTIFY("service-start");
        }

        protected override void OnStop()
        {
            // note that "exit" will run before this, after RllMain exits
            Rll.RllApp.SendNOTIFY("service-stop");
        }
    }
}
