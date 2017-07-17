using System;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using Syntactik.MonoDevelop.Commands;

namespace Syntactik.MonoDevelop.NodeBuilders
{
    public class SchemaFolderNodeBuilder : TypeNodeBuilder
    {
        #region Properties
        /// <summary>Gets the data type for the WebReferenceFolderNodeBuilder.</summary>
        /// <value>A Type containing the data type for WebReferenceFolderNodeBuilder.</value>
        public override Type NodeDataType => typeof(SchemaFolder);

        /// <summary>Gets the type of the CommandHandler with the WebReferenceFolderNodeBuilder.</summary>
        /// <value>A Type containing the reference for the CommandHandlerType for the WebReferenceFolderNodeBuilder.</value>
        public override Type CommandHandlerType => typeof(SchemaFolderCommandHandler);

        /// <summary>Gets the Addin path for the context menu for the WebReferenceFolderNodeBuilder.</summary>
        /// <value>A string containing the AddIn path for the context menu for the WebReferenceFolderNodeBuilder.</value>
        public override string ContextMenuAddinPath => "/MonoDevelop/Syntactik/ContextMenu/ProjectPad/SchemaFolder";

        #endregion

        /// <summary>Gets the node name for the current node.</summary>
        /// <param name="thisNode">An ITreeNavigator containing the current node settings.</param>
        /// <param name="dataObject">An object containing the data for the current object.</param>
        public override string GetNodeName(ITreeNavigator thisNode, object dataObject)
        {
            return "Schemas";
        }

        /// <summary>Build the node in the project tree.</summary>
        /// <param name="treeBuilder">An ITreeBuilder containing the project tree builder.</param>
        /// <param name="dataObject">An object containing the current builder child.</param>
        /// <param name="nodeInfo"></param>
        public override void BuildNode(ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
        {
            var folder = (SchemaFolder)dataObject;
            nodeInfo.Label = folder.Name;

            nodeInfo.Icon = Context.GetIcon(Stock.OpenReferenceFolder);
            nodeInfo.ClosedIcon = Context.GetIcon(Stock.ClosedReferenceFolder);
        }

        /// <summary>Checks if the node builder has contains any child nodes.</summary>
        /// <param name="builder">An ITreeBuilder containing all the node builder information.</param>
        /// <param name="dataObject">An object containing the current activated node.</param> 
        public override bool HasChildNodes(ITreeBuilder builder, object dataObject)
        {
            return true;
        }

        public override void BuildChildNodes(ITreeBuilder treeBuilder, object dataObject)
        {
            var folder = (SchemaFolder)dataObject;
            var path = folder.Path;
            ProjectFileCollection files;
            GetFolderContent(folder.Project, path, out files);

            foreach (ProjectFile file in files)
                treeBuilder.AddChild(file);
        }

        private static void GetFolderContent(global::MonoDevelop.Projects.Project project, string folder, out ProjectFileCollection files)
        {
            files = new ProjectFileCollection();

            foreach (ProjectFile file in project.Files)
            {
                if (file.Subtype == Subtype.Directory) continue;

                if (file.DependsOnFile != null)
                    continue;

                string dir = file.IsLink
                    ? project.BaseDirectory.Combine(file.ProjectVirtualPath).ParentDirectory
                    : file.FilePath.ParentDirectory;

                if (dir == folder)
                {
                    files.Add(file);
                }
            }
        }


        /// <summary>Compare two object with one another and returns a number based on their sort order.</summary>
        /// <returns>An integer containing the sort order for the objects.</returns>
        public override int CompareObjects(ITreeNavigator thisNode, ITreeNavigator otherNode)
        {
            return (otherNode.DataItem is ProjectReferenceCollection) ? 1 : -1;
        }
    }
}
