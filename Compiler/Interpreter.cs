using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;


namespace Compiler {
    public class Interpreter : Expr.IVisitor<LoxValue>, Stmt.IVisitor<object?> {

        private readonly IOutputStuff _log;
        private readonly IDictionary<Expr, int> _locals = new Dictionary<Expr, int>();
        public readonly Environment globals;
        private Environment _env;

        public Interpreter(IOutputStuff log) {
            globals = new Environment();
            _env = globals;
            _log = log;

            globals.Define("clock", LoxValue.New(new StdLib.Clock()));
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
            LoxValue function = LoxValue.New( new LoxFunction(stmt, _env));
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
            LoxValue value = Evaluate(stmt.Expr);
            _log.PrintValue(value);
            return null;
        }

        public object? VisitReturnStmt(Stmt.Return stmt) {
            LoxValue value = LoxValue.Nil;
            if (stmt.Value != null) {
                value = Evaluate(stmt.Value);
            }

            throw new Return(value);
        }

        public object? VisitVarStmt(Stmt.Var stmt) {
            LoxValue value = LoxValue.Nil;
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

        public LoxValue VisitAssignExpr(Expr.Assign expr) {
            LoxValue value = Evaluate(expr.Value);

            if (_locals.ContainsKey(expr)) {
                _env.AssignAt(_locals[expr], expr.Name, value);
            } else {
                globals.Assign(expr.Name, value);
            }

            return value;
        }

        public LoxValue VisitBinaryExpr(Expr.Binary expr) {
            LoxValue left = Evaluate(expr.Left);
            LoxValue right = Evaluate(expr.Right);

            switch (expr.Op.Type) {
            case TokenType.BangEqual:
                return LoxValue.New(!IsEqual(left, right));
            case TokenType.EqualEqual:
                return LoxValue.New(IsEqual(left, right));
            case TokenType.Greater:
                CheckNumberOperands(expr.Op, left, right);
                return (LoxValue.Number)left > (LoxValue.Number)right;
            case TokenType.GreaterEqual:
                CheckNumberOperands(expr.Op, left, right);
                return (LoxValue.Number)left >= (LoxValue.Number)right;
            case TokenType.Less:
                CheckNumberOperands(expr.Op, left, right);
                return (LoxValue.Number)left < (LoxValue.Number)right;
            case TokenType.LessEqual:
                CheckNumberOperands(expr.Op, left, right);
                return (LoxValue.Number)left <= (LoxValue.Number)right;
            case TokenType.Minus:
                CheckNumberOperands(expr.Op, left, right);
                return (LoxValue.Number)left - (LoxValue.Number)right;
            case TokenType.Slash:
                CheckNumberOperands(expr.Op, left, right);
                return (LoxValue.Number)left / (LoxValue.Number)right;
            case TokenType.Star:
                CheckNumberOperands(expr.Op, left, right);
                return (LoxValue.Number)left * (LoxValue.Number)right;
            case TokenType.Plus:
                if (left is LoxValue.Number && right is LoxValue.Number) {
                    return (LoxValue.Number)left + (LoxValue.Number)right;
                }
                if (left is LoxValue.String && right is LoxValue.String) {
                    return (LoxValue.String)left + (LoxValue.String)right;
                }
                throw new RuntimeError(expr.Op, "Operands must both be numbers or strings.");
            }
            return LoxValue.Nil;
        }

        public LoxValue VisitCallExpr(Expr.Call expr) {
            LoxValue callee = Evaluate(expr.Callee);

            IList<LoxValue> arguments = expr.Arguments
                .Select(e => Evaluate(e))
                .ToList();

            if (callee is LoxValue.Function f) {
                ILoxCallable function = f.Value;
                if (arguments.Count != function.Arity) {
                    throw new RuntimeError(expr.Paren, $"Expected {function.Arity} argument(s) but got {arguments.Count} instead.");
                }
                return function.Call(this, arguments);
            }
            throw new RuntimeError(expr.Paren, "Can only call functions and classes");
        }

        public LoxValue VisitGroupingExpr(Expr.Grouping expr) => Evaluate(expr.Expression);

        public LoxValue VisitIndexExpr(Expr.Index expr) {
            LoxValue target = Evaluate(expr.Target);
            LoxValue index = Evaluate(expr.I);

            if (target is ILoxIndexable t && index is LoxValue.Number i) {
                if (i.Value >= 0 && i.Value < t.Length) {
                    return t.GetAt(i.Value);
                }
            }
            throw new RuntimeError(expr.Paren, "Invalid target for index expression.");
        }

        public LoxValue VisitLiteralExpr(Expr.Literal expr) => expr.Value;

        public LoxValue VisitLogicalExpr(Expr.Logical expr) {
            LoxValue left = Evaluate(expr.Left);

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

        public LoxValue VisitUnaryExpr(Expr.Unary expr) {
            LoxValue right = Evaluate(expr.Right);

            switch (expr.Op.Type) {
            case TokenType.Bang:
                return LoxValue.New(!IsTruthy(right));
            case TokenType.Minus:
                CheckNumberOperand(expr.Op, right);
                return LoxValue.New(-1 * ((LoxValue.Number)right).Value);
            }

            return LoxValue.Nil;
        }

        public LoxValue VisitVariableExpr(Expr.Variable expr) {
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

        private LoxValue Evaluate(Expr expr) {
            return expr.Accept(this);
        }

        private LoxValue LookupVariable(Token name, Expr expr) {
            if (_locals.ContainsKey(expr)) {
                return _env.GetAt(_locals[expr], name.Lexeme);
            }
            return globals.Get(name);
        }

        internal void Resolve(Expr expr, int depth) {
            _locals.Add(expr, depth);
        }

        private static bool IsTruthy(LoxValue obj) {
            if (obj == LoxValue.Nil) {
                return false;
            } else if (obj is LoxValue.Bool) {
                return obj == LoxValue.True;
            } else {
                return true;
            }
        }

        private static bool IsEqual(LoxValue a, LoxValue b) {
            if (a == LoxValue.Nil && b == LoxValue.Nil) {
                return true;
            } else if (a == LoxValue.Nil) {
                return false;
            }
            return a == b;
        }

        private static void CheckNumberOperand(Token op, object? obj) {
            if (obj is LoxValue.Number) {
                return;
            }
            throw new RuntimeError(op, "Operand must be a number.");
        }

        private static void CheckNumberOperands(Token op, object? left, object? right) {
            if (left is LoxValue.Number && right is LoxValue.Number) {
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

        public LoxValue Value { get; }

        public Return(LoxValue value) : base() {
            Value = value;
        }

    }
}
