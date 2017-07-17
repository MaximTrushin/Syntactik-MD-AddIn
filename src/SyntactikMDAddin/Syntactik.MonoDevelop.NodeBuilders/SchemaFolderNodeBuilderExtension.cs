using System;
using MonoDevelop.Ide.Gui.Components;
using Syntactik.MonoDevelop.Commands;

namespace Syntactik.MonoDevelop.NodeBuilders
{
    public class SchemaFolderNodeBuilderExtension : NodeBuilderExtension
    {
        public override bool CanBuildNode(Type dataType)
        {
            return typeof(SchemaFolder).IsAssignableFrom(dataType);
        }

        public override Type CommandHandlerType => typeof(SchemaFolderCommandHandler);
    }
}
