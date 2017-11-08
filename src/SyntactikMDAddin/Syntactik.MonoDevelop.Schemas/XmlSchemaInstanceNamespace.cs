using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using Syntactik.DOM;
using Syntactik.MonoDevelop.Completion;

namespace Syntactik.MonoDevelop.Schemas
{
    internal class XmlSchemaInstanceNamespace : ISchemaProvider
    {
        public static string Url = "http://www.w3.org/2001/XMLSchema-instance";
        public IEnumerable<NamespaceInfo> GetNamespaces()
        {
            yield return new NamespaceInfo { Prefix = "xsi", Namespace = Url };
        }

        public void Validate(XmlDocument doc, Action<XmlNode, string> onErrorAction)
        {

        }

        public void PopulateContextInfo(Context context, ContextInfo contextInfo)
        {
            var lastNode = context.CompletionInfo.InTag == CompletionExpectation.NoExpectation
                    ? context.CompletionInfo.LastPair
                    : context.CompletionInfo.LastPair.Parent; //if we are inside pair which is not finished then context is the node's parent
            var contextElement = lastNode as IContainer;
            if (contextElement == null) return;

            if (context.CompletionInfo.InTag != CompletionExpectation.Attribute ||
                    contextElement.Entities.OfType<DOM.Attribute>().Count() > 1) //Do the following check only if we are not inside the sole attribute
            {
                if (contextElement.Entities.Any(e => e is DOM.Attribute && ((DOM.Attribute)e).NsPrefix != "xsi")) return;
            }
            //xsi - attribute can be added only if there are no other attribute present
            if (contextElement.Entities.Any(e => !(e is DOM.Attribute))) return;
            XmlSchemaElement element = contextInfo.Scope?.Parent as XmlSchemaElement;

            if (!(lastNode is Element))
            {
                contextInfo.Attributes.Add(new XmlSchemaAttribute {Use = XmlSchemaUse.Optional, Name = "xsi:type",});
            }
            else
            {
                if (!string.IsNullOrEmpty(contextInfo.Scope?.QualifiedName.Name))
                {
                    var type = contextInfo.AllTypes.FirstOrDefault(t => t.SchemaType.QualifiedName.Name == contextInfo.Scope?.QualifiedName.Name 
                                    && t.SchemaType.QualifiedName.Namespace == contextInfo.Scope?.QualifiedName.Namespace);
                    if (type != null && type.Descendants.Count > 0)
                        contextInfo.Attributes.Add(new XmlSchemaAttribute { Use = XmlSchemaUse.Optional, Name = "xsi:type"});
                } 
            }
            if (lastNode is AliasDefinition || lastNode is Alias || lastNode is Parameter || lastNode is Argument || element !=  null && element.IsNillable)
                contextInfo.Attributes.Add(new XmlSchemaAttribute { Use = XmlSchemaUse.Optional, Name = "xsi:nil"});
        }
    }
}
