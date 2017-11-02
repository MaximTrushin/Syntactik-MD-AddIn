using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gtk;
using MonoDevelop.Components;
using MonoDevelop.Core;

namespace Syntactik.MonoDevelop.Dialogs
{
    internal class VersionInformationTabPage : VBox
    {
        bool destroyed;

        public VersionInformationTabPage()
        {
            BorderWidth = 6;
            SetLabel(GettextCatalog.GetString("Loading..."));

            new System.Threading.Thread(() => {
                try
                {
                    var info = SystemInformation.GetDescription().ToArray();
                    Gtk.Application.Invoke(delegate {
                        if (destroyed)
                            return;
                        SetText(info);
                    });
                }
                catch (Exception ex)
                {
                    LoggingService.LogError("Failed to load version information", ex);
                    Gtk.Application.Invoke(delegate {
                        if (destroyed)
                            return;
                        SetLabel(GettextCatalog.GetString("Failed to load version information."));
                    });
                }
            }).Start();
        }

        void Clear()
        {
            foreach (var c in this.Children)
            {
                this.Remove(c);
            }
        }

        void SetLabel(string text)
        {
            Clear();
            var label = new Label(text);
            PackStart(label, true, true, 0);
            ShowAll();
        }

        void SetText(IEnumerable<ISystemInformationProvider> text)
        {
            Clear();

            var buf = new Gtk.Label
            {
                Selectable = true,
                Xalign = 0
            };

            StringBuilder sb = new StringBuilder();

            foreach (var info in text)
            {
                sb.Append("<b>").Append(GLib.Markup.EscapeText(info.Title)).Append("</b>\n");
                sb.Append(GLib.Markup.EscapeText(info.Description.Trim())).Append("\n\n");
            }

            buf.Markup = sb.ToString().Trim() + "\n";

            var contentBox = new VBox {BorderWidth = 4};
            contentBox.PackStart(buf, false, false, 0);

            var asmButton = new Gtk.Button("Show loaded assemblies");
            asmButton.Clicked += (sender, e) => {
                asmButton.Hide();
                contentBox.PackStart(CreateAssembliesTable(), false, false, 0);
            };
            var hb = new Gtk.HBox();
            hb.PackStart(asmButton, false, false, 0);
            contentBox.PackStart(hb, false, false, 0);

            var sw = new CompactScrolledWindow()
            {
                ShowBorderLine = true,
                BorderWidth = 2
            };
            sw.AddWithViewport(contentBox);
            sw.ShadowType = ShadowType.None;
            ((Gtk.Viewport)sw.Child).ShadowType = ShadowType.None;

            PackStart(sw, true, true, 0);
            ShowAll();
        }

        Gtk.Widget CreateAssembliesTable()
        {
            var box = new Gtk.VBox();
            box.PackStart(new Gtk.Label()
            {
                Markup = "<b>LoadedAssemblies</b>",
                Xalign = 0
            });
            var table = new Gtk.Table(0, 0, false) {ColumnSpacing = 3};
            uint line = 0;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic).OrderBy(a => a.FullName))
            {
                try
                {
                    var assemblyName = assembly.GetName();
                    table.Attach(new Gtk.Label(assemblyName.Name) { Xalign = 0 }, 0, 1, line, line + 1);
                    table.Attach(new Gtk.Label(assemblyName.Version.ToString()) { Xalign = 0 }, 1, 2, line, line + 1);
                    table.Attach(new Gtk.Label(System.IO.Path.GetFullPath(assembly.Location)) { Xalign = 0 }, 2, 3, line, line + 1);
                }
                catch
                {
                    // ignored
                }
                line++;
            }
            box.PackStart(table, false, false, 0);
            box.ShowAll();
            return box;
        }

        public override void Destroy()
        {
            base.Destroy();
            destroyed = true;
        }
    }
}
