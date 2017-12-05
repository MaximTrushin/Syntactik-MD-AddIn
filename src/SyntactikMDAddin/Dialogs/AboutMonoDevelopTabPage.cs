using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Gtk;
using MonoDevelop;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Dialogs;
using Image = Xwt.Drawing.Image;

namespace Syntactik.MonoDevelop.Dialogs
{
    class AboutMonoDevelopTabPage : VBox
    {
        public AboutMonoDevelopTabPage()
        {
            BorderWidth = 0;

            var aboutFile = BrandingService.GetFile(AboutDialogImage.Name);
            var imageSep = aboutFile != null ? Xwt.Drawing.Image.FromFile(aboutFile) : Xwt.Drawing.Image.FromResource(AboutDialogImage.Name);

            PackStart(new ImageView(imageSep), false, false, 0);

            Xwt.VBox infoBox = new Xwt.VBox();
            Xwt.FrameBox mbox = new Xwt.FrameBox(infoBox);

            infoBox.Spacing = 6;
            infoBox.Margin = 12;
            PackStart(mbox.ToGtkWidget(), false, false, 0);

            infoBox.PackStart(new Xwt.Label()
            {
                Text = GettextCatalog.GetString("Syntactik Addin Version"),
                Font = infoBox.Font.WithWeight(Xwt.Drawing.FontWeight.Bold)
            });
            infoBox.PackStart(new Xwt.Label()
            {
                Text = MonoDevelopVersion,
                MarginLeft = 12
            });

            infoBox.PackStart(new Xwt.Label()
            {
                Text = GettextCatalog.GetString("License"),
                Font = infoBox.Font.WithWeight(Xwt.Drawing.FontWeight.Bold)
            });
            var cbox = new Xwt.HBox()
            {
                Spacing = 0,
                MarginLeft = 12
            };
            cbox.PackStart(new Xwt.Label()
            {
                Text = GettextCatalog.GetString("License is available at ")
            });
            cbox.PackStart(new Xwt.LinkLabel()
            {
                Text = "http://syntactik.com/syntactik-editor-license",
                Uri = new Uri("http://syntactik.com/syntactik-editor-license")
            });
            infoBox.PackStart(cbox);

            infoBox.PackStart(new Xwt.Label()
            {
                Text = GettextCatalog.GetString("Copyright"),
                Font = infoBox.Font.WithWeight(Xwt.Drawing.FontWeight.Bold)
            });
            cbox = new Xwt.HBox()
            {
                Spacing = 0,
                MarginLeft = 12
            };
            cbox.PackStart(new Xwt.Label("© 2011-" + DateTime.Now.Year + " "));
            cbox.PackStart(new Xwt.LinkLabel()
            {
                Text = "Syntactik LLC.",
                Uri = new Uri("http://www.syntactik.com")
            });
            infoBox.PackStart(cbox);
            infoBox.PackStart(new Xwt.Label()
            {
                Text = "© 2004-" + DateTime.Now.Year + " MonoDevelop contributors",
                MarginLeft = 12
            });

            this.ShowAll();
        }

        public static string MonoDevelopVersion
        {
            get
            {
                string v = "";
#pragma warning disable 162
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                var ver = typeof(AboutMonoDevelopTabPage).Assembly.GetName().Version;
                return ver.ToString();
                if (BuildInfo.Version != BuildInfo.VersionLabel)
                    // ReSharper disable once HeuristicUnreachableCode
                    v += BuildInfo.Version;
#pragma warning restore 162
                if (Runtime.Version.Revision >= 0)
                {
                    if (v.Length > 0)
                        v += " ";
                    v += "build " + Runtime.Version.Revision;
                }
                if (v.Length == 0)
                    return BuildInfo.VersionLabel;
                return BuildInfo.VersionLabel + " (" + v + ")";
            }
        }
    }
}
