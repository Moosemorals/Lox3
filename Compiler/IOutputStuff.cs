using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Compiler {
    public interface IOutputStuff {
        public void ReportError(string message);
        public void ReportError(Token token, string message);
        public void PrintValue(LoxValue value);
    }
}
