using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Compiler {
    public class Interpreter : Expr.IVisitor<object?> {

        private readonly IErrorReporter _log;

        public Interpreter(IErrorReporter log) {
            _log = log;
        }

        public object? Interpret(Expr expr) {
            try {
                return Evaluate(expr);
            } catch (RuntimeError ex) {
                _log.ReportError(ex.Token, ex.Message);
            } 
            return null;
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
    }


    internal class RuntimeError : Exception {
        public Token Token { get; }

        internal RuntimeError(Token token, string message) : base(message) {
            if (token == null) {
                throw new ArgumentNullException(nameof(token));
            }
            Token = token;
        }
    }
}
