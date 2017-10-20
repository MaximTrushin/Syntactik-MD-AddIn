using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.TypeSystem;
using Syntactik.MonoDevelop.DisplayBinding;

namespace Syntactik.MonoDevelop.Parser
{
    public class ErrorHandlerTextEditorExtension: TextEditorExtension
    {
        CancellationTokenSource src = new CancellationTokenSource();
        bool isDisposed;
        protected override void Initialize()
        {
            DocumentContext.DocumentParsed += DocumentContext_DocumentParsed;
        }

        public override bool IsValidInContext(DocumentContext context)
        {
            return context.Name.EndsWith(".s4x") || context.Name.EndsWith(".s4j") || context is SyntactikDocument;
        }

        public override void Dispose()
        {
            if (isDisposed)
                return;
            isDisposed = true;
            RemoveErrorUnderlines();
            RemoveErrorUnderlinesResetTimerId();
            CancelDocumentParsedUpdate();
            DocumentContext.DocumentParsed -= DocumentContext_DocumentParsed;
            base.Dispose();
        }

        void DocumentContext_DocumentParsed(object sender, EventArgs e)
        {
            CancelDocumentParsedUpdate();
            var token = src.Token;
            var parsedDocument = DocumentContext.ParsedDocument;
            Task.Run(async () => {
                try
                {
                    var ctx = DocumentContext;
                    if (ctx == null)
                        return;
                    await UpdateErrorUnderlines(parsedDocument, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // ignore
                }
            }, token);
        }

        async Task UpdateErrorUnderlines(ParsedDocument parsedDocument, CancellationToken token)
        {
            if (parsedDocument == null || isDisposed)
                return;
            try
            {
                var errors = await parsedDocument.GetErrorsAsync(token).ConfigureAwait(false);
                Gtk.Application.Invoke(delegate {
                    if (token.IsCancellationRequested || isDisposed)
                        return;
                    RemoveErrorUnderlinesResetTimerId();
                    const uint timeout = 500;
                    resetTimerId = GLib.Timeout.Add(timeout, delegate {
                        if (token.IsCancellationRequested)
                        {
                            resetTimerId = 0;
                            return false;
                        }
                        RemoveErrorUnderlines();
                        // Else we underline the error
                        if (errors != null)
                        {
                            foreach (var error in errors)
                            {
                                UnderLineError(error);
                            }
                        }
                        resetTimerId = 0;
                        return false;
                    });
                });
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
        }

        readonly List<IErrorMarker> _errors = new List<IErrorMarker>();
        uint resetTimerId;

        private void RemoveErrorUnderlinesResetTimerId()
        {
            if (resetTimerId > 0)
            {
                GLib.Source.Remove(resetTimerId);
                resetTimerId = 0;
            }
        }

        private void RemoveErrorUnderlines()
        {
            _errors.ForEach(err => Editor.RemoveMarker(err));
            _errors.Clear();
        }

        private void UnderLineError(Error info)
        {
            var error = TextMarkerFactory.CreateErrorMarker(Editor, info);
            Editor.AddMarker(error);
            _errors.Add(error);
        }

        private void CancelDocumentParsedUpdate()
        {
            src.Cancel();
            src = new CancellationTokenSource();
        }

    }
}
