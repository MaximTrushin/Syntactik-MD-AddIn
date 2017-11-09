using System.Collections.Generic;
using MonoDevelop.Projects;

namespace Syntactik.MonoDevelop.Projects
{
    interface IProjectFilesProvider
    {
        IEnumerable<string> GetSchemaProjectFiles();
    }
}