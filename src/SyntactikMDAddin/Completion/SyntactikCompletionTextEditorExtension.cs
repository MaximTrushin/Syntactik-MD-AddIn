using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;
using Syntactik.Compiler;
using Syntactik.Compiler.IO;
using Syntactik.MonoDevelop.Completion.DOM;
using Syntactik.MonoDevelop.Parser;
using AliasDefinition = Syntactik.DOM.Mapped.AliasDefinition;

namespace Syntactik.MonoDevelop.Completion
{
    public class SyntactikCompletionTextEditorExtension : CompletionTextEditorExtension
    {
        public override string CompletionLanguage => "S4X";

        protected override void Initialize()
        {
            base.Initialize();
        }

        public override void Dispose()
        {
            base.Dispose();
        }

        public override Task<ICompletionDataList> CodeCompletionCommand(CodeCompletionContext completionContext)
        {
            int pos = completionContext.TriggerOffset;
            if (pos < 0)
                return null;
            return HandleCodeCompletion(completionContext, true, default(CancellationToken), 0);
        }

        public override Task<ICompletionDataList> HandleCodeCompletionAsync(CodeCompletionContext completionContext,
            char completionChar, CancellationToken token = default(CancellationToken))
        {
            int pos = completionContext.TriggerOffset;
            char ch = completionContext.TriggerOffset > 0 ? Editor.GetCharAt(completionContext.TriggerOffset - 1) : '\0';
            if (pos > 0 && ch == completionChar)
            {
                int triggerWordLength = 0;
                if (char.IsLetterOrDigit(completionChar) || completionChar == '_')
                {
                    if (completionContext.TriggerOffset > 1 && char.IsLetterOrDigit(Editor.GetCharAt(completionContext.TriggerOffset - 2)))
                        return null;
                    triggerWordLength = 1;
                }
                //tracker.UpdateEngine();
                return HandleCodeCompletion(completionContext, false, token, triggerWordLength);
            }
            return null;
        }

        protected virtual Task<ICompletionDataList> HandleCodeCompletion(CodeCompletionContext completionContext, bool forced, CancellationToken token, int triggerWordLength)
        {

            CompletionContext context = new CompletionContext(Editor.FileName, Editor.Text, Editor.CaretOffset, token);
            context.CalculateExpectations();

            return GetCompletionList(context, completionContext, triggerWordLength);
        }

        private Task<ICompletionDataList> GetCompletionList(CompletionContext context, CodeCompletionContext editorCompletionContext, int triggerWordLength)
        {
            var completionList = new CompletionDataList();
            completionList.TriggerWordLength = triggerWordLength;
            foreach (var expectation in context.Expectations.AsEnumerable())
            {
                if (expectation == CompletionExpectation.Alias)
                {
                    DoAliasCompletion(completionList, context, editorCompletionContext);
                }
            }
            return Task.FromResult<ICompletionDataList>(completionList);
        }

        private void DoAliasCompletion(CompletionDataList completionList, CompletionContext context, CodeCompletionContext editorCompletionContext, bool valuesOnly = false)
        {
            var items = new List<CompletionData>();
            var category = new SyntactikCompletionCategory { DisplayText = "Aliases", Order = 3 };

            var aliases = GetListOfBlockAliasDefinitions(valuesOnly).Select(a => a.Value.Name);
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
                    data.CompletionText = alias;
                    if (string.IsNullOrEmpty(rawPrefix) || rawPrefix.StartsWith("$") == false)
                        data.CompletionText = "$" + alias;
                    //data.CompletionText = name;
                }
                else
                {
                    data = new CompletionItem(ItemType.Alias);
                    items.Add(data);
                    data.CompletionCategory = category;
                    //data.Icon = MalinaIcons.Alias;
                    if (string.IsNullOrEmpty(prefix))
                        data.DisplayText = "$" + alias;
                    else
                        data.DisplayText = alias;
                    string text = alias;
                    //var aliasDef = aliasDefinitions.FirstOrDefault(a => a.Name == prefix + alias);
                    //if (aliasDef != null)
                    //{
                    //    var p = aliasDef.Parameters.FirstOrDefault();
                    //    var indent = GetCurrentIndent();
                    //    if (p != null && p.Name == "_")
                    //    {
                    //        text = alias + "::" + Environment.NewLine + indent + "\t";
                    //    }
                    //    //else if (aliasDef.Parameters.Any())
                    //    //    text = alias + ":" + Environment.NewLine + indent + "\t";
                    //}
                    data.CompletionText = text;
                    //if (rawPrefix.StartsWith("$") == false)
                    //    data.CompletionText = "$" + alias;
                }
            }
            completionList.AddRange(items.OrderBy(i => i.DisplayText));



            //foreach (var aliasDef in list)
            //{
            //    completionList.Add("$" + aliasDef.Value.Name);
            //}
        }

        private static string NameElement(string el, string prefix)
        {
            string result = el.Substring(prefix.Length);

            var pos = result.IndexOf('.');
            if (pos > 0)
                result = result.Substring(0, pos + 1);
            return result;
        }

        private string GetPrefixOfCurrentAlias(CompletionContext context, CodeCompletionContext editorCompletionContext)
        {
            var pair = context.LastPair;
            if (!(pair is Syntactik.DOM.Alias)) return null;
            var alias = (Syntactik.DOM.Mapped.Alias) pair;
            if (alias.NameInterval.End.Column < editorCompletionContext.TriggerLineOffset) return null;
            return alias.Name;
        }

        private IEnumerable<KeyValuePair<string, Syntactik.DOM.AliasDefinition>> GetListOfBlockAliasDefinitions(bool valuesOnly = false)
        {
            //Getting list of aliases
            if (DocumentContext.HasProject)
            {
                var project = DocumentContext.Project as SyntactikProject;
                if (project != null)
                {
                    return project.GetProjectAliasList().Where(a => ((AliasDefinition)a.Value).IsValueNode == valuesOnly);
                }
            }
            return null;
        }
    }
}