using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Gtk;
using ICSharpCode.NRefactory.Editor;
using Mono.TextEditor;
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
using Syntactik.MonoDevelop.Parser;
using Syntactik.MonoDevelop.Projects;
using Syntactik.MonoDevelop.Schemas;
using Document = Syntactik.DOM.Document;
using ISegment = ICSharpCode.NRefactory.Editor.ISegment;
using ITextSourceVersion = MonoDevelop.Core.Text.ITextSourceVersion;
using Module = Syntactik.DOM.Module;
using TextSegment = MonoDevelop.Core.Text.TextSegment;

namespace Syntactik.MonoDevelop.Completion
{
    public class SyntactikCompletionTextEditorExtension : CompletionTextEditorExtension, IPathedDocument, ITextPasteHandler
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
            var data = DocumentContext.GetContent<TextEditorData>();
            data.TextPasteHandler = this;
        }

        public override void Dispose()
        {
            Editor.CaretPositionChanged -= HandleCaretPositionChanged;
            CompletionWindowManager.WordCompleted -= CompletionWindowManager_WordCompleted;
            base.Dispose();
        }

        public override bool KeyPress(KeyDescriptor descriptor)
        {
            //Updating word selection for !@#$% because base class update it only for letters 
            var ret = base.KeyPress(descriptor);
            if ((CompletionLanguage == "S4X" || CompletionLanguage == "S4J") && CompletionWindowManager.IsVisible &&
                (descriptor.KeyChar == '$' || descriptor.KeyChar == '@' || descriptor.KeyChar == '!' ||
                 descriptor.KeyChar == '#' || descriptor.KeyChar == '%'))
            {
                CompletionWindowManager.UpdateWordSelection(CompletionWindowManager.Wnd.CurrentPartialWord);
            }
            return ret;
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
            return pos < 0 ? null : HandleCodeCompletion(completionContext, true, default(CancellationToken), 0, (char) 0);
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
            return await HandleCodeCompletion(completionContext, false, token, triggerWordLength, completionChar);
        }

        protected virtual async Task<ICompletionDataList> HandleCodeCompletion(CodeCompletionContext completionContext, bool forced, CancellationToken token, 
            int triggerWordLength, char completionChar)
        {
            var result = await GetCompletionListAsync(completionContext, token, triggerWordLength);
            ((CompletionDataList)result).DefaultCompletionString = result.Count > 0 ? result[0].CompletionText : null;
            if (completionChar != 0 && triggerWordLength == 1 && result.Count > 0)
            {
                //Don't return completion list if completion is triggered by single character and it has no completion choices after
                // ListWidget.FilterWords is called.
                var matcher = StringMatcher.GetMatcher(completionChar.ToString(), true);
                if (result.Any(i => matcher.IsMatch(i.DisplayText))) return result;
                return null;
            }
            return result;
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

        internal Task<CompletionContext> GetCompletionContextAsync(CancellationToken token, ITextSourceVersion version, int caretOffset, string fileName, string text, Func<Dictionary<string, Syntactik.DOM.AliasDefinition>> getAliasDefinitionList)
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
                ), version, caretOffset, token);
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
            foreach (var expectation in context.Expectations)
            {
                switch (expectation)
                {
                    case CompletionExpectation.NamespaceDefinition:
                        CompletionHelper.DoNamespaceDefinitionCompletion(completionList, context, editorCompletionContext, schemaInfo, schemasRepository);
                        break;
                    case CompletionExpectation.NoExpectation:
                        break;
                    case CompletionExpectation.Alias:
                        CompletionHelper.DoAliasCompletion(completionList, context, editorCompletionContext, aliasListFunc);
                        break;
                    case CompletionExpectation.AliasDefinition:
                        break;
                    case CompletionExpectation.Argument:
                        CompletionHelper.DoArgumentCompletion(completionList, context, aliasListFunc);
                        break;
                    case CompletionExpectation.Attribute:
                        CompletionHelper.DoAttributeCompletion(completionList, context, editorCompletionContext, schemaInfo, schemasRepository);
                        break;
                    case CompletionExpectation.Document:
                        break;
                    case CompletionExpectation.Element:
                        CompletionHelper.DoElementCompletion(completionList, context, editorCompletionContext, schemaInfo, schemasRepository);
                        break;
                    case CompletionExpectation.Value:
                        if (context.InTag == CompletionExpectation.NoExpectation && context.LastPair is DOM.NamespaceDefinition)
                        {
                            CompletionHelper.DoNamespaceDefinitionValueCompletion(completionList, context, editorCompletionContext, schemaInfo, schemasRepository);
                            break;
                        }
                        if (context.InTag == CompletionExpectation.NoExpectation && context.LastPair is DOM.Attribute)
                        {
                            CompletionHelper.DoAttributeValueCompletion(completionList, context, editorCompletionContext, schemaInfo, schemasRepository);
                            break;
                        }
                        if (context.InTag == CompletionExpectation.NoExpectation && context.LastPair is DOM.Element)
                        {
                            CompletionHelper.DoElementValueCompletion(completionList, context, editorCompletionContext, schemaInfo, schemasRepository);
                            break;
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            completionList.Sort(new DataItemComparer());
            return completionList;
        }

        class DataItemComparer : IComparer<CompletionData>
        {
            public int Compare(CompletionData a, CompletionData b)
            {
                var comparable1 = a as IComparable;
                var comparable2 = b as IComparable;
                if (comparable1 != null && comparable2 != null)
                    return comparable1.CompareTo(comparable2);
                return CompletionData.Compare(a, b);
            }
        }

        private void CompletionWindowManager_WordCompleted(object sender, CodeCompletionContextEventArgs e)
        {
            if (SelectedCompletionItem == null)
                return;
            if (SelectedCompletionItem.UndeclaredNamespaceUsed)
            {
                using (Editor.OpenUndoGroup())
                {
                    AddNewNamespaceToModule(SelectedCompletionItem.NsPrefix, SelectedCompletionItem.Namespace);
                }
            }
            AddXsiTypeAttribute();
        }

        private void AddXsiTypeAttribute()
        {
            if (SelectedCompletionItem.ElementType == null || IsRootType(SelectedCompletionItem.ElementType)) return;

            var doc = DocumentContext.ParsedDocument as SyntactikParsedDocument;
            var module = doc?.Ast as Module;
            if (module == null) return;

            var currentLine = Editor.CaretLine;
            var prevLineText = Editor.GetLineText(currentLine);
            var prevLineTextTrimmed = prevLineText.TrimEnd() + Editor.EolMarker;
            var prevIndent = Editor.GetLineIndent(currentLine);
            var indent = prevIndent +
                         new string(module.IndentSymbol == 0 ? '\t' : module.IndentSymbol,
                             module.IndentMultiplicity == 0 ? 1 : module.IndentMultiplicity);
            bool newTypePrefix;
            var typePrefix = CompletionHelper.GetNamespacePrefix(SelectedCompletionItem.ElementType.Namespace,
                SelectedCompletionItem.CompletionContextPair,
                ((SyntactikProject) DocumentContext.Project).SchemasRepository, out newTypePrefix);
            if (!string.IsNullOrEmpty(typePrefix)) typePrefix += ":"; 
            var typeAttr = $"@xsi.type = {typePrefix}{SelectedCompletionItem.ElementType.Name}";
            using (Editor.OpenUndoGroup())
            {
                //Trimming end of previous line
                Editor.ReplaceText(Editor.GetLine(currentLine).Offset, prevLineText.Length, prevLineTextTrimmed);
                Editor.ReplaceText(Editor.GetLine(currentLine + 1).Offset, 0, indent + typeAttr);
                if (SelectedCompletionItem.XsiUndeclared)
                    AddNewNamespaceToModule("xsi", XmlSchemaInstanceNamespace.Url);
                if (newTypePrefix && typePrefix != "xsi")
                    AddNewNamespaceToModule(typePrefix, SelectedCompletionItem.ElementType.Namespace);
            }
        }

        /// <summary>
        /// Returns true if type has no parent type
        /// </summary>
        /// <param name="elementType"></param>
        /// <returns></returns>
        private static bool IsRootType(ElementType elementType)
        {
            var complexType = elementType as ComplexType;
            if (complexType?.BaseType == null) return true;
            if (complexType.BaseType.Name == "anyType" &&
                complexType.BaseType.Namespace == "http://www.w3.org/2001/XMLSchema") return true;
            return false;
        }

        internal void AddNewNamespaceToModule(string nsPrefix, string ns)
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

        internal CompletionContextTask CompletionContextTask => _completionContextTask;

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
                    LoggingService.LogError("Unhandled exception in SyntactikCompletionTextEditorExtension.UpdatePath.", ex);
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

        internal static List<PathEntry> GetPathEntries(IEnumerable<Pair> pairs)
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

        public string FormatPlainText(int offset, string text, byte[] copyData)
        {
            if (copyData != null)
            {
                var firstLineIndent = BitConverter.ToInt32(copyData, 0);
                text = new string('\t', firstLineIndent) + text;
            }
            var line = Editor.GetLineByOffset(offset);
            var column = offset - line.Offset;

            text = Normalize(text, column);
            return text;
        }

        public byte[] GetCopyData(ISegment segment)
        {
            var startLine = Editor.GetLineByOffset(segment.Offset);
            var prefix = Editor.GetTextAt(new TextSegment(startLine.Offset, segment.Offset - startLine.Offset));
            var indent = 0;
            if (string.IsNullOrEmpty(prefix.Trim()))
                indent = prefix.Length;
            return BitConverter.GetBytes(indent);
        }

        static readonly Regex rxNormalizeIndents = new Regex(@"((?<indent>[\t ]*)(?<text>[^\n\r]*(\r\n|\n\r|\r|\n)?))");
        private string Normalize(string text, int column)
        {
            var matches = rxNormalizeIndents.Matches(text).Cast<Match>().Where(m => m.Length > 0).Select(m => new { Indent = m.Groups["indent"].Value, Text = m.Groups["text"].Value }).ToList();
            var nonEmpty = matches.Where(m => !string.IsNullOrEmpty(m.Text.Trim())).ToList();
            var minIndent = 0;
            if (nonEmpty.Any())
                minIndent = nonEmpty.Min(m => m.Indent.Length);

            var normlized = new StringBuilder();
            var rawLines = matches.Select(m => new { Indent = new string('\t', m.Indent.Length != 0 ? m.Indent.Length - minIndent : 0), m.Text }).ToList();
            foreach (var l in rawLines)
            {
                var indent = l.Indent;
                if (rawLines[0] != l)
                    indent += new string('\t', column);
                normlized.AppendFormat("{0}{1}", indent, l.Text);
            }
            var result = normlized.ToString();

            return result;
        }

    }
}