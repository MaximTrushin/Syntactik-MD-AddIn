using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gdk;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.CodeTemplates;
using MonoDevelop.Ide.Editor.Extension;

namespace Syntactik.MonoDevelop.Completion
{
    public enum ItemType
    {
        PrivateAttribute,
        Attribute,
        Entity,
        Namepsace,
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
