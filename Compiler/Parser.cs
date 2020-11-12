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
                if (Match(Fun)) {
                    return Function("function");
                }
                if (Match(Var)) {
                    return VarDeclaration();
                }
                return Statement();
            } catch (ParseError) {
                Synchronize();
                return null;
            }
        }

        public Stmt.Function Function(string kind) {
            Token name = Consume(Identifier, $"Expected {kind} name.");
            Consume(LeftParen, $"Expected '(' after {kind} name.");

            List<Token> parameters = new List<Token>();
            if (!Check(RightParen)) {
                do {
                    if (parameters.Count >= 255) {
                        Error(Peek(), "Can't have more than 255 parameters.");
                    }

                    parameters.Add(Consume(Identifier, "Expected parameter name."));
                } while (Match(Comma));
            }

            Consume(RightParen, "Expected ')' after parameters.");

            Consume(LeftBrace, $"Expect '{{' before {kind} body.");
            IList<Stmt> body = Block();
            return new Stmt.Function(name, parameters, body);
        }

        private Stmt Statement() {
            if (Match(For)) {
                return ForStatement();
            }
            if (Match(If)) {
                return IfStatement();
            }
            if (Match(Print)) {
                return PrintStatement();
            }
            if (Match(TokenType.Return)) {
                return ReturnStatement();
            }
            if (Match(While)) {
                return WhileStatement();
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

        private Stmt ForStatement() {
            Consume(LeftParen, "Expect '(' after 'for'.");

            Stmt? initializer;
            if (Match(Semicolon)) {
                initializer = null;
            } else {
                initializer = Match(Var) ? VarDeclaration() : ExpressionStatement();
            }

            Expr? condition = null;
            if (!Check(Semicolon)) {
                condition = Expression();
            }
            Consume(Semicolon, "Expect ';' after for condition.");

            Expr? increment = null;
            if (!Check(RightParen)) {
                increment = Expression();
            }
            Consume(RightParen, "Expect ')' after for clauses.");

            Stmt body = Statement();
            if (increment != null) {
                body = new Stmt.Block(new List<Stmt>() { body, new Stmt.Expression(increment) } ); 
            }

            if (condition == null) {
                condition = new Expr.Literal(true);
            }
            body = new Stmt.While(condition, body);

            if (initializer != null) {
                body = new Stmt.Block(new List<Stmt>() { initializer, body });
            }

            return body;
        }

        private Stmt IfStatement() {
            Consume(LeftParen, "Expected '(' after 'if'.");
            Expr condition = Expression();
            Consume(RightParen, "Expect ')' after 'if' condition.");

            Stmt thenBranch = Statement();
            Stmt? elseBranch = null;
            if (Match(Else)) {
                elseBranch = Statement();
            }

            return new Stmt.If(condition, thenBranch, elseBranch);
        }

        private Stmt PrintStatement() {
            Expr value = Expression();
            Consume(Semicolon, "Expected ';' after value.");
            return new Stmt.Print(value);
        }

        private Stmt ReturnStatement() {
            Token keyword = Previous();
            Expr? value = null;
            if (!Check(Semicolon)) {
                value = Expression();
            }

            Consume(Semicolon, "Expected ';' after return expression.");
            return new Stmt.Return(keyword, value);
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

        private Stmt WhileStatement() {
            Consume(LeftParen, "Expect '(' after 'while'.");
            Expr condition = Expression();
            Consume(RightParen, "Expect ')' after while condition.");
            Stmt body = Statement();

            return new Stmt.While(condition, body);
        }
        #endregion

        #region Expression

        private Expr Expression() {
            return Assignment();
        }

        private Expr Assignment() {
            Expr expr = Or();

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
        
        private Expr Or() { return GenericLogical(And, TokenType.Or); }

        private Expr And() { return GenericLogical(Equality, TokenType.And); }

        private Expr Equality() { return GenericBinary(Comparison, EqualEqual, BangEqual); }

        private Expr Comparison() { return GenericBinary(Term, Greater, GreaterEqual, Less, LessEqual); }

        private Expr Term() { return GenericBinary(Factor, Minus, Plus); }

        private Expr Factor() { return GenericBinary(Unary, Slash, Star); }

        private Expr GenericBinary(Func<Expr> fn, params TokenType[] types) {
            Expr expr = fn(); 
            while (Match(types)) {
                Token op = Previous();
                Expr right = fn();
                expr = new Expr.Binary(expr, op, right);
            } 
            return expr; 
        }

        private Expr GenericLogical(Func<Expr> fn, params TokenType[] types) {
            Expr expr = fn(); 
            while (Match(types)) {
                Token op = Previous();
                Expr right = fn();
                expr = new Expr.Logical(expr, op, right);
            } 
            return expr; 
        }


        private Expr Unary() {
            if (Match(Bang, Minus)) {
                Token op = Previous();
                Expr right = Unary();
                return new Expr.Unary(op, right);
            }
            return Call();
        }

        private Expr Call() {
            Expr expr = Primary();

            while (true) {
                if (Match(LeftParen)) {
                    expr = FinishCall(expr);
                } else {
                    break;
                }
            }

            return expr;
        }

        private Expr FinishCall(Expr callee) {
            List<Expr> arguments = new List<Expr>();

            if (!Check(RightParen)) {
                if (arguments.Count >= 255) {
                    Error(Peek(), "Function can't have more than 255 arguments.");
                }
                do {
                    arguments.Add(Expression());
                } while (Match(Comma));
            }

            Token paren = Consume(RightParen, "Expected ')' after function arguments.");

            return new Expr.Call(callee, paren, arguments);
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
                case TokenType.Return:
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
