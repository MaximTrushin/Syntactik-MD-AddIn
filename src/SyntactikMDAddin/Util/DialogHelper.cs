using Gtk;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using Dialog = Gtk.Dialog;
using Stock = MonoDevelop.Ide.Gui.Stock;
using Window = MonoDevelop.Components.Window;

namespace Syntactik.MonoDevelop.Util
{
    /// <summary>
    /// Monodevelop MessageService class incorrectly process modal dialog creation if app is not in focus.
    /// This class is created to be used instead of MessageService.
    /// </summary>
    public static class DialogHelper
    {
        internal static void ShowMessage(string message, Window parent)
        {
            if (Runtime.IsMainThread)
            {
                ShowMessageInternal(parent, message);
            }
            else
                Application.Invoke(delegate
                {
                    ShowMessageInternal(parent, message);
                });
        }

        internal static void ShowWarning(string message, Window parent)
        {
            if (Runtime.IsMainThread)
            {
                ShowMessageInternal(parent, message, Stock.Warning);
            }
            else
                Application.Invoke(delegate
                {
                    ShowMessageInternal(parent, message, Stock.Warning);
                });
        }

        internal static void ShowError(string message, Window parent)
        {
            if (Runtime.IsMainThread)
            {
                ShowMessageInternal(parent, message, Stock.Error);
            }
            else
                Runtime.RunInMainThread(delegate
                {
                    ShowMessageInternal(parent, message, Stock.Error);
                }).Wait();
        }

        internal static void ShowMessageInternal(Window parent, string message, string iconId = "md-information")
        {
            if (parent == null) parent = MessageService.RootWindow;
            var md = new MessageDialog(parent, DialogFlags.Modal & DialogFlags.DestroyWithParent, MessageType.Other,
                ButtonsType.Ok, true, message)
            { TransientFor = parent };
            var image = new ImageView
            {
                Yalign = 0.00f,
                Image = ImageService.GetIcon(iconId, IconSize.Dialog)
            };

            md.Image = image;
            md.Image.Show();
            md.Run();
            md.Destroy();
        }

        internal static int ShowCustomDialog(Dialog dialog, Window parent = null)
        {
            if (parent == null) return MessageService.ShowCustomDialog(dialog);
            
            dialog.TransientFor = parent;
            dialog.DestroyWithParent = true;
            try
            {
                return GtkWorkarounds.RunDialogWithNotification(dialog);
            }
            finally
            {
                dialog.Destroy();
            }
        }
    }
}
