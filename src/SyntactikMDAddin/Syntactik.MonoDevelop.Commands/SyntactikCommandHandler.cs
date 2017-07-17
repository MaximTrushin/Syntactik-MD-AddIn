using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Components;

namespace Syntactik.MonoDevelop.Commands
{
    public class SyntactikCommandHandler : NodeCommandHandler
    {
        static FilePath PreviousFolderPath
        {
            get;
            set;
        }
    }
}
