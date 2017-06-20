using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Internal;
using Syntactik.MonoDevelop.Parser;
using Syntactik.Compiler;
using Syntactik.Compiler.IO;

namespace SyntactikMDAddin.Tests
{
    [TestFixture()]
    public class SyntactikCompletionTests
    {
        [Test]
        public void Test1()
        {
            var code = "el1:";
            var compilerParameters = CreateCompilerParameters("test1.s4x", code);
            var compiler = new SyntactikCompiler(compilerParameters);
            var context = compiler.Run();

        }

        private CompilerParameters CreateCompilerParameters(string fileName, string content, CancellationToken cancellationToken = new CancellationToken())
        {
            var compilerParameters = new CompilerParameters { Pipeline = new CompilerPipeline() };
            compilerParameters.Pipeline.Steps.Add(new ParseForCompletionStep(cancellationToken));
            compilerParameters.Input.Add(new StringInput(fileName, content));
            return compilerParameters;
        }
    }
}
