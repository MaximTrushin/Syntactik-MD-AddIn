using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gtk;
using Syntactik.MonoDevelop.Projects;

namespace Syntactik.MonoDevelop.License
{
    public partial class LicenseInfoDialog : Gtk.Dialog
    {
        //private string _email;
        //private Timer timer;
        //private bool _needConfirm = false;
        //private string _confirmMessage;
        
        public LicenseInfoDialog()
        {
            Build();
            Gdk.Color col = new Gdk.Color(240, 240, 240);
            entryEmail.ModifyBase(StateType.Normal, col);
            entryName.ModifyBase(StateType.Normal, col);
            entryCompany.ModifyBase(StateType.Normal, col);
            entryPosition.ModifyBase(StateType.Normal, col);
            entryLicenseId.ModifyBase(StateType.Normal, col);
            entryLicenseType.ModifyBase(StateType.Normal, col);
            entryExpires.ModifyBase(StateType.Normal, col);
            var lic = new Licensing.License(SyntactikProject.GetLicenseFileName());
        }

    }
}
