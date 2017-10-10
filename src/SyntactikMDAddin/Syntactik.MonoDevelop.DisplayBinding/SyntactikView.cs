using System.Collections.Specialized;
using System.IO;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.GtkCore.GuiBuilder;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using Syntactik.MonoDevelop.Converter;
using Syntactik.MonoDevelop.Util;

namespace Syntactik.MonoDevelop.DisplayBinding
{
    public class SyntactikView : CombinedDesignView
    {
        private readonly TextEditor _syntactikEditor;
        private readonly ViewContent _viewContent;

        public SyntactikView(ViewContent content, IViewDisplayBinding db, FilePath fileName, string mimeType, Project ownerProject) : base(content)
        {
            _viewContent = db.CreateContent(Path.ChangeExtension(fileName, "s4x"), mimeType, ownerProject);
            _syntactikEditor = (TextEditor) _viewContent.Control;
            AddButton("Syntactik", _syntactikEditor);
        }

        public override string TabPageLabel => GettextCatalog.GetString("Xml");

        internal TextEditor SyntactikEditor => _syntactikEditor;

        public ViewContent ViewContent => _viewContent;

        protected override void OnPageShown(int npage)
        {
            if (npage == 1)
            {
                base.OnPageShown(npage);
                var crc = Control.GetNativeWidget<CommandRouterContainer>();
                var editor = ((ViewContent) crc.GetDelegatedCommandTarget()).Control as TextEditor;
                var converter = new XmlToSyntactikConverter(editor?.Text, true, true);
                string s4x;
                if (converter.Convert(0, '\t', 1, false, new ListDictionary(), out s4x))
                {
                    _syntactikEditor.Text = s4x;
                }
                else
                {
                    DialogHelper.ShowError("The editor contains the invalid XML fragment that can't be converted to Syntactik format.", null);
                    ShowPage(0);
                }
            }
        }
    }
}
