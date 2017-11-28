using System.Threading.Tasks;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Commands;
using Syntactik.DOM.Mapped;
using Syntactik.MonoDevelop.Completion;
using Syntactik.MonoDevelop.Dialogs;
using Syntactik.MonoDevelop.Licensing;
using Syntactik.MonoDevelop.Projects;

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

        [CommandHandler(HelpCommands.Help)]
        public void HelpResolver()
        {
            var textEditor = IdeApp.Workbench.ActiveDocument?.Editor;
            if (textEditor == null) return;

            var ext = textEditor.GetContent<SyntactikCompletionTextEditorExtension>();
            var task = ext.CompletionContextTask?.Task;
            if (task != null)
            {
#if DEBUG
                task.Wait(ext.CompletionContextTask.CancellationToken);
#else
                task.Wait(2000, ext.CompletionContextTask.CancellationToken);
#endif
                if (task.Status != TaskStatus.RanToCompletion) return;
                CompletionContext context = task.Result;
                
                var lastPair = context.LastPair as IMappedPair;
                if (lastPair == null) return;
                if (lastPair is Argument)
                {
                    DesktopService.ShowUrl(@"https://github.com/syntactik/Syntactik/blob/master/README.md#argument");
                }
                else if (lastPair is Alias)
                {
                    DesktopService.ShowUrl(@"https://github.com/syntactik/Syntactik/blob/master/README.md#alias");
                }
                else if (lastPair is AliasDefinition)
                {
                    DesktopService.ShowUrl(@"https://github.com/syntactik/Syntactik/blob/master/README.md#alias-definition");
                }
                else if (lastPair is Attribute)
                {
                    DesktopService.ShowUrl(@"https://github.com/syntactik/Syntactik/blob/master/README.md#attribute");
                }
                else if (lastPair is Document)
                {
                    DesktopService.ShowUrl(@"https://github.com/syntactik/Syntactik/blob/master/README.md#document");
                }
                else if (lastPair is Element)
                {
                    DesktopService.ShowUrl(@"https://github.com/syntactik/Syntactik/blob/master/README.md#element");
                }
                else if (lastPair is Module)
                {
                    DesktopService.ShowUrl(@"https://github.com/syntactik/Syntactik/blob/master/README.md#module");
                }
                else if (lastPair is NamespaceDefinition)
                {
                    DesktopService.ShowUrl(@"https://github.com/syntactik/Syntactik/blob/master/README.md#namespace-definition");
                }
                else if (lastPair is Parameter)
                {
                    DesktopService.ShowUrl(@"https://github.com/syntactik/Syntactik/blob/master/README.md#parameter");
                }
                else if (lastPair is Scope)
                {
                    DesktopService.ShowUrl(@"https://github.com/syntactik/Syntactik/blob/master/README.md#namespace-scope");
                }
            }
        }

        [CommandUpdateHandler(HelpCommands.Help)]
        public void HelpResolverUpdate(CommandInfo cinfo)
        {
            if (BrandingService.ApplicationName != "Syntactik Editor")
            {
                cinfo.Bypass = true;
            }
        }
    }
}
