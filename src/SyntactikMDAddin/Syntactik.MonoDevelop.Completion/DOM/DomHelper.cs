using Syntactik.DOM;
using Syntactik.DOM.Mapped;

namespace Syntactik.MonoDevelop.Completion.DOM
{
    public static class DomHelper
    {
        public static CharLocation GetPairEnd(Interval nameInterval, Interval delimiterInterval)
        {
            if (delimiterInterval != null) return delimiterInterval.End;
            return nameInterval.End;
        }

        public static CharLocation GetPairEnd(IMappedPair pair)
        {
            if (pair.ValueInterval != null) return pair.ValueInterval.End;
            if (pair.DelimiterInterval != null) return pair.DelimiterInterval.End;
            return pair.NameInterval.End;
        }
    }
}
