using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Gtk;
using MonoDevelop.Core;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.TypeSystem;

namespace Syntactik.MonoDevelop.Parser
{
    class FoldingTextEditorExtension : TextEditorExtension
    {
        bool _isDisposed;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        protected override void Initialize()
        {
            DocumentContext.DocumentParsed += DocumentContext_DocumentParsed;
        }

        public override void Dispose()
        {
            if (_isDisposed)
                return;
            _isDisposed = true;
            CancelDocumentParsedUpdate();
            DocumentContext.DocumentParsed -= DocumentContext_DocumentParsed;
            base.Dispose();
        }

        void CancelDocumentParsedUpdate()
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        void DocumentContext_DocumentParsed(object sender, EventArgs e)
        {
            CancelDocumentParsedUpdate();
            var caretLocation = Editor.CaretLocation;
            var parsedDocument = DocumentContext.ParsedDocument;
            Task.Run( () => {
                try
                {
                    if (!_isDisposed)
                        UpdateFoldings(Editor, parsedDocument, caretLocation, false, _cancellationTokenSource.Token);
                }
                catch (OperationCanceledException) { }
            }, _cancellationTokenSource.Token);
        }

        internal static void UpdateFoldings(TextEditor Editor, ParsedDocument parsedDocument, DocumentLocation caretLocation, bool firstTime = false, CancellationToken token = default(CancellationToken))
        {
            if (parsedDocument == null || !Editor.Options.ShowFoldMargin)
                return;

            try
            {
                var foldSegments = new List<IFoldSegment>();

                foreach (FoldingRegion region in ((SyntactikParsedDocument)parsedDocument).Foldings)
                {
                    if (token.IsCancellationRequested)
                        return;
                    var type = FoldingType.Unknown;
                    bool setFolded = false;
                    bool folded = false;
                    //decide whether the regions should be folded by default
                    switch (region.Type)
                    {
                        case FoldType.Member:
                            type = FoldingType.TypeMember;
                            break;
                        case FoldType.Type:
                            type = FoldingType.TypeDefinition;
                            break;
                        case FoldType.UserRegion:
                            type = FoldingType.Region;
                            setFolded = DefaultSourceEditorOptions.Instance.DefaultRegionsFolding;
                            folded = true;
                            break;
                        case FoldType.Comment:
                            type = FoldingType.Comment;
                            setFolded = DefaultSourceEditorOptions.Instance.DefaultCommentFolding;
                            folded = true;
                            break;
                        case FoldType.CommentInsideMember:
                            type = FoldingType.Comment;
                            setFolded = DefaultSourceEditorOptions.Instance.DefaultCommentFolding;
                            break;
                        case FoldType.Undefined:
                            setFolded = true;
                            folded = region.IsFoldedByDefault;
                            break;
                    }
                    var start = Editor.LocationToOffset(region.Region.Begin);
                    var end = Editor.LocationToOffset(region.Region.End);
                    var marker = FoldSegmentFactory.CreateFoldSegment(Editor, start, end - start, folded);
                    foldSegments.Add(marker);
                    marker.CollapsedText = region.Name;
                    marker.FoldingType = type;
                    //and, if necessary, set its fold state
                    if (setFolded && firstTime)
                    {
                        // only fold on document open, later added folds are NOT folded by default.
                        marker.IsCollapsed = folded;
                        continue;
                    }
                    if (region.Region.Contains(caretLocation.Line, caretLocation.Column))
                        marker.IsCollapsed = false;
                }
                if (firstTime)
                {
                    Editor.SetFoldings(foldSegments);
                }
                else
                {
                    Application.Invoke(delegate
                    {
                        if (!token.IsCancellationRequested)
                            Editor.SetFoldings(foldSegments);
                    });
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Unhandled exception in ParseInformationUpdaterWorkerThread", ex);
            }
        }

    }
}
