using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyCS = Nivra.DotNetAnalyzers.Test.CSharpCodeFixVerifier<
    Nivra.DotNetAnalyzers.NivraDotNetAnalyzersAnalyzer,
    Nivra.DotNetAnalyzers.NivraDotNetAnalyzersCodeFixProvider>;

namespace Nivra.DotNetAnalyzers.Test
{
    [TestClass]
    public class NivraDotNetAnalyzersUnitTest
    {
        // No diagnostics expected to show up
        [TestMethod]
        public async Task TestMethod1()
        {
            var test = @"          ";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task NoDiagnosticForProperlyFormattedParameters()
        {
            var test = @"
    using System;

    class TestClass
    {
        void TestMethod(
            int param1,
            string param2)
        {
        }

        void AnotherMethod()
        {
            TestMethod(
                1,
                ""test"");
        }
    }";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task DiagnosticAndFixForImproperlyFormattedParameters()
        {
            var test = @"
    using System;

    class TestClass
    {
        void TestMethod(int param1, string param2) { }

        void AnotherMethod()
        {
            TestMethod(0, ""test"");
        }
    }";

            var fixtest = @"
    using System;

    class TestClass
    {
        void TestMethod(
            int param1,
            string param2) { }

        void AnotherMethod()
        {
            TestMethod(
                0,
                ""test"");
        }
    }";

            var expected = new DiagnosticResult[]
            {
                VerifyCS.Diagnostic("NivraDotNetAnalyzers")
                    .WithMessage("Parameters should be on separate lines")
                    .WithSpan(10, 24, 10, 33),
                VerifyCS.Diagnostic("NivraDotNetAnalyzers")
                    .WithMessage("Parameters should be on separate lines")
                    .WithSpan(6, 25, 6, 50),
            };

            //await VerifyCS.VerifyAnalyzerAsync(test, expected);
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }
    }
}
