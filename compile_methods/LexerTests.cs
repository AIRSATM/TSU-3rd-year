using MiniLang.Lexing;
using Xunit;
using Xunit.Abstractions;

namespace MiniLang.Tests;

public class LexerTests
{
    private readonly ITestOutputHelper _output;
    public LexerTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Empty()
    {
        _output.WriteLine("Проверяем: пустой вход -> только EOF");
        var toks = Lexer.Tokenize("");
        Assert.Single(toks);
        Assert.Equal(TokenType.Eof, toks[0].Type);
    }

    [Fact]
    public void Operators()
    {
        _output.WriteLine("Проверяем: все операторы и пунктуацию");
        var toks = Lexer.Tokenize(":= + - * / ( ) [ ] ; , < > <= >= = <>");
        var expected = new[]
        {
            TokenType.Assign, TokenType.Plus, TokenType.Minus, TokenType.Mul,
            TokenType.Div, TokenType.LParen, TokenType.RParen,
            TokenType.LBrack, TokenType.RBrack, TokenType.Semi, TokenType.Comma,
            TokenType.Lt, TokenType.Gt, TokenType.Le, TokenType.Ge, TokenType.Eq, TokenType.Ne,
            TokenType.Eof,
        };
        Assert.Equal(expected, toks.Select(t => t.Type).ToArray());
    }

    [Fact]
    public void Keywords()
    {
         _output.WriteLine("Проверяем: ключевые слова");
        var toks = Lexer.Tokenize("if then else end while do read write array");
        Assert.Equal(new[]
        {
            TokenType.If, TokenType.Then, TokenType.Else, TokenType.End,
            TokenType.While, TokenType.Do, TokenType.Read, TokenType.Write,
            TokenType.Array, TokenType.Eof,
        }, toks.Select(t => t.Type).ToArray());
    }

    [Fact]
    public void IntAndReal()
    {
        _output.WriteLine("Проверяем: целые и вещественные числа");
        var toks = Lexer.Tokenize("0 12 345 0.5 12.75");
        Assert.Equal(TokenType.Int, toks[0].Type);
        Assert.Equal(0, toks[0].Value);
        Assert.Equal(12, toks[1].Value);
        Assert.Equal(345, toks[2].Value);
        Assert.Equal(TokenType.Real, toks[3].Type);
        Assert.Equal(0.5, toks[3].Value);
        Assert.Equal(12.75, toks[4].Value);
    }

    [Fact]
    public void IdentifiersAndUnderscore()
    {
        _output.WriteLine("Проверяем: идентификаторы и подчёркивание");
        var toks = Lexer.Tokenize("x abc _foo a1 _ ifx");
        Assert.All(toks.Take(6), t => Assert.Equal(TokenType.Ident, t.Type));
        Assert.Equal("_foo", toks[2].Lexeme);
        Assert.Equal("ifx", toks[5].Lexeme);
    }

    [Fact]
    public void Comment()
    {
        _output.WriteLine("Проверяем: комментарий игнорируется");
        var toks = Lexer.Tokenize("x := 1; // ignored stuff @@@\ny := 2;");
        // x := 1 ; y := 2 ; EOF
        Assert.Equal(9, toks.Count);
        Assert.Equal(TokenType.Ident, toks[0].Type);
        Assert.Equal("y", toks[4].Lexeme);
    }

    [Fact]
    public void LineAndCol()
    {
        _output.WriteLine("Проверяем: координаты токенов");
        var toks = Lexer.Tokenize("x\n  y");
        Assert.Equal(1, toks[0].Line);
        Assert.Equal(1, toks[0].Col);
        Assert.Equal(2, toks[1].Line);
        Assert.Equal(3, toks[1].Col);
    }

    [Fact]
    public void LexicalErrorReportsPositionAndMessage()
    {
        _output.WriteLine("Проверяем: лексическая ошибка с координатами и сообщением");
        var ex = Assert.Throws<LexException>(() => Lexer.Tokenize("x := 1 @ 2;"));
        Assert.Equal(1, ex.Line);
        Assert.Equal(8, ex.Col);
        Assert.Contains("недопустимый символ '@'", ex.Message);
    }

    [Fact]
    public void RealWithoutFractionFails_WithPosition()
    {
        _output.WriteLine("Проверяем: '12.' вызывает ошибку с координатами");
        var ex = Assert.Throws<LexException>(() => Lexer.Tokenize("12."));
        Assert.Equal(1, ex.Line);
        Assert.Equal(1, ex.Col);
        Assert.Contains("Лексическая ошибка", ex.Message);
    }

    [Fact]
    public void CommentWithoutNewline_EndsAtEof()
    {
        _output.WriteLine("Проверяем: комментарий в конце файла без перевода строки");
        var toks = Lexer.Tokenize("x := 1; // последний комментарий без переноса");
        // Токены: IDENT(x), ASSIGN, INT(1), SEMI, EOF
        Assert.Equal(5, toks.Count);
        Assert.Equal(TokenType.Ident, toks[0].Type);
        Assert.Equal(TokenType.Eof, toks[4].Type);
    }

    [Fact]
    public void UnterminatedOperator_ColonWithoutEq_Throws()
    {
        _output.WriteLine("Проверяем: ':' без '=' после него вызывает ошибку");
        var ex = Assert.Throws<LexException>(() => Lexer.Tokenize("x : 1;"));
        Assert.Equal(1, ex.Line);
        Assert.Equal(3, ex.Col);
        Assert.Contains("недопустимый символ", ex.Message); // или конкретнее
    }
}
