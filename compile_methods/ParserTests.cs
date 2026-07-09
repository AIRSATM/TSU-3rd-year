using MiniLang.Lexing;
using MiniLang.Parsing;
using Xunit;
using Xunit.Abstractions;

namespace MiniLang.Tests;

public class ParserTests
{
    private readonly ITestOutputHelper _output;

    public ParserTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static IReadOnlyList<RpnItem> Compile(string src) 
        => Parser.Parse(Lexer.Tokenize(src));

    [Fact]
    public void Precedence()
    {
        _output.WriteLine("Проверяем: приоритет операций (умножение раньше сложения)");
        var rpn = Compile("x := 1 + 2 * 3;");
        var symbols = rpn.Select(i => i.ToString()).ToArray();
        Assert.Equal(new[] { "x", "1", "2", "3", "*", "+", ":=", "HALT" }, symbols);
    }

    [Fact]
    public void UnaryMinus()
    {
        _output.WriteLine("Проверяем: унарный минус генерирует @-");
        var rpn = Compile("x := -5;");
        var symbols = rpn.Select(i => i.ToString()).ToArray();
        Assert.Equal(new[] { "x", "5", "@-", ":=", "HALT" }, symbols);
    }

    [Fact]
    public void IfWithoutElseHasJzPatched()
    {
        _output.WriteLine("Проверяем: if без else — одна JZ-метка с корректным адресом");
        var rpn = Compile("if 1 < 2 then write(1) end;");
        int jzCount = rpn.Count(i => i.Kind == RpnKind.Op && i.Op == OpCode.Jz);
        Assert.Equal(1, jzCount);
        foreach (var item in rpn.Where(i => i.Kind == RpnKind.Lbl))
            Assert.InRange(item.Addr, 0, rpn.Count);
    }

    [Fact]
    public void WhileEmitsJmpBack()
    {
        _output.WriteLine("Проверяем: while — одна JMP и одна JZ");
        var rpn = Compile("while 1 < 2 do x := 1 end;");
        int jmpCount = rpn.Count(i => i.Kind == RpnKind.Op && i.Op == OpCode.Jmp);
        int jzCount = rpn.Count(i => i.Kind == RpnKind.Op && i.Op == OpCode.Jz);
        Assert.Equal(1, jmpCount);
        Assert.Equal(1, jzCount);
    }

    [Fact]
    public void SyntaxErrorMissingOperand_ReportsPosition()
    {
        _output.WriteLine("Проверяем: синтаксическая ошибка с координатами и сообщением");
        var ex = Assert.Throws<ParseException>(() => Compile("x := 1 + ;"));
        Assert.Equal(1, ex.Line);
        Assert.Equal(10, ex.Col);
        Assert.Contains("Синтаксическая ошибка", ex.Message);
    }

    [Fact]
    public void SyntaxErrorIfWithoutThen_ReportsPosition()
    {
        _output.WriteLine("Проверяем: if без then -> ошибка");
        var ex = Assert.Throws<ParseException>(() => Compile("if 1 < 2 write(1) end;"));
        Assert.Equal(1, ex.Line);
        Assert.True(ex.Col > 0);
        Assert.Contains("неожиданный", ex.Message);
    }

    [Fact]
    public void SyntaxErrorWhileWithoutDo_ReportsPosition()
    {
        _output.WriteLine("Проверяем: while без do -> ошибка");
        var ex = Assert.Throws<ParseException>(() => Compile("while 1 < 2 x := 1 end;"));
        Assert.Equal(1, ex.Line);
        Assert.True(ex.Col > 0);
        Assert.Contains("неожиданный", ex.Message);
    }

    [Fact]
    public void ArrayDeclaration()
    {
        _output.WriteLine("Проверяем: объявление массива генерирует DECL");
        var rpn = Compile("array a[10];");
        var symbols = rpn.Select(i => i.ToString()).ToArray();
        Assert.Equal(new[] { "a", "10", "DECL", "HALT" }, symbols);
    }

    [Fact]
    public void RvalForIdentifierInExpression()
    {
        _output.WriteLine("Проверяем: переменная в выражении -> RVAL");
        var rpn = Compile("x := y + 1;");
        var symbols = rpn.Select(i => i.ToString()).ToArray();
        Assert.Equal(new[] { "x", "y", "RVAL", "1", "+", ":=", "HALT" }, symbols);
    }

    [Fact]
    public void SyntaxErrorUnclosedParen_ReportsPosition()
    {
        _output.WriteLine("Проверяем: незакрытая скобка -> ошибка");
        var ex = Assert.Throws<ParseException>(() => Compile("x := (1 + 2;"));
        Assert.Equal(1, ex.Line);
        Assert.True(ex.Col > 0);
        Assert.Contains("ожидался", ex.Message);
    }
}