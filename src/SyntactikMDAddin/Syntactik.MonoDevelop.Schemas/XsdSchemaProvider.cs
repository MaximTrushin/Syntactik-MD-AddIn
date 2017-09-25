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


            var path = GetCompletionPath(context);
            var elements = new List<XmlSchemaElement>(GlobalElements);
            var attributes = new List<XmlSchemaAttribute>(GlobalAttributes);
            List<XmlSchemaElementInfo> elementInfo = null;
            foreach (var pair in path)
            {
                elementInfo = null;
                contextInfo.InSequence = false;

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
                    contextInfo.Scope = complexType;

                    if (complexType == null)
                    {
                        elements = new List<XmlSchemaElement>(GlobalElements);
                        attributes = new List<XmlSchemaAttribute>(GlobalAttributes);
                        continue;
                    }
                }
                GetAttributesOfSchemaType(complexType, out attributes);
                var existingElements = new List<Element>((pair as IContainer).Entities.OfType<Element>());
                elementInfo = new List<XmlSchemaElementInfo>();
                ProcessSchemaSequence(elementInfo, 
                    new List<XmlSchemaParticle> { complexType.ContentTypeParticle }, 
                    existingElements,
                    complexType.ContentTypeParticle.MinOccurs,
                    complexType.ContentTypeParticle.MaxOccurs);
                elements = new List<XmlSchemaElement>(elementInfo.Select(e => e.Element));
            }
            if (elementInfo != null) elements = RemoveExistingElements(elementInfo);
            var contextElement = context.CompletionInfo.InTag == CompletionExpectation.NoExpectation
                ? context.CompletionInfo.LastPair
                : context.CompletionInfo.LastPair.Parent; //if we are inside pair which is not finished then context is the node's parent

            var container = contextElement as IContainer;
            if (container == null || container.Entities.Count == 0 || container.Entities.All(e => e is Attribute))
                contextInfo.Attributes.AddRange(attributes.Distinct());
            contextInfo.Elements.AddRange(elements);
        }

        private void GetAttributesOfSchemaType(XmlSchemaComplexType complexType, out List<XmlSchemaAttribute> attributes)
        {
            var resultAttributes = new List<XmlSchemaAttribute>();
            resultAttributes.AddRange(
                complexType.AttributeUses.Values.OfType<XmlSchemaAttribute>().Where(
                    a => resultAttributes.All(ca => ca.Name != a.Name || ca.QualifiedName.Namespace != a.QualifiedName.Namespace)));
            attributes = resultAttributes;
        }

        private List<XmlSchemaElement> RemoveExistingElements(List<XmlSchemaElementInfo> elements)
        {
            var result = new List<XmlSchemaElement>();
            foreach (var element in elements)
            {
                if (!element.Existing)
                {
                   result.Add(element.Element);
                }
            }
            return result;
        }

        /// <summary>
        /// Adding elements to contextInfo based on schema sequence.
        /// </summary>
        /// <param name="elements"></param>
        /// <param name="schemaParticles"></param>
        /// <param name="existingElements"></param>
        /// <param name="minOccurs"></param>
        /// <param name="maxOccurs"></param>
        /// <returns>returns false if processing of schema has to stop </returns>
        private static bool ProcessSchemaSequence(List<XmlSchemaElementInfo> elements, List<XmlSchemaParticle> schemaParticles, 
            List<Element> existingElements, decimal minOccurs, decimal maxOccurs)
        {
            var elementAdded = false;
            for (int occurs = 0; occurs < maxOccurs; occurs++)
            {
                foreach (var schemaParticle in schemaParticles)
                {
                    var schemaElement = schemaParticle as XmlSchemaElement;
                    if (schemaElement != null)
                    {
                        for (int i = 0; i < Math.Max(schemaElement.MinOccurs, 1); i++)
                        {
                            if (existingElements.Count > 0)
                            {
                                if (!ProcessSchemaElement(schemaElement, existingElements))
                                {
                                    if (schemaElement.MinOccurs > 0)
                                    {
                                        return false;
                                    }
                                }
                                else
                                {
                                    elements.Add(new XmlSchemaElementInfo {Element = schemaElement, Existing = true});
                                }
                            }
                            else
                            {
                                elements.Add(new XmlSchemaElementInfo { Element = schemaElement, Existing = false });
                                //Element is missing. Adding it to the completion list.
                                if (schemaElement.MinOccurs != 0)
                                    return true;
                                //if element is optional then try to add another element to the list
                            }
                        }
                        continue;
                    }

                    var choice = schemaParticle as XmlSchemaChoice;
                    if (choice != null)
                    {
                        if (existingElements.Count > 0)
                        {
                            if (!ProcessSchemaChoice(elements, choice.Items.OfType<XmlSchemaParticle>().ToList(),
                                existingElements, choice.MinOccurs, choice.MaxOccurs)) return false;
                        }
                        else
                        {
                            ProcessSchemaChoice(elements, choice.Items.OfType<XmlSchemaParticle>().ToList(),
                                existingElements, choice.MinOccurs, choice.MaxOccurs);
                            elementAdded = true;
                        }
                        continue;
                    }
                    var seq = schemaParticle as XmlSchemaSequence;
                    if (seq != null)
                    {
                        if (
                            !ProcessSchemaSequence(elements, seq.Items.OfType<XmlSchemaParticle>().ToList(),
                                existingElements, seq.MinOccurs, seq.MaxOccurs)) return false;
                    }
                    
                    var all = schemaParticle as XmlSchemaAll;
                    if (all != null)
                    {
                        if (
                            !ProcessSchemaAll(elements, all.Items.OfType<XmlSchemaParticle>().ToList(),
                                existingElements, all.MinOccurs, all.MaxOccurs)) return false;
                    }
                    return false;

                }
                if (elementAdded) return false;
                if (existingElements.Count == 0 && occurs + 1 < minOccurs) return false;
            }
            return true;
        }

        /// <summary>
        /// Adding elements to contextInfo based on schema "all" sequence.
        /// </summary>
        /// <param name="elements"></param>
        /// <param name="schemaParticles"></param>
        /// <param name="existingElements"></param>
        /// <param name="minOccurs"></param>
        /// <param name="maxOccurs"></param>
        /// <returns>returns false if processing of schema has to stop </returns>
        private static bool ProcessSchemaAll(List<XmlSchemaElementInfo> elements, List<XmlSchemaParticle> schemaParticles,
            List<Element> existingElements, decimal minOccurs, decimal maxOccurs)
        {
            for (int occurs = 0; occurs < maxOccurs; occurs++)
            {
                foreach (var schemaParticle in schemaParticles)
                {
                    var schemaElement = schemaParticle as XmlSchemaElement;
                    if (schemaElement != null)
                    {
                        for (int i = 0; i < Math.Max(schemaElement.MinOccurs, 1); i++)
                        {
                            if (existingElements.Count > 0)
                            {
                                if (!ProcessSchemaElement(schemaElement, existingElements))
                                {
                                    if (schemaElement.MinOccurs > 0)
                                    {
                                        return false;
                                    }
                                }
                                else
                                {
                                    elements.Add(new XmlSchemaElementInfo { Element = schemaElement, Existing = true });
                                }
                            }
                            else
                            {
                                elements.Add(new XmlSchemaElementInfo { Element = schemaElement, Existing = false });
                                //Element is missing. Adding it to the completion list.
                                if (schemaElement.MinOccurs != 0)
                                    return true;
                                //if element is optional then try to add another element to the list
                            }
                        }
                        continue;
                    }

                }
                if (existingElements.Count == 0 && occurs + 1 < minOccurs) return false;
            }
            return true;
        }


        /// <summary>
        /// Adding elements to contextInfo based on schema choice.
        /// </summary>
        /// <param name="elements"></param>
        /// <param name="schemaParticles"></param>
        /// <param name="existingElements"></param>
        /// <param name="minOccurs"></param>
        /// <param name="maxOccurs"></param>
        /// <returns>returns false if processing of schema has to stop </returns>
        private static bool ProcessSchemaChoice(List<XmlSchemaElementInfo> elements, List<XmlSchemaParticle> schemaParticles, 
            List<Element> existingElements, decimal minOccurs, decimal maxOccurs)
        {
            var elementAdded = false;
            for (int occurs = 0; occurs < maxOccurs; occurs++)
            {
                foreach (var schemaParticle in schemaParticles)
                {
                    var schemaElement = schemaParticle as XmlSchemaElement;
                    if (schemaElement != null)
                    {
                        if (existingElements.Count > 0)
                        {
                            if (ProcessSchemaElement(schemaElement, existingElements))
                            {
                                break;
                            }
                            elements.Add(new XmlSchemaElementInfo { Element = schemaElement, Existing = true });
                        }
                        else
                        {
                            elements.Add(new XmlSchemaElementInfo { Element = schemaElement, Existing = false });
                            elementAdded = true;
                        }
                        continue;
                    }

                    var choice = schemaParticle as XmlSchemaChoice;
                    if (choice != null)
                    {
                        if (existingElements.Count > 0)
                        {
                            if (!ProcessSchemaChoice(elements, choice.Items.OfType<XmlSchemaParticle>().ToList(),
                                existingElements, choice.MinOccurs, choice.MaxOccurs)) return false;
                        }
                        else
                        {
                            ProcessSchemaChoice(elements, choice.Items.OfType<XmlSchemaParticle>().ToList(),
                                existingElements, choice.MinOccurs, choice.MaxOccurs);
                            elementAdded = true;
                        }
                        continue;
                    }
                    var seq = schemaParticle as XmlSchemaSequence;
                    if (seq != null)
                    {
                        if (existingElements.Count > 0)
                        {
                            if (!ProcessSchemaSequence(elements, seq.Items.OfType<XmlSchemaParticle>().ToList(),
                                existingElements, seq.MinOccurs, seq.MaxOccurs)) return false;
                        }
                        else
                        {
                            ProcessSchemaChoice(elements, seq.Items.OfType<XmlSchemaParticle>().ToList(),
                                existingElements, seq.MinOccurs, seq.MaxOccurs);
                            elementAdded = true;
                        }
                        continue;
                    }
                    var all = schemaParticle as XmlSchemaAll;
                    if (all != null)
                    {
                        if (
                            !ProcessSchemaAll(elements, all.Items.OfType<XmlSchemaParticle>().ToList(),
                                existingElements, all.MinOccurs, all.MaxOccurs)) return false;
                    }
                    return false;
                }
                if (elementAdded) return false;
                if (existingElements.Count == 0 && occurs + 1 < minOccurs) return false;
            }
            return true;
        }

        /// <summary>
        /// Try to match schema element with the top element in the existingElements.
        /// </summary>
        /// <param name="schemaElement"></param>
        /// <param name="existingElements"></param>
        /// <returns>Returns true if match is found.</returns>
        private static bool ProcessSchemaElement(XmlSchemaElement schemaElement, List<Element> existingElements)
        {
            if (existingElements.Count == 0) return false;
            if (existingElements[0].Name == schemaElement.Name
                &&
                CompletionHelper.GetNamespace(existingElements[0]) ==
                schemaElement.QualifiedName.Namespace)
            {
                existingElements.RemoveAt(0);
                return true;
            }
            return false;
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