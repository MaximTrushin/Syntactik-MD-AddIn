using System.Threading;
using NUnit.Framework;
using Syntactik.MonoDevelop.Parser;
using Syntactik.Compiler;
using Syntactik.Compiler.IO;
using static SyntactikMDAddin.Tests.TestUtils;

namespace SyntactikMDAddin.Tests
{
    [TestFixture]
    public class CompletionTests
    {
        [Test, RecordedTest]
        public void Alias1()
        {
            DoCompletionTest();
        }
    }
}
