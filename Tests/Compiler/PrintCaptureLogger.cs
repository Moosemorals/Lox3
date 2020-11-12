using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Compiler;


namespace Tests.Compiler {
    internal class PrintCaptureLogger : IOutputStuff {
        public object? Result {get;set; }

        public void ReportError(string message) {
            // Does nothing
        }

        public void ReportError(Token token, string message) {
            // Does nothing
        }

        public void PrintValue(object? value) {
            Result = value;
        }
    }
}
