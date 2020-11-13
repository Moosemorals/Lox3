using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Compiler;

using NUnit.Framework;


namespace Tests.Compiler {

    [TestFixture]
    public class TokeniserTests {

        [TestCaseSource(nameof(SimpleTestCases))]
        public void SimpleTests(string input, IList<Token> expected) {

            Tokeniser t = new (new OutputLogger(new StringWriter()), input);

            IList<Token> actual = t.ScanTokens();

            Assert.AreEqual(expected.Count, actual.Count);

            for (int i = 0; i < expected.Count; i += 1) {
                Assert.AreEqual(expected[i], actual[i]);
            }
        }

        private static readonly Token EOF =  new Token(TokenType.EOF, "", null, 0) ;

        public readonly static object[] SimpleTestCases =new object[] {

            new object[] { "", new List<Token>() {EOF} },
            new object[] { "a", new List<Token>() { new Token(TokenType.Identifier, "a", null, 0), EOF } },
            new object[] { "16", new List<Token>() { new Token(TokenType.Number, "16", 16d, 0), EOF } },
            new object[] { "\"a\"", new List<Token>() { new Token(TokenType.String, "\"a\"", "a", 0), EOF } },
            new object[] { "and", new List<Token>() { new Token(TokenType.And, "and", null, 0), EOF } },
            new object[] { "!", new List<Token>() { new Token(TokenType.Bang, "!", null, 0), EOF } },
            new object[] { "!=", new List<Token>() { new Token(TokenType.BangEqual, "!=", null, 0), EOF } },

            new object[] { "1 2 3", new List<Token>() {
                new Token(TokenType.Number, "1", 1d, 0),
                new Token(TokenType.Number, "2", 2d, 2),
                new Token(TokenType.Number, "3", 3d, 4),
                new Token(TokenType.EOF, "", null, 4) ,
            } },

        };
    }
}
