using System;
using Dynamo.Utilities;
using NUnit.Framework;

namespace Dynamo.Tests
{
    [SetUpFixture]
    public class UITestSetup
    {
        [SetUp]
        public void RunBeforeAllTests()
        {
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyHelper.ResolveAssemblyDynamically;
        }

        [TearDown]
        public void RunAfterAllTests()
        {
        }
    }
}
