using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using MonoDevelop.Projects;

namespace Syntactik.MonoDevelop.Schemas
{
    public class XsdSchemaProvider : XmlSchemaProvider
    {
        XmlSchemaSet _schemaset;
        readonly Dictionary<string, XmlSchema> _includes = new Dictionary<string, XmlSchema>();

        private readonly Project _project;

        public XsdSchemaProvider(Project project)
        {
            _project = project;
        }

        private IEnumerable<ProjectFile> GetSchemaProjectFiles()
        {
            if (_project == null) return new List<ProjectFile>();
            var services = _project.Items.OfType<ProjectFile>()
                .Where(i => i.ProjectVirtualPath.ParentDirectory.FileNameWithoutExtension.ToLower() == "schemas" &&
                i.ProjectVirtualPath.ParentDirectory.ParentDirectory.FileNameWithoutExtension == "" &&
                i.FilePath.Extension == ".xsd");
            //var services = from projectItem in _project.Items.OfType<ProjectFile>()
            //    where projectItem.FilePath.IsChildPathOf(shemaFolder.FilePath) && projectItem.FilePath.Extension == ".xsd"
            //    select projectItem;
            return services;
        }

        protected virtual string[] GetSchemaFiles()
        {
            return GetSchemaProjectFiles().Select(f => f.FilePath.FullPath.ToString()).ToArray();
        }

        public void UpdateServices()
        {
            _schemaset = new XmlSchemaSet {XmlResolver = null};
            _includes.Clear();
            foreach (var projectFile in GetSchemaFiles())
            {
                if (File.Exists(projectFile))
                    if (!_includes.ContainsKey(projectFile))
                        using (XmlReader reader = XmlReader.Create(projectFile))
                        {
                            var schema = XmlSchema.Read(reader, null);
                            _includes.Add(projectFile, schema);
                            var dir = Path.GetDirectoryName(projectFile);
                            LoadIncludes(dir, _schemaset, schema);
                            _schemaset.Add(schema);
                        }
            }
            _schemaset.Compile();
        }

        private void LoadIncludes(string dir, XmlSchemaSet schemaSet, XmlSchema schema)
        {
            foreach (var i in schema.Includes)
            {
                if (i is XmlSchemaImport)
                {
                    var import = i as XmlSchemaImport;
                    var path = Path.Combine(dir, import.SchemaLocation);

                    if (_includes.ContainsKey(path))
                        import.Schema = _includes[path];
                    else
                    if (File.Exists(path))
                    {
                        using (XmlReader reader = XmlReader.Create(path))
                        {
                            var iSchema = XmlSchema.Read(reader, (sender, args) =>
                            {
                                System.Diagnostics.Debugger.Break();
                            });
                            _includes.Add(path, iSchema);
                            schemaSet.Add(iSchema);
                            LoadIncludes(dir, schemaSet, iSchema);
                            import.Schema = iSchema;
                        }

                    }
                }
                if (i is XmlSchemaInclude)
                {
                    var include = i as XmlSchemaInclude;
                    var path = Path.Combine(dir, include.SchemaLocation);

                    if (_includes.ContainsKey(path))
                        include.Schema = _includes[path];
                    else
                    if (File.Exists(path))
                    {
                        using (XmlReader reader = XmlReader.Create(path))
                        {
                            var iSchema = XmlSchema.Read(reader, (sender, args) =>
                            {
                                System.Diagnostics.Debugger.Break();
                            });
                            _includes.Add(path, iSchema);
                            schemaSet.Add(iSchema);
                            LoadIncludes(dir, schemaSet, iSchema);
                            include.Schema = iSchema;
                        }

                    }
                }

            }
        }

        protected override IEnumerable<XmlSchemaSet> GetSchemaSets()
        {
            UpdateServices();
            if (_schemaset == null)
                yield break;
            yield return _schemaset;
        }
    }
}