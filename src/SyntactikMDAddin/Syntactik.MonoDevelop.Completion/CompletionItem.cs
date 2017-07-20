using MonoDevelop.Ide.CodeCompletion;

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

        public CompletionItem(ItemType itemType)
        {
            ItemType = itemType;
        }


    }
}
