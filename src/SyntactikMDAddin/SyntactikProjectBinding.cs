using System;
using System.Collections.Generic;
using System.Xml;
using MonoDevelop.Core;
using MonoDevelop.Projects;
namespace Syntactik.MonoDevelop
{
    [ProjectModelDataItem/*, ExportProjectType("{2857B73E-F847-4B02-9238-064979017E34}", Extension = "synprj", Alias = "S4X")*/]
    public class SyntactikProjectBinding : Project
    {

        public SyntactikProjectBinding()
		{
        }

        public SyntactikProjectBinding(ProjectCreateInformation info, XmlElement projectOptions)
        {
            Configurations.Add(CreateConfiguration("Default"));
        }

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
