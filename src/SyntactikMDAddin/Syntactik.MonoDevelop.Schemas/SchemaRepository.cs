using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Syntactik.MonoDevelop.Projects;

namespace Syntactik.MonoDevelop.Schemas
{
    internal class SchemasRepository
    {
        readonly List<ISchemaProvider> _providers = new List<ISchemaProvider>();

        public SchemasRepository(IProjectFilesProvider provider)
        {
            _providers.Add(new XsdSchemaProvider(provider));
            _providers.Add(new XmlSchemaInstanceNamespace());
        }

        private IEnumerable<NamespaceInfo> _namespaces;
        public IEnumerable<NamespaceInfo> GetNamespaces()
        {
            if (_namespaces != null) return _namespaces;
            return _namespaces = _providers.SelectMany(p => p.GetNamespaces());
        }

        public void ResetNamespaces()
        {
            _namespaces = null;
        }

        public void Validate(XmlDocument doc, Action<XmlNode, string> onErrorAction)
        {
            _providers.ForEach(p => p.Validate(doc, onErrorAction));
        }

        public ContextInfo GetContextInfo(Context ctx)
        {
            var result = new ContextInfo();

            foreach (var schemaProvider in _providers)
            {
                schemaProvider.PopulateContextInfo(ctx, result);
            }
            return result;
        }
    }
}