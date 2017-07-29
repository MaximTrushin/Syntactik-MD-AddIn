using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using Syntactik.DOM;
using Syntactik.MonoDevelop.Completion;
using Syntactik.MonoDevelop.Projects;

namespace Syntactik.MonoDevelop.Schemas
{
    public class XsdSchemaProvider : ISchemaProvider
    {
        XmlSchemaSet _schemaset;
        readonly Dictionary<string, XmlSchema> _includes = new Dictionary<string, XmlSchema>();

        private readonly IProjectFilesProvider _provider;
        private List<ElementInfo> _fullListOfElements;
        private List<AttributeInfo> _fullListOfAttributes;

        public List<ElementType> Types { get; }
        public List<ElementInfo> GlobalElements { get; private set; }
        public List<AttributeInfo> GlobalAttributes { get; }
        public List<ElementTypeRef> TypeRefs { get; }

        public XsdSchemaProvider(IProjectFilesProvider provider)
        {
            _provider = provider;
            Types = new List<ElementType>();
            GlobalAttributes = new List<AttributeInfo>();
            TypeRefs = new List<ElementTypeRef>();
        }

        public IEnumerable<NamespaceInfo> GetNamespaces()
        {
            var allNamespaces = new List<NamespaceInfo>();
            var schemaSet = GetSchemaSet();

            foreach (var xmlSchema in schemaSet.Schemas().Cast<XmlSchema>())
            {
                var namespaces = xmlSchema.Namespaces.ToArray()
                    .Select(ns => new NamespaceInfo { Namespace = ns.Namespace, Prefix = ns.Name });
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
        public void Validate(XmlDocument doc, Action<XmlNode, string> onErrorAction)
        {
            var schemaSet = GetSchemaSet();
            if (schemaSet.Count == 0)
                return;
            doc.Schemas = schemaSet;

            doc.Validate((sender, args) =>
            {
                var exception = (XmlSchemaValidationException)args.Exception;
                onErrorAction((XmlNode)exception.SourceObject, args.Message);
            });
        }

        public void PopulateContextInfo(Context context, ContextInfo ctxInfo)
        {
            UpdateSchemaInfo();

            var lastNode = context.CompletionInfo.InTag == CompletionExpectation.NoExpectation
                ? context.CompletionInfo.LastPair
                : context.CompletionInfo.LastPair.Parent; //if we are inside pair which is not finished then context is the node's parent

            var contextElement = lastNode as Element;
            if (contextElement == null)
            {
                //If current node is not element then we show list of global elements and attributes,
                //because they do not dependend on context.
                ctxInfo.Elements.AddRange(GlobalElements);
                ctxInfo.Attributes.AddRange(GlobalAttributes);
                return;
            }

            var contextElementSchemaInfo = GlobalElements.FirstOrDefault(e => e.Name == contextElement.Name);
            if (contextElementSchemaInfo == null)
            {
                //Context element is not found in schema.
                //Displaying list of all elements and attributes difined in all schemas.
                ctxInfo.Elements.AddRange(FullListOfElements);
                ctxInfo.Attributes.AddRange(FullListOfAttributes);
                return;
            }

            ctxInfo.CurrentType = contextElementSchemaInfo.GetElementType();
            var complexType = ctxInfo.CurrentType as ComplexType;
            ctxInfo.Scope = complexType;

            if (complexType == null) return;

            var elements = new List<ElementInfo>(complexType.Elements);
            var attributes = new List<AttributeInfo>(complexType.Attributes);

            foreach (var descendant in complexType.Descendants)
            {
                elements.AddRange(descendant.Elements.Where(e => elements.All(ce => ce.Name != e.Name)));
                attributes.AddRange(descendant.Attributes.Where(a => attributes.All(ca => ca.Name != a.Name)));
            }
            
            ctxInfo.Elements.AddRange(elements);
            ctxInfo.Attributes.AddRange(attributes);
        }

        private List<ElementInfo> FullListOfElements => _fullListOfElements?? (_fullListOfElements = CalculateFullListOfElements());
        private List<AttributeInfo> FullListOfAttributes => _fullListOfAttributes ?? (_fullListOfAttributes = CalculateFullListOfAttributes());

        private List<AttributeInfo> CalculateFullListOfAttributes()
        {
            CalculateFullListOfElements();
            return _fullListOfAttributes;
        }

        private List<ElementInfo> CalculateFullListOfElements()
        {
            if (_fullListOfElements != null) return _fullListOfElements;

            var elements = new List<ElementInfo>(GlobalElements);
            var attributes = new List<AttributeInfo>(GlobalAttributes);
            foreach (var elementInfo in GlobalElements)
            {
                EnumerateSubElementsAndAttributes(elements, attributes, elementInfo);
            }
            elements.ForEach(e => e.InSequence = false);
            _fullListOfAttributes = attributes;
            _fullListOfElements = elements;
            return _fullListOfElements;
        }

        protected virtual IEnumerable<string> GetSchemaFiles()
        {
            return _provider.GetSchemaProjectFiles();
        }


        private void UpdateSchemaInfo() //TODO: Call update when project schema set changed.
        {
            if (GlobalElements != null) return;
            GlobalElements = new List<ElementInfo>();
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
        private void UpdateSchemaSet()
        {
            _schemaset = new XmlSchemaSet {XmlResolver = null};
            _includes.Clear();
            foreach (var projectFile in GetSchemaFiles())
            {
                if (File.Exists(projectFile))
                    if (!_includes.ContainsKey(projectFile))
                        using (XmlReader reader = XmlReader.Create(projectFile))
                        {
                            var schema = XmlSchema.Read(reader, null);
                            _includes.Add(projectFile, schema);
                            var dir = Path.GetDirectoryName(projectFile);
                            LoadIncludes(dir, _schemaset, schema);
                            _schemaset.Add(schema);
                        }
            }
            _schemaset.Compile();
        }

        private void LoadIncludes(string dir, XmlSchemaSet schemaSet, XmlSchema schema)
        {
            foreach (var i in schema.Includes)
            {
                if (i is XmlSchemaImport)
                {
                    var import = i as XmlSchemaImport;
                    var path = Path.Combine(dir, import.SchemaLocation);

                    if (_includes.ContainsKey(path))
                        import.Schema = _includes[path];
                    else
                    if (File.Exists(path))
                    {
                        using (XmlReader reader = XmlReader.Create(path))
                        {
                            var iSchema = XmlSchema.Read(reader, (sender, args) =>
                            {
                                System.Diagnostics.Debugger.Break();
                            });
                            _includes.Add(path, iSchema);
                            schemaSet.Add(iSchema);
                            LoadIncludes(dir, schemaSet, iSchema);
                            import.Schema = iSchema;
                        }

                    }
                }
                if (i is XmlSchemaInclude)
                {
                    var include = i as XmlSchemaInclude;
                    var path = Path.Combine(dir, include.SchemaLocation);

                    if (_includes.ContainsKey(path))
                        include.Schema = _includes[path];
                    else
                    if (File.Exists(path))
                    {
                        using (XmlReader reader = XmlReader.Create(path))
                        {
                            var iSchema = XmlSchema.Read(reader, (sender, args) =>
                            {
                                System.Diagnostics.Debugger.Break();
                            });
                            _includes.Add(path, iSchema);
                            schemaSet.Add(iSchema);
                            LoadIncludes(dir, schemaSet, iSchema);
                            include.Schema = iSchema;
                        }

                    }
                }

            }
        }

        protected virtual XmlSchemaSet GetSchemaSet()
        {
           
            if (_schemaset == null)
                UpdateSchemaSet();
            return _schemaset;
        }

        private void EnumerateSubElementsAndAttributes(List<ElementInfo> elements, List<AttributeInfo> attributes, ElementInfo element)
        {
            var type = element.GetElementType() as ComplexType;
            if (type != null)
            {

                var sAttributes = new List<AttributeInfo>();
                sAttributes.AddRange(type.Attributes);
                sAttributes.AddRange(type.Descendants.SelectMany(d => d.Attributes));
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
                    EnumerateSubElementsAndAttributes(elements, attributes, subElement);
                }
            }
        }

        private ElementType GetElementType(XmlSchemaType sType)
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
                var type = new SimpleType
                {
                    Name = sSimpleType.QualifiedName.Name,
                    Namespace = sSimpleType.QualifiedName.Namespace
                };
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
            AttributeInfo attributeInfo = new AttributeInfo(parent)
            {
                Name = sAttr.QualifiedName.Name,
                Namespace = sAttr.QualifiedName.Namespace,
                Optional = sAttr.Use != XmlSchemaUse.Required
            };
            var schema = GetSchemaFromElement(sAttr);
            attributeInfo.Qualified = schema.ElementFormDefault == XmlSchemaForm.Qualified;

            return attributeInfo;
        }

        private ElementInfo GetElementInfo(XmlSchemaElement sElement, ComplexType parent)
        {
            ElementInfo elementInfo = new ElementInfo(parent)
            {
                Name = sElement.QualifiedName.Name,
                Namespace = sElement.QualifiedName.Namespace,
                Optional = sElement.MinOccurs == 0,
                MaxOccurs = sElement.MaxOccurs
            };

            var schema = GetSchemaFromElement(sElement);
            elementInfo.Qualified = schema.ElementFormDefault == XmlSchemaForm.Qualified || !string.IsNullOrEmpty(sElement.RefName.Name);


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

        private XmlSchema GetSchemaFromElement(XmlSchemaObject obj)
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

    }
}