using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;

namespace Syntactik.MonoDevelop
{
    internal class Initializer : CommandHandler
    {

        protected override void Run()
        {
            IdeApp.CommandService.RegisterGlobalHandler(new GlobalCommandHandler());
        }
    }

    class GlobalCommandHandler
    {
        // This handler will hide the Run menu if there are no debuggers installed.

        [CommandHandler("ReportBug")]
        public void OnRun()
        {
        }

        [CommandUpdateHandler("ReportBug")]
        public void OnRunUpdate(CommandInfo cinfo)
        {
            cinfo.Visible = true;
          
        }
    }
}
