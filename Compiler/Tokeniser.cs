using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace Compiler {
    public class Tokeniser {

        private readonly string _source;
        private readonly List<Token> _tokens;
        private readonly IOutputStuff _log;
        private readonly IDictionary<string, TokenType> _keywords = new Dictionary<string, TokenType>() {

            { "and", TokenType.And },
            { "class", TokenType.Class },
            { "else", TokenType.Else },
            { "false", TokenType.False },
            { "for", TokenType.For },
            { "fun", TokenType.Fun },
            { "if", TokenType.If },
            { "nil", TokenType.Nil },
            { "or", TokenType.Or },
            { "print", TokenType.Print },
            { "return", TokenType.Return },
            { "super", TokenType.Super },
            { "this", TokenType.This },
            { "true", TokenType.True },
            { "var", TokenType.Var },
            { "while", TokenType.While },
        };
        private int _start;
        private int _current;

        public Tokeniser(IOutputStuff log, string source) {
            _log = log;
            _source = source;
            _tokens = new List<Token>();
            _start = 0;
            _current = 0;
        }

        public IList<Token> ScanTokens() {
            while (!IsAtEnd()) {
                _start = _current;
                ScanToken();
            }

            _tokens.Add(new Token(TokenType.EOF, "", null, _start));
            return _tokens;
        }

        private void ScanToken() {
            char c = Advance();
            switch (c) {
            case '(': AddToken(TokenType.LeftParen); break;
            case ')': AddToken(TokenType.RightParen); break;
            case '{': AddToken(TokenType.LeftBrace); break;
            case '}': AddToken(TokenType.RightBrace); break;
            case ',': AddToken(TokenType.Comma); break;
            case '.': AddToken(TokenType.Dot); break;
            case '-': AddToken(TokenType.Minus); break;
            case '+': AddToken(TokenType.Plus); break;
            case ';': AddToken(TokenType.Semicolon); break;
            case '*': AddToken(TokenType.Star); break;
            case '!':
                AddToken(Match('=') ? TokenType.BangEqual : TokenType.Bang);
                break;
            case '=':
                AddToken(Match('=') ? TokenType.EqualEqual : TokenType.Equal);
                break;
            case '<':
                AddToken(Match('=') ? TokenType.LessEqual : TokenType.Less);
                break;
            case '>':
                AddToken(Match('=') ? TokenType.GreaterEqual : TokenType.GreaterEqual);
                break;
            case '/':
                if (Match('/')) {
                    while (Peek() != '\n' && !IsAtEnd()) {
                        Advance();
                    }
                } else {
                    AddToken(TokenType.Slash);
                }
                break;
            case '"':
                String();
                break;
            case ' ':
            case '\r':
            case '\t':
            case '\n':
                // Ignoring whitespace
                break;

            default:
                if (IsDigit(c)) {
                    Number();
                } else if (IsAlpha(c)) {
                    Identifier();
                } else {
                    _log.ReportError($"Unexpected character '{c}' at position {_start}");
                }
                break;
            }

        }

        private void AddToken(TokenType type, object? literal = null) {
            string lexeme = _source[_start.._current];
            _tokens.Add(new Token(type, lexeme, literal, _start));
        }
        private char Advance() {
            _current += 1;
            return _source[_current - 1];
        }

        private void Identifier() {
            while (IsAlphanumeric(Peek())) {
                Advance();
            }

            string text = _source[_start.._current];
            if (_keywords.ContainsKey(text)) {
                AddToken(_keywords[text]);
            } else {
                AddToken(TokenType.Identifier);
            }
        }

        private static bool IsAlpha(char c) {
            return c >= 'a' && c <= 'z'
                || c >= 'A' && c <= 'Z';
        }

        private static bool IsAlphanumeric(char c) {
            return IsAlpha(c) || IsDigit(c);
        }

        private bool IsAtEnd() {
            return _current >= _source.Length;
        }

        private static bool IsDigit(char c) {
            return c >= '0' && c <= '9';
        }

        private bool Match(char expected) {
            if (IsAtEnd() || _source[_current] != expected) {
                return false;
            }
            _current += 1;
            return true;
        }

        private void Number() {
            while (IsDigit(Peek())) {
                Advance();
            }

            if (Peek() == '.' && IsDigit(PeekNext())) {
                Advance();
                while (IsDigit(Peek())) {
                    Advance();
                }
            }

            AddToken(TokenType.Number, double.Parse(_source[_start.._current]));
        }

        private char Peek() {
            return IsAtEnd() ? '\0' : _source[_current];
        }

        private char PeekNext() {
            return _current + 1 >= _source.Length ? '\0' : _source[_current + 1];
        }

        private void String() {
            while (!IsAtEnd() && !(Peek() == '"')) {
                Advance();
            }

            if (IsAtEnd()) {
                _log.ReportError($"Unterminated string starting from {_start}");
            }

            // Grab closing quote
            Advance();

            AddToken(TokenType.String, _source.Substring(_start + 1, _current - _start - 2));
        }
    }
}
