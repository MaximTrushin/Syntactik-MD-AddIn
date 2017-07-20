using System;
using System.Collections.Generic;
using System.Xml;
using Syntactik.MonoDevelop.Completion;

namespace Syntactik.MonoDevelop.Schemas
{
    public class XmlSchemaNamespace : ISchemaProvider
    {
        public static string Url = "http://www.w3.org/XML/1998/namespace";
        public IEnumerable<NamespaceInfo> GetNamespaces()
        {
            yield return new NamespaceInfo { Name = "xml", Namespace = Url };
        }

        public void Validate(XmlDocument doc, Action<XmlNode, string> onErrorAction)
        {
        }

        public void PopulateContextInfo(Context context, ContextInfo ctxInfo)
        {
            ctxInfo.Attributes.Add(new AttributeInfo { Name = "lang", Namespace = Url, IsPrivate = true });
            ctxInfo.Attributes.Add(new AttributeInfo { Name = "id", Namespace = Url, IsPrivate = true });
        }
    }
}
