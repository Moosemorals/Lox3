using System;
using System.Collections.Generic;
using System.IO;
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
            StringWriter writer = new StringWriter();

            OutputLogger log = new OutputLogger(writer);
            IList<Token> tokens = new Tokeniser(log, input).ScanTokens();

            Assert.IsFalse(log.HadError, writer.ToString());

            IList<Stmt> statements = new Parser(log, tokens).Parse();

            Assert.IsFalse(log.HadError, writer.ToString());

            Interpreter interpreter = new Interpreter(log);

            Resolver resolver = new Resolver(log, interpreter);

            resolver.Resolve(statements);
 
            Assert.IsFalse(log.HadError, writer.ToString()); 

            interpreter.Interpret(statements);

            Assert.IsFalse(log.HadError, writer.ToString()); 

            Assert.AreEqual(expected, writer.ToString());
        }
 
        public readonly static object[] InterpreterTestCases = new object[] {
            new object[] { "print 1;", "1" },
            new object[] { "print 1 + 1;", "2" },
            new object[] { "print 2 * 3;", "6" },
            new object[] { "print 15 < 12;", "False" },
            new object[] { "print 12 < 15;", "True" },
            new object?[] { "print nil;", "nil" },
            new object[] { "print 2 + 3 * 2;", "8" },
            new object[] { "print (2 + 3) * 2;", "10" },
            new object[] { "print !(2 > 3);", "True" },
            new object[] { "print \"a\" + \"b\";", "ab" },
            new object[] { "var a = 2; var b = 3; print  a + b;", "5" },
            new object[] { "var a = 2; var b = 3; a =  a + b; print a;", "5" },
            new object[] { "var a = 2; { var b = 3; a =  a + b; } print a;", "5" },
            new object[] { "var a; if (true) { a= 3; } else { a= 4 ;} print a;", "3" },
            new object[] { "var a; if (false) { a= 3 ;} else { a= 4; } print a;", "4" },
            new object[] { "var a = 0; for (var b = 3; b < 6; b = b + 1) { a = a + b; } print a;", "12" },
            new object[] { "while (false) {} print 8;", "8" },
            new object[] { "print true and true;", "True" },
            new object[] { "print true and false;", "False" },
            new object[] { "print false and true;", "False" },
            new object[] { "print false and false;", "False" },
            new object[] { "print true or true;", "True" },
            new object[] { "print true or false;", "True" },
            new object[] { "print false or true;", "True" },
            new object[] { "print false or false;", "False" },

            new object[] { "print \"hello\"[2];", "l" },

            new object[] { "fun say(word) { print \"Hello \" + word; } say(\"world\");", "Hello world" },
            new object[] { "fun say() { print \"Hello\"; } say();", "Hello" },

            new object[] { "fun seven() { return 7; } print seven();", "7" },

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
             "17" }
        };
    }
}
