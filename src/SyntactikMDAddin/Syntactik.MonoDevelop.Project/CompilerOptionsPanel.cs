using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gtk;
using MonoDevelop.Components;
using MonoDevelop.Ide.Gui.Dialogs;

namespace Syntactik.MonoDevelop.Project
{
    public class CompilerOptionsPanel : ItemOptionsPanel
    {
        CompilerOptionsWidget widget;

        public override Control CreatePanelWidget()
        {
            widget = new CompilerOptionsWidget(this.ConfiguredProject);
            return widget;
        }

        public override bool ValidateChanges()
        {
            return widget.ValidateChanges();
        }

        public override void ApplyChanges()
        {
            widget.Store(ConfiguredSolutionItem.Configurations);
        }
    }
}
