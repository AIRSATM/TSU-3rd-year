using System.Globalization;
using MiniLang.Lexing;

namespace MiniLang.Parsing;

// =============================================================================
// ФОРМАТ ОПС (Обратная Польская Запись)
// =============================================================================
//
// ОПС — это последовательность элементов (RpnItem). Каждый элемент имеет тип:
//
//   Num   — числовая константа (поле Num)
//   Var   — имя переменной (поле Name); кладёт на стек ССЫЛКУ (lvalue)
//   Op    — операция (поле Op)
//   Lbl   — метка (адрес перехода); поле Addr — индекс в массиве элементов
//
// Метки используются операциями переходов JMP / JZ.
// =============================================================================

public enum OpCode
{
    Add, Sub, Mul, Div, Neg,
    Lt, Gt, Le, Ge, Eq, Ne,
    Assign, Index, Decl,
    Read, Write,
    Jmp, Jz,
    Rval,
    Halt,
}

public static class OpCodeExtensions
{
    public static string Display(this OpCode op) => op switch
    {
        OpCode.Add => "+", OpCode.Sub => "-", OpCode.Mul => "*", OpCode.Div => "/",
        OpCode.Neg => "@-",
        OpCode.Lt => "<", OpCode.Gt => ">", OpCode.Le => "<=",
        OpCode.Ge => ">=", OpCode.Eq => "=", OpCode.Ne => "<>",
        OpCode.Assign => ":=", OpCode.Index => "[]", OpCode.Decl => "DECL",
        OpCode.Read => "READ", OpCode.Write => "WRITE",
        OpCode.Jmp => "JMP", OpCode.Jz => "JZ",
        OpCode.Rval => "RVAL", OpCode.Halt => "HALT",
        _ => "?",
    };
}

public enum RpnKind
{
    Num,
    Var,
    Op,
    Lbl,
}

/// <summary>
/// Элемент ОПС. Поля <see cref="Line"/>/<see cref="Col"/> переносят
/// позицию исходного токена для диагностики ошибок выполнения.
/// </summary>
public sealed class RpnItem
{
    public RpnKind Kind { get; init; }
    public double Num { get; init; }
    public string Name { get; init; } = string.Empty;
    public OpCode Op { get; init; }

    /// <summary>Адрес перехода. -1 — ещё не разрешён (back-patching).</summary>
    public int Addr { get; set; }

    public int Line { get; init; }
    public int Col { get; init; }

    public override string ToString() => Kind switch
    {
        RpnKind.Num => Num.ToString("G", CultureInfo.InvariantCulture),
        RpnKind.Var => Name,
        RpnKind.Op => Op.Display(),
        RpnKind.Lbl => $"L({Addr})",
        _ => "?",
    };

    public static RpnItem MakeNum(double v, Token t) => new()
    {
        Kind = RpnKind.Num, Num = v, Line = t.Line, Col = t.Col,
    };

    public static RpnItem MakeVar(string name, Token t) => new()
    {
        Kind = RpnKind.Var, Name = name, Line = t.Line, Col = t.Col,
    };

    public static RpnItem MakeOp(OpCode op, Token t) => new()
    {
        Kind = RpnKind.Op, Op = op, Line = t.Line, Col = t.Col,
    };

    public static RpnItem MakeLbl(int addr) => new()
    {
        Kind = RpnKind.Lbl, Addr = addr,
    };
}

public static class RpnFormatter
{
    public static string Format(IReadOnlyList<RpnItem> rpn)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < rpn.Count; i++)
            sb.AppendLine($"{i,3}: {rpn[i]}");
        return sb.ToString();
    }
    public static string FormatHorizontal(IReadOnlyList<RpnItem> rpn)
    {
        return string.Join(" ", rpn.Select(item => item.ToString()));
    }
}
