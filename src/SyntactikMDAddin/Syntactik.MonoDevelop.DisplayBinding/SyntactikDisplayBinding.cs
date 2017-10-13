using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;

namespace Syntactik.MonoDevelop.DisplayBinding
{
    public class SyntactikDisplayBinding: IViewDisplayBinding
    {
        private bool _excludeThis;
        internal const string TextXmlMimeType = "text/xml";
        internal const string ApplicationXmlMimeType = "application/xml";
        internal const string S4xMimeType = "text/x-syntactik4xml";
        public bool CanHandle(FilePath fileName, string mimeType, Project ownerProject)
        {
            if (_excludeThis)
                return false;

            if (fileName.IsNullOrEmpty)
                return false;

            if (!(mimeType == TextXmlMimeType || mimeType == ApplicationXmlMimeType)) return false;
            _excludeThis = true;
            var db = DisplayBindingService.GetDefaultViewBinding(fileName, mimeType, ownerProject);
            _excludeThis = false;
            return db != null;
        }

        public bool CanUseAsDefault => true;
        public ViewContent CreateContent(FilePath fileName, string mimeType, Project ownerProject)
        {
            _excludeThis = true;
            var db = DisplayBindingService.GetDefaultViewBinding(fileName, mimeType, ownerProject);
            var content = db.CreateContent(fileName, mimeType, ownerProject);
            _excludeThis = false;
            var cdv = new SyntactikView(content, db, fileName, S4xMimeType, ownerProject);
            return cdv;
        }

        public string Name => GettextCatalog.GetString("Syntactik Editor");

    }
}
