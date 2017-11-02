using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Commands;

namespace Syntactik.MonoDevelop
{
    internal class Initializer : CommandHandler
    {

        protected override void Run()
        {
            IdeApp.CommandService.RegisterGlobalHandler(new GlobalCommandHandler());
        }
    }

    class GlobalCommandHandler: CommandHandler
    {

        //Hidding Check for Updates menu for Syntactik Editor branding
        [CommandUpdateHandler("MonoDevelop.Ide.Updater.UpdateCommands.CheckForUpdates")]
        public void OnCheckForUpdatesUpdate(CommandInfo cinfo)
        {
            base.Update(cinfo);
            if (BrandingService.ApplicationName == "Syntactik Editor")
                cinfo.Visible = false;
        }

        [CommandHandler(HelpCommands.About)]
        public void OnAbout()
        {

            //cinfo.Visible = false;
        }
        [CommandUpdateHandler(HelpCommands.About)]
        public void OnUpdateAbout(CommandInfo cinfo)
        {
            if (BrandingService.ApplicationName != "Syntactik Editor")
            {
                cinfo.Bypass = true;
            }
        }
    }
}
