using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compiler {
    public class Resolver : Expr.IVisitor<object?>, Stmt.IVisitor<object?> {

        private readonly Interpreter _interpreter;
        private readonly IOutputStuff _log;
        private readonly Stack<IDictionary<string, bool>> _scopes = new Stack<IDictionary<string, bool>>();
        private FunctionType currentFunction = FunctionType.None;

        public Resolver(IOutputStuff log, Interpreter interpreter) {
            _log = log;
            _interpreter = interpreter;
        }

        #region Statements
        public object? VisitBlockStmt(Stmt.Block stmt) {
            BeginScope();
            Resolve(stmt.Statements);
            EndScope();
            return null;
        }

        public object? VisitExpressionStmt(Stmt.Expression stmt) {
            Resolve(stmt.Expr);
            return null;
        }

        public object? VisitFunctionStmt(Stmt.Function stmt) {
            Declare(stmt.Name);
            Define(stmt.Name);

            ResolveFunction(stmt, FunctionType.Function);
            return null;
        }

        public object? VisitIfStmt(Stmt.If stmt) {
            Resolve(stmt.Condition);
            Resolve(stmt.ThenBranch);
            if (stmt.ElseBranch != null) {
                Resolve(stmt.ElseBranch);
            }
            return null;
        }

        public object? VisitPrintStmt(Stmt.Print stmt) {
            Resolve(stmt.Expr);
            return null;
        }

        public object? VisitReturnStmt(Stmt.Return stmt) {
            if (currentFunction == FunctionType.None) {
                throw new RuntimeError(stmt.Keyword, "Can't return from top level code.");
            }
            if (stmt.Value != null) {
                Resolve(stmt.Value);
            }
            return null;
        }

        public object? VisitWhileStmt(Stmt.While stmt) {
            Resolve(stmt.Condition);
            Resolve(stmt.Body);
            return null;
        }

        public object? VisitVarStmt(Stmt.Var stmt) {
            Declare(stmt.Name);

            if (stmt.Initilizer != null) {
                Resolve(stmt.Initilizer);
            }

            Define(stmt.Name);
            return null;
        }

        #endregion

        #region Expressions

        public object? VisitAssignExpr(Expr.Assign expr) {
            Resolve(expr.Value);
            ResolveLocal(expr, expr.Name);
            return null;
        }

        public object? VisitBinaryExpr(Expr.Binary expr) {
            Resolve(expr.Left);
            Resolve(expr.Right);
            return null;
        }

        public object? VisitCallExpr(Expr.Call expr) {
            Resolve(expr.Callee);

            foreach (Expr arg in expr.Arguments) {
                Resolve(arg);
            }

            return null;
        }

        public object? VisitGroupingExpr(Expr.Grouping expr) {
            Resolve(expr.Expression);
            return null;
        }

        public object? VisitLiteralExpr(Expr.Literal expr) => null;

        public object? VisitLogicalExpr(Expr.Logical expr) {
            Resolve(expr.Left);
            Resolve(expr.Right);
            return null;
        }

        public object? VisitUnaryExpr(Expr.Unary expr) {
            Resolve(expr.Right);
            return null;
        }

        public object? VisitVariableExpr(Expr.Variable expr) {
            if (!_scopes.IsEmpty()) {
                IDictionary<string, bool> scope = _scopes.Peek();
                if (scope.ContainsKey(expr.Name.Lexeme) && scope[expr.Name.Lexeme] == false) {
                    throw new RuntimeError(expr.Name, "Can't use local variable in it's own initilizer.");
                }
            }

            ResolveLocal(expr, expr.Name);
            return null;
        }

        #endregion

        #region Support

        private void BeginScope() {
            _scopes.Push(new Dictionary<string, bool>());
        }

        private void Declare(Token name) {
            if (_scopes.IsEmpty()) { return; }
            IDictionary<string, bool> scope = _scopes.Peek();
            if (scope.ContainsKey(name.Lexeme)) {
                throw new RuntimeError(name, $"Already a variable called {name.Lexeme} in this scope.");
            }
            scope.Add(name.Lexeme, false);
        }

        private void Define(Token name) {
            if (!_scopes.IsEmpty()) {
                _scopes.Peek()[name.Lexeme] = true;
            }
        }

        private void EndScope() => _scopes.Pop();

        public void Resolve(IList<Stmt> statements) {
            try {
                foreach (Stmt s in statements) {
                    Resolve(s);
                }
            } catch (RuntimeError error) {
                _log.ReportError(error.Token, error.Message);
            }
        }

        private void Resolve(Stmt statement) => statement.Accept(this);

        private void Resolve(Expr expression) => expression.Accept(this);

        private void ResolveFunction(Stmt.Function function, FunctionType type) {
            FunctionType enclosingFunction = currentFunction;
            currentFunction = type;

            BeginScope();
            foreach (Token p in function.Params) {
                Declare(p);
                Define(p);
            }
            Resolve(function.Body);
            EndScope();

            currentFunction = enclosingFunction;
        }

        private void ResolveLocal(Expr expr, Token name) {
            for (int i = _scopes.Count - 1; i >= 0; i -= 1) {
                // when i is zero, we want the most recent scope
                // which is _scopes.Count - 1. 
                if (_scopes.ElementAt(_scopes.Count - 1 - i).ContainsKey(name.Lexeme)) {
                    _interpreter.Resolve(expr, _scopes.Count - 1 - i);
                    return;
                }
            }
        }

        enum FunctionType {
            None,
            Function,
        }

        #endregion
    }
}
