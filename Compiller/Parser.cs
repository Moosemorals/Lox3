using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;


namespace Compiler {
    public class Parser {

        private readonly IErrorReporter _log;
        private readonly IList<Token> _tokens;
        private int _current;

        public Parser(IErrorReporter log, IList<Token> tokens) {
            _current = 0;
            _log = log;
            _tokens = tokens;
        }

        public Expr? Parse() {
            try {
                return Expression();
            } catch (ParseError) {
                return null;
            }
        }


        #region Parser
        private Expr Expression() {
            return Equality();
        }

        private Expr Equality() { return GenericBool(Comparison, TokenType.Equal); }

        private Expr Comparison() { return GenericBool(Term, TokenType.Greater, TokenType.GreaterEqual, TokenType.Less, TokenType.LessEqual); }

        private Expr Term() { return GenericBool(Factor, TokenType.Minus, TokenType.Plus); }

        private Expr Factor() { return GenericBool(Unary, TokenType.Slash, TokenType.Star); }

        private Expr GenericBool(Func<Expr> fn, params TokenType[] types) {
            Expr expr = fn();

            while (Match(types)) {
                Token op = Previous();
                Expr right = fn();
                expr = new Expr.Binary(expr, op, right);
            }

            return expr;


        }

        private Expr Unary() {
            if (Match(TokenType.Bang, TokenType.Minus)) {
                Token op = Previous();
                Expr right = Unary();
                return new Expr.Unary(op, right);
            }
            return Primary();
        }

        private Expr Primary() {
            if (Match(TokenType.False)) { return new Expr.Literal(false); }
            if (Match(TokenType.True)) { return new Expr.Literal(true); }
            if (Match(TokenType.Nil)) { return new Expr.Literal(null); }

            if (Match(TokenType.Number, TokenType.String)) {
                return new Expr.Literal(Previous().Literal);
            }

            if (Match(TokenType.LeftParen)) {
                Expr expr = Expression();
                Consume(TokenType.RightParen, "Expected ')' after expression.");
                return new Expr.Grouping(expr);
            }

            throw Error(Peek(), "Expected expression.");
        }

        #endregion

        #region Support

        private Token Advance() {
            if (!IsAtEnd()) {
                _current += 1;
            }

            return Previous();
        }

        private bool Check(TokenType type) => !IsAtEnd() && Peek().Type == type;

        private Token Consume(TokenType type, string message) {
            return Check(type) ? Advance() : throw Error(Peek(), message);
        }

        private ParseError Error(Token token, string message) {
            _log.ReportError(token, message);
            return new ParseError();
        }

        private bool IsAtEnd() => Peek().Type == TokenType.EOF;

        private bool Match(params TokenType[] types) {
            if (types.Any(t => Check(t))) {
                Advance();
                return true;
            }
            return false;
        }

        private Token Peek() => _tokens[_current];

        private Token Previous() => _tokens[_current - 1];

        private void Synchronize() {
            Advance();

            while (!IsAtEnd()) {
                if (Previous().Type == TokenType.Semicolon) {
                    return;
                }

                switch (Peek().Type) {
                case TokenType.Class:
                case TokenType.For:
                case TokenType.Fun:
                case TokenType.If:
                case TokenType.Print:
                case TokenType.Return:
                case TokenType.Var:
                case TokenType.While:
                    return;
                }

                Advance();
            }
        }

        class ParseError : Exception { }
        #endregion
    }

}
