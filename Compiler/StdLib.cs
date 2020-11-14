using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compiler {
    public static class StdLib {

        public class Clock : ILoxCallable {
            public int Arity => 0;

            public LoxValue Call(Interpreter interpreter, IList<LoxValue> arguments) {
                return LoxValue.New(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            }

            public override string ToString() => "<native fn clock>";
        }
    }
}
