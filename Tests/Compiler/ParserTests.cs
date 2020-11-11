using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Compiler;

using NUnit.Framework;

namespace Tests.Compiler {

    [TestFixture]
    public class ParserTests {

        [TestCaseSource(nameof(ParserTestCases))]
        public void ParserTest(string input, Expr expected) {

            IErrorReporter log = new NullLogger();

            IList<Token> tokens =new Tokeniser(log, input).ScanTokens();

            Expr? actual = new Parser(log, tokens).Parse();

            Assert.IsNotNull(actual);

            if (actual != null) {
                Assert.AreEqual(expected.GetType(), actual.GetType());
            }
        }

        public readonly static object[] ParserTestCases = new object[] {
            new object[] { "\"a\"", new Expr.Literal("a") },

        };
    }
}
