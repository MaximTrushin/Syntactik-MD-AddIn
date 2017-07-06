using System;
using Xwt;
using MonoDevelop.Components;
using MonoDevelop.Ide.Execution;
using MonoDevelop.Projects;

namespace Syntactik.MonoDevelop.Project
{
    class SyntactikProjectRunConfigurationEditor: RunConfigurationEditor
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
