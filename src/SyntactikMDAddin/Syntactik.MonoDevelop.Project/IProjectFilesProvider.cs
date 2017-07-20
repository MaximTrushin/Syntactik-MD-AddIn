using System.Collections.Generic;
using MonoDevelop.Projects;

namespace Syntactik.MonoDevelop.Projects
{
    public interface IProjectFilesProvider
    {
        IEnumerable<string> GetSchemaProjectFiles();
    }
}