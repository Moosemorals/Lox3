using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compiler {
    public class Environment {

        public Environment? Enclosing { get; }

        private readonly IDictionary<string, LoxValue> _values = new Dictionary<string, LoxValue>();

        public Environment() {
            Enclosing = null;
        }

        public Environment(Environment enclosing) {
            Enclosing = enclosing;
        }

        public void Assign(Token name, LoxValue value) {
            if (_values.ContainsKey(name.Lexeme)) {
                _values[name.Lexeme] = value;
                return;
            }

            if (Enclosing != null) {
                Enclosing.Assign(name, value);
                return;
            }

            throw new RuntimeError(name, $"Undefined variable '{name}'.");
        }

        public void AssignAt(int distance, Token name, LoxValue value) {
            Ancestor(distance)._values[name.Lexeme] = value;
        }

        public void Define(string name, LoxValue value) {
            if (!_values.ContainsKey(name)) {
                _values.Add(name, value);
            } else {
                throw new RuntimeError(
                    new Token(TokenType.Identifier, name, null, 0),
                    $"Attempt to redefine variable '{name}'."
                );
            }
        }

        public void Define(Token name, LoxValue value) {
            if (!_values.ContainsKey(name.Lexeme)) {
                _values.Add(name.Lexeme, value);
            } else {
                throw new RuntimeError(name, $"Attempt to redefine variable '{name.Lexeme}'.");
            }
        }

        public LoxValue Get(Token name) {
            if (_values.ContainsKey(name.Lexeme)) {
                return _values[name.Lexeme];
            }

            if (Enclosing != null) {
                return Enclosing.Get(name);
            }

            throw new RuntimeError(name, $"Undefined variable '{name.Lexeme}'.");
        }

        public LoxValue GetAt(int distance, string name) {
            return Ancestor(distance)._values[name];
        }

        private Environment Ancestor(int distance) {
            Environment? result = this;
            for (int i = 0; i < distance; i += 1) {
                if (result == null) {
                    throw new NullReferenceException("Environment has no parent");
                }
                result = result.Enclosing;
            }
            if (result == null) {
                throw new NullReferenceException("Environment has no parent");
            }
            return result;
        }
    }
}
