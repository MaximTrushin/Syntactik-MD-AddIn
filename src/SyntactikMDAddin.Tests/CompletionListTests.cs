﻿using NUnit.Framework;
using static SyntactikMDAddin.Tests.TestUtils;

namespace SyntactikMDAddin.Tests
{
    [TestFixture]
    public class CompletionListTests
    {
        [Test, RecordedTest]
        public void Alias1()
        {
            DoCompletionListTest();
        }
    }
}