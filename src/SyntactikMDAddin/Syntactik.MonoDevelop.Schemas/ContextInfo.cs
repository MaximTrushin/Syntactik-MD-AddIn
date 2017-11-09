using System.Collections.Generic;
using System.Xml.Schema;

namespace Syntactik.MonoDevelop.Schemas
{
    class ContextInfo
    {
        public List<XmlSchemaElement> Elements { get; private set; }
        public List<XmlSchemaAttribute> Attributes { get; private set; }
        public XmlSchemaType CurrentType { get; set; }
        public XmlSchemaComplexType Scope { get; set; }
        public List<TypeInfo> AllTypes { get; set; }
        public bool InSequence { get; set; }

        public ContextInfo()
        {
            Elements = new List<XmlSchemaElement>();
            Attributes = new List<XmlSchemaAttribute>();
            InSequence = false;
        }
    }
}
