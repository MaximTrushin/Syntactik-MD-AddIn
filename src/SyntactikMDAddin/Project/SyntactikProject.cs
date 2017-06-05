using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using MonoDevelop.Core;
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
        object _syncRoot = new object();
        public SyntactikProject()
		{
        }

        public SyntactikProject(ProjectCreateInformation info, XmlElement projectOptions)
        {
            Configurations.Add(CreateConfiguration("Default"));
        }

        internal Dictionary<string, Task<SyntactikParsedDocument>> CompiledDocuments { get; } = new Dictionary<string, Task<SyntactikParsedDocument>>();

        protected override void OnInitializeFromTemplate(ProjectCreateInformation projectCreateInfo, XmlElement template)
        {
            Configurations.Add(CreateConfiguration("Default"));
        }

        protected override SolutionItemConfiguration OnCreateConfiguration(string name, ConfigurationKind kind)
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
                where pfile.FilePath.Extension.ToLower() == ".ml"
                select projectItem.FilePath.FullPath.ToString();
            foreach (var file in files)
            {
                ParseProjectFile(file);
            }

        }

        private void ParseProjectFile(string file)
        {
            lock (_syncRoot)
            {
                
            }
        }

        internal SyntactikParsedDocument ParseSyntactikDocument(ParseOptions options, CancellationToken cancellationToken)
        {
            var result = new SyntactikParsedDocument(options.FileName, options.Content.Version);
            var compilerParameters = CreateCompilerParameters(options, result, cancellationToken);
            var compiler = new SyntactikCompiler(compilerParameters);
            var context = compiler.Run();
            return result;
        }

        private CompilerParameters CreateCompilerParameters(ParseOptions options, SyntactikParsedDocument result, CancellationToken cancellationToken)
        {
            var compilerParameters = new CompilerParameters { Pipeline = new CompilerPipeline() };
            compilerParameters.Pipeline.Steps.Add(new ParseAndCreateFoldingStep(result, cancellationToken));
            compilerParameters.Pipeline.Steps.Add(new ProcessAliasesAndNamespaces());
            compilerParameters.Pipeline.Steps.Add(new ValidateDocuments());
            compilerParameters.Input.Add(new StringInput(options.FileName, options.Content.Text));
            return compilerParameters;
        }

        //protected override ProjectFeatures OnGetSupportedFeatures()
        //{
        //    return ProjectFeatures.Build;
        //}

        [ProjectModelDataItem]
        public class SyntactikProjectConfiguration : ProjectConfiguration
        {
            public SyntactikProjectConfiguration(string id) : base(id)
            {
            }
        }

    }
}
