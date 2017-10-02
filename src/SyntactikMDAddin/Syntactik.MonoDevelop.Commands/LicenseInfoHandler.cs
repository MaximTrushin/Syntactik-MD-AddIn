using Gtk;
using MonoDevelop.Components.Commands;
using Syntactik.MonoDevelop.License;

namespace Syntactik.MonoDevelop.Commands
{
    public class LicenseInfoHandler : CommandHandler
    {
        protected override void Run()
        {
            base.Run();
            var dlg = new LicenseInfo();
            dlg.SetPosition(WindowPosition.CenterAlways);
            var res = (ResponseType)dlg.Run();
            dlg.Hide();
        }

    }
}
