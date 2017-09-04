using MonoDevelop.Components.Chart;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Editor.Extension;
using Syntactik.DOM;
using Syntactik.MonoDevelop.Schemas;

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
        private int _priorityGroup;
        public ItemType ItemType { get; set; }
        public bool UndeclaredNamespaceUsed { get; set; }
        public bool XsiUndeclared { get; set; }
        public string Namespace { get; set; }
        public string NsPrefix { get; set; }
        public ElementType ElementType { get; set; } //xsd schema element type
        public override int PriorityGroup => _priorityGroup;
        public Pair CompletionContextPair { get; set; }

        public int Priority
        {
            set { _priorityGroup = value; }
        }

        public override void InsertCompletionText(CompletionListWindow window, ref KeyActions ka, KeyDescriptor descriptor)
        {
            ((SyntactikCompletionTextEditorExtension)window.Extension).SelectedCompletionItem = this;
            var editor = window.Extension.Editor;
            if (window.CodeCompletionContext.TriggerOffset > 0)
            {
                var c = editor.Text[window.CodeCompletionContext.TriggerOffset - 1];
                if (c == '=') CompletionText = " " + CompletionText;
            }
            base.InsertCompletionText(window, ref ka, descriptor);
        }

        public override int CompareTo(object obj)
        {
            var item = obj as CompletionData;
            if (item == null)
                return 0;
            var result = item.PriorityGroup.CompareTo(PriorityGroup);
            return result != 0 ? result : Compare(this, (CompletionData) obj);
        }
    }
}
