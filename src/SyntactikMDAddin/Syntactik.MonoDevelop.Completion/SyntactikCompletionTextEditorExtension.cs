using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gtk;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.Gui.Content;
using Syntactik.DOM;
using Syntactik.MonoDevelop.Project;
using Alias = Syntactik.DOM.Mapped.Alias;
using AliasDefinition = Syntactik.DOM.Mapped.AliasDefinition;
using Argument = Syntactik.DOM.Mapped.Argument;

namespace Syntactik.MonoDevelop.Completion
{
    public class SyntactikCompletionTextEditorExtension : CompletionTextEditorExtension, IPathedDocument
    {
        private readonly object _syncLock = new object();
        private PathEntry[] _currentPath;
        private bool _pathUpdateQueued;
        private CompletionContextTask _completionContextTask;
        public override string CompletionLanguage => "S4X";


        protected override void Initialize()
        {
            base.Initialize();
            Editor.CaretPositionChanged += HandleCaretPositionChanged;
        }

        public override void Dispose()
        {
            Editor.CaretPositionChanged -= HandleCaretPositionChanged;
            base.Dispose();
        }

        public override Task<ICompletionDataList> CodeCompletionCommand(CodeCompletionContext completionContext)
        {
            Editor.EnsureCaretIsNotVirtual();
            var pos = completionContext.TriggerOffset;
            if (pos < 0)
                return null;
            return HandleCodeCompletion(completionContext, true, default(CancellationToken), 0);
        }

        public override async Task<ICompletionDataList> HandleCodeCompletionAsync(CodeCompletionContext completionContext,
            char completionChar, CancellationToken token = default(CancellationToken))
        {
            Editor.EnsureCaretIsNotVirtual();
            int pos = completionContext.TriggerOffset;
            char ch = completionContext.TriggerOffset > 0 ? Editor.GetCharAt(completionContext.TriggerOffset - 1) : '\0';

            if (pos <= 0 || ch != completionChar) return null;

            if (!IsCompletionChar(completionChar)) return null;
            if (completionContext.TriggerOffset > 1 && char.IsLetterOrDigit(Editor.GetCharAt(completionContext.TriggerOffset - 2)))
                return null;
            const int triggerWordLength = 1;
            return await HandleCodeCompletion(completionContext, false, token, triggerWordLength);
        }

        protected virtual Task<ICompletionDataList> HandleCodeCompletion(CodeCompletionContext completionContext, bool forced, 
            CancellationToken token, int triggerWordLength)
        {
            return Task.Run(
                async () => {
                    //CompletionContext context = new CompletionContext(Editor.FileName, Editor.Text, Editor.CaretOffset, ((SyntactikProject)DocumentContext.Project).GetAliasDefinitionList, token);
                    
                    CompletionContext context = await GetCompletionContextAsync(token, Editor.Version, Editor.CaretOffset, 
                        Editor.FileName, Editor.Text, ((SyntactikProject)DocumentContext.Project).GetAliasDefinitionList);
                    context.CalculateExpectations();
                    return GetCompletionList(context, completionContext, triggerWordLength,
                        ((SyntactikProject) DocumentContext.Project).GetAliasDefinitionList);
                }, token
            );
        }

        private Task<CompletionContext> GetCompletionContextAsync(CancellationToken token, ITextSourceVersion version, int caretOffset, string fileName, string text, Func<Dictionary<string, Syntactik.DOM.AliasDefinition>> getAliasDefinitionList)
        {
            lock (_syncLock)
            {
                if (_completionContextTask != null)
                {
                    if (_completionContextTask.Version.BelongsToSameDocumentAs(version) &&
                    _completionContextTask.Version.CompareAge(version) == 0 && _completionContextTask.Offset == caretOffset)
                        return _completionContextTask.Task;
                }
                _completionContextTask = new CompletionContextTask (Task.Run(
                    () => {
                        CompletionContext context = new CompletionContext(fileName, text, caretOffset, getAliasDefinitionList, token);
                        context.Parse();
                        return context;
                    }, token
                ), version, caretOffset);
                return _completionContextTask.Task;
            }
        }

        protected internal static ICompletionDataList GetCompletionList(CompletionContext context, 
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
                if (expectation==CompletionExpectation.Argument)
                    DoArgumentCompletion(completionList, context, aliasListFunc);
            }
            return completionList;
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
                    data.Icon = SyntactikIcons.Namespace;
                    var name = alias.Substring(0, alias.Length - 1);
                    if (string.IsNullOrEmpty(prefix))
                        data.DisplayText = "$" + name;
                    else
                        data.DisplayText = name;
                    data.CompletionText = data.DisplayText;
                    if (string.IsNullOrEmpty(rawPrefix) || rawPrefix.StartsWith("$") == false)
                        data.CompletionText = "$" + name;

                }
                else
                {
                    data = new CompletionItem(ItemType.Alias);
                    items.Add(data);
                    data.CompletionCategory = category;
                    data.Icon = SyntactikIcons.Alias;
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

        private static void DoArgumentCompletion(CompletionDataList completionList, CompletionContext context,
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
                CompletionData data = new CompletionItem(ItemType.Argument);
                items.Add(data);
                data.CompletionCategory = category;
                data.DisplayText = "%" + parameter.Name + (parameter.IsValueNode?" =": ": ");
                data.CompletionText = data.DisplayText;
                data.Icon = SyntactikIcons.Argument;
            }
            completionList.AddRange(items.OrderBy(i => i.DisplayText));
        }

        // return false if completion can't be shown
        public override bool GetCompletionCommandOffset(out int cpos, out int wlen)
        {
            Editor.EnsureCaretIsNotVirtual();
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
            return char.IsLetterOrDigit(c) || c == '_' || c == '!' || c == '@' || c == '#' || c == '$' || c == '%';
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

        public Control CreatePathWidget(int index)
        {
            var menu = new Menu();
            var mi = new MenuItem("Select");
            mi.Activated += delegate
            {
                SelectPath(index);
            };
            menu.Add(mi);
            mi = new MenuItem("Select Content");
            mi.Activated += delegate
            {
                SelectContent(index);
            };
            menu.Add(mi);
            mi = new MenuItem("Select Next Node");
            mi.Activated += delegate
            {
                SelectNextNode(index);
            };
            menu.Add(mi);
            menu.ShowAll();
            return menu;
        }

        private void SelectNextNode(int index)
        {
            
        }

        private void SelectContent(int index)
        {
            
        }

        private void SelectPath(int index)
        {

        }

        public PathEntry[] CurrentPath => _currentPath;
        public event EventHandler<DocumentPathChangedEventArgs> PathChanged;

        private void HandleCaretPositionChanged(object sender, EventArgs e)
        {
            if (_pathUpdateQueued)
                return;
            _pathUpdateQueued = true;
            var editor = Editor;
            editor.EnsureCaretIsNotVirtual();
            GLib.Timeout.Add(500, delegate
                {
                    _pathUpdateQueued = false;
                    Task.Run(() =>
                    {
                        UpdatePath(editor.Version, editor.CaretOffset, editor.FileName, editor.Text, ((SyntactikProject)DocumentContext.Project).GetAliasDefinitionList);
                    });
                    return false;
                }
            );
        }


        private CancellationTokenSource _pathUpdateTokenSource;

        private void UpdatePath(ITextSourceVersion version, int caretOffset, string fileName, string text, Func<Dictionary<string, Syntactik.DOM.AliasDefinition>> getAliasDefinitionList)
        {
            lock (_syncLock)
            {
                //var content = (DocumentContext as global::MonoDevelop.Ide.Gui.Document).
                try
                {
                    if (_pathUpdateTokenSource != null && !_pathUpdateTokenSource.IsCancellationRequested)
                    {
                        _pathUpdateTokenSource.Cancel();
                        _pathUpdateTokenSource.Dispose();
                        _pathUpdateTokenSource = null;
                    }
                    _pathUpdateTokenSource = new CancellationTokenSource();
                    var task = GetCompletionContextAsync(_pathUpdateTokenSource.Token, version, caretOffset,
                        fileName, text, getAliasDefinitionList);
#if DEBUG
                    task.Wait(_pathUpdateTokenSource.Token);
#else
                    task.Wait(2000, _pathUpdateTokenSource.Token);
#endif


                    if (task.Status != TaskStatus.RanToCompletion) return;
                    CompletionContext context = task.Result;

                    List<PathEntry> path = GetPath(context.LastPair);

                    PathEntry[] oldPath = _currentPath;
                    _currentPath = path.ToArray();

                    Gtk.Application.Invoke(delegate
                    {
                        PathChanged?.Invoke(this, new DocumentPathChangedEventArgs(oldPath));
                    });
                }
                catch (Exception ex)
                {
                    LoggingService.LogError("Unhandled exception in FoldingTextEditorExtension.UpdateFoldings.", ex);
                }
            }
        }

        private List<PathEntry> GetPath(Pair lastPair)
        {
            var pair = lastPair;
            var list = new List<PathEntry>();
            while (pair != null)
            {
                list.Add(new PathEntry(pair.Name));
                pair = pair.Parent;
                if (pair is Module) break;
            }
            return list;
        }
    }
}