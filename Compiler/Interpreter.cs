using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;


namespace Compiler {
    public class Interpreter : Expr.IVisitor<object?>, Stmt.IVisitor<object?> {

        private readonly IOutputStuff _log;
        private readonly IDictionary<Expr, int> _locals = new Dictionary<Expr, int>();
        public readonly Environment globals;
        private Environment _env;

        public Interpreter(IOutputStuff log) {
            globals = new Environment();
            _env = globals;
            _log = log;

            globals.Define("clock", new StdLib.Clock());
        }

        public void Interpret(IList<Stmt> statements) {
            try {
                foreach (Stmt stmt in statements) {
                    Execute(stmt);
                }
            } catch (RuntimeError ex) {
                _log.ReportError(ex.Token, ex.Message);
            }
        }

        #region Statements

        public object? VisitBlockStmt(Stmt.Block stmt) {
            ExecuteBlock(stmt.Statements, new Environment(_env));
            return null;
        }

        public object? VisitExpressionStmt(Stmt.Expression stmt) => Evaluate(stmt.Expr);

        public object? VisitFunctionStmt(Stmt.Function stmt) {
            LoxFunction function = new LoxFunction(stmt, _env);
            _env.Define(stmt.Name.Lexeme, function);
            return null;
        }

        public object? VisitIfStmt(Stmt.If stmt) {
            if (IsTruthy(Evaluate(stmt.Condition))) {
                Execute(stmt.ThenBranch);
            } else if (stmt.ElseBranch != null) {
                Execute(stmt.ElseBranch);
            }

            return null;
        }

        public object? VisitPrintStmt(Stmt.Print stmt) {
            object? value = Evaluate(stmt.Expr);
            _log.PrintValue(value);
            return null;
        }

        public object? VisitReturnStmt(Stmt.Return stmt) {
            object? value = null;
            if (stmt.Value != null) {
                value = Evaluate(stmt.Value);
            }

            throw new Return(value);
        }

        public object? VisitVarStmt(Stmt.Var stmt) {
            object? value = null;
            if (stmt.Initilizer != null) {
                value = Evaluate(stmt.Initilizer);
            }

            _env.Define(stmt.Name, value);
            return null;
        }

        public object? VisitWhileStmt(Stmt.While stmt) {
            while (IsTruthy(Evaluate(stmt.Condition))) {
                Execute(stmt.Body);
            }
            return null;
        }

        #endregion

        #region Expressions

        public object? VisitAssignExpr(Expr.Assign expr) {
            object? value = Evaluate(expr.Value);

            if (_locals.ContainsKey(expr)) {
                _env.AssignAt(_locals[expr], expr.Name, value);
            } else {
                globals.Assign(expr.Name, value);
            }


            return value;
        }

        public object? VisitBinaryExpr(Expr.Binary expr) {
            object? left = Evaluate(expr.Left);
            object? right = Evaluate(expr.Right);

            switch (expr.Op.Type) {
            case TokenType.BangEqual:
                return !IsEqual(left, right);
            case TokenType.EqualEqual:
                return !IsEqual(left, right);
            case TokenType.Greater:
#pragma warning disable CS8605 // Unboxing a possibly null value.
                CheckNumberOperands(expr.Op, left, right);
                return (double)left > (double)right;
            case TokenType.GreaterEqual:
                CheckNumberOperands(expr.Op, left, right);
                return (double)left >= (double)right;
            case TokenType.Less:
                CheckNumberOperands(expr.Op, left, right);
                return (double)left < (double)right;
            case TokenType.LessEqual:
                CheckNumberOperands(expr.Op, left, right);
                return (double)left <= (double)right;
            case TokenType.Minus:
                CheckNumberOperands(expr.Op, left, right);
                return (double)left - (double)right;
            case TokenType.Slash:
                CheckNumberOperands(expr.Op, left, right);
                return (double)left / (double)right;
            case TokenType.Star:
                CheckNumberOperands(expr.Op, left, right);
                return (double)left * (double)right;
#pragma warning restore CS8605 // Unboxing a possibly null value.
            case TokenType.Plus:
                if (left is double && right is double) {
                    return (double)left + (double)right;
                }
                if (left is string && right is string) {
                    return (string)left + (string)right;
                }
                throw new RuntimeError(expr.Op, "Operands must both be numbers or strings.");
            }
            return null;
        }

        public object? VisitCallExpr(Expr.Call expr) {
            object? callee = Evaluate(expr.Callee);

            IList<object?> arguments = expr.Arguments
                .Select(e => Evaluate(e))
                .ToList();

            if (callee is ILoxCallable function) {
                if (arguments.Count != function.Arity) {
                    throw new RuntimeError(expr.Paren, $"Expected {function.Arity} argument(s) but got {arguments.Count} instead.");
                }
                return function.Call(this, arguments);
            }
            throw new RuntimeError(expr.Paren, "Can only call functions and classes");
        }

        public object? VisitGroupingExpr(Expr.Grouping expr) => Evaluate(expr.Expression);

        public object? VisitLiteralExpr(Expr.Literal expr) => expr.Value;

        public object? VisitLogicalExpr(Expr.Logical expr) {
            object? left = Evaluate(expr.Left);

            if (expr.Op.Type == TokenType.Or) {
                if (IsTruthy(left)) {
                    return left;
                }
            } else {
                if (!IsTruthy(left)) {
                    return left;
                }
            }
            return Evaluate(expr.Right);
        }

        public object? VisitUnaryExpr(Expr.Unary expr) {
            object? right = Evaluate(expr.Right);

            switch (expr.Op.Type) {
            case TokenType.Bang:
                return !IsTruthy(right);
            case TokenType.Minus:
                CheckNumberOperand(expr.Op, right);
#pragma warning disable CS8605 // Unboxing a possibly null value.
                return -(double)right;
#pragma warning restore CS8605 // Unboxing a possibly null value.
            }

            return null;
        }

        public object? VisitVariableExpr(Expr.Variable expr) {
            return LookupVariable(expr.Name, expr);
        }
        #endregion

        #region Support

        private void Execute(Stmt stmt) {
            stmt.Accept(this);
        }

        public void ExecuteBlock(IList<Stmt> statements, Environment environment) {
            Environment previous = _env;
            try {
                _env = environment;

                foreach (Stmt s in statements) {
                    Execute(s);
                }

            } finally {
                _env = previous;
            }
        }

        private object? Evaluate(Expr expr) {
            return expr.Accept(this);
        }

        private object? LookupVariable(Token name, Expr expr) {
            if (_locals.ContainsKey(expr)) {
                return _env.GetAt(_locals[expr], name.Lexeme);
            }
            return globals.Get(name);
        }

        internal void Resolve(Expr expr, int depth) {
            _locals.Add(expr, depth);
        }

        private static bool IsTruthy(object? obj) {
            return obj != null && (obj is not bool b || b);
        }

        private static bool IsEqual(object? a, object? b) {
            if (a == null && b == null) {
                return true;
            }
            return a == null ? true : a.Equals(b);
        }

        private static void CheckNumberOperand(Token op, object? obj) {
            if (obj is double) {
                return;
            }
            throw new RuntimeError(op, "Operand must be a number.");
        }

        private static void CheckNumberOperands(Token op, object? left, object? right) {
            if (left is double && right is double) {
                return;
            }
            throw new RuntimeError(op, "Operands must be numbers.");
        }

        #endregion
    }

    internal class RuntimeError : Exception {
        public Token Token { get; }

        internal RuntimeError(Token token, string message) : base(message) {
            Token = token ?? throw new ArgumentNullException(nameof(token));
        }
    }


    public class Return : Exception {

        public object? Value { get; }

        public Return(object? value) : base() {
            Value = value;
        }

    }
}
