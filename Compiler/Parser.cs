using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;

using static Compiler.TokenType;

namespace Compiler {
    public class Parser {

        private readonly IOutputStuff _log;
        private readonly IList<Token> _tokens;
        private int _current;

        public Parser(IOutputStuff log, IList<Token> tokens) {
            _current = 0;
            _log = log;
            _tokens = tokens;
        }

        public IList<Stmt> Parse() {
            List<Stmt> statements = new List<Stmt>();

            while (!IsAtEnd()) {
                Stmt? stmt = Declaration();
                if (stmt != null) {
                    statements.Add(stmt);
                }
            }

            return statements;
        }


        #region Statements

        private Stmt? Declaration() {
            try {
                if (Match(Var)) {
                    return VarDeclaration();
                }
                return Statement();
            } catch (ParseError) {
                Synchronize();
                return null;
            }
        }

        private Stmt Statement() {
            if (Match(Print)) {
                return PrintStatement();
            }
            if (Match(LeftBrace)) {
                return new Stmt.Block(Block());
            }
            return ExpressionStatement();
        }

        private IList<Stmt> Block() {
            IList<Stmt> statements = new List<Stmt>();

            while (!Check(RightBrace) && !IsAtEnd()) {
                Stmt? stmt = Declaration();
                if (stmt != null) {
                    statements.Add(stmt);
                }
            }

            Consume(RightBrace, "Expected '}' after block.");
            return statements;
        }

        private Stmt ExpressionStatement() {
            Expr expr = Expression();
            Consume(Semicolon, "Expected ';' after expression.");
            return new Stmt.Expression(expr);
        }

        private Stmt PrintStatement() {
            Expr value = Expression();
            Consume(Semicolon, "Expected ';' after value.");
            return new Stmt.Print(value);
        }

        private Stmt VarDeclaration() {
            Token name = Consume(Identifier, "Expected variable name.");
            
            Expr? initializer = null;
            if (Match(Equal)) {
                initializer = Expression();
            }

            Consume(Semicolon, "Expected ';' after variable declaration.");
            return new Stmt.Var(name, initializer);


        }

        #endregion

        #region Expression

        private Expr Expression() {
            return Assignment();
        }

        private Expr Assignment() {
            Expr expr = Equality();

            if (Match(Equal)) {
                Token equals = Previous();
                Expr value = Assignment();

                if (expr is Expr.Variable v) {
                    return new Expr.Assign(v.Name, value);
                }

                Error(equals, "Invalid assignment target.");
            }

            return expr;
        }

        private Expr Equality() { return GenericBool(Comparison, EqualEqual, BangEqual); }

        private Expr Comparison() { return GenericBool(Term, Greater, GreaterEqual, Less, LessEqual); }

        private Expr Term() { return GenericBool(Factor, Minus, Plus); }

        private Expr Factor() { return GenericBool(Unary, Slash, Star); }

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
            if (Match(Bang, Minus)) {
                Token op = Previous();
                Expr right = Unary();
                return new Expr.Unary(op, right);
            }
            return Primary();
        }

        private Expr Primary() {
            if (Match(False)) { return new Expr.Literal(false); }
            if (Match(True)) { return new Expr.Literal(true); }
            if (Match(Nil)) { return new Expr.Literal(null); }

            if (Match(Number, TokenType.String)) {
                return new Expr.Literal(Previous().Literal);
            }

            if (Match(Identifier)) {
                return new Expr.Variable(Previous());
            }

            if (Match(LeftParen)) {
                Expr expr = Expression();
                Consume(RightParen, "Expected ')' after expression.");
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

        private bool IsAtEnd() => Peek().Type == EOF;

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
                if (Previous().Type == Semicolon) {
                    return;
                }

                switch (Peek().Type) {
                case Class:
                case For:
                case Fun:
                case If:
                case Print:
                case Return:
                case Var:
                case While:
                    return;
                }

                Advance();
            }
        }

        class ParseError : Exception { }
        #endregion
    }

}
