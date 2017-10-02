using Gtk;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using Syntactik.MonoDevelop.License;

namespace Syntactik.MonoDevelop.Commands
{
    public class LicenseInfoHandler : CommandHandler
    {
        protected override void Run()
        {
            base.Run();
            using (var dlg = new LicenseInfo())
            {
                dlg.SetPosition(WindowPosition.CenterAlways);
                var res = (ResponseType) MessageService.ShowCustomDialog(dlg);
            }
        }

    }
}
