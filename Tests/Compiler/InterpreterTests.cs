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
            new object[] { "var a; if (true) { a= 3; } else { a= 4 ;}; print a;", 3d },
            new object[] { "var a; if (false) { a= 3 ;} else { a= 4; }; print a;", 4d },
            new object[] { "var a = 0; for (var b = 3; b < 6; b = b + 1) { a = a + b; }; print a;", 12d },
            new object[] { "while (false) {}; print 8;", 8d },
            new object[] { "print true and true;", true },
            new object[] { "print true and false;", false },
            new object[] { "print false and true;", false },
            new object[] { "print false and false;", false },
            new object[] { "print true or true;", true },
            new object[] { "print true or false;", true },
            new object[] { "print false or true;", true },
            new object[] { "print false or false;", false },

            new object[] { "fun say(word) { print \"Hello \" + word; } say(\"world\");", "Hello world" },
            new object[] { "fun say() { print \"Hello\"; } say();", "Hello" },

            new object[] { "fun seven() { return 7; } print seven();", 7d },

            new object[] { 
                @"fun MakeCounter(init) { 
                    var i = init; 
                    fun count() { 
                        i = i + 1; 
                        return i;
                    } 
                    return count;
                } 
                var counter = MakeCounter(7);
                print counter() + counter();",
             (8d + 9d) }
        };
    }
}
