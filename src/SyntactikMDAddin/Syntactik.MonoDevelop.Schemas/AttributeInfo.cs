using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Syntactik.MonoDevelop.Schemas
{
    public class AttributeInfo
    {
        public string Name { get; set; }
        public bool Optional { get; set; }
        public string Namespace { get; set; }
        public bool Qualified { get; set; }
        internal ComplexType Parent { get; private set; }
        public bool IsGlobal { get; set; }
        public AttributeInfo(ComplexType parent)
        {
            Parent = parent;
        }
        public AttributeInfo()
        {
        }
        public bool IsPrivate { get; set; }
    }
}
