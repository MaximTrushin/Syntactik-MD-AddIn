using Syntactik.DOM;

namespace Syntactik.MonoDevelop.Projects
{
    class CompileUnit: DOM.CompileUnit
    {
        private PairCollection<Module> _modules;
        public override PairCollection<Module> Modules
        {
            get { return _modules ?? (_modules = new PairCollection<Module>(this)); }
            set
            {
                //Needed to override this setter to remove InitializeParent.
                _modules = value;
            }
        }
    }
}
