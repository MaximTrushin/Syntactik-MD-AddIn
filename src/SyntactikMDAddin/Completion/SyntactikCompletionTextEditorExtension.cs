﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Projects;
using AliasDefinition = Syntactik.DOM.Mapped.AliasDefinition;

namespace Syntactik.MonoDevelop.Completion
{
    public class SyntactikCompletionTextEditorExtension : CompletionTextEditorExtension
    {
        public override string CompletionLanguage => "S4X";

        //protected override void Initialize()
        //{
        //    base.Initialize();
        //}

        //public override void Dispose()
        //{
        //    base.Dispose();
        //}

        public override Task<ICompletionDataList> CodeCompletionCommand(CodeCompletionContext completionContext)
        {
            var pos = completionContext.TriggerOffset;
            if (pos < 0)
                return null;
            return HandleCodeCompletion(completionContext, true, default(CancellationToken), 0);
        }

        public override Task<ICompletionDataList> HandleCodeCompletionAsync(CodeCompletionContext completionContext,
            char completionChar, CancellationToken token = default(CancellationToken))
        {
            int pos = completionContext.TriggerOffset;
            char ch = completionContext.TriggerOffset > 0 ? Editor.GetCharAt(completionContext.TriggerOffset - 1) : '\0';

            if (pos <= 0 || ch != completionChar) return null;

            int triggerWordLength = 0;
            if (char.IsLetterOrDigit(completionChar) || completionChar == '_' || completionChar == '$')
            {
                if (completionContext.TriggerOffset > 1 && char.IsLetterOrDigit(Editor.GetCharAt(completionContext.TriggerOffset - 2)))
                    return null;
                triggerWordLength = 1;
            }
            return HandleCodeCompletion(completionContext, false, token, triggerWordLength);
        }

        protected virtual Task<ICompletionDataList> HandleCodeCompletion(CodeCompletionContext completionContext, bool forced, 
            CancellationToken token, int triggerWordLength)
        {
            CompletionContext context = new CompletionContext(Editor.FileName, Editor.Text, Editor.CaretOffset, ((SyntactikProject)DocumentContext.Project).GetAliasDefinitionList, token);
            context.CalculateExpectations();

            return GetCompletionList(context, completionContext, triggerWordLength, ((SyntactikProject)DocumentContext.Project).GetAliasDefinitionList);
        }

        protected internal static Task<ICompletionDataList> GetCompletionList(CompletionContext context, 
            CodeCompletionContext editorCompletionContext, 
            int triggerWordLength,
            Func<Dictionary<string, Syntactik.DOM.AliasDefinition>> aliasListFunc)
        {
            var completionList = new CompletionDataList {TriggerWordLength = triggerWordLength};
            foreach (var expectation in context.Expectations.AsEnumerable())
            {
                if (expectation == CompletionExpectation.Alias)
                {
                    DoAliasCompletion(completionList, context, editorCompletionContext, aliasListFunc);
                }
            }
            return Task.FromResult<ICompletionDataList>(completionList);
        }

        private static void DoAliasCompletion(CompletionDataList completionList, CompletionContext context, 
            CodeCompletionContext editorCompletionContext, Func<Dictionary<string, Syntactik.DOM.AliasDefinition>> aliasListFunc, bool valuesOnly = false)
        {
            var items = new List<CompletionData>();
            var category = new SyntactikCompletionCategory { DisplayText = "Aliases", Order = 3 };

            var aliases = GetListOfBlockAliasDefinitions(aliasListFunc, valuesOnly).Select(a => a.Value.Name);
            var rawPrefix = GetPrefixOfCurrentAlias(context, editorCompletionContext);
            var prefix = rawPrefix??string.Empty;

            if (rawPrefix != null) rawPrefix = "$" + rawPrefix;

            if (!prefix.EndsWith("."))
            {
                var pos = prefix.LastIndexOf(".", StringComparison.Ordinal);
                prefix = pos > 0 ? prefix.Substring(0, pos + 1) : string.Empty;
            }

            var grouped = aliases.Where(a => a.ToLower().StartsWith(prefix.ToLower())).Select(a => NameElement(a, prefix)).Distinct();
            foreach (var alias in grouped)
            {
                CompletionData data;
                if (alias.EndsWith("."))
                {
                    data = new CompletionItem(ItemType.AliasNamespace);
                    items.Add(data);
                    data.CompletionCategory = category;
                    //data.Icon = MalinaIcons.Namespace;
                    
                    if (string.IsNullOrEmpty(prefix))
                        data.DisplayText = "$" + alias;
                    else
                        data.DisplayText = alias;
                    data.CompletionText = data.DisplayText;
                    if (string.IsNullOrEmpty(rawPrefix) || rawPrefix.StartsWith("$") == false)
                        data.CompletionText = "$" + alias;

                }
                else
                {
                    data = new CompletionItem(ItemType.Alias);
                    items.Add(data);
                    data.CompletionCategory = category;
                    //data.Icon = MalinaIcons.Alias;
                    if (string.IsNullOrEmpty(prefix))
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
            completionList.AddRange(items.OrderBy(i => i.DisplayText));
        }

        // return false if completion can't be shown
        public override bool GetCompletionCommandOffset(out int cpos, out int wlen)
        {
            cpos = wlen = 0;
            int pos = Editor.CaretOffset - 1;
            while (pos >= 0)
            {
                char c = Editor.GetCharAt(pos);
                if (!IsCompletionChar(c))
                    break;
                pos--;
            }
            if (pos == -1)
                return false;

            pos++;
            cpos = pos;
            int len = Editor.Length;

            while (pos < len)
            {
                char c = Editor.GetCharAt(pos);
                if (!IsCompletionChar(c))
                    break;
                pos++;
            }
            wlen = pos - cpos;
            return true;
        }

        private static bool IsCompletionChar(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_' || c == '$' || c == '!' || c == '@' || c == '%' || c == '#' || c == '!';
        }


        private static string NameElement(string el, string prefix)
        {
            string result = el.Substring(prefix.Length);

            var pos = result.IndexOf('.');
            if (pos > 0)
                result = result.Substring(0, pos + 1);
            return result;
        }

        private static string GetPrefixOfCurrentAlias(CompletionContext context, CodeCompletionContext editorCompletionContext)
        {
            var pair = context.LastPair;
            if (context.InTag != CompletionExpectation.Alias) return null;
            var alias = (Syntactik.DOM.Mapped.Alias) pair;
            if (alias.NameInterval.End.Column < editorCompletionContext.TriggerLineOffset) return null;
            return alias.Name;
        }

        private static IEnumerable<KeyValuePair<string, Syntactik.DOM.AliasDefinition>> GetListOfBlockAliasDefinitions(
            Func<Dictionary<string, Syntactik.DOM.AliasDefinition>> aliasListFunc, bool valuesOnly = false)
        {
            //Getting list of aliases
            return
                aliasListFunc?.Invoke().Where(a => ((AliasDefinition) a.Value).IsValueNode == valuesOnly);
        }
    }
}