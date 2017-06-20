using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoDevelop.Ide.CodeCompletion;

namespace Syntactik.MonoDevelop.Completion
{
    public class MutableDataList : CompletionDataList, IMutableCompletionDataList
    {
        public MutableDataList(ICompletionKeyHandler completionKeyHandler)
        {
            this.AddKeyHandler(completionKeyHandler);
        }

        public void Dispose()
        {
        }

        public void BeginChange()
        {
            IsChanging = true;
            Changing?.Invoke(this, new EventArgs());
        }

        public void EndChange()
        {
            IsChanging = false;
            Changed?.Invoke(this, new EventArgs());
        }

        public bool IsChanging { get; private set; }
        public event EventHandler Changing;
        public event EventHandler Changed;
    }
}
