using Syntactik.DOM;
using Syntactik.DOM.Mapped;

namespace Syntactik.MonoDevelop.Completion.DOM
{
    static class DomHelper
    {
        public static CharLocation GetPairEnd(Interval nameInterval, Interval assignmentInterval)
        {
            if (assignmentInterval != null) return assignmentInterval.End;
            return nameInterval.End;
        }

        public static CharLocation GetPairEnd(IMappedPair pair)
        {
            if (pair.ValueInterval != null) return pair.ValueInterval.End;
            if (pair.AssignmentInterval != null) return pair.AssignmentInterval.End;
            return pair.NameInterval.End;
        }
    }
}
