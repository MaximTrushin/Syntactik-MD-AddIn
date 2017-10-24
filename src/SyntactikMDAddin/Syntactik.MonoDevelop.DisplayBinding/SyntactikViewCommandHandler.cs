using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.Gui.Content;

namespace Syntactik.MonoDevelop.DisplayBinding
{
    class SyntactikViewCommandHandler : TextEditorExtension
    {
        public override bool IsValidInContext(DocumentContext context)
        {
            return context is SyntactikDocument;
        }

        [CommandHandler(EditCommands.Undo)]
        protected void OnUndo()
        {
            var editable = Editor.GetContent<IUndoHandler>();
            editable?.Undo();
        }

        [CommandUpdateHandler(EditCommands.Undo)]
        protected void OnUpdateUndo(CommandInfo info)
        {
            var textBuffer = Editor.GetContent<IUndoHandler>();
            info.Enabled = textBuffer != null && textBuffer.EnableUndo;
        }

        [CommandHandler(EditCommands.Redo)]
        protected void OnRedo()
        {
            IUndoHandler editable = Editor.GetContent<IUndoHandler>();
            editable?.Redo();
        }

        [CommandUpdateHandler(EditCommands.Redo)]
        protected void OnUpdateRedo(CommandInfo info)
        {
            IUndoHandler textBuffer = Editor.GetContent<IUndoHandler>();
            info.Enabled = textBuffer != null && textBuffer.EnableRedo;
        }

        [CommandHandler(EditCommands.Cut)]
        protected void OnCut()
        {
            IClipboardHandler handler = Editor.GetContent<IClipboardHandler>();
            handler?.Cut();
        }

        [CommandUpdateHandler(EditCommands.Cut)]
        protected void OnUpdateCut(CommandInfo info)
        {
            bool inWpf = false;
#if WIN32
			if (System.Windows.Input.Keyboard.FocusedElement != null)
				inWpf = true;
#endif
            IClipboardHandler handler = Editor.GetContent<IClipboardHandler>();
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (!inWpf && handler != null && handler.EnableCut)
                info.Enabled = true;
            else
                info.Bypass = true;
        }

        [CommandHandler(EditCommands.Copy)]
        protected void OnCopy()
        {
            IClipboardHandler handler = Editor.GetContent<IClipboardHandler>();
            handler?.Copy();
        }

        [CommandUpdateHandler(EditCommands.Copy)]
        protected void OnUpdateCopy(CommandInfo info)
        {
            bool inWpf = false;
#if WIN32
			if (System.Windows.Input.Keyboard.FocusedElement != null)
				inWpf = true;
#endif
            IClipboardHandler handler = Editor.GetContent<IClipboardHandler>();
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (!inWpf && handler != null && handler.EnableCopy)
                info.Enabled = true;
            else
                info.Bypass = true;
        }

        [CommandHandler(EditCommands.Paste)]
        protected void OnPaste()
        {
            var handler = Editor.GetContent<IClipboardHandler>();
            handler?.Paste();
        }

        [CommandUpdateHandler(EditCommands.Paste)]
        protected void OnUpdatePaste(CommandInfo info)
        {
            bool inWpf = false;
#if WIN32
			if (System.Windows.Input.Keyboard.FocusedElement != null)
				inWpf = true;
#endif
            var handler = Editor.GetContent<IClipboardHandler>();
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (!inWpf && handler != null && handler.EnablePaste)
                info.Enabled = true;
            else
                info.Bypass = true;
        }

        [CommandHandler(EditCommands.Delete)]
        protected void OnDelete()
        {
            IClipboardHandler handler = Editor.GetContent<IClipboardHandler>();
            handler?.Delete();
        }

        [CommandUpdateHandler(EditCommands.Delete)]
        protected void OnUpdateDelete(CommandInfo info)
        {
            IClipboardHandler handler = Editor.GetContent<IClipboardHandler>();
            if (handler != null)
                info.Enabled = handler.EnableDelete;
            else
                info.Bypass = true;
        }

        [CommandHandler(EditCommands.SelectAll)]
        protected void OnSelectAll()
        {
            IClipboardHandler handler = Editor.GetContent<IClipboardHandler>();
            handler?.SelectAll();
        }

        [CommandUpdateHandler(EditCommands.SelectAll)]
        protected void OnUpdateSelectAll(CommandInfo info)
        {
            bool inWpf = false;
#if WIN32
			if (System.Windows.Input.Keyboard.FocusedElement != null)
				inWpf = true;
#endif
            IClipboardHandler handler = Editor.GetContent<IClipboardHandler>();
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (!inWpf && handler != null)
                info.Enabled = handler.EnableSelectAll;
            else
                info.Bypass = true;
        }

        [CommandHandler(EditCommands.UppercaseSelection)]
        public void OnUppercaseSelection()
        {
            var buffer = Editor.GetContent<TextEditor>();
            if (buffer == null)
                return;

            string selectedText = buffer.SelectedText;
            if (string.IsNullOrEmpty(selectedText))
            {
                int pos = buffer.CaretOffset;
                string ch = buffer.GetTextAt(pos, pos + 1);
                string upper = ch.ToUpper();
                if (upper == ch)
                {
                    buffer.CaretOffset = pos + 1;
                    return;
                }
                using (buffer.OpenUndoGroup())
                {
                    buffer.RemoveText(pos, 1);
                    buffer.InsertText(pos, upper);
                    buffer.CaretOffset = pos + 1;
                }
            }
            else
            {
                string newText = selectedText.ToUpper();
                if (newText == selectedText)
                    return;
                int startPos = buffer.SelectionRange.Offset;
                using (buffer.OpenUndoGroup())
                {
                    buffer.RemoveText(startPos, selectedText.Length);
                    buffer.InsertText(startPos, newText);
                    buffer.SetSelection(startPos, startPos + newText.Length);
                }
            }
        }

        [CommandUpdateHandler(EditCommands.UppercaseSelection)]
        protected void OnUppercaseSelection(CommandInfo info)
        {
            var buffer = Editor.GetContent<TextEditor>();
            info.Enabled = buffer != null;
        }

        [CommandHandler(EditCommands.LowercaseSelection)]
        public void OnLowercaseSelection()
        {
            var buffer = Editor.GetContent<TextEditor>();
            if (buffer == null)
                return;

            string selectedText = buffer.SelectedText;
            if (string.IsNullOrEmpty(selectedText))
            {
                int pos = buffer.CaretOffset;
                string ch = buffer.GetTextAt(pos, pos + 1);
                string lower = ch.ToLower();
                if (lower == ch)
                {
                    buffer.CaretOffset = pos + 1;
                    return;
                }
                using (buffer.OpenUndoGroup())
                {
                    buffer.RemoveText(pos, 1);
                    buffer.InsertText(pos, lower);
                    buffer.CaretOffset = pos + 1;
                }
            }
            else
            {
                string newText = selectedText.ToLower();
                if (newText == selectedText)
                    return;
                int startPos = buffer.SelectionRange.Offset;
                using (buffer.OpenUndoGroup())
                {
                    buffer.RemoveText(startPos, selectedText.Length);
                    buffer.InsertText(startPos, newText);
                    buffer.SetSelection(startPos, startPos + newText.Length);
                }
            }
        }

        [CommandUpdateHandler(EditCommands.LowercaseSelection)]
        protected void OnLowercaseSelection(CommandInfo info)
        {
            var buffer = Editor.GetContent<TextEditor>();
            info.Enabled = buffer != null;
        }
    }
}
