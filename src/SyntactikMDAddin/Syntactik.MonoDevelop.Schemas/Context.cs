using System.Collections.Generic;
using Syntactik.MonoDevelop.Completion;

namespace Syntactik.MonoDevelop.Schemas
{
    public class Context
    {
        public List<NamespaceInfo> IncludedNamespaces { get; private set; }

        public Context()
        {
            IncludedNamespaces = new List<NamespaceInfo>();
        }

        public CompletionContext CompletionInfo { get; set; }
        //public string RootElementName { get; set; }
        //public bool FlattenRoot { get; set; }
    }
}
