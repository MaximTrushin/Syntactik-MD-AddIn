using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
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

        //Hidding Check for Updates menu for Syntactik Editor branding
        [CommandUpdateHandler("MonoDevelop.Ide.Updater.UpdateCommands.CheckForUpdates")]
        public void OnRunUpdate(CommandInfo cinfo)
        {
            if (BrandingService.ApplicationName == "Syntactik Editor")
                cinfo.Visible = false;
        }
    }
}
