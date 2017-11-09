using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using Syntactik.MonoDevelop.License;
using Syntactik.MonoDevelop.Util;

namespace Syntactik.MonoDevelop.Commands
{
    class LicenseInfoHandler : CommandHandler
    {
        protected override void Run()
        {
            base.Run();
            using (var dlg = new LicenseInfoDialog())
            {
                DialogHelper.ShowCustomDialog(dlg, null);
            }
        }

    }
}
