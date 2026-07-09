using MiniLang.Interpreting;
using MiniLang.Lexing;
using MiniLang.Parsing;
using Xunit;
using Xunit.Abstractions;

namespace MiniLang.Tests;

public class InterpreterTests
{
    private readonly ITestOutputHelper _output;

    public InterpreterTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static string Run(string src, string input = "")
    {
        var tokens = Lexer.Tokenize(src);
        var rpn = Parser.Parse(tokens);
        using var output = new StringWriter();
        using var reader = new StringReader(input);
        Interpreter.Run(rpn, reader, output);
        return output.ToString();
    }

    [Fact]
    public void Simple()
    {
        _output.WriteLine("Проверяем: 2 + 3 * 4 = 14");
        var output = Run("x := 2 + 3 * 4; write(x);");
        Assert.Equal("14", output.Trim());
    }

    [Fact]
    public void PrecedenceAndParens()
    {
        _output.WriteLine("Проверяем: (2 + 3) * 4 = 20");
        var output = Run("x := (2 + 3) * 4; write(x);");
        Assert.Equal("20", output.Trim());
    }

    [Fact]
    public void ReadWrite()
    {
        _output.WriteLine("Проверяем: read(a); read(b); write(a+b) с вводом 10 32 -> 42");
        var output = Run("read(a); read(b); write(a + b);", "10 32");
        Assert.Equal("42", output.Trim());
    }

    [Fact]
    public void RealNumbers()
    {
        _output.WriteLine("Проверяем: read(x); write(x * 2.5); с вводом 4 -> 10");
        var output = Run("read(x); write(x * 2.5);", "4");
        Assert.Equal("10", output.Trim());
    }

    [Fact]
    public void UnaryMinus()
    {
        _output.WriteLine("Проверяем: x := -5; write(x + 10) -> 5");
        var output = Run("x := -5; write(x + 10);");
        Assert.Equal("5", output.Trim());
    }

    [Theory]
    [InlineData("5", "1")]
    [InlineData("-3", "0")]
    [InlineData("0", "0")]
    public void IfThen(string input, string expected)
    {
        _output.WriteLine($"Проверяем: if x > 0 then write(1) else write(0) для x={input} -> {expected}");
        var src = @"
            read(x);
            if x > 0 then write(1) end;
            if x <= 0 then write(0) end;
        ";
        Assert.Equal(expected, Run(src, input).Trim());
    }

    [Theory]
    [InlineData("5", "1")]
    [InlineData("-3", "2")]
    public void IfElse(string input, string expected)
    {
        _output.WriteLine($"Проверяем: if x > 0 then write(1) else write(2) для x={input} -> {expected}");
        var src = @"
            read(x);
            if x > 0 then
                write(1)
            else
                write(2)
            end;
        ";
        Assert.Equal(expected, Run(src, input).Trim());
    }

    [Fact]
    public void WhileSum()
    {
        _output.WriteLine("Проверяем: сумма 1..10 = 55");
        var src = @"
            read(n);
            i := 1;
            s := 0;
            while i <= n do
                s := s + i;
                i := i + 1
            end;
            write(s);
        ";
        Assert.Equal("55", Run(src, "10").Trim());
    }

    [Fact]
    public void ArrayBasic()
    {
        _output.WriteLine("Проверяем: array a[3]; a[0]=10; a[1]=20; a[2]=30; сумма=60");
        var src = @"
            array a[3];
            a[0] := 10;
            a[1] := 20;
            a[2] := 30;
            write(a[0] + a[1] + a[2]);
        ";
        Assert.Equal("60", Run(src).Trim());
    }

    [Fact]
    public void ArraySort()
    {
        _output.WriteLine("Проверяем: сортировка пузырьком [3,1,4,1,5] -> [1,1,3,4,5]");
        var src = @"
            read(n);
            array a[100];
            i := 0;
            while i < n do
                read(a[i]);
                i := i + 1
            end;

            i := 0;
            while i < n - 1 do
                j := 0;
                while j < n - 1 - i do
                    if a[j] > a[j + 1] then
                        t := a[j];
                        a[j] := a[j + 1];
                        a[j + 1] := t
                    end;
                    j := j + 1
                end;
                i := i + 1
            end;

            i := 0;
            while i < n do
                write(a[i]);
                i := i + 1
            end;
        ";
        var output = Run(src, "5  3 1 4 1 5");
        var got = output.Split(new[] { ' ','\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(new[] { "1", "1", "3", "4", "5" }, got);
    }

    [Fact]
    public void DivisionByZeroThrows_WithMessageAndPosition()
    {
        _output.WriteLine("Проверяем: деление на ноль -> RuntimeException с координатами и сообщением");
        var ex = Assert.Throws<RuntimeException>(() => Run("x := 1 / 0;"));
        Assert.Equal(1, ex.Line);
        Assert.True(ex.Col > 0);
        Assert.Contains("деление на ноль", ex.Message);
    }

    [Fact]
    public void IndexOutOfBoundsThrows_WithMessageAndPosition()
    {
        _output.WriteLine("Проверяем: выход за границы массива -> RuntimeException с координатами");
        var ex = Assert.Throws<RuntimeException>(() => Run("array a[3]; write(a[5]);"));
        Assert.Equal(1, ex.Line);
        Assert.True(ex.Col > 0);
        Assert.Contains("индекс 5 вне границ", ex.Message);
    }

    [Fact]
    public void UninitializedVarThrows_WithMessageAndPosition()
    {
        _output.WriteLine("Проверяем: неинициализированная переменная -> RuntimeException");
        var ex = Assert.Throws<RuntimeException>(() => Run("write(z);"));
        Assert.Equal(1, ex.Line);
        Assert.True(ex.Col > 0);
        Assert.Contains("не инициализирована", ex.Message);
    }

    [Fact]
    public void Factorial()
    {
        _output.WriteLine("Проверяем: 6! = 720");
        var src = @"
            read(n);
            f := 1;
            i := 2;
            while i <= n do
                f := f * i;
                i := i + 1
            end;
            write(f);
        ";
        Assert.Equal("720", Run(src, "6").Trim());
    }

    [Fact]
    public void Gcd()
    {
        _output.WriteLine("Проверяем: НОД(48,18) = 6");
        var src = @"
            read(a);
            read(b);
            while a <> b do
                if a > b then
                    a := a - b
                else
                    b := b - a
                end
            end;
            write(a);
        ";
        Assert.Equal("6", Run(src, "48 18").Trim());
    }

    [Fact]
    public void ArrayNegativeIndex_Throws()
    {
        _output.WriteLine("Проверяем: отрицательный индекс -> RuntimeException");
        var ex = Assert.Throws<RuntimeException>(() => Run("array a[5]; write(a[-1]);"));
        Assert.Equal(1, ex.Line);
        Assert.True(ex.Col > 0);
        Assert.Contains("индекс -1 вне границ", ex.Message);
    }

    [Fact]
    public void ComplexExpressionWithRealAndInt()
    {
        _output.WriteLine("Проверяем: смешанные вычисления с целыми и вещественными");
        var output = Run("x := 2.5 + 3 * 4 - 1.5 / 2; write(x);");
        // 2.5 + 12 - 0.75 = 13.75
        Assert.Equal("13.75", output.Trim());
    }

    [Fact]
    public void WriteReal_Formatting()
    {
        _output.WriteLine("Проверяем: форматирование вещественных чисел (целые без десятичной точки)");
        var output = Run("write(2.0);");
        Assert.Equal("2", output.Trim());
    }

    [Fact]
    public void NestedIf()
    {
        _output.WriteLine("Проверяем: вложенный if");
        var src = @"
            read(x);
            read(y);
            if x > 0 then
                if y > 0 then
                    write(1)
                else
                    write(2)
                end
            else
                write(3)
            end;
        ";
        Assert.Equal("1", Run(src, "5 3").Trim()); // x>0, y>0 -> 1
        Assert.Equal("2", Run(src, "5 -1").Trim()); // x>0, y<=0 -> 2
        Assert.Equal("3", Run(src, "-5 3").Trim()); // x<=0 -> 3
    }
}