using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Editor.Extension;

namespace Syntactik.MonoDevelop.Completion
{
    public enum ItemType
    {
        PrivateAttribute,
        Attribute,
        Entity,
        Namespace,
        Alias,
        AliasNamespace,
        Argument
    }
    public class CompletionItem : CompletionData
    {
        public ItemType ItemType { get; set; }
        public bool UndeclaredNamespaceUsed { get; set; }
        public string Namespace { get; set; }
        public string NsPrefix { get; set; }

        public override void InsertCompletionText(CompletionListWindow window, ref KeyActions ka, KeyDescriptor descriptor)
        {
            ((SyntactikCompletionTextEditorExtension)window.Extension).SelectedCompletionItem = this;
            base.InsertCompletionText(window, ref ka, descriptor);
        }
    }
}
