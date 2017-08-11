using System;
using System.Collections.Generic;
using System.Xml;

namespace Syntactik.MonoDevelop.Schemas
{
    public interface ISchemaProvider
    {
        IEnumerable<NamespaceInfo> GetNamespaces();
        void Validate(XmlDocument doc, Action<XmlNode, string> onErrorAction);
        void PopulateContextInfo(Context context, ContextInfo contextInfo);
    }
}
