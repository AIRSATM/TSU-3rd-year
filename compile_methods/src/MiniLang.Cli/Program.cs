using System.Globalization;
using System.Text;
using MiniLang.Interpreting;
using MiniLang.Lexing;
using MiniLang.Parsing;
using MiniLang.Utils;

namespace MiniLang.Cli;

/// <summary>
/// minilang — учебный транслятор-интерпретатор языка MiniLang.
///
/// Использование:
///   minilang &lt;файл.ml&gt;             — выполнить программу
///   minilang --tokens &lt;файл.ml&gt;    — показать список лексем
///   minilang --rpn    &lt;файл.ml&gt;    — показать ОПС
///   minilang --all    &lt;файл.ml&gt;    — показать всё и выполнить
/// </summary>
internal static class Program
{
    private static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        bool showTokens = false, showRpn = false, showAll = false;
        string? path = null;

        foreach (var a in args)
        {
            switch (a)
            {
                // Основные режимы
                case "-tokens": case "--tokens": showTokens = true; break;
                case "-rpn":    case "--rpn":    showRpn = true; break;
                case "-all":    case "--all":    showAll = true; break;
                case "-h":      case "--help":   Usage(); return 0;

                // Теоретические описания (Консоль)
                case "--lex-table":   Lexer.PrintTransitionTableCsv(); return 0;
                case "--grammar":     Parser.PrintArtifactsGrammar(); return 0;
                case "--raw-grammar": Parser.PrintRawGrammar(); return 0;
                case "--sem-actions": Parser.PrintSemanticActionsArtifact(); return 0;
                case "--greibach":    Parser.PrintGreibachCompact(); return 0;
                case "--opcodes":
                    foreach (OpCode op in Enum.GetValues(typeof(OpCode)))
                        Console.WriteLine($"{op} -> {op.Display()}");
                    return 0;

                // Артефакты (CSV форматы для отчета)
                case "--artifacts-lexer":      Lexer.PrintLexemeList(); return 0;
                case "--artifacts-transition": Lexer.PrintTransitionTableCsv(); return 0;
                case "--artifacts-ops":
                    Console.WriteLine("Синтаксис,Операция,Действие");
                    foreach (OpCode op in Enum.GetValues(typeof(OpCode)))
                        Console.WriteLine($"{op.Display()},{op},Действие операции"); // Для краткости
                    return 0;

                // Артефакты (HTML)
                case "--artifacts-lexer-html":     ArtifactPrinter.PrintLexemeListHtml(); return 0;
                case "--artifacts-grammar-html":   ArtifactPrinter.PrintGrammarHtml(); return 0;
                case "--artifacts-greibach-html":  ArtifactPrinter.PrintGreibachHtml(); return 0;
                case "--artifacts-semantics-html": ArtifactPrinter.PrintSemanticActionsHtml(); return 0;
                case "--artifacts-lltable-html":   ArtifactPrinter.PrintLL1TableHtml(); return 0;
                case "--artifacts-ops-html":       ArtifactPrinter.PrintOpsHtml(); return 0;

                // Обработка имени файла
                default:
                    if (!a.StartsWith("-"))
                    {
                        path = a;
                    }
                    else
                    {
                        Console.Error.WriteLine($"Неизвестный флаг: {a}");
                        Usage();
                        return 2;
                    }
                    break;
            }
        }

        if (path is null)
        {
            Usage();
            return 2;
        }

        string source;
        try
        {
            source = File.ReadAllText(path);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Не удалось прочитать {path}: {e.Message}");
            return 1;
        }

        try
        {
            // 1) ЛЕКСИЧЕСКИЙ АНАЛИЗ
            var tokens = Lexer.Tokenize(source);
            if (showTokens || showAll) PrintTokens(tokens);
            if (showTokens) return 0;

            // 2) СИНТАКСИЧЕСКИЙ АНАЛИЗ + ГЕНЕРАЦИЯ ОПС
            var rpn = Parser.Parse(tokens);
            if (showRpn || showAll)
            {
                Console.WriteLine("=== ОПС (вертикальный формат) ===");
                Console.Write(RpnFormatter.Format(rpn));

                Console.WriteLine("\n=== ОПС (горизонтальный формат) ===");
                Console.WriteLine(RpnFormatter.FormatHorizontal(rpn));
            }
            if (showRpn) return 0;

            if (showAll) Console.WriteLine("=== ВЫПОЛНЕНИЕ ===");

            // 3) ИНТЕРПРЕТАЦИЯ
            Interpreter.Run(rpn, Console.In, Console.Out);
            return 0;
        }
        catch (LexException e)    { Console.Error.WriteLine(e.Message); return 1; }
        catch (ParseException e)  { Console.Error.WriteLine(e.Message); return 1; }
        catch (RuntimeException e) { Console.Error.WriteLine(e.Message); return 1; }
    }

    private static void PrintTokens(IReadOnlyList<Token> tokens)
    {
        Console.WriteLine("=== ЛЕКСЕМЫ ===");
        Console.WriteLine($"{"№",-4}  {"тип",-8}  {"значение",-12}  позиция");
        for (int i = 0; i < tokens.Count; i++)
        {
            var t = tokens[i];
            string val = (t.Type == TokenType.Int || t.Type == TokenType.Real)
                ? t.Value.ToString("G", CultureInfo.InvariantCulture)
                : t.Lexeme;
            Console.WriteLine($"{i,-4}  {t.Type.Display(),-8}  {val,-12}  стр {t.Line}, кол {t.Col}");
        }
    }

    private static void Usage()
    {
        Console.Error.WriteLine(@"MiniLang — транслятор-интерпретатор (порт на C#).

    Использование:
    minilang [флаги] <файл.ml>

    Флаги:
    --tokens                 только лексический анализ — печать списка лексем
    --rpn                    печать сгенерированной ОПС (без выполнения)
    --all                    печать лексем + ОПС + выполнение
    --lex-table              показать таблицу переходов
    --grammar                показать грамматики
    --raw-grammar            показать чистую КС-грамматику (без привязки к токенам)
    --sem-actions            показать семантические действия
    --greibach               показать грамматику в форме Грейбах
    --opcodes                показать список операций ОПС
    --artifacts-lexer-html   список лексем (HTML)
    --artifacts-grammar-html КС-грамматика (HTML)
    --artifacts-greibach-html КС-грамматика в форме Грейбах (HTML)
    --artifacts-semantics-html семантические действия (HTML)
    --artifacts-lltable-html LL(1)-таблица (HTML)
    --artifacts-ops-html     список операций ОПС (HTML)

    Примеры:
    minilang program.ml
    echo ""10 32"" | minilang program.ml
    minilang --all program.ml");
    }
}