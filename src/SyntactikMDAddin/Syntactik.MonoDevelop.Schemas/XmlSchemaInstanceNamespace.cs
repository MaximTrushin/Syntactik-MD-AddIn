using System;
using System.Collections.Generic;
using System.Xml;

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
            ctxInfo.Attributes.Add(new AttributeInfo { Name = "type", Namespace = Url, IsPrivate = true });
            ctxInfo.Attributes.Add(new AttributeInfo { Name = "nil", Namespace = Url, IsPrivate = true });
        }
    }
}
