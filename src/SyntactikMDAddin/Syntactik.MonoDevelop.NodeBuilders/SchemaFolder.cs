using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Pads.ProjectPad;
using MonoDevelop.Projects;

namespace Syntactik.MonoDevelop.NodeBuilders
{
    public class SchemaFolder : ProjectFolder
    {
        public SchemaFolder(FilePath absolutePath, WorkspaceObject parentWorkspaceObject) : base(absolutePath, parentWorkspaceObject)
        {
        }

        public SchemaFolder(FilePath absolutePath, WorkspaceObject parentWorkspaceObject, object parent) : base(absolutePath, parentWorkspaceObject, parent)
        {
        }
    }
}
