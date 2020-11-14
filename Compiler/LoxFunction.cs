using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compiler {
    public class LoxFunction : ILoxCallable {
        
        private readonly Stmt.Function _declaration;
        private readonly Environment _closure;

        public LoxFunction(Stmt.Function declaration, Environment closure) {
            _closure = closure;
            _declaration = declaration;
        }

        public int Arity => _declaration.Params.Count;

        public LoxValue Call(Interpreter interpreter, IList<LoxValue> arguments) {
            Environment environemnt = new Environment(_closure);

            for (int i =0; i < _declaration.Params.Count; i += 1) {
                environemnt.Define(_declaration.Params[i].Lexeme, arguments[i]);
            }

            try {
                interpreter.ExecuteBlock(_declaration.Body, environemnt);
            } catch (Return returnValue) {
                return returnValue.Value;
            }

            return LoxValue.Nil;
        }

        public override string ToString() => $"<fn {_declaration.Name.Lexeme}>";
    }
}
