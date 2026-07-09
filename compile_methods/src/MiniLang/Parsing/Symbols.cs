using MiniLang.Lexing;

namespace MiniLang.Parsing;

/// <summary>
/// Элемент магазина синтаксического анализатора.
/// Используется единый числовой код, совместимый со старой Go-реализацией:
///   [  0;  100)  — терминалы (значение <see cref="TokenType"/>)
///   [100;  200)  — нетерминалы (<see cref="Nonterm"/>)
///   [200; ...)   — семантические действия (<see cref="SemAction"/>)
/// </summary>
public readonly record struct Symbol(int Code)
{
    public static implicit operator Symbol(TokenType t) => new((int)t);
    public static implicit operator Symbol(Nonterm n) => new((int)n);
    public static implicit operator Symbol(SemAction a) => new((int)a);

    public bool IsTerminal => Code >= 0 && Code < 100;
    public bool IsNonterm => Code >= 100 && Code < 200;
    public bool IsAction => Code >= 200;

    public TokenType AsToken => (TokenType)Code;
    public Nonterm AsNonterm => (Nonterm)Code;
    public SemAction AsAction => (SemAction)Code;

    public override string ToString()
    {
        if (IsTerminal) return AsToken.Display();
        if (IsNonterm) return AsNonterm.ToString();
        if (IsAction) return AsAction.ToString();
        return $"?({Code})";
    }
}

/// <summary>Нетерминалы грамматики (см. docs/grammar.md).</summary>
public enum Nonterm
{
    Program = 100,
    StmtList,
    StmtListTail,
    Stmt,
    AssignTail,
    LvalIndexTail,
    Expr,
    ExprTail,
    Term,
    TermTail,
    Fact,
    FactIdTail,
    Cond,
    RelOp,
    IfTail,
}

/// <summary>Семантические действия, генерирующие элементы ОПС.</summary>
public enum SemAction
{
    PushNum = 200,
    PushVar,
    OpAdd,
    OpSub,
    OpMul,
    OpDiv,
    OpNeg,
    OpRel,
    OpIndex,
    OpRval,
    OpAssign,
    OpRead,
    OpWrite,
    OpDecl,
    IfAfterCond,
    IfAfterThen,
    IfNoElse,
    IfEnd,
    WhileBegin,
    WhileAfterCond,
    WhileEnd,
}

/// <summary>Синтаксическая ошибка с указанием строки и колонки.</summary>
public sealed class ParseException : Exception
{
    public int Line { get; }
    public int Col { get; }

    public ParseException(int line, int col, string message)
        : base($"Синтаксическая ошибка [строка {line}, символ {col}]: {message}")
    {
        Line = line;
        Col = col;
    }
}
