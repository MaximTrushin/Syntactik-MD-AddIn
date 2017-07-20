namespace Syntactik.MonoDevelop.Schemas
{
    public class ElementInfo
    {
        public string Name { get; set; }
        public string Namespace { get; set; }

        internal ElementType Type;

        public bool Optional { get; set; }
        public decimal MaxOccurs { get; set; }
        public bool Qualified { get; set; }
        internal ComplexType Parent { get; private set; }
        public bool IsGlobal { get; set; }

        public bool InSequence { get; set; }

        public ElementInfo(ComplexType parent)
        {
            Parent = parent;
            InSequence = true;
        }
        
        public ElementType GetElementType()
        {
            var @ref = Type as ElementTypeRef;
            if (@ref != null)
            {
                var tRef = @ref;

                if (tRef.ResolvedType == null)
                {
                    var simpleType = new SimpleType
                    {
                        Name = tRef.Name,
                        Namespace = tRef.Namespace
                    };
                    return simpleType;
                }

                return @ref.ResolvedType;
            }
            return Type;
        }


    }
}
