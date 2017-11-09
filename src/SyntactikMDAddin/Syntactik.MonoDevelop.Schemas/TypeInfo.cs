using System.Collections.Generic;
using System.Xml.Schema;

namespace Syntactik.MonoDevelop.Schemas
{
    class TypeInfo
    {
        public List<TypeInfo> Descendants { get; private set; }

        public XmlSchemaType BaseSchemaType { get; set; }

        public TypeInfo BaseTypeInfo { get; set; }

        public XmlSchemaType SchemaType { get; }

        public TypeInfo(XmlSchemaType schemaType)
        {
            SchemaType = schemaType;
            Descendants = new List<TypeInfo>();
        }
    }
}
