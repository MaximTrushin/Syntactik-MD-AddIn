using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using MonoDevelop.Core;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Projects;
using Syntactik.Compiler;
using Syntactik.Compiler.IO;
using Syntactik.Compiler.Steps;
using Syntactik.MonoDevelop.Parser;

namespace Syntactik.MonoDevelop
{
    [ProjectModelDataItem]
    public class SyntactikProject : Project
    {
        internal class ParseInfo
        {
            public ITextSourceVersion Version;
            public SyntactikParsedDocument Document;
            public CompilerContext Context;
        }

        readonly object _syncRoot = new object();
        internal Dictionary<string, ParseInfo> CompileInfo { get; } = new Dictionary<string, ParseInfo>();

        public SyntactikProject()
		{
        }

        public SyntactikProject(ProjectCreateInformation info, XmlElement projectOptions): base(info, projectOptions)
        {
            Configurations.Add(CreateConfiguration("Default"));
        }

        internal Dictionary<string, Task<SyntactikParsedDocument>> CompiledDocuments { get; } = new Dictionary<string, Task<SyntactikParsedDocument>>();

        protected override void OnInitializeFromTemplate(ProjectCreateInformation projectCreateInfo, XmlElement template)
        {
            Configurations.Add(CreateConfiguration("Default"));
        }

        protected override SolutionItemConfiguration OnCreateConfiguration(string name, ConfigurationKind kind = ConfigurationKind.Blank)
        {
            ProjectConfiguration conf = new ProjectConfiguration(name);
            return conf;
        }

        protected override void OnGetTypeTags(HashSet<string> types)
        {
            base.OnGetTypeTags(types);
            types.Add("SyntactikProject");
        }

        protected override void OnWriteConfiguration(ProgressMonitor monitor, ProjectConfiguration config, IPropertySet pset)
        {
            base.OnWriteConfiguration(monitor, config, pset);
            pset.SetValue("OutputPath", config.OutputDirectory);
        }

        protected override void OnEndLoad()
        {
            base.OnEndLoad();
            ParseProjectFiles();
        }

        private void ParseProjectFiles()
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

        protected override string[] OnGetSupportedLanguages()
        {
            return new[] { "", //Adds html, txt and xml to the list of file templates in the New File dialog
                "S4X", "S4J" };
        }

        private void ParseProjectFile(string fileName)
        {
            string content = File.ReadAllText(fileName);
            var document = ParseSyntactikDocument(fileName, content, null, new CancellationToken());

            lock (_syncRoot)
            {
                CompileInfo.Remove(fileName);
                CompileInfo.Add(fileName, 
                    new ParseInfo()
                    {
                        Document = document,
                        Version = null
                    }
                );
           }
        }

        internal SyntactikParsedDocument ParseSyntactikDocument(string fileName, string content, ITextSourceVersion version, CancellationToken cancellationToken)
        {
            ParseInfo info;
            if (version != null && CompileInfo.TryGetValue(fileName, out info))
            {
                if (info.Version != null && version.BelongsToSameDocumentAs(info.Version) && version.CompareAge(info.Version) == 0)
                {
                    return info.Document;
                }
            }

            var result = new SyntactikParsedDocument(fileName, version);
            var compilerParameters = CreateCompilerParameters(fileName, content, result, cancellationToken);
            var compiler = new SyntactikCompiler(compilerParameters);
            var context = compiler.Run();
            result.AddErrors(context.Errors.Where(error => error.LexicalInfo.Line >= 0 && error.LexicalInfo.Column >= 0));
            CompileInfo.Remove(fileName);
            CompileInfo.Add(fileName,
                new ParseInfo
                {
                    Document = result,
                    Context = context,
                    Version = version
                }
            );
            
            return result;
        }

        private CompilerParameters CreateCompilerParameters(string fileName, string content, SyntactikParsedDocument result, CancellationToken cancellationToken)
        {
            var compilerParameters = new CompilerParameters { Pipeline = new CompilerPipeline() };
            compilerParameters.Pipeline.Steps.Add(new ParseAndCreateFoldingStep(result, cancellationToken));
            //compilerParameters.Pipeline.Steps.Add(new ProcessAliasesAndNamespaces());
            //compilerParameters.Pipeline.Steps.Add(new ValidateDocuments());
            compilerParameters.Input.Add(new StringInput(fileName, content));
            return compilerParameters;
        }

        [ProjectModelDataItem]
        public class SyntactikProjectConfiguration : ProjectConfiguration
        {
            public SyntactikProjectConfiguration(string id) : base(id)
            {
            }
        }

    }
}
