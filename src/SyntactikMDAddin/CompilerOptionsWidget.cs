using System;
using MonoDevelop.Ide;
using MonoDevelop.Projects;
using Syntactik.MonoDevelop.Project;

namespace Syntactik.MonoDevelop
{
    [System.ComponentModel.ToolboxItem(true)]
    public partial class CompilerOptionsWidget : Gtk.Bin
    {
        public CompilerOptionsWidget(global::MonoDevelop.Projects.Project project)
        {
            this.Build();

            var configuration = (SyntactikProjectConfiguration)project.GetConfiguration(IdeApp.Workspace.ActiveConfiguration);
            this.folderEntry.Path = configuration.XMLOutputFolder;
        }

        public bool ValidateChanges()
        {
            return true;
        }

        public void Store(SolutionItemConfigurationCollection configs)
        {
            foreach (var itemConfiguration in configs)
            {
                var configuration = (SyntactikProjectConfiguration) itemConfiguration;
                configuration.XMLOutputFolder = folderEntry.Path;
            }

        }
    }
}

