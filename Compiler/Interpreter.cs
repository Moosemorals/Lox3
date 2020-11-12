using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Compiler {
    public class Interpreter : Expr.IVisitor<object?>, Stmt.IVisitor<object?> {

        private readonly IOutputStuff _log;
        private Environment _env = new Environment();

        public Interpreter(IOutputStuff log) {
            _log = log;
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
        
        public object? VisitPrintStmt(Stmt.Print stmt) { 
            object? value = Evaluate(stmt.Expr);
            _log.PrintValue(value);
            return null;
        }

        public object? VisitVarStmt(Stmt.Var stmt) {
            object? value = null;
            if (stmt.Initilizer != null) {
                value = Evaluate(stmt.Initilizer);
            }

            _env.Define(stmt.Name, value);
            return null;
        }
        #endregion

        #region Expressions

        public object? VisitAssignExpr(Expr.Assign expr) {
            object? value = Evaluate(expr.Value);
            _env.Assign(expr.Name, value);
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

        public object? VisitGroupingExpr(Expr.Grouping expr) => Evaluate(expr.Expression);

        public object? VisitLiteralExpr(Expr.Literal expr) => expr.Value;

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
            return _env.Get(expr.Name);            
        }
        #endregion

        #region Support

        private void Execute(Stmt stmt) {
            stmt.Accept(this);
        }

        private void ExecuteBlock(IList<Stmt> statements, Environment environment) { 
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

        private static bool IsTruthy(object? obj) {
            return obj != null && (obj is not bool b || b);
        }

        private static bool IsEqual(object? a, object? b) {
            if (a == null && b == null) {
                return true;
            }
            return a == null ? true : a.Equals(b);
        }

        private object? Evaluate(Expr expr) {
            return expr.Accept(this);
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

        internal class RuntimeError : Exception {
            public Token Token { get; }

            internal RuntimeError(Token token, string message) : base(message) {
                Token = token ?? throw new ArgumentNullException(nameof(token));
            }
        }

        #endregion
    }
}
