using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compiler {
    public class Environment {

        public Environment? Enclosing { get; }

        private readonly IDictionary<string, object?> _values = new Dictionary<string, object?>();

        public Environment() {
            Enclosing = null;
        }

        public Environment(Environment enclosing) {
            Enclosing = enclosing;
        }

        public void Assign(Token name, object? value) {
            if (_values.ContainsKey(name.Lexeme)) {
                _values[name.Lexeme] = value;
                return;
            }

            if (Enclosing != null) {
                Enclosing.Assign(name, value);
                return;
            }

            throw new Interpreter.RuntimeError(name, $"Undefined variable '{name}'.");
        }

        public void Define(string name, object? value) { 
            if (!_values.ContainsKey(name)) {
                _values.Add(name, value);
            } else {
                throw new Interpreter.RuntimeError(
                    new Token(TokenType.Identifier, name, null, 0),
                    $"Attempt to redefine variable '{name}'."
                );
            }
        }

        public void Define(Token name, object? value) {
            if (!_values.ContainsKey(name.Lexeme)) {
                _values.Add(name.Lexeme, value);
            } else {
                throw new Interpreter.RuntimeError(name, $"Attempt to redefine variable '{name.Lexeme}'.");
            }
        }

        public object? Get(Token name) {
            if (_values.ContainsKey(name.Lexeme)) {
                return _values[name.Lexeme];
            }

            if (Enclosing != null) {
                return Enclosing.Get(name);
            }

            throw new Interpreter.RuntimeError(name, $"Undefined variable '{name.Lexeme}'.");
        }
    }
}
