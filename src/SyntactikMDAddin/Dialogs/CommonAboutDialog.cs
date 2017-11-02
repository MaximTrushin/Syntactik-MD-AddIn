using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gtk;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide;

namespace Syntactik.MonoDevelop.Dialogs
{
    internal class CommonAboutDialog : IdeDialog
    {
        public CommonAboutDialog()
        {
            Name = "wizard_dialog";
            Title = string.Format(GettextCatalog.GetString("About {0}"), BrandingService.ApplicationLongName);
            TransientFor = IdeApp.Workbench.RootWindow;
            AllowGrow = false;
            HasSeparator = false;
            BorderWidth = 0;

            var notebook = new Notebook();
            notebook.ShowTabs = false;
            notebook.ShowBorder = false;
            notebook.BorderWidth = 0;
            notebook.AppendPage(new AboutMonoDevelopTabPage(), new Label(Title));
            notebook.AppendPage(new VersionInformationTabPage(), new Label(GettextCatalog.GetString("Version Information")));
            VBox.PackStart(notebook, true, true, 0);

            var copyButton = new Button() { Label = GettextCatalog.GetString("Copy Information") };
            copyButton.Clicked += (sender, e) => CopyBufferToClipboard();
            ActionArea.PackEnd(copyButton, false, false, 0);
            copyButton.NoShowAll = true;

            var backButton = new Button() { Label = GettextCatalog.GetString("Show Details") };
            ActionArea.PackEnd(backButton, false, false, 0);
            backButton.Clicked += (sender, e) => {
                if (notebook.Page == 0)
                {
                    backButton.Label = GettextCatalog.GetString("Hide Details");
                    copyButton.Show();
                    notebook.Page = 1;
                }
                else
                {
                    backButton.Label = GettextCatalog.GetString("Show Details");
                    copyButton.Hide();
                    notebook.Page = 0;
                }
            };

            AddButton(Gtk.Stock.Close, (int)ResponseType.Close);

            ShowAll();
        }

        static void CopyBufferToClipboard()
        {
            var text = SystemInformation.GetTextDescription();
            var clipboard = Clipboard.Get(Gdk.Atom.Intern("CLIPBOARD", false));
            clipboard.Text = text.ToString();
            clipboard = Clipboard.Get(Gdk.Atom.Intern("PRIMARY", false));
            clipboard.Text = text.ToString();
        }

        void ChangeColor(Gtk.Widget w)
        {
            w.ModifyBg(Gtk.StateType.Normal, new Gdk.Color(69, 69, 94));
            w.ModifyBg(Gtk.StateType.Active, new Gdk.Color(69, 69, 94));
            w.ModifyFg(Gtk.StateType.Normal, new Gdk.Color(255, 255, 255));
            w.ModifyFg(Gtk.StateType.Active, new Gdk.Color(255, 255, 255));
            w.ModifyFg(Gtk.StateType.Prelight, new Gdk.Color(255, 255, 255));
            Gtk.Container c = w as Gtk.Container;
            if (c != null)
            {
                foreach (Widget cw in c.Children)
                    ChangeColor(cw);
            }
        }

        static CommonAboutDialog instance;

        public static void ShowAboutDialog()
        {
            if (Platform.IsMac)
            {
                if (instance == null)
                {
                    instance = new CommonAboutDialog();
                    MessageService.PlaceDialog(instance, IdeApp.Workbench.RootWindow);
                    instance.Response += delegate {
                        instance.Destroy();
                        instance.Dispose();
                        instance = null;
                    };
                }
                instance.Present();
                return;
            }

            using (var dlg = new CommonAboutDialog())
                MessageService.ShowCustomDialog(dlg);
        }
    }
}
