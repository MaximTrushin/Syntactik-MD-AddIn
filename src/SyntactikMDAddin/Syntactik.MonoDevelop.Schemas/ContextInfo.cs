using System.Collections.Generic;

namespace Syntactik.MonoDevelop.Schemas
{
    public class ContextInfo
    {
        public ElementType CurrentType { get; set; }
        public ComplexType Scope { get; set; }
        public List<ElementInfo> Elements { get; private set; }
        public List<AttributeInfo> Attributes { get; private set; }
        public List<ComplexType> AllDescendants { get; private set; }

        public ContextInfo()
        {
            Elements = new List<ElementInfo>();
            Attributes = new List<AttributeInfo>();
            AllDescendants = new List<ComplexType>();
        }
    }
}
