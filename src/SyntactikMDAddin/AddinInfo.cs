using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly: Addin(
    "Syntactik MonoDevelop Addin",
    Namespace = "Syntactik.Monodevelop",
    Version = "0.1"
)]

[assembly: AddinName("Syntactik MonoDevelop Addin")]
[assembly: AddinCategory("IDE extensions")]
[assembly: AddinDescription("Syntactik MonoDevelop Addin")]
[assembly: AddinAuthor("Maxim Trushin")]

[assembly: AddinDependency("::MonoDevelop.Core", MonoDevelop.BuildInfo.Version)]
[assembly: AddinDependency("::MonoDevelop.Ide", MonoDevelop.BuildInfo.Version)]
[assembly: AddinDependency("::MonoDevelop.SourceEditor2", MonoDevelop.BuildInfo.Version)]