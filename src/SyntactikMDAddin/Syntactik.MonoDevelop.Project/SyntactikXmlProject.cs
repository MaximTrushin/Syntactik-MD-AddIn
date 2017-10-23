using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using MonoDevelop.Projects;
using Syntactik.MonoDevelop.Schemas;

namespace Syntactik.MonoDevelop.Projects
{
    [ProjectModelDataItem]
    public class SyntactikXmlProject : SyntactikProject, IProjectFilesProvider
    {
        private readonly object _syncRoot = new object();
        public SchemasRepository SchemasRepository { get; private set; }
        public SyntactikXmlProject()
		{
        }

        public SyntactikXmlProject(ProjectCreateInformation info, XmlElement projectOptions): base(info, projectOptions)
        {
        }

        protected override string[] OnGetSupportedLanguages()
        {
            return new[] { "", //Adds html, txt and xml to the list of file templates in the New File dialog
                "S4X" };
        }

        protected override void ParseProjectFiles()
        {
            var files =
                from projectItem in this.Items.OfType<ProjectFile>()
                let pfile = projectItem
                let isDir = Directory.Exists(pfile.FilePath.FullPath)
                where !isDir
                where pfile.FilePath.Extension.ToLower() == ".s4x"
                select projectItem.FilePath.FullPath.ToString();
            foreach (var file in files)
            {
                ParseProjectFile(file);
            }
        }

        protected override IEnumerable<string> GetProjectFiles(SyntactikProject project)
        {
            var sources = project.Items.GetAll<ProjectFile>()
                .Where(i => i.Subtype != Subtype.Directory && (i.FilePath.Extension.ToLower() == ".s4x" || i.FilePath.Extension.ToLower() == ".xsd"))
                .Select(i => i.FilePath.FullPath.ToString());
            return sources;
        }

        protected override void OnFileAddedToProject(ProjectFileEventArgs e)
        {
            base.OnFileAddedToProject(e);
            var schemasAdded = false;
            foreach (var file in e)
            {
                if (file.ProjectFile.ProjectVirtualPath.ParentDirectory.FileName == "Schemas")
                    schemasAdded = true;
                
            }
            if (schemasAdded)
                SchemasRepository = new SchemasRepository(this);
        }

        protected override void OnFileRemovedFromProject(ProjectFileEventArgs e)
        {
            base.OnFileRemovedFromProject(e);
            var schemaRemoved = false;
            foreach (var file in e)
            {
                if (file.ProjectFile.ProjectVirtualPath.ParentDirectory.FileName == "Schemas")
                    schemaRemoved = true;

            }
            if (schemaRemoved)
                SchemasRepository = new SchemasRepository(this);
        }

        protected override void OnFileRenamedInProject(ProjectFileRenamedEventArgs e)
        {
            base.OnFileRenamedInProject(e);
            var sourceFileRenamed = false;
            foreach (var file in e)
            {
                lock (_syncRoot)
                {
                    CompileInfo.Remove(file.OldName);
                }
                if (file.NewName.IsDirectory || file.NewName.Extension != ".s4x") continue;
                ParseProjectFile(file.NewName);
                sourceFileRenamed = true;
            }
            if (sourceFileRenamed)
                CompilerContext = ValidateModules(CompileInfo);

        }

        public IEnumerable<string> GetSchemaProjectFiles()
        {
            var services = Items.OfType<ProjectFile>()
                .Where(i => i.ProjectVirtualPath.ParentDirectory.FileNameWithoutExtension.ToLower() == "schemas" &&
                i.ProjectVirtualPath.ParentDirectory.ParentDirectory.FileNameWithoutExtension == "" &&
                i.FilePath.Extension == ".xsd").Select(i => i.FilePath.ToString());
            return services;
        }

    }
}
