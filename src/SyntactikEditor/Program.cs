using System;
using MonoDevelop.Ide;


namespace SyntactikEditor
{
    internal class MainClass
    {
        [STAThread]
        public static int Main(string[] args)
        {
            return IdeStartup.Main(args, null);
        }
    }


}
