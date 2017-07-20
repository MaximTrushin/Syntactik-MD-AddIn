using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Syntactik.MonoDevelop.Schemas
{
    public class ComplexType : ElementType
    {
        public List<AttributeInfo> Attributes { get; private set; }
        public List<ElementInfo> Elements { get; private set; }

        public List<ComplexType> Descendants { get; private set; }

        public ElementTypeRef BaseTypeRef { get; set; }
        public ComplexType BaseType => (ComplexType) BaseTypeRef?.ResolvedType;

        public ComplexType()
        {
            Attributes = new List<AttributeInfo>();
            Elements = new List<ElementInfo>();
            Descendants = new List<ComplexType>();
        }

        public override bool IsComplex => true;

        public override string ToString() => $"{Name}";
    }
}
