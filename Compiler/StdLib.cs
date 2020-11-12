using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compiler {
    public static class StdLib {

        public class Clock : ILoxCallable {
            public int Arity => 0;

            public object? Call(Interpreter interpreter, IList<object?> arguments) {
                return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }

            public override string ToString() => "<native fn clock>";
        }
    }
}
