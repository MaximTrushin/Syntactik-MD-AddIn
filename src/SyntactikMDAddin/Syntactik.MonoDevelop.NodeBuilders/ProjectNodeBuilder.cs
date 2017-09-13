using System;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide.Gui.Pads.ProjectPad;
using Syntactik.MonoDevelop.Commands;
using Syntactik.MonoDevelop.Projects;

namespace Syntactik.MonoDevelop.NodeBuilders
{
    public class ProjectNodeBuilder : NodeBuilderExtension
    {
        public override Type CommandHandlerType => typeof(SyntactikCommandHandler);

        public override bool CanBuildNode(Type dataType) => typeof(SyntactikProject).IsAssignableFrom(dataType);

        public override void BuildChildNodes(ITreeBuilder treeBuilder, object dataObject)
        {
            var project = (SyntactikProject) dataObject;
            var pos = treeBuilder.CurrentPosition;

            IterateTreeBuilder(treeBuilder);
            treeBuilder.MoveToPosition(pos);
            FilePath rootName = project.BaseDirectory.Combine("Schemas");
            treeBuilder.AddChild(new SchemaFolder(rootName, project));
        }

        private void IterateTreeBuilder(ITreeBuilder treeBuilder)
        {
            if (treeBuilder.HasChildren())
            {
                treeBuilder.MoveToFirstChild();
                IterateTreeBuilder(treeBuilder);
                treeBuilder.MoveToParent();
            }
            bool move = false;
            do
            {
                if (treeBuilder.DataItem is SchemaFolder)
                    continue;
                if (IsHiddenItem(treeBuilder.DataItem))
                {
                    treeBuilder.Remove();
                }
                move = treeBuilder.MoveNext();
            } while (move);
        }

        private bool IsHiddenItem(object dataItem)
        {
            var folder = dataItem as ProjectFolder;
            return folder != null && folder.Name == "Schemas";
        }
    }
}