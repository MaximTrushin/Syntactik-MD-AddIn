using System.Collections.Generic;

namespace Syntactik.MonoDevelop.Schemas
{
    public class ContextInfo
    {
        public List<ElementInfo> Elements { get; private set; }
        public List<AttributeInfo> Attributes { get; private set; }
        public ElementType CurrentType { get; set; }
        public ComplexType Scope { get; set; }

        public ContextInfo()
        {
            Elements = new List<ElementInfo>();
            Attributes = new List<AttributeInfo>();
        }
    }
}
