using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compiler {
    public interface ILoxCallable {
        public int Arity {get; }
        public LoxValue Call(Interpreter interpreter, IList<LoxValue> arguments);
    }
}
