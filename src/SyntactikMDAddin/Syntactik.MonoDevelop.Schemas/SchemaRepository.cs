using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Syntactik.MonoDevelop.Projects;

namespace Syntactik.MonoDevelop.Schemas
{
    public class SchemasRepository
    {
        readonly List<ISchemaProvider> _providers = new List<ISchemaProvider>();

        public SchemasRepository(IProjectFilesProvider provider)
        {
            _providers.Add(new XmlSchemaInstanceNamespace());
            _providers.Add(new XmlSchemaNamespace());
            _providers.Add(new XsdSchemaProvider(provider));
        }

        public IEnumerable<NamespaceInfo> GetNamespaces()
        {
            return _providers.SelectMany(p => p.GetNamespaces());
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

        public void AddProvider(ISchemaProvider provider)
        {
            _providers.Add(provider);
        }
    }
}