using System.Collections.Generic;

namespace Syntactik.MonoDevelop.Schemas
{
    public class SimpleType : ElementType
    {
        public List<string> EnumValues { get; private set; }
        public SimpleType()
        {
            EnumValues = new List<string>();
        }
    }
}
