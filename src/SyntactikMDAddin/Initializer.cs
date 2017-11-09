using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Commands;
using Syntactik.MonoDevelop.Dialogs;

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
            Update(cinfo);
            if (BrandingService.ApplicationName == "Syntactik Editor")
                cinfo.Visible = false;
        }

        [CommandHandler(HelpCommands.About)]
        public void OnAbout()
        {
            CommonAboutDialog.ShowAboutDialog();
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
