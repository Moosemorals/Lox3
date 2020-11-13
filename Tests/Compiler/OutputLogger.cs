using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Compiler;


namespace Tests.Compiler {
    internal class OutputLogger : IOutputStuff {

        private readonly TextWriter _writer;

        public OutputLogger(TextWriter writer) {
            _writer = writer;
        }

        public bool HadError { get; private set; }

        public void ReportError(string message) {
            HadError = true;
            _writer.WriteLine("Error {0}", message);
        }

        public void ReportError(Token token, string message) {
            _writer.WriteLine("Error at '{0}' (char {1}): {2}", token.Lexeme, token.Offset, message);
        }

        public void PrintValue(object? value) {
            if (value is null) {
                _writer.Write("nil");
            }
            _writer.Write(value);
        }
    }
}
