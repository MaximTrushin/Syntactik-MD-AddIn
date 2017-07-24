using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gtk;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.Gui.Content;
using Syntactik.DOM;
using Syntactik.DOM.Mapped;
using Syntactik.IO;
using Syntactik.MonoDevelop.Completion.DOM;
using Syntactik.MonoDevelop.Projects;
using Syntactik.MonoDevelop.Schemas;
using Alias = Syntactik.DOM.Mapped.Alias;
using AliasDefinition = Syntactik.DOM.Mapped.AliasDefinition;
using Argument = Syntactik.DOM.Mapped.Argument;
using Document = Syntactik.DOM.Document;
using Module = Syntactik.DOM.Module;
using NamespaceDefinition = Syntactik.DOM.NamespaceDefinition;

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
            CompletionWindowManager.WordCompleted += CompletionWindowManager_WordCompleted;
            Editor.CaretPositionChanged += HandleCaretPositionChanged;

        }

        public override void Dispose()
        {
            Editor.CaretPositionChanged -= HandleCaretPositionChanged;
            CompletionWindowManager.WordCompleted -= CompletionWindowManager_WordCompleted;
            base.Dispose();
        }

        /// <summary>
        /// This method is called when completion command is executed (Ctrl+Space).
        /// </summary>
        /// <param name="completionContext"></param>
        /// <returns></returns>
        public override Task<ICompletionDataList> CodeCompletionCommand(CodeCompletionContext completionContext)
        {
            Editor.EnsureCaretIsNotVirtual();
            var pos = completionContext.TriggerOffset;
            if (pos < 0)
                return null;
            return HandleCodeCompletion(completionContext, true, default(CancellationToken), 0);
        }

        /// <summary>
        /// This method is triggered when completion is triggered interactively during typing.
        /// </summary>
        /// <param name="completionContext"></param>
        /// <param name="completionChar">Symbol which started the completion.</param>
        /// <param name="token"></param>
        /// <returns></returns>
        public override async Task<ICompletionDataList> HandleCodeCompletionAsync(CodeCompletionContext completionContext,
            char completionChar, CancellationToken token = default(CancellationToken))
        {
            Editor.EnsureCaretIsNotVirtual();
            int pos = completionContext.TriggerOffset;
            char ch = completionContext.TriggerOffset > 0 ? Editor.GetCharAt(completionContext.TriggerOffset - 1) : '\0';

            if (pos <= 0 || ch != completionChar) return null;

            if (!IsCompletionChar(completionChar)) return null;
            //Don't start completion in the middle of the word if completion char is entered
            if (completionChar != '.' && IsCompletionChar(completionChar) && completionContext.TriggerOffset > 1 && IsCompletionChar(Editor.GetCharAt(completionContext.TriggerOffset - 2)))
                return null;
            var triggerWordLength = completionChar != '.'?1:0;


            int cpos, wlen;
            if (!GetCompletionCommandOffset(out cpos, out wlen))
            {
                cpos = Editor.CaretOffset;
                wlen = 0;
            }
            CurrentCompletionContext.TriggerOffset = cpos;
            CurrentCompletionContext.TriggerWordLength = wlen;



            return await HandleCodeCompletion(completionContext, false, token, triggerWordLength);
        }

        protected virtual async Task<ICompletionDataList> HandleCodeCompletion(CodeCompletionContext completionContext, bool forced, 
            CancellationToken token, int triggerWordLength)
        {
            return await GetCompletionListAsync(completionContext, token, triggerWordLength);
        }

        private async Task<ICompletionDataList> GetCompletionListAsync(CodeCompletionContext completionContext,
            CancellationToken token, int triggerWordLength)
        {
            CompletionContext context = await GetCompletionContextAsync(token, Editor.Version, Editor.CaretOffset,
                   Editor.FileName, Editor.Text, ((SyntactikProject)DocumentContext.Project).GetAliasDefinitionList);
            context.CalculateExpectations();
            return GetCompletionList(context, completionContext, triggerWordLength,
                ((SyntactikProject)DocumentContext.Project).GetAliasDefinitionList,
                ((SyntactikProject)DocumentContext.Project).SchemasRepository);
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
                    () => CompletionContext.CreateCompletionContext(fileName, text, caretOffset, getAliasDefinitionList, token), token
                ), version, caretOffset);
                return _completionContextTask.Task;
            }
        }

        protected internal static ICompletionDataList GetCompletionList(CompletionContext context, 
            CodeCompletionContext editorCompletionContext, 
            int triggerWordLength,
            Func<Dictionary<string, Syntactik.DOM.AliasDefinition>> aliasListFunc,
            SchemasRepository schemasRepository)
        {
            
            var schemaInfo = schemasRepository.GetContextInfo(new Context { CompletionInfo = context });
            var completionList = new CompletionDataList {TriggerWordLength = triggerWordLength};
            foreach (var expectation in context.Expectations.AsEnumerable())
            {
                if (expectation == CompletionExpectation.Alias)
                    DoAliasCompletion(completionList, context, editorCompletionContext, aliasListFunc);
                
                if (expectation==CompletionExpectation.Argument)
                    DoArgumentCompletion(completionList, context, aliasListFunc);

                if (expectation == CompletionExpectation.Element)
                    DoElementCompletion(completionList, context, editorCompletionContext, schemaInfo, schemasRepository);
            }
            return completionList;
        }

        private static void DoElementCompletion(CompletionDataList completionList, CompletionContext completionContext,
            CodeCompletionContext editorCompletionContext, ContextInfo schemaInfo, SchemasRepository schemasRepository)
        {
            var items = new List<CompletionData>();
            var completionCategory = new SyntactikCompletionCategory { DisplayText = "Elements", Order = 2 };
            var rawPrefix = GetPrefixForCurrentPair(completionContext, editorCompletionContext, CompletionExpectation.Element);

            foreach (var element in schemaInfo.Elements)
            {
                bool newNs = false;
                string prefix = string.IsNullOrEmpty(element.Namespace)?"":ResolveNamespacePrefix(element.Namespace, completionContext.LastPair, schemasRepository, out newNs);
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
                    string postfix = string.Empty;
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
                    data.NewNamespace = newNs;
                }
            }
            completionList.AddRange(items.OrderBy(i => i.DisplayText));
        }

        private static List<ElementType> GetElementTypes(ElementType elementType, out bool haveExtensions)
        {
            haveExtensions = false;
            var types = new List<ElementType> {elementType};
            if (elementType is ComplexType && ((ComplexType) elementType).Descendants.Count > 0)
            {
                types.AddRange(((ComplexType) elementType).Descendants);
                haveExtensions = true;
            }
            return types;
        }

        internal static string ResolveNamespacePrefix(string @namespace, Pair completionContextLastPair, SchemasRepository schemasRepository, out bool newNs)
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
            return schemasRepository.GetNamespaces().First(n => n.Namespace == @namespace).Name;
        }

        private static void DoAliasCompletion(CompletionDataList completionList, CompletionContext context, 
            CodeCompletionContext editorCompletionContext, Func<Dictionary<string, Syntactik.DOM.AliasDefinition>> aliasListFunc, bool valuesOnly = false)
        {
            var items = new List<CompletionData>();
            var category = new SyntactikCompletionCategory { DisplayText = "Aliases", Order = 3 };

            var aliases = GetListOfBlockAliasDefinitions(aliasListFunc, valuesOnly).Select(a => a.Value.Name);
            var rawPrefix = GetPrefixForCurrentPair(context, editorCompletionContext, CompletionExpectation.Alias);
            var prefix = rawPrefix??string.Empty;
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
                    data = new CompletionItem { ItemType = ItemType.AliasNamespace};
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
                    data = new CompletionItem { ItemType = ItemType.Alias};
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
            if (prefix.EndsWith("."))
            {
                //If alias is composite then increasing TriggerOffset by the length of prefix. +1 is for $
                editorCompletionContext.TriggerOffset += prefix.Length + 1; 
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
                CompletionData data = new CompletionItem {ItemType = ItemType.Argument};
                items.Add(data);
                data.CompletionCategory = category;
                data.DisplayText = "%" + parameter.Name + (parameter.IsValueNode?" =": ": ");
                data.CompletionText = data.DisplayText;
                data.Icon = SyntactikIcons.Argument;
            }
            completionList.AddRange(items.OrderBy(i => i.DisplayText));
        }

        private void CompletionWindowManager_WordCompleted(object sender, CodeCompletionContextEventArgs e)
        {
            if (SelectedCompletionItem == null)
                return;
            if (SelectedCompletionItem.NewNamespace)
                AddNewNamespaceToModule(SelectedCompletionItem.NsPrefix, SelectedCompletionItem.Namespace);
        }

        private void AddNewNamespaceToModule(string nsPrefix, string ns)
        {
            var document = DocumentContext.ParsedDocument;
            var module = document.Ast as Syntactik.DOM.Mapped.Module;
            int offset = FindOffsetForNewNamespaceInsertion(module);
            var text = (offset == 0?"":"\r\n") + $"!#{nsPrefix} = {ns}" + (offset == 0 ? "\r\n": "");
            Editor.InsertText(offset, text);
        }

        private int FindOffsetForNewNamespaceInsertion(Syntactik.DOM.Mapped.Module module)
        {
            var result = 0;
            foreach (var nsDef in module.NamespaceDefinitions)
            {
                var index = DomHelper.GetPairEnd((IMappedPair) nsDef).Index;
                if (index > result) result = index;
            }
            if (result == 0) return 0;
            result++;
            var line = Editor.GetLineByOffset(result);
            var text = Editor.GetLineText(line);
            var offset = result - line.Offset + 1;
            while (offset < text.Length && !IntegerCharExtensions.IsEndOfOpenName(text[offset]))
            {
                result++;
                offset++;
            }
            return result;
        }

        // return false if completion can't be shown
        public override bool GetCompletionCommandOffset(out int cpos, out int wlen)
        {
            Editor.EnsureCaretIsNotVirtual();

            int pos = Editor.CaretOffset - 1;
            while (pos >= 0)
            {
                char c = Editor.GetCharAt(pos);
                if (!IsCompletionChar(c))
                    break;
                pos--;
            }
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
            return char.IsLetterOrDigit(c) || c == '_' || c == '!' || c == '@' || c == '#' || c == '$' || c == '%' || c == '.';
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
            var mappedPair = (IMappedPair) contextPair;
            var nsPair = contextPair as INsNode;
            string ns = "";
            if (nsPair != null && !string.IsNullOrEmpty(nsPair.NsPrefix)) ns = nsPair.NsPrefix + ".";
            if (mappedPair.NameInterval.End.Column < editorCompletionContext.TriggerLineOffset) return null;
            var prefix = ns + contextPair.Name;
            if (string.IsNullOrEmpty(prefix)) return prefix;

            return prefix.Substring(0, prefix.Length - (mappedPair.NameInterval.End.Index - context.Offset));
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
            var mi = new MenuItem("Go to");
            mi.Activated += delegate
            {
                Goto(_currentPath[index].Tag as Pair);
            };
            menu.Add(mi);
            //mi.Activated += delegate
            //{
            //    SelectPath(index);
            //};
            //menu.Add(mi);
            //mi = new MenuItem("Select Content");
            //mi.Activated += delegate
            //{
            //    SelectContent(index);
            //};
            //menu.Add(mi);
            //mi = new MenuItem("Select Next Node");
            //mi.Activated += delegate
            //{
            //    SelectNextNode(index);
            //};
            //menu.Add(mi);
            menu.ShowAll();
            return menu;
        }

        private void Goto(Pair pair)
        {
            var p = pair as IMappedPair;
            var start = GetPairStart(p);
            Editor.SetCaretLocation(start.Line, start.Column);
        }

        private CharLocation GetPairStart(IMappedPair pair)
        {
            if (pair.NameInterval != null) return pair.NameInterval.Begin;
            if (pair.DelimiterInterval != null) return pair.DelimiterInterval.Begin;
            return pair.ValueInterval.Begin;
        }

        //private void SelectNextNode(int index)
        //{
            
        //}

        //private void SelectContent(int index)
        //{
            
        //}

        //private void SelectPath(int index)
        //{

        //}

        public PathEntry[] CurrentPath => _currentPath;
        protected internal CompletionItem SelectedCompletionItem { get; internal set; }

        public event EventHandler<DocumentPathChangedEventArgs> PathChanged;


        private const uint UpdatePathInterval = 500;
        private void HandleCaretPositionChanged(object sender, EventArgs e)
        {
            if (_pathUpdateQueued)
                return;
            _pathUpdateQueued = true;
            var editor = Editor;
            editor.EnsureCaretIsNotVirtual();
            GLib.Timeout.Add(UpdatePathInterval, delegate
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
            //var content = (DocumentContext as global::MonoDevelop.Ide.Gui.Document).
            CancellationTokenSource src = null;
                try
                {
                    lock (_syncLock)
                    {
                        if (_pathUpdateTokenSource != null && !_pathUpdateTokenSource.IsCancellationRequested)
                        {
                            _pathUpdateTokenSource.Cancel();
                        }
                        _pathUpdateTokenSource = src = new CancellationTokenSource();
                    }

                    var task = GetCompletionContextAsync(_pathUpdateTokenSource.Token, version, caretOffset,
                        fileName, text, getAliasDefinitionList);
#if DEBUG
                    task.Wait(_pathUpdateTokenSource.Token);
#else
                    task.Wait(2000, _pathUpdateTokenSource.Token);
#endif
                    if (task.Status != TaskStatus.RanToCompletion) return;
                    CompletionContext context = task.Result;

                    var path = GetPathEntries(context.GetPath());

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
                finally
                {
                    lock (_syncLock)
                    {
                        src?.Dispose();
                        if (_pathUpdateTokenSource == src) _pathUpdateTokenSource = null;
                    }
                }
        }

        private List<PathEntry> GetPathEntries(IEnumerable<Pair> pairs)
        {
            var list = new List<PathEntry>();
            foreach (var pair in pairs)
            {
                if (pair is Module) break;
                if (pair is Document && (pair.Parent as Module)?.ModuleDocument == pair) break;
                list.Add(new PathEntry(ImageService.GetIcon(GetIconSourceName(pair)), GetMarkup(pair)) { Tag = pair });
            }
            list.Reverse();
            return list;
        }

        private static string GetMarkup(Pair pair)
        {
            var nsNode = pair as INsNode;
            var ns = nsNode == null ? "" : string.IsNullOrEmpty(nsNode.NsPrefix)?"": nsNode.NsPrefix + ".";

            if (pair is Syntactik.DOM.Element) return ns + pair.Name;
            if (pair is Syntactik.DOM.Alias) return "$" + pair.Name;
            if (pair is Syntactik.DOM.Argument) return "%" + pair.Name;
            if (pair is Syntactik.DOM.Attribute) return "@" + ns + pair.Name;
            if (pair is Syntactik.DOM.Scope) return "#" + pair.Name;
            if (pair is Syntactik.DOM.NamespaceDefinition) return "!#" + pair.Name;
            if (pair is Syntactik.DOM.Parameter) return "!%" + pair.Name;
            if (pair is Syntactik.DOM.Document) return "!" + pair.Name;
            if (pair is Syntactik.DOM.AliasDefinition) return "!$" + pair.Name;
            return SyntactikIcons.Enum;
        }

        private static string GetIconSourceName(Pair pair)
        {
            if (pair is Syntactik.DOM.Element) return SyntactikIcons.Element;
            if (pair is Syntactik.DOM.Alias) return SyntactikIcons.Alias;
            if (pair is Syntactik.DOM.Argument) return SyntactikIcons.Argument;
            if (pair is Syntactik.DOM.Attribute) return SyntactikIcons.Attribute;
            if (pair is Syntactik.DOM.Scope) return SyntactikIcons.NamespaceDefinition;
            if (pair is Syntactik.DOM.NamespaceDefinition) return SyntactikIcons.NamespaceDefinition;
            if (pair is Syntactik.DOM.Parameter) return SyntactikIcons.Argument;
            if (pair is Syntactik.DOM.Document) return SyntactikIcons.Document;
            if (pair is Syntactik.DOM.AliasDefinition) return SyntactikIcons.AliasDef;
            return SyntactikIcons.Enum;
        }
    }
}