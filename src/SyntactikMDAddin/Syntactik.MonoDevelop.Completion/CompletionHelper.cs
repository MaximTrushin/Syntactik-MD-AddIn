﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using MonoDevelop.Ide.CodeCompletion;
using Syntactik.DOM;
using Syntactik.DOM.Mapped;
using Syntactik.MonoDevelop.Schemas;
using Alias = Syntactik.DOM.Mapped.Alias;
using AliasDefinition = Syntactik.DOM.Mapped.AliasDefinition;
using Argument = Syntactik.DOM.Mapped.Argument;
using Attribute = Syntactik.DOM.Attribute;
using Module = Syntactik.DOM.Module;
using NamespaceDefinition = Syntactik.DOM.NamespaceDefinition;

namespace Syntactik.MonoDevelop.Completion
{
    public class CompletionHelper
    {
        private static int AttributePriority = 10000;
        private static int ElementPriority = 0;

        internal static void DoAliasCompletion(CompletionDataList completionList, CompletionContext context,
            CodeCompletionContext editorCompletionContext, Func<Dictionary<string, Syntactik.DOM.AliasDefinition>> aliasListFunc, bool valuesOnly = false)
        {
            var items = new List<CompletionData>();
            var category = new SyntactikCompletionCategory { DisplayText = "Aliases", Order = 3 };

            var aliases = GetListOfBlockAliasDefinitions(aliasListFunc, valuesOnly).Select(a => a.Value.Name);
            var rawPrefix = GetCompletionPrefixForCurrentPair(context, editorCompletionContext, CompletionExpectation.Alias);
            var prefix = rawPrefix ?? string.Empty;
            if (!prefix.EndsWith("."))
            {
                var pos = prefix.LastIndexOf(".", StringComparison.Ordinal);
                if (pos > 0)
                    editorCompletionContext.TriggerWordLength = prefix.Length - (pos + 1);
                prefix = pos > 0 ? prefix.Substring(0, pos + 1) : string.Empty;

            }
            else
            {
                editorCompletionContext.TriggerWordLength = 0;
                completionList.TriggerWordLength = 0;
            }

            var grouped = aliases.Where(a => a.ToLower().StartsWith(prefix.ToLower())).Select(a => NameElement(a, prefix)).Distinct();
            foreach (var alias in grouped)
            {
                CompletionData data;
                if (alias.EndsWith("."))
                {
                    data = new CompletionItem { ItemType = ItemType.AliasNamespace, Priority = int.MinValue};
                    items.Add(data);
                    data.CompletionCategory = category;
                    data.Icon = SyntactikIcons.Namespace;
                    var name = alias.Substring(0, alias.Length - 1);
                    if (string.IsNullOrEmpty(prefix))
                        data.DisplayText = "$" + name;
                    else
                        data.DisplayText = name;
                    data.CompletionText = data.DisplayText;
                }
                else
                {
                    data = new CompletionItem { ItemType = ItemType.Alias, Priority = int.MinValue };
                    items.Add(data);
                    data.CompletionCategory = category;
                    data.Icon = SyntactikIcons.Alias;
                    data.DisplayText = string.IsNullOrEmpty(prefix) ? "$" + alias : alias;
                    data.CompletionText = data.DisplayText;
                }
            }
            if (prefix.EndsWith("."))
            {
                //If alias is composite then increasing TriggerOffset by the length of prefix. +1 is for $
                editorCompletionContext.TriggerOffset += prefix.Length + 1;
            }
            completionList.AddRange(items.OrderBy(i => i.DisplayText));
        }
        internal static void DoAttributeCompletion(CompletionDataList completionList, CompletionContext completionContext,
            CodeCompletionContext editorCompletionContext, ContextInfo schemaInfo, SchemasRepository schemasRepository)
        {
            if (schemasRepository == null) return;
            if (schemaInfo.CurrentType is SimpleType)
                return;

            var items = new List<CompletionData>();
            var completionCategory = new SyntactikCompletionCategory { DisplayText = "Attributes", Order = 1 };

            var attributes = schemaInfo.Attributes;
            var contextElement = GetContextElement(completionContext);

            foreach (var attribute in attributes)
            {
                var newNs = false;
                string prefix = string.IsNullOrEmpty(attribute.Namespace) ? "" : GetNamespacePrefix(attribute.Namespace, completionContext.LastPair, schemasRepository, out newNs);
                //Attribute has max quantity = 1. Checking if this attribute is already added to the element
                if (contextElement != null && contextElement.Entities.Any(e => e is Attribute && 
                    e.Name == attribute.Name && (((INsNode) e).NsPrefix??"") == (prefix??""))) continue;

                var displayText = (string.IsNullOrEmpty(prefix) ? "" : (prefix + ".")) + attribute.Name;
                
                var data = new CompletionItem {
                    ItemType = ItemType.Attribute,
                    Namespace = attribute.Namespace,
                    NsPrefix = prefix,
                    Priority = attribute.Builtin?AttributePriority: (AttributePriority + 1) //Low priority for xsi attributes
                };
                items.Add(data);
                data.DisplayText = $"@{displayText} = ";
                data.CompletionText = $"@{displayText} =";
                data.CompletionCategory = completionCategory;
                data.Icon = attribute.Optional ? SyntactikIcons.OptAttribute : SyntactikIcons.Attribute;
                data.UndeclaredNamespaceUsed = newNs;
            }
            completionList.AddRange(items.OrderBy(i => i.DisplayText));
        }

        internal static IContainer GetContextElement(CompletionContext completionContext)
        {
            var container = completionContext.LastPair as IContainer;
            if (container == null && completionContext.LastPair is DOM.Attribute)
                container = ((DOM.Attribute) completionContext.LastPair).Parent as IContainer;
            return container;
        }
        internal static void DoElementCompletion(CompletionDataList completionList, CompletionContext completionContext,
                CodeCompletionContext editorCompletionContext, ContextInfo schemaInfo, SchemasRepository schemasRepository)
        {
            if (schemasRepository == null) return;
            var items = new List<CompletionData>();
            var completionCategory = new SyntactikCompletionCategory { DisplayText = "Elements", Order = 2 };
            var priority = ElementPriority;
            bool xsiUndeclared;
            GetNamespacePrefix(XmlSchemaInstanceNamespace.Url, completionContext.LastPair, schemasRepository, out xsiUndeclared);
            foreach (var element in schemaInfo.Elements)
            {
                bool newNs = false;
                string prefix = string.IsNullOrEmpty(element.Namespace) ? "" : GetNamespacePrefix(element.Namespace, completionContext.LastPair, schemasRepository, out newNs);
                var displayText = (string.IsNullOrEmpty(prefix) ? "" : (prefix + ".")) + element.Name;

                var elementType = element.GetElementType();
                bool haveExtensions;
                var types = GetElementTypes(elementType, out haveExtensions);
                foreach (var type in types)
                {
                    var data = new CompletionItem
                    {
                        ItemType = ItemType.Entity,
                        Namespace = element.Namespace,
                        NsPrefix = prefix,
                        ElementType = type,
                        Priority = priority,
                        CompletionContextPair = completionContext.LastPair
                    };
                    items.Add(data);
                    string postfix = string.Empty;
                    if (type != elementType || haveExtensions)
                    {
                        postfix = $" ({type.Name})";
                    }

                    if (type.IsComplex)
                    {
                        data.DisplayText = $"{displayText}:{postfix} ";
                        data.CompletionText = $"{displayText}: ";
                    }
                    else
                    {
                        data.DisplayText = $"{displayText} = {postfix}";
                        data.CompletionText = $"{displayText} =";
                    }

                    data.CompletionCategory = completionCategory;
                    data.Icon = element.Optional ? SyntactikIcons.OptElement : SyntactikIcons.Element;
                    data.UndeclaredNamespaceUsed = newNs;
                    data.XsiUndeclared = xsiUndeclared;
                }
                if (element.InSequence) priority--;
            }
            completionList.AddRange(items);
        }

        internal static void DoNamespaceDefinitionCompletion(CompletionDataList completionList, CompletionContext context, CodeCompletionContext editorCompletionContext, ContextInfo schemaInfo, SchemasRepository schemasRepository)
        {
            if (schemasRepository == null) return;
            ModuleMember moduleMember;
            Module module;
            List<NamespaceDefinition> declaredNamespaceDefinitions = GetDeclaredNamespaceDefinitions(context, out module, out moduleMember);
            var undeclaredNamespaces =
                schemasRepository.GetNamespaces()
                    .Where(ns => declaredNamespaceDefinitions.All(dns => dns.Value != ns.Namespace));
            var category = new SyntactikCompletionCategory{ DisplayText = "Namespaces", Order = 0 };
            foreach (var ns in undeclaredNamespaces)
            {
                if (ns.Prefix == "xml")
                    continue;
                if (string.IsNullOrEmpty(ns.Prefix))
                {
                    ns.Prefix = CreatePrefix(schemasRepository, moduleMember, module);
                }
                string text = $"!#{ns.Prefix} = {ns.Namespace}";
                var completionItem = new CompletionItem
                {
                    ItemType = ItemType.Namespace,
                    Icon  = SyntactikIcons.NamespaceDefinition,
                    DisplayText = text,
                    CompletionText = text,
                    CompletionCategory = category,
                    Priority = int.MaxValue
                };
                completionList.Add(completionItem);
            }
        }

        internal static void DoNamespaceDefinitionValueCompletion(CompletionDataList completionList, CompletionContext context, CodeCompletionContext editorCompletionContext, ContextInfo schemaInfo, SchemasRepository schemasRepository)
        {
            if (schemasRepository == null) return;
            var category = new SyntactikCompletionCategory { DisplayText = "Namespaces", Order = 0 };
            foreach (var ns in schemasRepository.GetNamespaces())
            {
                if (ns.Prefix == "xml")
                    continue;
                var data = completionList.Add(ns.Namespace, SyntactikIcons.NamespaceDefinition, "", ns.Namespace);
                data.CompletionCategory = category;
            }
        }

        public static void DoAttributeValueCompletion(CompletionDataList completionList, CompletionContext context,
            CodeCompletionContext editorCompletionContext, ContextInfo schemaInfo, SchemasRepository schemasRepository)
        {
            if (schemasRepository == null) return;
            var attribute = context.LastPair as DOM.Attribute;
            if (attribute?.Name == "type" && attribute.NsPrefix == "xsi")
            {
                DoTypeAttributeValueCompletion(completionList, context, editorCompletionContext, schemaInfo,
                    schemasRepository);
                return;
            }
        }

        public static void DoElementValueCompletion(CompletionDataList completionList, CompletionContext context,
            CodeCompletionContext editorCompletionContext, ContextInfo schemaInfo, SchemasRepository schemasRepository)
        {
            if (schemasRepository == null) return;
            var simpleType = schemaInfo.CurrentType as SimpleType;
            if (simpleType == null) return;
            var category = new SyntactikCompletionCategory { DisplayText = "Values", Order = 0 };
            foreach (var value in simpleType.EnumValues)
            {
                var completionItem = new CompletionItem
                {
                    ItemType = ItemType.Attribute,
                    Icon = SyntactikIcons.Enum,
                    DisplayText = value,
                    CompletionText = context.LastPair.Delimiter == DelimiterEnum.E ? value : EncodeSQString(value, true),
                    CompletionCategory = category
                };
                completionList.Add(completionItem);
            }
        }

        public static void DoTypeAttributeValueCompletion(CompletionDataList completionList, CompletionContext context,
            CodeCompletionContext editorCompletionContext, ContextInfo schemaInfo, SchemasRepository schemasRepository)
        {
            var attribute = context.LastPair as DOM.Attribute;
            if (attribute?.Name != "type" || attribute.NsPrefix != "xsi") return;
            if (attribute.ValueInterval != null && attribute.ValueInterval != Interval.Empty)
                editorCompletionContext.TriggerWordLength = attribute.ValueInterval.End.Index - attribute.ValueInterval.Begin.Index + 1 - (attribute.ValueQuotesType == 0?0:1);
            var category = new SyntactikCompletionCategory { DisplayText = "Values", Icon = SyntactikIcons.Enum };
            if (schemaInfo.Scope != null)
            {
                var nsPrefix = "";
                bool newNs;
                var ns = GetNamespacePrefix(schemaInfo.Scope.Namespace, context.LastPair, schemasRepository, out newNs);
                if (ns != null)
                    nsPrefix = ns + ":";
                
                foreach (var d in schemaInfo.Scope.Descendants)
                {
                    string text = $"{nsPrefix}{d.Name}";
                    var completionItem = new CompletionItem
                    {
                        ItemType = ItemType.Attribute,
                        Icon = SyntactikIcons.Enum,
                        DisplayText = text,
                        CompletionText = attribute.Delimiter == DelimiterEnum.E ? text : EncodeSQString(text, true),
                        CompletionCategory = category
                    };
                    if (newNs)
                    {
                        completionItem.UndeclaredNamespaceUsed = true;
                        completionItem.NsPrefix = ns;
                        completionItem.Namespace = schemaInfo.Scope.Namespace;
                    }
                    completionList.Add(completionItem);
                }
                return;
            }

            foreach (var desc in schemaInfo.AllTypes)
            {
                var nsPrefix = "";
                bool newNs;
                var ns = GetNamespacePrefix(desc.Namespace, context.LastPair, schemasRepository, out newNs);
                if (ns != null)
                    nsPrefix = ns + ":";

                string text = $"{nsPrefix}{desc.Name}";
                var completionItem = new CompletionItem
                {
                    ItemType = ItemType.Attribute,
                    Icon = SyntactikIcons.Enum,
                    DisplayText = text,
                    CompletionText = attribute.Delimiter == DelimiterEnum.E ? text : EncodeSQString(text, true),
                    CompletionCategory = category
                };
                if (newNs)
                {
                    completionItem.UndeclaredNamespaceUsed = true;
                    completionItem.NsPrefix = ns;
                    completionItem.Namespace = desc.Namespace;
                }
                completionList.Add(completionItem);
            }
        }

        private static string EncodeSQString(string text, bool addSingleBrackets)
        {
            var sb = new StringBuilder(addSingleBrackets ? "'" : "");
            sb.Append(HttpUtility.JavaScriptStringEncode(text));
            if (addSingleBrackets) sb.Append("'");
            return sb.ToString();
        }

        private static void AdjustEditorCompletionContext(CodeCompletionContext editorCompletionContext, Interval valueInterval)
        {
            var delta = editorCompletionContext.TriggerOffset - valueInterval.Begin.Index;
            editorCompletionContext.TriggerOffset = valueInterval.Begin.Index;
            editorCompletionContext.TriggerLineOffset -= delta;
            editorCompletionContext.TriggerWordLength += delta;
        }

        private static List<NamespaceDefinition> GetDeclaredNamespaceDefinitions(CompletionContext context, out Module module, out ModuleMember moduleMember)
        {
            module = null;
            moduleMember = null;
            var result = new List<NamespaceDefinition>();
            var completionContextLastPair = context.LastPair;
            while (completionContextLastPair != null)
            {
                moduleMember = completionContextLastPair as ModuleMember;
                if (moduleMember != null)
                {
                    result.AddRange(moduleMember.NamespaceDefinitions);
                }
                else
                {
                    module = completionContextLastPair as Module;
                    if (module != null)
                    {
                        result.AddRange(module.NamespaceDefinitions);
                    }
                }
                if (completionContextLastPair.Parent is CompileUnit) break;
                completionContextLastPair = completionContextLastPair.Parent;
            }
            return result;
        }


        internal static void DoArgumentCompletion(CompletionDataList completionList, CompletionContext context,
            Func<Dictionary<string, Syntactik.DOM.AliasDefinition>> aliasListFunc, bool valuesOnly = false)
        {
            Alias alias;
            if (context.InTag == CompletionExpectation.Argument)
            {
                alias = (context.LastPair as Argument)?.Parent as Alias;
            }
            else
                alias = context.LastPair as Alias;
            if (alias == null) return;
            var aliasDef = GetListOfBlockAliasDefinitions(aliasListFunc, valuesOnly).FirstOrDefault(a => a.Key == alias.Name).Value as AliasDefinition;

            if (aliasDef == null) return;
            var args = aliasDef.Parameters.Where(p => alias.Arguments.FirstOrDefault(a => a.Name == p.Name) == null);
            var items = new List<CompletionData>();
            var category = new SyntactikCompletionCategory { DisplayText = "Arguments", Order = 3 };
            foreach (var parameter in args)
            {
                CompletionData data = new CompletionItem { ItemType = ItemType.Argument };
                items.Add(data);
                data.CompletionCategory = category;
                data.DisplayText = "%" + parameter.Name + (parameter.IsValueNode ? " =" : ": ");
                data.CompletionText = data.DisplayText;
                data.Icon = SyntactikIcons.Argument;
            }
            completionList.AddRange(items.OrderBy(i => i.DisplayText));
        }

        internal static string GetNamespacePrefix(string @namespace, Pair pair, SchemasRepository schemasRepository, out bool newNs)
        {
            NamespaceDefinition nsDef = null;
            newNs = false;
            ModuleMember moduleMember = null;
            Module module = null;
            while (pair != null)
            {
                moduleMember = pair as ModuleMember;
                if (moduleMember != null)
                {
                    nsDef = moduleMember.NamespaceDefinitions.FirstOrDefault(n => n.Value == @namespace);
                }
                else
                {
                    module = pair as Module;
                    if (module != null)
                    {
                        nsDef = module.NamespaceDefinitions.FirstOrDefault(n => n.Value == @namespace);
                    }
                }

                if (nsDef != null || pair.Parent is CompileUnit) break;
                pair = pair.Parent;
            }
            if (nsDef != null) return nsDef.Name;

            //If namespace prefix is not found in the module then getting prefix from schema.
            newNs = true;
            var ns = schemasRepository.GetNamespaces().FirstOrDefault(n => n.Namespace == @namespace);
            if (ns == null) return "";
            var prefix = ns.Prefix;
            //if schema doesn't define default prefix for the namespace:
            if (string.IsNullOrEmpty(prefix))
            {
                prefix = CreatePrefix(schemasRepository, moduleMember, module);
            }
            ns.Prefix = prefix;
            return prefix;
        }

        private static string CreatePrefix(SchemasRepository schemasRepository, ModuleMember moduleMember, Module module)
        {
            string prefix;
            var i = 1;
            while (true)
            {
                prefix = "n" + i++;
                //Checking if this prefix already defined in module
                if (moduleMember?.NamespaceDefinitions.FirstOrDefault(n => n.Name == prefix) != null) continue;
                if (module?.NamespaceDefinitions.FirstOrDefault(n => n.Name == prefix) != null) continue;
                //Checking if this prefix is already used in schema repository
                if (schemasRepository.GetNamespaces().FirstOrDefault(n => n.Prefix == prefix) != null) continue;

                break;
            }
            return prefix;
        }

        internal static string GetNamespace( Pair pair)
        {
            var prefix = (pair as INsNode)?.NsPrefix;
            if (prefix == null) return string.Empty;
            return GetNamespace(pair, prefix);
        }

        internal static string GetNamespace(Pair pair, string prefix)
        {
            NamespaceDefinition nsDef = null;
            while (pair != null)
            {
                var moduleMember = pair as ModuleMember;
                if (moduleMember != null)
                {
                    nsDef = moduleMember.NamespaceDefinitions.FirstOrDefault(n => n.Name == prefix);
                }
                else
                {
                    var module = pair as Module;
                    if (module != null)
                    {
                        nsDef = module.NamespaceDefinitions.FirstOrDefault(n => n.Name == prefix);
                    }
                }

                if (pair.Parent is CompileUnit) break;
                pair = pair.Parent;
            }
            if (nsDef != null) return nsDef.Value;

            return string.Empty;
        }

        internal static IEnumerable<KeyValuePair<string, Syntactik.DOM.AliasDefinition>> GetListOfBlockAliasDefinitions(
                Func<Dictionary<string, Syntactik.DOM.AliasDefinition>> aliasListFunc, bool valuesOnly = false)
        {
            //Getting list of aliases
            return
                aliasListFunc?.Invoke().Where(a => ((AliasDefinition)a.Value).IsValueNode == valuesOnly);
        }
        private static string NameElement(string el, string prefix)
        {
            string result = el.Substring(prefix.Length);

            var pos = result.IndexOf('.');
            if (pos > 0)
                result = result.Substring(0, pos + 1);
            return result;
        }

        private static string GetCompletionPrefixForCurrentPair(CompletionContext context, CodeCompletionContext editorCompletionContext, CompletionExpectation expectedNode)
        {
            var contextPair = context.LastPair;
            if (context.InTag != expectedNode) return null;
            var mappedPair = (IMappedPair)contextPair;
            var nsPair = contextPair as INsNode;
            string ns = "";
            if (!String.IsNullOrEmpty(nsPair?.NsPrefix)) ns = nsPair.NsPrefix + ".";
            if (mappedPair.NameInterval.End.Column < editorCompletionContext.TriggerLineOffset) return null;
            var prefix = ns + contextPair.Name;
            if (String.IsNullOrEmpty(prefix)) return prefix;

            return prefix.Substring(0, prefix.Length - (mappedPair.NameInterval.End.Index - context.Offset));
        }

        private static List<ElementType> GetElementTypes(ElementType elementType, out bool haveExtensions)
        {
            haveExtensions = false;
            var types = new List<ElementType> { elementType };
            var type = elementType as ComplexType;
            if (type != null && type.Descendants.Count > 0)
            {
                types.AddRange(type.Descendants);
                haveExtensions = true;
            }
            return types;
        }
    }

}
