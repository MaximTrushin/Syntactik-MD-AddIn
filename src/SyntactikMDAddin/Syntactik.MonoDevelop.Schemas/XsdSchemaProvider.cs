using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using Syntactik.DOM;
using Syntactik.MonoDevelop.Completion;
using Syntactik.MonoDevelop.Projects;
using Attribute = Syntactik.DOM.Attribute;

namespace Syntactik.MonoDevelop.Schemas
{
    public class XsdSchemaProvider : ISchemaProvider
    {
        XmlSchemaSet _schemaset;
        readonly Dictionary<string, XmlSchema> _includes = new Dictionary<string, XmlSchema>();

        private readonly IProjectFilesProvider _provider;
        private List<ElementInfo> _fullListOfElements;
        private List<AttributeInfo> _fullListOfAttributes;
        private List<ComplexType> _allTypes;

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

        public void PopulateContextInfo(Context context, ContextInfo contextInfo)
        {
            UpdateSchemaInfo();
            contextInfo.AllTypes = AllTypes;
            var contextElement = context.CompletionInfo.InTag == CompletionExpectation.NoExpectation
                ? context.CompletionInfo.LastPair
                : context.CompletionInfo.LastPair.Parent; //if we are inside pair which is not finished then context is the node's parent


            var path = GetCompletionPath(context);
            var elements = new List<ElementInfo>(GlobalElements);
            var attributes = new List<AttributeInfo>(GlobalAttributes);
            foreach (var pair in path)
            {
                if (pair is Document || pair is Module)
                {
                    elements = new List<ElementInfo>(GlobalElements);
                    attributes = new List<AttributeInfo>(GlobalAttributes);
                    continue;
                }

                ComplexType complexType;
                if (pair is AliasDefinition || pair is Alias || pair is Argument || pair is Parameter)
                {
                    complexType = GetSchemaType(pair, contextInfo, elements) as ComplexType;

                    if (complexType == null)
                    {
                        attributes = new List<AttributeInfo>(FullListOfAttributes);
                        elements = new List<ElementInfo>(FullListOfElements);
                        continue;
                    }
                }
                else
                {
                    var element = pair as Element;
                    if (element == null) //TODO: Create logic for scope
                    {
                        elements = new List<ElementInfo>(GlobalElements);
                        attributes = new List<AttributeInfo>(GlobalAttributes);
                        continue;
                    }

                    contextInfo.CurrentType = GetSchemaType(pair, contextInfo, elements);

                    complexType = contextInfo.CurrentType as ComplexType;
                    contextInfo.Scope = complexType;

                    if (complexType == null)
                    {
                        elements = new List<ElementInfo>(GlobalElements);
                        attributes = new List<AttributeInfo>(GlobalAttributes);
                        continue;
                    }
                }

                GetEntitiesOfSchemaType(complexType, out attributes, out elements);

                elements = new List<ElementInfo>(elements.Select(
                        e =>
                        {
                            var clone = (ElementInfo) e.Clone();
                            clone.InSequence = true;
                            return clone;
                        }
                    ));
            }
            
            var container = contextElement as IContainer;
            var existingElements = container != null ? new List<Element>(container.Entities.OfType<Element>()) : new List<Element>();
            if (container == null || container.Entities.Count == 0 || container.Entities.All(e => e is Attribute))
                contextInfo.Attributes.AddRange(attributes.Distinct());

            foreach (var elementFromSchema in elements)
            {
                var existingElementsToRemove = new List<Element>();
                var found = false;
                var maxCount = elementFromSchema.MaxOccurs;

                //1. Delete leading existing elements that are not part of schema
                DeleteLeadingNonSchemaElements(existingElements, elements);
                foreach (var existingElement in existingElements)
                {
                    if (existingElement.Name == elementFromSchema.Name
                            && CompletionHelper.GetNamespace(existingElement) == elementFromSchema.Namespace)
                    {
                        existingElementsToRemove.Add(existingElement);
                        maxCount--;
                        if (maxCount != 0) continue;
                        found = true;
                    }
                    break;
                }
                existingElementsToRemove.ForEach(e => existingElements.Remove(e));
                if (!found && existingElements.Count == 0)
                {
                    contextInfo.Elements.Add(elementFromSchema);
                }
            }
        }

        private void GetEntitiesOfSchemaType(ComplexType complexType, out List<AttributeInfo> attributes, out List<ElementInfo> elements)
        {
            var resultAttributes = new List<AttributeInfo>();
            var resultElements = new List<ElementInfo>();

            foreach (var descendant in GetDescendancyPath(complexType))
            {
                resultElements.AddRange(
                    descendant.Elements.Where(
                        e => resultElements.All(ce => ce.Name != e.Name || ce.Namespace != e.Namespace)));
                resultAttributes.AddRange(
                    descendant.Attributes.Where(
                        a => resultAttributes.All(ca => ca.Name != a.Name || ca.Namespace != a.Namespace)));
            }
            attributes = resultAttributes;
            elements = resultElements;
        }

        internal static IEnumerable<ComplexType> GetDescendancyPath(ComplexType complexType)
        {
            return GetDescendancyReversedPath(complexType).Reverse();
        }

        internal static IEnumerable<ComplexType> GetDescendancyReversedPath(ComplexType complexType)
        {
            while (complexType != null && complexType.Name != "anyType")
            {
                yield return complexType;
                complexType = complexType.BaseType;
            }
        }

        private ElementType GetSchemaType(Pair pair, ContextInfo contextInfo, List<ElementInfo> elements)
        {
            var container = pair as IContainer;
            if (container != null)
            {
                var explicitType = container.Entities.OfType<Attribute>().FirstOrDefault(a => a.Name == "type" && ((INsNode)a).NsPrefix == "xsi")?.Value;
                var typeInfo = explicitType?.Split(':');
                if (typeInfo?.Length > 1)
                {
                    var typeName = typeInfo[1];
                    var typeNamespace = CompletionHelper.GetNamespace(pair, typeInfo[0]);
                    var complexType = contextInfo.AllTypes.FirstOrDefault(t => t.Name == typeName && t.Namespace == typeNamespace);
                    if (complexType != null) return complexType;
                }
                if (!(pair is Element)) return null;

                var element = pair as Element;
                string @namespace = CompletionHelper.GetNamespace(element);
                var contextElementSchemaInfo =
                    elements.FirstOrDefault(e => e.Name == element.Name && (e.Namespace ?? "") == @namespace);
                return contextElementSchemaInfo?.GetElementType();
            }

            return null;
        }

        /// <summary>
        /// Deletes all leading existing elements from the list which are not found in the schema info.
        /// </summary>
        /// <param name="existingElements"></param>
        /// <param name="elements"></param>
        private void DeleteLeadingNonSchemaElements(List<Element> existingElements, List<ElementInfo> elements)
        {
            var existingElementsToRemove = existingElements.Where(existingElement => !elements.Any(e => existingElement.Name == e.Name && CompletionHelper.GetNamespace(existingElement) == e.Namespace)).ToList();
            existingElementsToRemove.ForEach(e => existingElements.Remove(e));
        }

        private static IEnumerable<Pair> GetCompletionPath(Context context)
        {
            var contextElement = context.CompletionInfo.LastPair;
            contextElement = context.CompletionInfo.InTag == CompletionExpectation.NoExpectation
                ? contextElement
                : contextElement.Parent; //if we are inside pair which is not finished then context is the node's parent
            return GetReversedCompletionPath(contextElement).Reverse();
        }

        /// <summary>
        /// Returns reversed completion path.
        /// It goes up the chain using property parent till first pair which is not Element.
        /// </summary>
        /// <param name="contextElement"></param>
        /// <returns></returns>
        private static IEnumerable<Pair> GetReversedCompletionPath(Pair contextElement)
        {
            Pair pair = contextElement;
            while (pair != null)
            {
                yield return pair;
                pair = pair.Parent;
                if (!(pair is IContainer)) yield break;
            }
        }

        private List<ElementInfo> FullListOfElements => _fullListOfElements?? (_fullListOfElements = CalculateFullListOfElements());
        private List<AttributeInfo> FullListOfAttributes => _fullListOfAttributes ?? (_fullListOfAttributes = CalculateFullListOfAttributes());
        private List<ComplexType> AllTypes => _allTypes?? (_allTypes = CalculateListOfAllTypes());

        private List<ComplexType> CalculateListOfAllTypes()
        {
            CalculateFullListOfElements();
            return _allTypes;
        }

        private List<AttributeInfo> CalculateFullListOfAttributes()
        {
            CalculateFullListOfElements();
            return _fullListOfAttributes;
        }

        private List<ElementInfo> CalculateFullListOfElements()
        {
            if (_fullListOfElements != null) return _fullListOfElements;

            _fullListOfElements = new List<ElementInfo>(GlobalElements);
            _fullListOfAttributes = new List<AttributeInfo>(GlobalAttributes);
            _allTypes = new List<ComplexType>();
            foreach (var elementInfo in GlobalElements)
            {
                EnumerateSubElementsAndAttributes(_fullListOfElements, _fullListOfAttributes, _allTypes, elementInfo);
            }
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

        private void EnumerateSubElementsAndAttributes(List<ElementInfo> elementList, List<AttributeInfo> attributeList, List<ComplexType> allDescendants, ElementInfo element)
        {
            var type = element.GetElementType() as ComplexType;
            if (type == null) return;

            if (!string.IsNullOrEmpty(type.Name) && !allDescendants.Any(a => a.Name == type.Name && a.Namespace == type.Namespace))
                allDescendants.Add(type);

            allDescendants.AddRange(
                type.Descendants.Where(d => !string.IsNullOrEmpty(d.Name) && !allDescendants.Any(a => a.Name == d.Name && a.Namespace == d.Namespace)));

            var attributes = new List<AttributeInfo>();
            attributes.AddRange(type.Attributes);
            attributes.AddRange(type.Descendants.SelectMany(d => d.Attributes));
            attributeList.AddRange(attributes);
            
            var elements = new List<ElementInfo>();
            elements.AddRange(type.Elements);
            elements.AddRange(type.Descendants.SelectMany(d => d.Elements));
            foreach (var subElement in elements)
            {
                if (elementList.Any(e => e.Name == subElement.Name && e.Namespace == subElement.Namespace))
                    continue;

                elementList.Add(subElement);
                EnumerateSubElementsAndAttributes(elementList, attributeList, allDescendants, subElement);
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

                var baseComplexType = sComplexType.BaseXmlSchemaType as XmlSchemaComplexType;
                if (baseComplexType != null)
                {
                    var typeRef = new ElementTypeRef
                    {
                        Name = baseComplexType.QualifiedName.Name,
                        Namespace = baseComplexType.QualifiedName.Namespace
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
                var simpleType = new SimpleType
                {
                    Name = sSimpleType.QualifiedName.Name,
                    Namespace = sSimpleType.QualifiedName.Namespace
                };
                var restrition = sSimpleType.Content as XmlSchemaSimpleTypeRestriction;
                if (restrition != null)
                {
                    foreach (var f in restrition.Facets.OfType<XmlSchemaEnumerationFacet>())
                    {
                        simpleType.EnumValues.Add(f.Value);
                    }
                }
                return simpleType;
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