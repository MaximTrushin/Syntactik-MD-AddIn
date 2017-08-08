using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Syntactik.DOM;
using Syntactik.MonoDevelop.Completion;

namespace Syntactik.MonoDevelop.Schemas
{
    public class XmlSchemaInstanceNamespace : ISchemaProvider
    {
        public static string Url = "http://www.w3.org/2001/XMLSchema-instance";
        public IEnumerable<NamespaceInfo> GetNamespaces()
        {
            yield return new NamespaceInfo { Prefix = "xsi", Namespace = Url };
        }

        public void Validate(XmlDocument doc, Action<XmlNode, string> onErrorAction)
        {

        }

        public void PopulateContextInfo(Context context, ContextInfo ctxInfo)
        {
            var lastNode = context.CompletionInfo.InTag == CompletionExpectation.NoExpectation
                    ? context.CompletionInfo.LastPair
                    : context.CompletionInfo.LastPair.Parent; //if we are inside pair which is not finished then context is the node's parent
            var contextElement = lastNode as IContainer;
            if (contextElement == null) return;

            if (contextElement.Entities.Any(e => e is DOM.Attribute && ((DOM.Attribute) e).NsPrefix != "xsi")) return;
            if (contextElement.Entities.Any(e => e is DOM.Element)) return;

            //xsi - attribute can be added only if there are no other attribute present
            ctxInfo.Attributes.Add(new AttributeInfo { Name = "type", Namespace = Url, Builtin = true, Optional = true });
            ctxInfo.Attributes.Add(new AttributeInfo { Name = "nil", Namespace = Url, Builtin = true, Optional = true });
        }
    }
}
