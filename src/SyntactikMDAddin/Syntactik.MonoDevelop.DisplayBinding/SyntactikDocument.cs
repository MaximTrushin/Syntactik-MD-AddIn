using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Gui;

namespace Syntactik.MonoDevelop.DisplayBinding
{
    class SyntactikDocument : Document
    {
        private readonly TextEditor _editor;

        public SyntactikDocument(HiddenWorkbenchWindow window, TextEditor editor) : base(window)
        {
            _editor = editor;
            window.Document = this;
        }

        public override T GetContent<T>()
        {
            if (Window?.ActiveViewContent == null) return null;
            if (typeof(T) == typeof(TextEditor))
            {
                return _editor as T;
            }
            return base.GetContent<T>();
        }
    }
}
