﻿using MonoDevelop.Components.Chart;
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
        private int _priorityGroup;
        public ItemType ItemType { get; set; }
        public bool UndeclaredNamespaceUsed { get; set; }
        public string Namespace { get; set; }
        public string NsPrefix { get; set; }
        public override int PriorityGroup => _priorityGroup;

        public int Priority
        {
            set { _priorityGroup = value; }
        }

        public override void InsertCompletionText(CompletionListWindow window, ref KeyActions ka, KeyDescriptor descriptor)
        {
            ((SyntactikCompletionTextEditorExtension)window.Extension).SelectedCompletionItem = this;
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
