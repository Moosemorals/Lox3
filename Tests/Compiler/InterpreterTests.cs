using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Compiler;

using NUnit.Framework;

namespace Tests.Compiler {


    [TestFixture]
    public class InterpreterTests {

        [TestCaseSource(nameof(InterpreterTestCases))]
        public void InterpreterTest(string input, object? expected) {

            IErrorReporter log = new NullLogger();

            IList<Token> tokens = new Tokeniser(log, input).ScanTokens();

            Expr? expression = new Parser(log, tokens).Parse();

            if (expression == null) {
                Assert.IsNotNull(expression);
                return;
            }

            object? actual = new Interpreter(log).Interpret(expression);

            Assert.AreEqual(expected, actual);
        }


        public readonly static object[] InterpreterTestCases = new object[] {
            new object[] { "1", 1d },
            new object[] { "1 + 1", 2d },
            new object[] { "2 * 3", 6d },
            new object[] { "15 < 12", false },
            new object[] { "12 < 15", true },
            new object?[] { "nil", null },
            new object[] { "2 + 3 * 2", 8d },
            new object[] { "(2 + 3) * 2", 10d },
            new object[] { "!(2 > 3)", true },
            new object[] { "\"a\" + \"b\"", "ab" },
            new object?[] { "\"a\" + 2", null },

        };
    }
}
