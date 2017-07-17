using System.Linq;
using MonoDevelop.Components.Commands;
using MonoDevelop.Components.Extensions;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide.Gui.Pads.ProjectPad;
using MonoDevelop.Ide.Projects;
using Syntactik.MonoDevelop.Project;

namespace Syntactik.MonoDevelop.Commands
{
    public class SchemaFolderCommandHandler : NodeCommandHandler
    {
        static FilePath PreviousFolderPath
        {
            get;
            set;
        }

        [CommandUpdateHandler(SyntactikCommands.AddSchema)]
        public void AddViewUpdate(CommandInfo info)
        {
            ProjectFolder pf = (ProjectFolder)CurrentNode.DataItem;
            FilePath rootName = pf.Project.BaseDirectory.Combine("Schemas");
            info.Enabled = info.Visible = (pf.Path == rootName || pf.Path.IsChildPathOf(rootName));
        }

        [CommandHandler(SyntactikCommands.AddSchema)]
        public void AddSchema()
        {
            // Get the project and project folder
            var project = (SyntactikProject) CurrentNode.GetParentDataItem(typeof(SyntactikProject), true);

            var targetRoot = project.BaseDirectory.CanonicalPath;

            var fdiag = new AddFileDialog(GettextCatalog.GetString("Add Schema"))
            {
                BuildActions = new string[] {},
                CurrentFolder = !PreviousFolderPath.IsNullOrEmpty ? PreviousFolderPath : targetRoot,
                SelectMultiple = true,
                TransientFor = IdeApp.Workbench.RootWindow
            };
            fdiag.Filters.Clear();
            fdiag.AddFilter(new SelectFileDialogFilter(
                GettextCatalog.GetString("XSD Schema Files"),
                new string[] { "*.xsd" }
                ));

            if (!fdiag.Run())
                return;
            PreviousFolderPath = fdiag.SelectedFiles.Select(f => f.FullPath.ParentDirectory).FirstOrDefault();
            FilePath baseDirectory = project.BaseDirectory.Combine("Schemas");

            var files = fdiag.SelectedFiles;

            IdeApp.ProjectOperations.AddFilesToProject(project, files, baseDirectory);
            IdeApp.ProjectOperations.SaveAsync(project);
            CurrentNode.Expanded = true;
            //IdeApp.OpenFiles(new[] { new FileOpenInformation(files.First(), project), });

        }

    }
}
