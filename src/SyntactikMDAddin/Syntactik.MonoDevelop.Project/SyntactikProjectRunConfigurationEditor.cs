using System;
using Xwt;
using MonoDevelop.Components;
using MonoDevelop.Ide.Execution;
using MonoDevelop.Projects;

namespace Syntactik.MonoDevelop.Project
{
    /// <summary>
    /// This class is needed to display project's option dialog.
    /// Extension /MonoDevelop/Ide/RunConfigurationEditors is looking for runConfigurationType="MonoDevelop.Projects.ProjectRunConfiguration".
    /// This class is registered as the handler of this configuration type.
    /// </summary>
    class SyntactikProjectRunConfigurationEditor : RunConfigurationEditor
    {
        private readonly Notebook _widget;

        public SyntactikProjectRunConfigurationEditor()
        {
            _widget = new Notebook();
        }
        public override Control CreateControl()
        {
            return new XwtControl(_widget);
        }

        public override void Load(global::MonoDevelop.Projects.Project project, SolutionItemRunConfiguration config)
        {
            
        }

        public override void Save()
        {
            
        }
    }
}
