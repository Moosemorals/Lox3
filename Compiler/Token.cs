namespace Compiler {
    public record Token(TokenType Type, string Lexeme, object? Literal, int Offset);
}
