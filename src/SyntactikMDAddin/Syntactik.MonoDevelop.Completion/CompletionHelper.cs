using System;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Ide.CodeCompletion;
using Syntactik.DOM;
using Syntactik.DOM.Mapped;
using Syntactik.MonoDevelop.Schemas;
using Alias = Syntactik.DOM.Mapped.Alias;
using AliasDefinition = Syntactik.DOM.Mapped.AliasDefinition;
using Argument = Syntactik.DOM.Mapped.Argument;
using Module = Syntactik.DOM.Module;
using NamespaceDefinition = Syntactik.DOM.NamespaceDefinition;

namespace Syntactik.MonoDevelop.Completion
{
    public class CompletionHelper
    {
        internal static void DoAliasCompletion(CompletionDataList completionList, CompletionContext context,
            CodeCompletionContext editorCompletionContext, Func<Dictionary<string, Syntactik.DOM.AliasDefinition>> aliasListFunc, bool valuesOnly = false)
        {
            var items = new List<CompletionData>();
            var category = new SyntactikCompletionCategory { DisplayText = "Aliases", Order = 3 };

            var aliases = GetListOfBlockAliasDefinitions(aliasListFunc, valuesOnly).Select(a => a.Value.Name);
            var rawPrefix = GetPrefixForCurrentPair(context, editorCompletionContext, CompletionExpectation.Alias);
            var prefix = rawPrefix ?? String.Empty;
            if (!prefix.EndsWith("."))
            {
                var pos = prefix.LastIndexOf(".", StringComparison.Ordinal);
                prefix = pos > 0 ? prefix.Substring(0, pos + 1) : String.Empty;
            }

            var grouped = aliases.Where(a => a.ToLower().StartsWith(prefix.ToLower())).Select(a => NameElement(a, prefix)).Distinct();
            foreach (var alias in grouped)
            {
                CompletionData data;
                if (alias.EndsWith("."))
                {
                    data = new CompletionItem { ItemType = ItemType.AliasNamespace };
                    items.Add(data);
                    data.CompletionCategory = category;
                    data.Icon = SyntactikIcons.Namespace;
                    var name = alias.Substring(0, alias.Length - 1);
                    if (String.IsNullOrEmpty(prefix))
                        data.DisplayText = "$" + name;
                    else
                        data.DisplayText = name;
                    data.CompletionText = data.DisplayText;
                }
                else
                {
                    data = new CompletionItem { ItemType = ItemType.Alias };
                    items.Add(data);
                    data.CompletionCategory = category;
                    data.Icon = SyntactikIcons.Alias;
                    if (String.IsNullOrEmpty(prefix))
                    {
                        data.DisplayText = "$" + alias;
                    }
                    else
                    {
                        data.DisplayText = alias;
                    }
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

        internal static void DoElementCompletion(CompletionDataList completionList, CompletionContext completionContext,
                CodeCompletionContext editorCompletionContext, ContextInfo schemaInfo, SchemasRepository schemasRepository)
        {
            var items = new List<CompletionData>();
            var completionCategory = new SyntactikCompletionCategory { DisplayText = "Elements", Order = 2 };
            var rawPrefix = GetPrefixForCurrentPair(completionContext, editorCompletionContext, CompletionExpectation.Element);

            foreach (var element in schemaInfo.Elements)
            {
                bool newNs = false;
                string prefix = string.IsNullOrEmpty(element.Namespace) ? "" : GetNamespacePrefix(element.Namespace, completionContext.LastPair, schemasRepository, out newNs);
                var displayText = (string.IsNullOrEmpty(prefix) ? "" : (prefix + ".")) + element.Name;

                //Skip element if it conflicts with the current completion text
                if (rawPrefix != null && !displayText.Contains(rawPrefix)) continue;
                var elementType = element.GetElementType();
                bool haveExtensions;
                var types = GetElementTypes(elementType, out haveExtensions);
                foreach (var type in types)
                {
                    var data = new CompletionItem { ItemType = ItemType.Entity, Namespace = element.Namespace, NsPrefix = prefix };
                    items.Add(data);
                    string postfix = String.Empty;
                    if (type != elementType || haveExtensions)
                    {
                        postfix = $" ({type.Name})";
                    }

                    if (type.IsComplex)
                    {
                        data.DisplayText = $"{displayText}:{postfix}";
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
                }
            }
            completionList.AddRange(items.OrderBy(i => i.DisplayText));
        }

        internal static void DoNamespaceDefinitionCompletion(CompletionDataList completionList, CompletionContext context, CodeCompletionContext editorCompletionContext, ContextInfo schemaInfo, SchemasRepository schemasRepository)
        {
            List<NamespaceDefinition> declaredNamespaceDefinitions = GetDeclaredNamespaceDefinitions(context);
            var undeclaredNamespaces =
                schemasRepository.GetNamespaces()
                    .Where(ns => declaredNamespaceDefinitions.All(dns => dns.Value != ns.Namespace));
            var category = new SyntactikCompletionCategory{ DisplayText = "Namespaces", Order = 0 };
            foreach (var ns in undeclaredNamespaces)
            {
                if (ns.Prefix == "xml")
                    continue;

                string text = $"!#{ns.Prefix} = {ns.Namespace}";
                var data = completionList.Add(text, SyntactikIcons.NamespaceDefinition);
                data.CompletionCategory = category;
            }

        }

        private static List<NamespaceDefinition> GetDeclaredNamespaceDefinitions(CompletionContext context)
        {
            var result = new List<NamespaceDefinition>();
            var completionContextLastPair = context.LastPair;
            while (completionContextLastPair != null)
            {
                var moduleMember = completionContextLastPair as ModuleMember;
                if (moduleMember != null)
                {
                    result.AddRange(moduleMember.NamespaceDefinitions);
                }
                else
                {
                    var module = completionContextLastPair as Module;
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

        internal static string GetNamespacePrefix(string @namespace, Pair completionContextLastPair, SchemasRepository schemasRepository, out bool newNs)
        {
            NamespaceDefinition nsDef = null;
            newNs = false;
            while (completionContextLastPair != null)
            {
                var moduleMember = completionContextLastPair as ModuleMember;
                if (moduleMember != null)
                {
                    nsDef = moduleMember.NamespaceDefinitions.FirstOrDefault(n => n.Value == @namespace);
                }
                else
                {
                    var module = completionContextLastPair as Module;
                    if (module != null)
                    {
                        nsDef = module.NamespaceDefinitions.FirstOrDefault(n => n.Value == @namespace);
                    }
                }

                if (completionContextLastPair.Parent is CompileUnit) break;
                completionContextLastPair = completionContextLastPair.Parent;
            }
            if (nsDef != null) return nsDef.Name;

            newNs = true;
            return schemasRepository.GetNamespaces().First(n => n.Namespace == @namespace).Prefix;
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

        private static string GetPrefixForCurrentPair(CompletionContext context, CodeCompletionContext editorCompletionContext, CompletionExpectation expectedNode)
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
