using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using Syntactik.DOM;

namespace Syntactik.MonoDevelop.Schemas
{
    public abstract class XmlSchemaProvider : ISchemaProvider
    {
        public List<ElementType> Types { get; private set; }
        public List<ElementInfo> GlobalElements { get; private set; }
        public List<AttributeInfo> GlobalAttributes { get; private set; }
        public List<ElementTypeRef> TypeRefs { get; private set; }

        protected abstract XmlSchemaSet GetSchemaSet();

        protected XmlSchemaProvider()
        {
            Types = new List<ElementType>();
            GlobalElements = new List<ElementInfo>();
            GlobalAttributes = new List<AttributeInfo>();
            TypeRefs = new List<ElementTypeRef>();
        }

        void Update()
        {
            TypeRefs.Clear();
            Types.Clear();
            GlobalElements.Clear();
            GlobalAttributes.Clear();

            var schemaSet = GetSchemaSet();
            foreach (XmlSchemaType sType in schemaSet.GlobalTypes.Values)
            {
                var elementType = GetElementType(sType);
                elementType.IsGlobal = true;
                Types.Add(elementType);
            }
            foreach (XmlSchemaElement sElement in schemaSet.GlobalElements.Values)
            {
                var elementInfo = GetElementInfo(sElement, null);
                elementInfo.IsGlobal = true;
                GlobalElements.Add(elementInfo);
            }
            foreach (XmlSchemaAttribute sAttrib in schemaSet.GlobalAttributes.Values)
            {
                var attributeInfo = GetAttributeInfo(sAttrib, null);
                attributeInfo.IsGlobal = true;
                GlobalAttributes.Add(attributeInfo);
            }

            foreach (var tRef in TypeRefs)
            {
                var type = Types.FirstOrDefault(t => t.Name == tRef.Name && t.Namespace == tRef.Namespace);
                tRef.ResolvedType = type;
            }

            foreach (var type in Types.OfType<ComplexType>())
            {
                var baseType = type.BaseType;
                while (baseType != null)
                {
                    baseType.Descendants.Add(type);
                    baseType = baseType.BaseType;
                }
            }
        }

        public void PopulateContextInfo(Context context, ContextInfo ctxInfo)
        {
            Update();

            var elements = new List<ElementInfo>(GlobalElements);
            var attributes = new List<AttributeInfo>(GlobalAttributes);

            var path = context.CompletionInfo.GetPath().Where(pair => !(pair is Document) && !(pair is Module) && !(pair is CompileUnit)).ToList();

            ElementType targetType = null;

            ctxInfo.CurrentType = null;
            ctxInfo.Scope = null;

            //if (!path.Any() && context.RootElementName != null)
            //{
            //    var root = elements.FirstOrDefault(e => e.Name == context.RootElementName);
            //    if (root != null)
            //    {
            //        attributes.Clear();
            //        elements.Clear();
            //        elements.Add(root);
            //    }
            //}

            //if (context.FlattenRoot)
            //{
            //    var newPath = new List<Pair>(path);
            //    newPath.Insert(0, new Element {Name = context.RootElementName});
            //    path = newPath;
            //}

            foreach (var n in path)
            {
                var aliasDef = n as AliasDefinition;
                if (aliasDef != null)
                {
                    var rootElements = elements.ToList();
                    foreach (var rElement in rootElements)
                    {
                        EnumerateSubElementsAndAttributes(elements, attributes, rElement, ctxInfo);
                    }
                    elements.ForEach(e => e.InSequence = false);
                    continue;
                }

                var element = n as Element;
                if (element == null)
                {
                    targetType = null;
                    break;
                }
                var fElement = elements.FirstOrDefault(e => e.Name == element.Name);
                if (fElement == null)
                {
                    targetType = null;
                    var rootElements = elements.ToList();
                    foreach (var rElement in rootElements)
                    {
                        EnumerateSubElementsAndAttributes(elements, attributes, rElement, ctxInfo);
                    }
                    elements.ForEach(e => e.InSequence = false);
                    break;
                }

                targetType = fElement.GetElementType();
                var cTargetType = targetType as ComplexType;
                if (cTargetType != null)
                    ctxInfo.Scope = cTargetType;
                else
                    break;

                elements.Clear();
                attributes.Clear();

                elements.AddRange(cTargetType.Elements);
                attributes.AddRange(cTargetType.Attributes);

                foreach (var d in cTargetType.Descendants)
                {
                    var dElements = d.Elements.Where(e => elements.All(ce => ce.Name != e.Name)).ToList();
                    var dAttributes = d.Attributes.Where(a => attributes.All(ca => ca.Name != a.Name)).ToList();
                    elements.AddRange(dElements);
                    attributes.AddRange(dAttributes);
                }
            }
            ctxInfo.CurrentType = targetType;
            foreach (var element in elements)
                ctxInfo.Elements.Add(element);

            foreach (var attribute in attributes)
                ctxInfo.Attributes.Add(attribute);

        }

        private void EnumerateSubElementsAndAttributes(List<ElementInfo> elements, List<AttributeInfo> attributes, ElementInfo rElement, ContextInfo ctxInfo)
        {
            var type = rElement.GetElementType() as ComplexType;
            if (type != null)
            {

                var sAttributes = new List<AttributeInfo>();
                sAttributes.AddRange(type.Attributes);
                sAttributes.AddRange(type.Descendants.SelectMany(d => d.Attributes));
                ctxInfo.AllDescendants.AddRange(type.Descendants.Where(d => !ctxInfo.AllDescendants.Any(a => a.Name == d.Name && a.Namespace == d.Namespace)));
                foreach (var a in sAttributes)
                {
                    attributes.Add(a);
                }
                var sElements = new List<ElementInfo>();
                sElements.AddRange(type.Elements);
                sElements.AddRange(type.Descendants.SelectMany(d => d.Elements));
                foreach (var subElement in sElements)
                {
                    if (elements.Any(e => e.Name == subElement.Name && e.Namespace == subElement.Namespace))
                        continue;

                    elements.Add(subElement);
                    EnumerateSubElementsAndAttributes(elements, attributes, subElement, ctxInfo);
                }
            }
        }

        ElementType GetElementType(XmlSchemaType sType)
        {
            var sComplexType = sType as XmlSchemaComplexType;
            if (sComplexType != null)
            {
                var type = new ComplexType
                {
                    Name = sComplexType.QualifiedName.Name,
                    Namespace = sComplexType.QualifiedName.Namespace
                };

                var complexType = sComplexType.BaseXmlSchemaType as XmlSchemaComplexType;
                if (complexType != null)
                {
                    var sBase = complexType;
                    var typeRef = new ElementTypeRef
                    {
                        Name = sBase.QualifiedName.Name,
                        Namespace = sBase.QualifiedName.Namespace
                    };
                    type.BaseTypeRef = typeRef;
                    TypeRefs.Add(typeRef);

                }

                foreach (var sAttr in sComplexType.AttributeUses.Values.OfType<XmlSchemaAttribute>())
                {
                    var attr = GetAttributeInfo(sAttr, type);
                    type.Attributes.Add(attr);
                }

                var sequence = sComplexType.ContentTypeParticle as XmlSchemaSequence;
                if (sequence != null)
                    foreach (var item in sequence.Items.OfType<XmlSchemaElement>())
                    {
                        var elementInfo = GetElementInfo(item, type);
                        type.Elements.Add(elementInfo);
                    }
                return type;
            }
            var sSimpleType = sType as XmlSchemaSimpleType;
            if (sSimpleType != null)
            {
                var type = new SimpleType();
                type.Name = sSimpleType.QualifiedName.Name;
                type.Namespace = sSimpleType.QualifiedName.Namespace;
                var restrition = sSimpleType.Content as XmlSchemaSimpleTypeRestriction;
                if (restrition != null)
                {
                    foreach (var f in restrition.Facets.OfType<XmlSchemaEnumerationFacet>())
                    {
                        type.EnumValues.Add(f.Value);
                    }
                }
                return type;
            }
            return null;
        }

        private AttributeInfo GetAttributeInfo(XmlSchemaAttribute sAttr, ComplexType parent)
        {
            AttributeInfo attributeInfo = new AttributeInfo(parent);
            attributeInfo.Name = sAttr.QualifiedName.Name;
            attributeInfo.Namespace = sAttr.QualifiedName.Namespace;
            attributeInfo.Optional = sAttr.Use != XmlSchemaUse.Required;
            var schema = GetSchemaFromElement(sAttr);
            attributeInfo.Qualified = schema.ElementFormDefault == XmlSchemaForm.Qualified;

            return attributeInfo;
        }

        XmlSchema GetSchemaFromElement(XmlSchemaObject obj)
        {
            var el = obj;
            while (el != null)
            {
                var schema = el as XmlSchema;
                if (schema != null)
                    return schema;
                el = el.Parent;
            }
            return null;
        }

        ElementInfo GetElementInfo(XmlSchemaElement sElement, ComplexType parent)
        {
            ElementInfo elementInfo = new ElementInfo(parent)
            {
                Name = sElement.QualifiedName.Name,
                Namespace = sElement.QualifiedName.Namespace,
                Optional = sElement.MinOccurs == 0,
                MaxOccurs = sElement.MaxOccurs
            };

            var schema = GetSchemaFromElement(sElement);
            elementInfo.Qualified = schema.ElementFormDefault == XmlSchemaForm.Qualified;

            if (!string.IsNullOrEmpty(sElement.RefName.Name))
                elementInfo.Qualified = true;


            if (string.IsNullOrEmpty(sElement.ElementSchemaType.QualifiedName.Name))
            {
                var elementType = GetElementType(sElement.ElementSchemaType);
                elementInfo.Type = elementType;

            }
            else
            {
                if (sElement.ElementSchemaType is XmlSchemaSimpleType)
                {
                    var typeRef = new ElementTypeRef
                    {
                        Name = sElement.ElementSchemaType.QualifiedName.Name,
                        Namespace = sElement.ElementSchemaType.QualifiedName.Namespace
                    };
                    elementInfo.Type = typeRef;
                    TypeRefs.Add(typeRef);
                }
                else
                {
                    var typeRef = new ElementTypeRef
                    {
                        Name = sElement.ElementSchemaType.QualifiedName.Name,
                        Namespace = sElement.ElementSchemaType.QualifiedName.Namespace
                    };
                    elementInfo.Type = typeRef;
                    TypeRefs.Add(typeRef);
                }
            }
            return elementInfo;
        }

        public void Validate(XmlDocument doc, Action<XmlNode, string> onErrorAction)
        {
            var schemaSet = GetSchemaSet();
            if (schemaSet.Count == 0)
                return;
            doc.Schemas = schemaSet;

            doc.Validate((sender, args) =>
            {
                var exception = (XmlSchemaValidationException) args.Exception;
                onErrorAction((XmlNode)exception.SourceObject, args.Message);
            });
            
        }

        public IEnumerable<NamespaceInfo> GetNamespaces()
        {
            var allNamespaces = new List<NamespaceInfo>();
            var schemaSet = GetSchemaSet();

            foreach (var xmlSchema in schemaSet.Schemas().Cast<XmlSchema>())
            {
                var namespaces = xmlSchema.Namespaces.ToArray()
                    .Select(ns => new NamespaceInfo { Namespace = ns.Namespace, Name = ns.Name });
                allNamespaces.AddRange(namespaces);

            }

            var result = from ns in allNamespaces
                         where !new[]
                {
                    "http://www.w3.org/2001/XMLSchema",
                    "http://schemas.microsoft.com/2003/10/Serialization/"
                }.Contains(ns.Namespace)
                         group ns by ns.Namespace
                             into g
                         select g.FirstOrDefault();

            return result;
        }
    }
}
