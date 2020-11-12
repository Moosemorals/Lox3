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

            PrintCaptureLogger log = new PrintCaptureLogger();

            IList<Token> tokens = new Tokeniser(log, input).ScanTokens();

            IList<Stmt> expressions = new Parser(log, tokens).Parse();

             new Interpreter(log).Interpret(expressions);

            Assert.AreEqual(expected, log.Result);
        }


        public readonly static object[] InterpreterTestCases = new object[] {
            new object[] { "print 1;", 1d },
            new object[] { "print 1 + 1;", 2d },
            new object[] { "print 2 * 3;", 6d },
            new object[] { "print 15 < 12;", false },
            new object[] { "print 12 < 15;", true },
            new object?[] { "print nil;", null },
            new object[] { "print 2 + 3 * 2;", 8d },
            new object[] { "print (2 + 3) * 2;", 10d },
            new object[] { "print !(2 > 3);", true },
            new object[] { "print \"a\" + \"b\";", "ab" },
            new object?[] { "print \"a\" + 2;", null },
            new object[] { "var a = 2; var b = 3; print  a + b;", 5d },
            new object[] { "var a = 2; var b = 3; a =  a + b; print a;", 5d },
            new object[] { "var a = 2; { var b = 3; a =  a + b; } print a;", 5d },

        };
    }
}
