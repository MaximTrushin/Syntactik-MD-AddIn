using System;
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
        private List<XmlSchemaElement> _fullListOfElements;
        private List<XmlSchemaAttribute> _fullListOfAttributes;

        public List<TypeInfo> Types { get; }
        public List<XmlSchemaElement> GlobalElements { get; private set; }
        public List<XmlSchemaAttribute> GlobalAttributes { get; }
        public List<XmlSchemaType> TypeRefs { get; }

        public XsdSchemaProvider(IProjectFilesProvider provider)
        {
            _provider = provider;
            Types = new List<TypeInfo>();
            GlobalAttributes = new List<XmlSchemaAttribute>();
            TypeRefs = new List<XmlSchemaType>();
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
            contextInfo.AllTypes = Types;
            var contextElement = context.CompletionInfo.InTag == CompletionExpectation.NoExpectation
                ? context.CompletionInfo.LastPair
                : context.CompletionInfo.LastPair.Parent; //if we are inside pair which is not finished then context is the node's parent


            var path = GetCompletionPath(context);
            var elements = new List<XmlSchemaElement>(GlobalElements);
            var attributes = new List<XmlSchemaAttribute>(GlobalAttributes);
            foreach (var pair in path)
            {
                if (pair is Document || pair is Module)
                {
                    elements = new List<XmlSchemaElement>(GlobalElements);
                    attributes = new List<XmlSchemaAttribute>(GlobalAttributes);
                    continue;
                }

                XmlSchemaComplexType complexType;
                if (pair is AliasDefinition || pair is Alias || pair is Argument || pair is Parameter)
                {
                    complexType = GetSchemaType(pair, contextInfo, elements) as XmlSchemaComplexType;

                    if (complexType == null)
                    {
                        attributes = new List<XmlSchemaAttribute>(FullListOfAttributes);
                        elements = new List<XmlSchemaElement>(FullListOfElements);
                        continue;
                    }
                }
                else
                {
                    var element = pair as Element;
                    if (element == null) //TODO: Create logic for scope
                    {
                        elements = new List<XmlSchemaElement>(GlobalElements);
                        attributes = new List<XmlSchemaAttribute>(GlobalAttributes);
                        continue;
                    }

                    contextInfo.CurrentType = GetSchemaType(pair, contextInfo, elements);

                    complexType = contextInfo.CurrentType as XmlSchemaComplexType;
                    contextInfo.Scope = string.IsNullOrEmpty(complexType?.QualifiedName.Name)?null:complexType;

                    if (complexType == null)
                    {
                        elements = new List<XmlSchemaElement>(GlobalElements);
                        attributes = new List<XmlSchemaAttribute>(GlobalAttributes);
                        continue;
                    }
                }
                GetEntitiesOfSchemaType(complexType, out attributes, out elements);

                contextInfo.InSequence = true;
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
                            && CompletionHelper.GetNamespace(existingElement) == elementFromSchema.QualifiedName.Namespace)
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

        private void GetEntitiesOfSchemaType(XmlSchemaComplexType complexType, out List<XmlSchemaAttribute> attributes, out List<XmlSchemaElement> elements)
        {
            var resultAttributes = new List<XmlSchemaAttribute>();
            var resultElements = new List<XmlSchemaElement>();

            resultElements.AddRange(
                GetElements(complexType).Where(
                    e => resultElements.All(ce => ce.Name != e.Name || ce.QualifiedName.Namespace != e.QualifiedName.Namespace)));
            resultAttributes.AddRange(
                complexType.AttributeUses.Values.OfType<XmlSchemaAttribute>().Where(
                    a => resultAttributes.All(ca => ca.Name != a.Name || ca.QualifiedName.Namespace != a.QualifiedName.Namespace)));

            attributes = resultAttributes;
            elements = resultElements;
        }

        private IEnumerable<XmlSchemaElement> GetElements(XmlSchemaComplexType type)
        {
            var sequence = type.ContentTypeParticle as XmlSchemaSequence;
            if (sequence == null) yield break;
            foreach (var item in sequence.Items)
            {
                var element = item as XmlSchemaElement;
                if (element != null)
                {
                    yield return element;
                }
                var choice = item as XmlSchemaChoice;
                if (choice != null)
                {
                    foreach (var xmlSchemaElement in GetElements(choice))
                    {
                        yield return xmlSchemaElement;
                    }
                    continue;
                }
                var seq = item as XmlSchemaSequence;
                if (seq != null)
                {
                    foreach (var xmlSchemaElement in GetElements(seq))
                    {
                        yield return xmlSchemaElement;
                    }
                    continue;
                }
            }
        }

        private IEnumerable<XmlSchemaElement> GetElements(XmlSchemaChoice choice)
        {
            foreach (var item in choice.Items)
            {
                var element = item as XmlSchemaElement;
                if (element != null)
                {
                    yield return element;
                    continue;
                }

                var sequence = item as XmlSchemaSequence;
                if (sequence != null)
                {
                    foreach (var xmlSchemaElement in GetElements(sequence))
                    {
                        yield return xmlSchemaElement;
                    }
                    continue;
                }
                var ch = item as XmlSchemaChoice;
                if (ch != null)
                {
                    foreach (var xmlSchemaElement in GetElements(ch))
                    {
                        yield return xmlSchemaElement;
                    }
                    continue;
                }
            }
        }

        private IEnumerable<XmlSchemaElement> GetElements(XmlSchemaSequence sequence)
        {
            foreach (var item in sequence.Items)
            {
                var element = item as XmlSchemaElement;
                if (element != null)
                {
                    yield return element;
                }

                var choice = item as XmlSchemaChoice;
                if (choice != null)
                {
                    foreach (var xmlSchemaElement in GetElements(choice))
                    {
                        yield return xmlSchemaElement;
                    }
                }

                var seq = item as XmlSchemaSequence;
                if (seq != null)
                {
                    foreach (var xmlSchemaElement in GetElements(seq))
                    {
                        yield return xmlSchemaElement;
                    }
                    continue;
                }

            }
        }

        internal IEnumerable<XmlSchemaComplexType> GetDescendancyPath(XmlSchemaComplexType complexType)
        {
            yield return complexType;
            var typeInfo = GetTypeInfo(complexType);
            if (typeInfo == null) yield break;
            foreach (var d in typeInfo.Descendants)
            {
                yield return (XmlSchemaComplexType)d.SchemaType;
            }
        }

        private XmlSchemaType GetSchemaType(Pair pair, ContextInfo contextInfo, List<XmlSchemaElement> elements)
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
                    var complexType = contextInfo.AllTypes.FirstOrDefault(t => t.SchemaType.Name == typeName && t.SchemaType.QualifiedName.Namespace == typeNamespace);
                    if (complexType != null) return complexType.SchemaType;
                }
                if (!(pair is Element)) return null;

                var element = pair as Element;
                string @namespace = CompletionHelper.GetNamespace(element);
                var contextElementSchemaInfo =
                    elements.FirstOrDefault(e => e.Name == element.Name && (e.QualifiedName.Namespace ?? "") == @namespace);

                return contextElementSchemaInfo?.ElementSchemaType;
            }
            return null;
        }

        /// <summary>
        /// Deletes all leading existing elements from the list which are not found in the schema info.
        /// </summary>
        /// <param name="existingElements"></param>
        /// <param name="elements"></param>
        private void DeleteLeadingNonSchemaElements(List<Element> existingElements, List<XmlSchemaElement> elements)
        {
            var existingElementsToRemove = existingElements.Where(existingElement => !elements.Any(e => existingElement.Name == e.Name && CompletionHelper.GetNamespace(existingElement) == e.QualifiedName.Namespace)).ToList();
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

        private List<XmlSchemaElement> FullListOfElements => _fullListOfElements?? (_fullListOfElements = CalculateFullListOfElements());
        private List<XmlSchemaAttribute> FullListOfAttributes => _fullListOfAttributes ?? (_fullListOfAttributes = CalculateFullListOfAttributes());

        private List<XmlSchemaAttribute> CalculateFullListOfAttributes()
        {
            CalculateFullListOfElements();
            return _fullListOfAttributes;
        }

        private List<XmlSchemaElement> CalculateFullListOfElements()
        {
            if (_fullListOfElements != null) return _fullListOfElements;

            _fullListOfElements = new List<XmlSchemaElement>(GlobalElements);
            _fullListOfAttributes = new List<XmlSchemaAttribute>(GlobalAttributes);
            GlobalElements.ForEach(
                e => EnumerateSubElementsAndAttributes(_fullListOfElements, _fullListOfAttributes, e.ElementSchemaType));
            Types.ForEach(t => EnumerateSubElementsAndAttributes(_fullListOfElements, _fullListOfAttributes, t.SchemaType));
            return _fullListOfElements;
        }

        protected virtual IEnumerable<string> GetSchemaFiles()
        {
            return _provider.GetSchemaProjectFiles();
        }

        private void UpdateSchemaInfo()
        {
            if (GlobalElements != null) return;
            GlobalElements = new List<XmlSchemaElement>();
            TypeRefs.Clear();
            Types.Clear();
            GlobalAttributes.Clear();

            var schemaSet = GetSchemaSet();
            foreach (XmlSchemaType schemaType in schemaSet.GlobalTypes.Values)
            {
                if (!(schemaType is XmlSchemaComplexType) || schemaType.Name == null) continue;
                var typeInfo = new TypeInfo(schemaType);
                
                //Processing descendants
                Types.ForEach(t =>
                {
                    if (t.BaseSchemaType != schemaType) return;
                    t.BaseTypeInfo = typeInfo;

                    //Adding descendants to the new typeInfo object
                    if (typeInfo.Descendants.FirstOrDefault(i => i.SchemaType == t.SchemaType) == null)
                        typeInfo.Descendants.Add(t);
                    t.Descendants.ForEach(t2 =>
                    {
                        if (typeInfo.Descendants.FirstOrDefault(i => i.SchemaType == t2.SchemaType) == null)
                            typeInfo.Descendants.Add(t2);
                    });
                });

                //Processing ancestors
                if (schemaType.BaseXmlSchemaType?.Name != null)
                {
                    typeInfo.BaseSchemaType = schemaType.BaseXmlSchemaType;
                    typeInfo.BaseTypeInfo = GetTypeInfo(Types, schemaType.BaseXmlSchemaType);

                    var ancestor = typeInfo.BaseTypeInfo;
                    while (ancestor != null)
                    {
                        if (ancestor.Descendants.FirstOrDefault(i => i.SchemaType == typeInfo.SchemaType) == null)
                            ancestor.Descendants.Add(typeInfo);
                        typeInfo.Descendants.ForEach(t =>
                        {
                            if (ancestor.Descendants.FirstOrDefault(i => i.SchemaType == t.SchemaType) == null)
                                ancestor.Descendants.Add(t);
                        });
                        ancestor = ancestor.BaseTypeInfo;
                    }
                }
                Types.Add(typeInfo);
            }
            foreach (XmlSchemaElement sElement in schemaSet.GlobalElements.Values)
            {
                GlobalElements.Add(sElement);
            }
            foreach (XmlSchemaAttribute sAttrib in schemaSet.GlobalAttributes.Values)
            {
                GlobalAttributes.Add(sAttrib);
            }
        }

        internal static TypeInfo GetTypeInfo(List<TypeInfo> types, XmlSchemaType schemaType)
        {
            return types.FirstOrDefault(t => t.SchemaType == schemaType);
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

        private void EnumerateSubElementsAndAttributes(List<XmlSchemaElement> elementList, List<XmlSchemaAttribute> attributeList, 
                        XmlSchemaType schemaType)
        {
            var schemaComplexType = schemaType as XmlSchemaComplexType;
            if (schemaComplexType == null) return;

            var attributes = new List<XmlSchemaAttribute>();
            attributes.AddRange(schemaComplexType.AttributeUses.Values.OfType<XmlSchemaAttribute>());
            if (schemaComplexType.Name != null)
                attributes.AddRange(GetTypeInfo(schemaComplexType).Descendants.Where(t => t.SchemaType is XmlSchemaComplexType).
                    SelectMany(d => ((XmlSchemaComplexType) d.SchemaType).AttributeUses.Values.OfType<XmlSchemaAttribute>()));
            
            attributes.ForEach(e =>
            {
                if (!attributeList.Any(a => a.Name == e.Name && a.QualifiedName?.Namespace == e.QualifiedName?.Namespace))
                    attributeList.Add(e);
            });
            
            var elements = new List<XmlSchemaElement>();
            elements.AddRange(GetElements(schemaComplexType));
            if (schemaComplexType.Name != null)
                elements.AddRange(GetTypeInfo(schemaComplexType).Descendants.Where(t => t.SchemaType is XmlSchemaComplexType).
                SelectMany(t => GetElements((XmlSchemaComplexType) t.SchemaType)));
                
            foreach (var subElement in elements)
            {
                if (elementList.Any(e => e.QualifiedName.Name == subElement.QualifiedName.Name && e.QualifiedName.Namespace == subElement.QualifiedName.Namespace))
                    continue;

                elementList.Add(subElement);
                EnumerateSubElementsAndAttributes(elementList, attributeList, subElement.ElementSchemaType);
            }
        }

        private TypeInfo GetTypeInfo(XmlSchemaComplexType type)
        {
            return GetTypeInfo(Types, type);
        }
    }
}