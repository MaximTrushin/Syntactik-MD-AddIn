﻿namespace Syntactik.MonoDevelop.Schemas
{
    public class ElementType
    {
        public string Name { get; set; }
        public string Namespace { get; set; }
        public virtual bool IsComplex => false;
        public bool IsGlobal { get; set; }
    }
}