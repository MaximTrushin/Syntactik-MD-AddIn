namespace Syntactik.MonoDevelop.Schemas
{
    public class AttributeInfo
    {
        public string Name { get; set; }
        public bool Optional { get; set; }
        public string Namespace { get; set; }
        public bool Qualified { get; set; }
        internal ComplexType ParentType { get; private set; }
        public bool IsGlobal { get; set; }
        public AttributeInfo(ComplexType parentType)
        {
            ParentType = parentType;
        }
        public AttributeInfo()
        {
        }
        public bool Builtin { get; set; } //xsi namespace
    }
}
