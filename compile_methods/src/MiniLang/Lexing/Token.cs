namespace MiniLang.Lexing;

/// <summary>
/// Тип лексемы. Номер лексемы (значение enum) используется в КС-грамматике
/// и в таблице синтаксического анализа.
/// </summary>
public enum TokenType
{
    // --- служебные ---
    Eof = 0,        // конец входа
    Begin = 1,
    Error = 2,      // ошибочная лексема
    Newline = 3,    // перевод строки (внутреннее)

    // --- литералы и идентификаторы ---
    Ident = 4,      // идентификатор
    Int = 5,        // целочисленная константа
    Real = 6,       // вещественная константа

    // --- ключевые слова ---
    If = 7,
    Then = 8,
    Else = 9,
    End = 10,
    While = 11,
    Do = 12,
    Read = 13,
    Write = 14,
    Array = 15,

    // --- операторы и пунктуация ---
    Assign = 16,    // :=
    Plus = 17,
    Minus = 18,
    Mul = 19,
    Div = 20,
    LParen = 21,
    RParen = 22,
    LBrack = 23,
    RBrack = 24,
    Semi = 25,
    Comma = 26,

    // --- сравнения ---
    Lt = 27,
    Gt = 28,
    Le = 29,
    Ge = 30,
    Eq = 31,
    Ne = 32,
}

// для красивого вывода
public static class TokenTypeExtensions
{
    private static readonly string[] Names = BuildNames();

    private static string[] BuildNames()
    {
        // Находим максимальное числовое значение среди всех TokenType
        var maxCode = Enum.GetValues(typeof(TokenType)).Cast<int>().Max();
        var names = new string[maxCode + 1];

        // Заполняем все известные типы
        names[(int)TokenType.Eof] = "EOF";
        names[(int)TokenType.Begin] = "BEGIN";
        names[(int)TokenType.Error] = "ERROR";
        names[(int)TokenType.Newline] = "NEWLINE";
        names[(int)TokenType.Ident] = "IDENT";
        names[(int)TokenType.Int] = "INT";
        names[(int)TokenType.Real] = "REAL";
        names[(int)TokenType.If] = "IF";
        names[(int)TokenType.Then] = "THEN";
        names[(int)TokenType.Else] = "ELSE";
        names[(int)TokenType.End] = "END";
        names[(int)TokenType.While] = "WHILE";
        names[(int)TokenType.Do] = "DO";
        names[(int)TokenType.Read] = "READ";
        names[(int)TokenType.Write] = "WRITE";
        names[(int)TokenType.Array] = "ARRAY";
        names[(int)TokenType.Assign] = ":=";
        names[(int)TokenType.Plus] = "+";
        names[(int)TokenType.Minus] = "-";
        names[(int)TokenType.Mul] = "*";
        names[(int)TokenType.Div] = "/";
        names[(int)TokenType.LParen] = "(";
        names[(int)TokenType.RParen] = ")";
        names[(int)TokenType.LBrack] = "[";
        names[(int)TokenType.RBrack] = "]";
        names[(int)TokenType.Semi] = ";";
        names[(int)TokenType.Comma] = ",";
        names[(int)TokenType.Lt] = "<";
        names[(int)TokenType.Gt] = ">";
        names[(int)TokenType.Le] = "<=";
        names[(int)TokenType.Ge] = ">=";
        names[(int)TokenType.Eq] = "=";
        names[(int)TokenType.Ne] = "<>";
        return names;
    }

    public static string Display(this TokenType t)
    {
        var i = (int)t;
        return i >= 0 && i < Names.Length && Names[i] != null ? Names[i] : "?";
    }

    public static int Number(this TokenType t) => (int)t;
}

/// <summary>Экземпляр лексемы.</summary>
public readonly record struct Token(
    TokenType Type,
    string Lexeme,
    double Value,
    int Line,
    int Col);

/// <summary>Лексическая ошибка с указанием строки и колонки.</summary>
public sealed class LexException : Exception
{
    public int Line { get; }
    public int Col { get; }

    public LexException(int line, int col, string message)
        : base($"Лексическая ошибка [строка {line}, символ {col}]: {message}")
    {
        Line = line;
        Col = col;
    }
}
