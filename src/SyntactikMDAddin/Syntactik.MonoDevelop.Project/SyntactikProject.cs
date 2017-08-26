﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Mono.Debugging.Client;
using MonoDevelop.Core;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Projects;
using MonoDevelop.Debugger;
using Syntactik.Compiler;
using Syntactik.Compiler.IO;
using Syntactik.Compiler.Pipelines;
using Syntactik.Compiler.Steps;
using Syntactik.DOM;
using Syntactik.MonoDevelop.Parser;
using Syntactik.MonoDevelop.Schemas;

namespace Syntactik.MonoDevelop.Projects
{
    [ProjectModelDataItem]
    public class SyntactikProject : Project, IProjectFilesProvider
    {
        internal class ParseInfo
        {
            public ITextSourceVersion Version;
            public SyntactikParsedDocument Document;
        }

        public SchemasRepository SchemasRepository { get; private set; }
        public CompilerContext CompilerContext { get; private set; }

        readonly object _syncRoot = new object();
        internal Dictionary<string, ParseInfo> CompileInfo { get; } = new Dictionary<string, ParseInfo>();

        public SyntactikProject()
		{
            Init();
        }

        public SyntactikProject(ProjectCreateInformation info, XmlElement projectOptions): base(info, projectOptions)
        {
            Configurations.Add(CreateConfiguration("Default"));
            Init();
        }

        private void Init()
        {
            //Preventing setting of brakepoints in files of Syntactik project.
            var breakpoints = DebuggingService.Breakpoints;
            breakpoints.CheckingReadOnly += BreakpointsOnCheckingReadOnly;
            SchemasRepository = new SchemasRepository(this);
        }

        protected override void OnDispose()
        {
            var breakpoints = DebuggingService.Breakpoints;
            breakpoints.CheckingReadOnly -= BreakpointsOnCheckingReadOnly;
            base.OnDispose();
        }

        private void BreakpointsOnCheckingReadOnly(object sender, ReadOnlyCheckEventArgs readOnlyCheckEventArgs)
        {
            readOnlyCheckEventArgs.SetReadOnly(true);
        }

        internal Dictionary<string, Task<SyntactikParsedDocument>> CompiledDocuments { get; } = new Dictionary<string, Task<SyntactikParsedDocument>>();

        protected override void OnInitializeFromTemplate(ProjectCreateInformation projectCreateInfo, XmlElement template)
        {
            Configurations.Add(CreateConfiguration("Default"));
        }

        protected override SolutionItemConfiguration OnCreateConfiguration(string name, ConfigurationKind kind = ConfigurationKind.Blank)
        {
            var conf = new SyntactikProjectConfiguration(name);
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
            ParseSyntactikDocument(fileName, content, null, new CancellationToken());
        }

        internal Task<SyntactikParsedDocument> ParseSyntactikDocument(string fileName, string content,
            ITextSourceVersion version, CancellationToken cancellationToken, bool parseOnly = false)
        {
            try
            {
                ParseInfo info;
                if (version != null && CompileInfo.TryGetValue(fileName, out info))
                {
                    if (info.Version != null && version.BelongsToSameDocumentAs(info.Version) &&
                        version.CompareAge(info.Version) == 0)
                    {
                        return Task.FromResult(info.Document);
                    }
                }

                var result = new SyntactikParsedDocument(fileName, version);
                var compilerParameters = CreateParsingOnlyCompilerParameters(fileName, content, result,
                    cancellationToken);
                var compiler = new SyntactikCompiler(compilerParameters);
                var context = compiler.Run();
                result.AddErrors(
                    context.Errors.Where(error => error.LexicalInfo.Line >= 0 && error.LexicalInfo.Column >= 0));
                var module = context.CompileUnit.Modules[0];
                result.Ast = module;

                
                lock (_syncRoot)
                {
                    if (CompileInfo.TryGetValue(fileName, out info))
                    {
                        info.Document = result;
                        info.Version = version;
                    }
                    else
                        CompileInfo.Add(fileName,
                            new ParseInfo
                            {
                                Document = result,
                                Version = version
                            }
                        );
                    if (!parseOnly)
                    {
                        var modules = new PairCollection<Module>();
                        foreach (var item in CompileInfo)
                        {
                            modules.Add((Module) item.Value.Document.Ast);
                        }
                        compilerParameters = CreateValidationOnlyCompilerParameters(); //TODO: Use cancellation token
                        compiler = new SyntactikCompiler(compilerParameters);
                        context = compiler.Run(new CompileUnit {Modules = modules});
                        CleanNonParserErrors();
                        AddValidationError(context.Errors);
                        CompilerContext = context;
                    }
                }
                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Unhandled exception in SyntactikProject.ParseSyntactikDocument.", ex);
                throw;
            }
        }

        private void AddValidationError(SortedSet<CompilerError> errors)
        {
            foreach (var error in errors)
            {
                ParseInfo info;
                if (CompileInfo.TryGetValue(error.LexicalInfo.FileName, out info))
                {
                    info.Document.Add(error);
                }
            }
        }

        private void CleanNonParserErrors()
        {
            foreach (var info in CompileInfo)
            {
                var errors = new List<Error>();
                foreach (var error in info.Value.Document.Errors)
                    if (IsParserError(error.Id)) errors.Add(error);

                info.Value.Document.Errors = errors;
            }
        }

        private bool IsParserError(string errorId)
        {
            return errorId == "MCE0007" || errorId == "MCE0032" || errorId == "MCE0100" || errorId == "MCE0100" //TODO: Add property IsParserError to the error class.
                    || errorId == "MCE0101" || errorId == "MCE0102";
        }

        private CompilerParameters CreateParsingOnlyCompilerParameters(string fileName, string content, SyntactikParsedDocument result, CancellationToken cancellationToken)
        {
            var compilerParameters = new CompilerParameters { Pipeline = new CompilerPipeline() };
            compilerParameters.Pipeline.Steps.Add(new ParseAndCreateFoldingStep(result, cancellationToken));
            compilerParameters.Input.Add(new StringInput(fileName, content));
            return compilerParameters;
        }

        private CompilerParameters CreateValidationOnlyCompilerParameters()
        {
            var compilerParameters = new CompilerParameters { Pipeline = new CompilerPipeline() };
            compilerParameters.Pipeline.Steps.Add(new ProcessAliasesAndNamespaces());
            compilerParameters.Pipeline.Steps.Add(new ValidateDocuments());
            return compilerParameters;
        }

        public Dictionary<string, AliasDefinition> GetAliasDefinitionList()
        {
            var aliasDefs = new Dictionary<string, AliasDefinition>();
            lock (_syncRoot)
            {
                foreach (var item in CompileInfo)
                {
                    var module = item.Value.Document.Ast as Module;
                    if (module == null) continue;
                    foreach (var moduleMember in module.Members)
                    {
                        var aliasDef = moduleMember as AliasDefinition;
                        if (aliasDef != null && !aliasDefs.ContainsKey(aliasDef.Name))
                            aliasDefs.Add(aliasDef.Name, aliasDef);
                    }
                }
            }
            return aliasDefs;
        }

        protected override void OnFileRemovedFromProject(ProjectFileEventArgs e)
        {
            base.OnFileRemovedFromProject(e);
            foreach (var file in e)
            {
                lock (_syncRoot)
                {
                    CompileInfo.Remove(file.ProjectFile.FilePath);
                }
            }
        }

        protected override void OnFileRenamedInProject(ProjectFileRenamedEventArgs e)
        {
            base.OnFileRenamedInProject(e);
            foreach (var file in e)
            {
                lock (_syncRoot)
                {
                    CompileInfo.Remove(file.OldName);
                }
            }
        }

        protected override Task<BuildResult> DoBuild(ProgressMonitor monitor, ConfigurationSelector configuration)
        {
            var projectConfig = (SyntactikProjectConfiguration)this.GetConfiguration(configuration);

            try
            {
                if (Directory.Exists(projectConfig.XMLOutputFolder))
                {
                    Directory.Delete(projectConfig.XMLOutputFolder, true);
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Unhandled exception in SyntactikProject.DoBuild().", ex);
            }


            var compilerParameters = CreateCompilerParameters(projectConfig.XMLOutputFolder, GetProjectFiles(this));
            var compiler = new SyntactikCompiler(compilerParameters);
            var context = compiler.Run();
            return Task.FromResult(GetBuildResult(context));
        }

        private BuildResult GetBuildResult(CompilerContext context)
        {
            var result = new BuildResult();
            foreach (var error in context.Errors)
            {
                result.AddError(error.LexicalInfo.FileName, error.LexicalInfo.Line, error.LexicalInfo.Column, error.Code, error.Message);
            }
            return result;
        }

        private static IEnumerable<string> GetProjectFiles(SyntactikProject project)
        {
            var sources = project.Items.GetAll<ProjectFile>()
                .Where(i => i.Subtype != Subtype.Directory && (i.FilePath.Extension.ToLower() == ".s4x" || i.FilePath.Extension.ToLower() == ".xsd"))
                .Select(i => i.FilePath.FullPath.ToString());
            return sources;
        }
        public IEnumerable<string> GetSchemaProjectFiles()
        {
            var services = Items.OfType<ProjectFile>()
                .Where(i => i.ProjectVirtualPath.ParentDirectory.FileNameWithoutExtension.ToLower() == "schemas" &&
                i.ProjectVirtualPath.ParentDirectory.ParentDirectory.FileNameWithoutExtension == "" &&
                i.FilePath.Extension == ".xsd").Select(i => i.FilePath.ToString());
            return services;
        }

        private static CompilerParameters CreateCompilerParameters(string outputDirectory, IEnumerable<string> files)
        {
            var compilerParameters = new CompilerParameters
            {
                Pipeline = new CompileToFiles(),
                OutputDirectory = outputDirectory
            };
            foreach (var fileName in files)
            {
                if (fileName.EndsWith(".s4x"))
                {
                    compilerParameters.Input.Add(new FileInput(fileName));
                    continue;
                }
                if (fileName.EndsWith(".xsd")) compilerParameters.XmlSchemaSet.Add(null, fileName);
            }

            return compilerParameters;
        }
    }
}
