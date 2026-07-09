using System;
using System.Globalization;
using System.Text;
using System.IO;

namespace MiniLang.Lexing;

// =============================================================================
// ЛЕКСИЧЕСКИЙ АНАЛИЗАТОР — ТАБЛИЧНЫЙ КОНЕЧНЫЙ АВТОМАТ
// =============================================================================
//
// Реализован в виде таблично-управляемого детерминированного конечного автомата.
// Таблица переходов:  TransitionTable[state][charClass] = (nextState, action)
//
// Каждый входной символ сначала классифицируется (классы символов — столбцы
// таблицы), затем по текущему состоянию (строка таблицы) выбирается
// следующее состояние и действие.
//
// Действия:
//   Accum   — добавить символ в буфер лексемы и продолжить
//   Emit    — выдать токен указанного типа; символ НЕ потребляется
//             (откат позиции на 1)
//   EmitC   — выдать токен и потребить текущий символ
//   Skip    — отбросить символ (пробелы, тело комментария)
//   Error   — лексическая ошибка
// =============================================================================

internal enum LexState
{
    Start,
    Ident,
    Int,
    Real,
    Dot,
    Colon,
    Lt,
    Gt,
    Slash,
    Comment,
    _Count,
}

internal enum CharClass
{
    Letter,
    Digit,
    Dot,
    Colon,
    Eq,
    Lt,
    Gt,
    Plus,
    Minus,
    Star,
    Slash,
    LParen,
    RParen,
    LBrack,
    RBrack,
    Semi,
    Comma,
    Space,
    Newline,
    Eof,
    Other,
    _Count,
}

internal enum LexAction
{
    Accum,
    Emit,
    EmitC,
    Skip,
    Error,
}

internal readonly record struct Cell(LexState Next, LexAction Action, TokenType EmitTok);

/// Лексический анализатор учебного языка MiniLang.
public sealed class Lexer
{
    // =========================================================================
    // ТАБЛИЦА ПЕРЕХОДОВ (общая для всех экземпляров; собирается один раз).
    // =========================================================================
    private static readonly Cell[,] TransitionTable = BuildTransitionTable();

    private static Cell[,] BuildTransitionTable()
    {
        var t = new Cell[(int)LexState._Count, (int)CharClass._Count];

        // По умолчанию — ошибка.
        for (int s = 0; s < (int)LexState._Count; s++)
            for (int c = 0; c < (int)CharClass._Count; c++)
                t[s, c] = Err();

        // ---- Start ----
        Set(LexState.Start, CharClass.Letter, GoAccum(LexState.Ident));
        Set(LexState.Start, CharClass.Digit, GoAccum(LexState.Int));
        Set(LexState.Start, CharClass.Dot, Err()); // число должно начинаться с цифры
        Set(LexState.Start, CharClass.Colon, GoAccum(LexState.Colon));
        Set(LexState.Start, CharClass.Eq, EmitC(TokenType.Eq));
        Set(LexState.Start, CharClass.Lt, GoAccum(LexState.Lt));
        Set(LexState.Start, CharClass.Gt, GoAccum(LexState.Gt));
        Set(LexState.Start, CharClass.Plus, EmitC(TokenType.Plus));
        Set(LexState.Start, CharClass.Minus, EmitC(TokenType.Minus));
        Set(LexState.Start, CharClass.Star, EmitC(TokenType.Mul));
        Set(LexState.Start, CharClass.Slash, GoAccum(LexState.Slash));
        Set(LexState.Start, CharClass.LParen, EmitC(TokenType.LParen));
        Set(LexState.Start, CharClass.RParen, EmitC(TokenType.RParen));
        Set(LexState.Start, CharClass.LBrack, EmitC(TokenType.LBrack));
        Set(LexState.Start, CharClass.RBrack, EmitC(TokenType.RBrack));
        Set(LexState.Start, CharClass.Semi, EmitC(TokenType.Semi));
        Set(LexState.Start, CharClass.Comma, EmitC(TokenType.Comma));
        Set(LexState.Start, CharClass.Space, Skip(LexState.Start));
        Set(LexState.Start, CharClass.Newline, Skip(LexState.Start));
        Set(LexState.Start, CharClass.Eof, EmitC(TokenType.Eof));

        // ---- Ident: буква/цифра — продолжаем; иначе — IDENT (или keyword) ----
        Set(LexState.Ident, CharClass.Letter, GoAccum(LexState.Ident));
        Set(LexState.Ident, CharClass.Digit, GoAccum(LexState.Ident));
        foreach (var c in NonIdentClasses())
            Set(LexState.Ident, c, Emit(TokenType.Ident));

        // ---- Int: цифра — продолжаем, точка — переходим к Real, иначе — INT ----
        Set(LexState.Int, CharClass.Digit, GoAccum(LexState.Int));
        Set(LexState.Int, CharClass.Dot, GoAccum(LexState.Dot));
        foreach (var c in NonIntClasses())
            Set(LexState.Int, c, Emit(TokenType.Int));

        // ---- Dot: после точки обязательна цифра ----
        Set(LexState.Dot, CharClass.Digit, GoAccum(LexState.Real));
        // прочие — ошибка (уже заполнено по умолчанию).

        // ---- Real: цифры продолжаются, прочее — выдать REAL ----
        Set(LexState.Real, CharClass.Digit, GoAccum(LexState.Real));
        foreach (var c in NonRealClasses())
            Set(LexState.Real, c, Emit(TokenType.Real));

        // ---- Colon: после ':' должно идти '=' ----
        Set(LexState.Colon, CharClass.Eq, EmitC(TokenType.Assign));

        // ---- Lt: '<' + '=' = LE, '<' + '>' = NE, иначе — LT ----
        Set(LexState.Lt, CharClass.Eq, EmitC(TokenType.Le));
        Set(LexState.Lt, CharClass.Gt, EmitC(TokenType.Ne));
        foreach (var c in NonLtClasses())
            Set(LexState.Lt, c, Emit(TokenType.Lt));

        // ---- Gt: '>' + '=' = GE, иначе — GT ----
        Set(LexState.Gt, CharClass.Eq, EmitC(TokenType.Ge));
        foreach (var c in NonGtClasses())
            Set(LexState.Gt, c, Emit(TokenType.Gt));

        // ---- Slash: '/' + '/' = комментарий, иначе — DIV ----
        Set(LexState.Slash, CharClass.Slash, Skip(LexState.Comment));
        foreach (var c in NonSlashClasses())
            Set(LexState.Slash, c, Emit(TokenType.Div));

        // ---- Comment: всё съедаем до перевода строки ----
        for (int c = 0; c < (int)CharClass._Count; c++)
            t[(int)LexState.Comment, c] = Skip(LexState.Comment);
        Set(LexState.Comment, CharClass.Newline, Skip(LexState.Start));
        Set(LexState.Comment, CharClass.Eof, EmitC(TokenType.Eof));

        return t;

        void Set(LexState s, CharClass c, Cell cell) => t[(int)s, (int)c] = cell;
    }

    private static Cell GoAccum(LexState s) => new(s, LexAction.Accum, TokenType.Eof);
    private static Cell Emit(TokenType tt) => new(LexState.Start, LexAction.Emit, tt);
    private static Cell EmitC(TokenType tt) => new(LexState.Start, LexAction.EmitC, tt);
    private static Cell Skip(LexState s) => new(s, LexAction.Skip, TokenType.Eof);
    private static Cell Err() => new(LexState.Start, LexAction.Error, TokenType.Eof);

    private static IEnumerable<CharClass> NonIdentClasses() => new[]
    {
        CharClass.Dot, CharClass.Colon, CharClass.Eq, CharClass.Lt, CharClass.Gt,
        CharClass.Plus, CharClass.Minus, CharClass.Star, CharClass.Slash,
        CharClass.LParen, CharClass.RParen, CharClass.LBrack, CharClass.RBrack,
        CharClass.Semi, CharClass.Comma,
        CharClass.Space, CharClass.Newline, CharClass.Eof, CharClass.Other,
    };

    private static IEnumerable<CharClass> NonIntClasses() => new[]
    {
        CharClass.Letter, CharClass.Colon, CharClass.Eq, CharClass.Lt, CharClass.Gt,
        CharClass.Plus, CharClass.Minus, CharClass.Star, CharClass.Slash,
        CharClass.LParen, CharClass.RParen, CharClass.LBrack, CharClass.RBrack,
        CharClass.Semi, CharClass.Comma,
        CharClass.Space, CharClass.Newline, CharClass.Eof, CharClass.Other,
    };

    private static IEnumerable<CharClass> NonRealClasses() => new[]
    {
        CharClass.Letter, CharClass.Dot, CharClass.Colon, CharClass.Eq,
        CharClass.Lt, CharClass.Gt,
        CharClass.Plus, CharClass.Minus, CharClass.Star, CharClass.Slash,
        CharClass.LParen, CharClass.RParen, CharClass.LBrack, CharClass.RBrack,
        CharClass.Semi, CharClass.Comma,
        CharClass.Space, CharClass.Newline, CharClass.Eof, CharClass.Other,
    };

    private static IEnumerable<CharClass> NonLtClasses() => new[]
    {
        CharClass.Letter, CharClass.Digit, CharClass.Dot, CharClass.Colon,
        CharClass.Lt,
        CharClass.Plus, CharClass.Minus, CharClass.Star, CharClass.Slash,
        CharClass.LParen, CharClass.RParen, CharClass.LBrack, CharClass.RBrack,
        CharClass.Semi, CharClass.Comma,
        CharClass.Space, CharClass.Newline, CharClass.Eof, CharClass.Other,
    };

    private static IEnumerable<CharClass> NonGtClasses() => new[]
    {
        CharClass.Letter, CharClass.Digit, CharClass.Dot, CharClass.Colon,
        CharClass.Lt, CharClass.Gt,
        CharClass.Plus, CharClass.Minus, CharClass.Star, CharClass.Slash,
        CharClass.LParen, CharClass.RParen, CharClass.LBrack, CharClass.RBrack,
        CharClass.Semi, CharClass.Comma,
        CharClass.Space, CharClass.Newline, CharClass.Eof, CharClass.Other,
    };

    private static IEnumerable<CharClass> NonSlashClasses() => new[]
    {
        CharClass.Letter, CharClass.Digit, CharClass.Dot, CharClass.Colon,
        CharClass.Eq, CharClass.Lt, CharClass.Gt,
        CharClass.Plus, CharClass.Minus, CharClass.Star,
        CharClass.LParen, CharClass.RParen, CharClass.LBrack, CharClass.RBrack,
        CharClass.Semi, CharClass.Comma,
        CharClass.Space, CharClass.Newline, CharClass.Eof, CharClass.Other,
    };

    // =========================================================================
    // Таблица ключевых слов.
    // =========================================================================
    private static readonly Dictionary<string, TokenType> Keywords = new()
    {
        ["begin"] = TokenType.Begin,
        ["if"] = TokenType.If,
        ["then"] = TokenType.Then,
        ["else"] = TokenType.Else,
        ["end"] = TokenType.End,
        ["while"] = TokenType.While,
        ["do"] = TokenType.Do,
        ["read"] = TokenType.Read,
        ["write"] = TokenType.Write,
        ["array"] = TokenType.Array,
        // ДОБАВЛЯЕМ НОВЫЕ КЛЮЧЕВЫЕ СЛОВА:
        // Если есть ещё "end" уже есть, "begin" добавлен, "sqrt"/"exp"/"log" — тоже.
    };

    // =========================================================================
    // Классификация символа.
    // =========================================================================
    private static CharClass Classify(int ch)
    {
        if (ch < 0) return CharClass.Eof;
        if (ch == ' ' || ch == '\t') return CharClass.Space;
        if (ch == '\n' || ch == '\r') return CharClass.Newline;
        if (ch == '_' || char.IsLetter((char)ch)) return CharClass.Letter;
        if (char.IsDigit((char)ch)) return CharClass.Digit;
        return (char)ch switch
        {
            '.' => CharClass.Dot,
            ':' => CharClass.Colon,
            '=' => CharClass.Eq,
            '<' => CharClass.Lt,
            '>' => CharClass.Gt,
            '+' => CharClass.Plus,
            '-' => CharClass.Minus,
            '*' => CharClass.Star,
            '/' => CharClass.Slash,
            '(' => CharClass.LParen,
            ')' => CharClass.RParen,
            '[' => CharClass.LBrack,
            ']' => CharClass.RBrack,
            ';' => CharClass.Semi,
            ',' => CharClass.Comma,
            _ => CharClass.Other,
        };
    }

    // =========================================================================
    // Экземплярное состояние.
    // =========================================================================
    private readonly string _src;
    private int _pos;
    private int _line = 1;
    private int _col = 1;

    public Lexer(string source) => _src = source;

    private int Peek() => _pos >= _src.Length ? -1 : _src[_pos];

    private void Advance()
    {
        if (_pos >= _src.Length) return;
        if (_src[_pos] == '\n')
        {
            _line++;
            _col = 1;
        }
        else
        {
            _col++;
        }
        _pos++;
    }

    /// Получить следующий токен, прогоняя автомат по таблице.
    public Token NextToken()
    {
        var cur = LexState.Start;
        var buf = new StringBuilder();
        int startLine = _line, startCol = _col;

        while (true)
        {
            int ch = Peek();
            var cls = Classify(ch);
            var cell = TransitionTable[(int)cur, (int)cls];

            switch (cell.Action)
            {
                case LexAction.Accum:
                    if (cur == LexState.Start)
                    {
                        startLine = _line;
                        startCol = _col;
                    }
                    buf.Append((char)ch);
                    Advance();
                    cur = cell.Next;
                    break;

                case LexAction.Skip:
                    Advance();
                    if (cell.Next == LexState.Start)
                    {
                        buf.Clear();
                        startLine = _line;
                        startCol = _col;
                    }
                    cur = cell.Next;
                    break;

                case LexAction.Emit:
                    return MakeToken(cell.EmitTok, buf, startLine, startCol);

                case LexAction.EmitC:
                    if (buf.Length == 0)
                    {
                        startLine = _line;
                        startCol = _col;
                    }
                    buf.Append((char)ch);
                    Advance();
                    return MakeToken(cell.EmitTok, buf, startLine, startCol);

                case LexAction.Error:
                {
                    string msg = ch < 0
                        ? "неожиданный конец входа"
                        : $"недопустимый символ '{(char)ch}'";
                    if (ch >= 0) Advance();
                    throw new LexException(startLine, startCol, msg);
                }
            }
        }
    }

    private static Token MakeToken(TokenType type, StringBuilder buf, int line, int col)
    {
        string lex = buf.ToString();
        double value = 0;
        if (type == TokenType.Ident && Keywords.TryGetValue(lex, out var kw))
            type = kw;

        if (type == TokenType.Int)
        {
            if (!long.TryParse(lex, NumberStyles.Integer, CultureInfo.InvariantCulture, out var iv))
                throw new LexException(line, col, $"ошибка разбора целого числа: {lex}");
            value = iv;
        }
        else if (type == TokenType.Real)
        {
            if (!double.TryParse(lex, NumberStyles.Float, CultureInfo.InvariantCulture, out var dv))
                throw new LexException(line, col, $"ошибка разбора вещественного числа: {lex}");
            value = dv;
        }

        return new Token(type, lex, value, line, col);
    }

    /// Собрать все токены до EOF (включительно).
    public static List<Token> Tokenize(string source)
    {
        var lex = new Lexer(source);
        var output = new List<Token>();
        while (true)
        {
            var t = lex.NextToken();
            output.Add(t);
            if (t.Type == TokenType.Eof) return output;
        }
    }

    // таблица переходов
    public static void PrintTransitionTable()
    {
        var states = Enum.GetNames(typeof(LexState));
        var classes = Enum.GetNames(typeof(CharClass));

        var html = new System.Text.StringBuilder();
        html.AppendLine("<html><head><meta charset='utf-8'><style>");
        html.AppendLine("table { border-collapse: collapse; }");
        html.AppendLine("td, th { border: 1px solid black; padding: 4px 8px; font-family: monospace; font-size: 12px; white-space: nowrap; }");
        html.AppendLine("th { background-color: #ddd; }");
        html.AppendLine("</style></head><body>");
        html.AppendLine("<table>");

        // Заголовок
        html.AppendLine("<tr><th>State</th>");
        foreach (var c in classes)
            html.AppendLine($"<th>{c}</th>");
        html.AppendLine("</tr>");

        // Строки
        for (int s = 0; s < (int)LexState._Count; s++)
        {
            html.AppendLine("<tr>");
            html.AppendLine($"<td><b>{states[s]}</b></td>");
            for (int ch = 0; ch < (int)CharClass._Count; ch++)
            {
                var cell = TransitionTable[s, ch];
                string desc = cell.Action switch
                {
                    LexAction.Accum => "Acc",
                    LexAction.Emit  => "Emt",
                    LexAction.EmitC => "EmC",
                    LexAction.Skip => "Skp",
                    LexAction.Error => "Err",
                    _ => "?"
                };
                if (cell.Action == LexAction.Emit || cell.Action == LexAction.EmitC)
                    desc += cell.EmitTok.ToString();
                string full = $"{cell.Next},{desc}";
                html.AppendLine($"<td>{full}</td>");
            }
            html.AppendLine("</tr>");
        }

        html.AppendLine("</table></body></html>");

        string path = Path.GetTempFileName() + ".html";
        File.WriteAllText(path, html.ToString(), Encoding.UTF8);
        // Открыть в браузере по умолчанию
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }
    public static void PrintLexemeList()
    {
        Console.WriteLine("Группа,Лексема,Код (ID),Описание");
        // Используем словарь для сопоставления наших TokenType с эталонными кодами
        var map = new Dictionary<TokenType, (int code, string group, string desc)>()
        {
            // Служебные слова
            { TokenType.If, (3, "Служебные слова", "Условный оператор") },
            { TokenType.Then, (4, "Служебные слова", "Ветвь при истинном условии") },
            { TokenType.Else, (5, "Служебные слова", "Ветвь при ложном условии") },
            { TokenType.While, (6, "Служебные слова", "Начало цикла с предусловием") },
            { TokenType.Do, (7, "Служебные слова", "Разделитель между условием и телом цикла") },
            { TokenType.Read, (8, "Служебные слова", "Ввод") },
            { TokenType.Write, (9, "Служебные слова", "Вывод") },
            // Встроенные функции (у нас их нет, но можно оставить заглушки)
            // Идентификаторы
            { TokenType.Ident, (20, "Идентификаторы", "Имена переменных, констант, типов, процедур") },
            // Литералы
            { TokenType.Int, (21, "Литералы", "Целое число (напр. 42)") },
            { TokenType.Real, (22, "Литералы", "Вещественное число (напр. 3.14)") },
            // Операторы
            { TokenType.Assign, (30, "Операторы", "Присваивание") },
            { TokenType.Plus, (31, "Операторы", "Сложение") },
            { TokenType.Minus, (32, "Операторы", "Вычитание") },
            { TokenType.Mul, (33, "Операторы", "Умножение") },
            { TokenType.Div, (34, "Операторы", "Деление") },
            // Сравнения
            { TokenType.Eq, (40, "Сравнения", "Равно") },
            { TokenType.Ne, (41, "Сравнения", "Не равно") },
            { TokenType.Lt, (42, "Сравнения", "Меньше") },
            { TokenType.Le, (43, "Сравнения", "Меньше или равно") },
            { TokenType.Gt, (44, "Сравнения", "Больше") },
            { TokenType.Ge, (45, "Сравнения", "Больше или равно") },
            // Разделители
            { TokenType.LParen, (50, "Разделители", "Открывающая круглая скобка") },
            { TokenType.RParen, (51, "Разделители", "Закрывающая круглая скобка") },
            { TokenType.LBrack, (52, "Разделители", "Открывающая квадратная скобка") },
            { TokenType.RBrack, (53, "Разделители", "Закрывающая квадратная скобка") },
            { TokenType.Semi, (54, "Разделители", "Точка с запятой") },
            // Специальные
            { TokenType.Eof, (99, "Специальные", "Конец файла/строки") },
            // Для Error отдельно
        };

        foreach (var kvp in map)
        {
            var token = kvp.Key;
            var (code, group, desc) = kvp.Value;
            string lexeme = token.Display(); // Наше отображение (например, "IF", ":=", ...)
            Console.WriteLine($"{group},{lexeme},{code},{desc}");
        }

        // Добавляем ERROR (100)
        Console.WriteLine("Специальные,ERROR,100,Лексическая ошибка");
    }
    public static void PrintTransitionTableCsv()
    {
        // Заголовки как в артефакте 2
        Console.WriteLine("Состояние,Буква [a-zA-Z],Цифра [0-9],Точка .,Двоеточие :,Равно =,Меньше <,Больше >,Одиночные символы +-*/()[];,Пробельные символы (WS),EOF,Другое");

        // Маппинг наших состояний на S0, S1...
        var stateNames = new[] {
            "S0 — Начальное состояние.",
            "S1 — Чтение идентификатора.",
            "S2 — Чтение целого числа.",
            "S3 — Прочитана точка в числе (ожидание дробной части).",
            "S4 — Чтение дробной части (вещественное число).",
            "S5 — Прочитано двоеточие (ожидание = для присваивания :=).",
            "S6 — Прочитан знак < (состояние проверки на <=, <>).",
            "S7 — Прочитан знак > (состояние проверки на >=)."
        };

        // Классы символов в порядке столбцов: Letter, Digit, Dot, Colon, Eq, Lt, Gt, (Plus,Minus,Star,Slash,LParen,RParen,LBrack,RBrack,Semi,Comma) -> "ОД", Space, Newline -> "WS", Eof, Other
        var charClasses = new[] {
            CharClass.Letter, CharClass.Digit, CharClass.Dot, CharClass.Colon, CharClass.Eq,
            CharClass.Lt, CharClass.Gt,
            // Одиночные символы — обобщаем
            CharClass.Plus, CharClass.Minus, CharClass.Star, CharClass.Slash,
            CharClass.LParen, CharClass.RParen, CharClass.LBrack, CharClass.RBrack,
            CharClass.Semi, CharClass.Comma,
            CharClass.Space, CharClass.Newline, CharClass.Eof, CharClass.Other
        };

        // Для каждого состояния (0..7) выводим строку
        for (int s = 0; s < 8; s++) // У нас _Count = 10, но S0..S7 соответствуют Start..Slash
        {
            var state = (LexState)s;
            string stateName = s < stateNames.Length ? stateNames[s] : "S" + s;

            Console.Write(stateName + ",");
            foreach (var cc in charClasses)
            {
                var cell = TransitionTable[(int)state, (int)cc];
                string action = cell.Action switch
                {
                    LexAction.Accum => "Accum",
                    LexAction.Emit => $"F({cell.EmitTok})",
                    LexAction.EmitC => $"F({cell.EmitTok})",
                    LexAction.Skip => "Skip",
                    LexAction.Error => "Err",
                    _ => ""
                };
                // Формируем вывод как в артефакте: например "S1" или "*F (21)"
                string nextState = cell.Next.ToString();
                string result = $"{nextState},{action}";
                Console.Write(result + ",");
            }
            Console.WriteLine();
        }
    }
}

