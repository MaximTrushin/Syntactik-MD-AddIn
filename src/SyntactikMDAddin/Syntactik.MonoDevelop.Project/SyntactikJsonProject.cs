using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using MonoDevelop.Projects;

namespace Syntactik.MonoDevelop.Projects
{
    [ProjectModelDataItem]
    public class SyntactikJsonProject : SyntactikProject
    {
        public SyntactikJsonProject()
        {
        }

        public SyntactikJsonProject(ProjectCreateInformation info, XmlElement projectOptions): base(info, projectOptions)
        {
        }

        protected override void ParseProjectFiles()
        {
            var files =
                from projectItem in this.Items.OfType<ProjectFile>()
                let pfile = projectItem
                let isDir = Directory.Exists(pfile.FilePath.FullPath)
                where !isDir
                where pfile.FilePath.Extension.ToLower() == ".s4j"
                select projectItem.FilePath.FullPath.ToString();
            foreach (var file in files)
            {
                ParseProjectFile(file);
            }
        }

        protected override IEnumerable<string> GetProjectFiles(SyntactikProject project)
        {
            var sources = project.Items.GetAll<ProjectFile>()
                .Where(i => i.Subtype != Subtype.Directory && (i.FilePath.Extension.ToLower() == ".s4j"))
                .Select(i => i.FilePath.FullPath.ToString());
            return sources;
        }

        protected override string[] OnGetSupportedLanguages()
        {
            return new[] { "", //Adds html, txt and xml to the list of file templates in the New File dialog
                "S4J" };
        }
    }
}
