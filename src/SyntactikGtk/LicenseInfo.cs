using System;
using Timer = System.Threading.Timer;
namespace Syntactik.MonoDevelop.License
{
	public partial class LicenseInfo : Gtk.Dialog
	{
		//private string _email;
		//private Timer timer;
		//private bool _needConfirm = false;
		//private string _confirmMessage;
		public IO.Swagger.Model.LicenseInfo License { get; set; }
		public LicenseInfo()
		{
            this.Build();
		}
		Gdk.Color col = new Gdk.Color(240, 240, 240);
	}
}
