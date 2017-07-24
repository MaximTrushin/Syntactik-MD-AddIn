using System.Collections.Generic;

namespace Syntactik.MonoDevelop.Schemas
{
    public class ContextInfo
    {
        public List<ElementInfo> Elements { get; private set; }
        public List<AttributeInfo> Attributes { get; private set; }
        public ContextInfo()
        {
            Elements = new List<ElementInfo>();
            Attributes = new List<AttributeInfo>();
        }
    }
}
