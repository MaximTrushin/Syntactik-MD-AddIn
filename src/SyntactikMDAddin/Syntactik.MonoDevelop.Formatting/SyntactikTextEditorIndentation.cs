using Mono.TextEditor;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;
using Syntactik.DOM;
using Syntactik.MonoDevelop.Parser;
using IndentStyle = MonoDevelop.Ide.Editor.IndentStyle;

namespace Syntactik.MonoDevelop.Formatting
{
    class SyntactikTextEditorIndentation: TextEditorExtension
    {
        public override bool KeyPress(KeyDescriptor descriptor)
        {
            if (Editor.Options.IndentStyle != IndentStyle.Smart) return base.KeyPress(descriptor);

            var newLine = Editor.CaretLine + 1;
            var ret = base.KeyPress(descriptor);
            if (descriptor.SpecialKey != SpecialKey.Return || Editor.CaretLine != newLine) return ret;
            var prevLineText = Editor.GetLineText(newLine - 1);
            if (!prevLineText.TrimEnd().EndsWith(":")) return ret;
            var textEditorData = DocumentContext.GetContent<TextEditorData>();
                    
            var line = textEditorData.GetLine(newLine);
            if (line.StartSpan == null || line.StartSpan.Count == 0)
            { //If new line have start spans then previous line ended with open string and we don't need to increase indent
                var doc = DocumentContext.ParsedDocument as SyntactikParsedDocument;
                var module = doc?.Ast as Module;
                if (module != null)
                {
                    var oldIndent = Editor.GetLineIndent(newLine);
                    var prevIndent = Editor.GetLineIndent(newLine - 1);
                    var indent = prevIndent + new string(module.IndentSymbol == 0?'\t': module.IndentSymbol, module.IndentMultiplicity == 0?1: module.IndentMultiplicity);
                    using (Editor.OpenUndoGroup())
                    {
                        Editor.ReplaceText(Editor.GetLine(newLine).Offset, oldIndent.Length, indent);
                    }
                }
            }
            return ret;
        }
    }
}
