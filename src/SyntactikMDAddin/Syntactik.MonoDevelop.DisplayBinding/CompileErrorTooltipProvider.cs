// CompileErrorTooltipProvider.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//

using System.Linq;
using MonoDevelop.Ide.Editor;
using System.Threading.Tasks;
using System.Threading;
using Mono.TextEditor;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using Xwt;
using TooltipItem = MonoDevelop.Ide.Editor.TooltipItem;
using TooltipProvider = MonoDevelop.Ide.Editor.TooltipProvider;


namespace Syntactik.MonoDevelop.DisplayBinding
{
    class CompileErrorTooltipProvider : TooltipProvider
    {
        private TextEditor _editor;
        #region ITooltipProvider implementation 
        public override Task<TooltipItem> GetItem(TextEditor editor, DocumentContext ctx, int offset, CancellationToken token = default(CancellationToken))
        {
            var d = ctx as Document;
            var ted = d?.GetContent<TextEditorData>();
            
            var hw = d?.Window.ActiveViewContent.WorkbenchWindow as HiddenWorkbenchWindow;
            _editor = hw?.Document.Editor;
            string errorInformation = GetErrorInformationAt(offset, ted?.Document);
            if (string.IsNullOrEmpty(errorInformation))
                return Task.FromResult<TooltipItem>(null);

            return Task.FromResult(new TooltipItem(errorInformation, _editor.GetLineByOffset(offset)));
        }

        internal string GetErrorInformationAt(int offset, TextDocument document)
        {
            if (document == null) return null;
            var location = document.OffsetToLocation(offset);
            DocumentLine line = document.GetLine(location.Line);
            if (line == null)
                return null;

            // ReSharper disable once SuspiciousTypeConversion.Global
            var error = document.GetTextSegmentMarkersAt(offset).OfType<IErrorMarker>().FirstOrDefault();

            if (error != null)
            {
                if (error.Error.ErrorType == global::MonoDevelop.Ide.TypeSystem.ErrorType.Warning)
                    return GettextCatalog.GetString("<b>Warning</b>: {0}",
                        GLib.Markup.EscapeText(error.Error.Message));
                return GettextCatalog.GetString("<b>Error</b>: {0}",
                    GLib.Markup.EscapeText(error.Error.Message));
            }
            return null;
        }

        public override Control CreateTooltipWindow(TextEditor editor, DocumentContext ctx, TooltipItem item, int offset, Xwt.ModifierKeys modifierState)
        {
            var result = new LanguageItemWindow( (string)item.Item);
            return result.IsEmpty ? null : result;
        }

        public override void ShowTooltipWindow(TextEditor editor, Control tipWindow, TooltipItem item, ModifierKeys modifierState, int mouseX,
            int mouseY)
        {
            base.ShowTooltipWindow(_editor, tipWindow, item, modifierState, mouseX, mouseY);
        }

        public override void GetRequiredPosition(TextEditor editor, Control tipWindow, out int requiredWidth, out double xalign)
        {
            var win = (LanguageItemWindow)tipWindow;
            requiredWidth = win.SetMaxWidth(win.Screen.Width / 4);
            xalign = 0.5;
        }
        #endregion
    }
}
