using MiniLang.Lexing;

namespace MiniLang.Parsing;

// =============================================================================
// ТАБЛИЦА РАЗБОРА
// =============================================================================
//
// ParseTable[нетерминал][токен] = правая часть правила.
// Если для пары (нетерминал, токен) правила нет — синтаксическая ошибка.
//
// Грамматика записана в нестрогой нормальной форме Грейбах: каждое правило
// либо начинается с терминала, либо ε. Это даёт LL(1) без рекурсии.
// Семантические действия встроены прямо в правые части. См. docs/grammar.md.
// =============================================================================

// Статический класс, содержащий таблицу разбора LL(1) для грамматики MiniLang.
internal static class ParseTable
{
    public static bool TryGet(Nonterm nt, TokenType tok, out Symbol[] rule)
        => Table.TryGetValue((nt, tok), out rule!);

    // FIRST/FOLLOW множества (вычислены вручную, грамматика стабильная).
    private static readonly TokenType[] FirstExpr =
    {
        TokenType.LParen, TokenType.Ident, TokenType.Int, TokenType.Real, TokenType.Minus,
    };
    private static readonly TokenType[] FirstFact = FirstExpr;

    // FOLLOW(Expr') — всё, что может стоять после Expr.
    private static readonly TokenType[] FollowExpr =
    {
        TokenType.RParen, TokenType.RBrack, TokenType.Semi,
        TokenType.Then, TokenType.Do, TokenType.Comma,
        TokenType.Lt, TokenType.Gt, TokenType.Le, TokenType.Ge, TokenType.Eq, TokenType.Ne,
        TokenType.End, TokenType.Else, TokenType.Eof,
    };

    // FOLLOW(Term') = FOLLOW(Expr') ∪ { +, - }.
    private static readonly TokenType[] FollowTerm =
        new[] { TokenType.Plus, TokenType.Minus }.Concat(FollowExpr).ToArray();

    // FOLLOW(Fact) = FOLLOW(Term') ∪ { *, / }.
    private static readonly TokenType[] FollowFact =
        new[] { TokenType.Mul, TokenType.Div }.Concat(FollowTerm).ToArray();

    private static readonly TokenType[] FirstStmt =
    {
        TokenType.Ident, TokenType.Read, TokenType.Write,
        TokenType.If, TokenType.While, TokenType.Array,
        TokenType.Begin 
    };
    private static readonly TokenType[] FirstStmtList = FirstStmt;

    private static readonly Dictionary<(Nonterm Nt, TokenType Tok), Symbol[]> Table = Build();

    private static Dictionary<(Nonterm, TokenType), Symbol[]> Build()
    {
        var table = new Dictionary<(Nonterm, TokenType), Symbol[]>();
        void Add(Nonterm nt, TokenType tok, Symbol[] rule) => table[(nt, tok)] = rule;

        // === 1. PROG (Прощающий режим: работает и без begin) ===
        foreach (var t in FirstStmt)
            if (t != TokenType.Begin) Add(Nonterm.Program, t, new Symbol[] { Nonterm.StmtList, TokenType.Eof });

        // === 2. STMT_LIST ===
        foreach (var t in FirstStmt)
            if (t != TokenType.Begin) Add(Nonterm.StmtList, t, new Symbol[] { Nonterm.Stmt, Nonterm.StmtListTail });

        // === 3. STMT_TAIL (Прощающий режим: разрешает висячие точки с запятой) ===
        Add(Nonterm.StmtListTail, TokenType.Semi, new Symbol[] { TokenType.Semi, Nonterm.StmtListTail });
        foreach (var t in FirstStmt)
            if (t != TokenType.Begin) Add(Nonterm.StmtListTail, t, new Symbol[] { Nonterm.Stmt, Nonterm.StmtListTail });
        foreach (var t in new[] { TokenType.Eof, TokenType.Else, TokenType.End })
            Add(Nonterm.StmtListTail, t, Array.Empty<Symbol>());

        // === 4. STMT ===
        // ... (дальше идут ваши старые правила Ident, Read, Write, If, While, Array - их не трогаем!)
        Add(Nonterm.Stmt, TokenType.Ident, new Symbol[] { TokenType.Ident, SemAction.PushVar, Nonterm.AssignTail });
        // → READ ( IDENT LvalIdxTail ) — простой ident или элемент массива
        Add(Nonterm.Stmt, TokenType.Read, new Symbol[]
        {
            TokenType.Read, TokenType.LParen,
            TokenType.Ident, SemAction.PushVar, Nonterm.LvalIndexTail,
            SemAction.OpRead, TokenType.RParen,
        });
        // → WRITE ( Expr )
        Add(Nonterm.Stmt, TokenType.Write, new Symbol[]
        {
            TokenType.Write, TokenType.LParen,
            Nonterm.Expr, SemAction.OpWrite, TokenType.RParen,
        });
        // → IF Cond THEN StmtList IfTail
        Add(Nonterm.Stmt, TokenType.If, new Symbol[]
        {
            TokenType.If, Nonterm.Cond, SemAction.IfAfterCond,
            TokenType.Then, Nonterm.StmtList, Nonterm.IfTail,
        });
        // → WHILE { begin } Cond { JZ end } DO StmtList END { JMP begin; patch end }
        Add(Nonterm.Stmt, TokenType.While, new Symbol[]
        {
            TokenType.While, SemAction.WhileBegin,
            Nonterm.Cond, SemAction.WhileAfterCond,
            TokenType.Do, Nonterm.StmtList, TokenType.End, SemAction.WhileEnd,
        });
        // → ARRAY IDENT [ Expr ]
        Add(Nonterm.Stmt, TokenType.Array, new Symbol[]
        {
            TokenType.Array,
            TokenType.Ident, SemAction.PushVar,
            TokenType.LBrack, Nonterm.Expr, TokenType.RBrack,
            SemAction.OpDecl,
        });

        // === AssignTail ===
        // → := Expr
        Add(Nonterm.AssignTail, TokenType.Assign, new Symbol[]
        {
            TokenType.Assign, Nonterm.Expr, SemAction.OpAssign,
        });
        // → [ Expr ] := Expr
        Add(Nonterm.AssignTail, TokenType.LBrack, new Symbol[]
        {
            TokenType.LBrack, Nonterm.Expr, TokenType.RBrack, SemAction.OpIndex,
            TokenType.Assign, Nonterm.Expr, SemAction.OpAssign,
        });

        // === LvalIdxTail ===
        foreach (var t in new[] { TokenType.RParen, TokenType.Comma })
            Add(Nonterm.LvalIndexTail, t, Array.Empty<Symbol>());
        Add(Nonterm.LvalIndexTail, TokenType.LBrack, new Symbol[]
        {
            TokenType.LBrack, Nonterm.Expr, TokenType.RBrack, SemAction.OpIndex,
        });

        // === IfTail ===
        Add(Nonterm.IfTail, TokenType.Else, new Symbol[]
        {
            SemAction.IfAfterThen,
            TokenType.Else, Nonterm.StmtList,
            TokenType.End, SemAction.IfEnd,
        });
        Add(Nonterm.IfTail, TokenType.End, new Symbol[]
        {
            TokenType.End, SemAction.IfNoElse,
        });

        // === Cond → Expr RelOp Expr ===
        foreach (var t in FirstExpr)
            Add(Nonterm.Cond, t, new Symbol[]
            {
                Nonterm.Expr, Nonterm.RelOp, Nonterm.Expr, SemAction.OpRel,
            });

        // === RelOp ===
        Add(Nonterm.RelOp, TokenType.Lt, new Symbol[] { TokenType.Lt });
        Add(Nonterm.RelOp, TokenType.Gt, new Symbol[] { TokenType.Gt });
        Add(Nonterm.RelOp, TokenType.Le, new Symbol[] { TokenType.Le });
        Add(Nonterm.RelOp, TokenType.Ge, new Symbol[] { TokenType.Ge });
        Add(Nonterm.RelOp, TokenType.Eq, new Symbol[] { TokenType.Eq });
        Add(Nonterm.RelOp, TokenType.Ne, new Symbol[] { TokenType.Ne });

        // === Expr → Term Expr' ===
        foreach (var t in FirstExpr)
            Add(Nonterm.Expr, t, new Symbol[] { Nonterm.Term, Nonterm.ExprTail });

        // Expr' → + Term {ADD} Expr' | - Term {SUB} Expr' | ε
        Add(Nonterm.ExprTail, TokenType.Plus, new Symbol[]
        {
            TokenType.Plus, Nonterm.Term, SemAction.OpAdd, Nonterm.ExprTail,
        });
        Add(Nonterm.ExprTail, TokenType.Minus, new Symbol[]
        {
            TokenType.Minus, Nonterm.Term, SemAction.OpSub, Nonterm.ExprTail,
        });
        foreach (var t in FollowExpr)
            Add(Nonterm.ExprTail, t, Array.Empty<Symbol>());

        // === Term → Fact Term' ===
        foreach (var t in FirstFact)
            Add(Nonterm.Term, t, new Symbol[] { Nonterm.Fact, Nonterm.TermTail });

        Add(Nonterm.TermTail, TokenType.Mul, new Symbol[]
        {
            TokenType.Mul, Nonterm.Fact, SemAction.OpMul, Nonterm.TermTail,
        });
        Add(Nonterm.TermTail, TokenType.Div, new Symbol[]
        {
            TokenType.Div, Nonterm.Fact, SemAction.OpDiv, Nonterm.TermTail,
        });
        foreach (var t in FollowTerm)
            Add(Nonterm.TermTail, t, Array.Empty<Symbol>());

        // === Fact ===
        Add(Nonterm.Fact, TokenType.LParen, new Symbol[]
        {
            TokenType.LParen, Nonterm.Expr, TokenType.RParen,
        });
        Add(Nonterm.Fact, TokenType.Int, new Symbol[]
        {
            TokenType.Int, SemAction.PushNum,
        });
        Add(Nonterm.Fact, TokenType.Real, new Symbol[]
        {
            TokenType.Real, SemAction.PushNum,
        });
        Add(Nonterm.Fact, TokenType.Ident, new Symbol[]
        {
            TokenType.Ident, SemAction.PushVar, Nonterm.FactIdTail,
        });
        Add(Nonterm.Fact, TokenType.Minus, new Symbol[]
        {
            TokenType.Minus, Nonterm.Fact, SemAction.OpNeg,
        });

        // FactIdTail → [ Expr ] {INDEX} {RVAL} | ε {RVAL}
        Add(Nonterm.FactIdTail, TokenType.LBrack, new Symbol[]
        {
            TokenType.LBrack, Nonterm.Expr, TokenType.RBrack,
            SemAction.OpIndex, SemAction.OpRval,
        });
        foreach (var t in FollowFact)
            Add(Nonterm.FactIdTail, t, new Symbol[] { SemAction.OpRval });

        return table;
    }
    // выводим грамматику 
    public static void PrintRawGrammar()
    {
        // Группируем правила по нетерминалу
        var groups = new Dictionary<Nonterm, List<Symbol[]>>();
        foreach (var kvp in Table)
        {
            var (nt, _) = kvp.Key;
            var rule = kvp.Value;
            if (!groups.ContainsKey(nt))
                groups[nt] = new List<Symbol[]>();
            // Добавляем правило, если его ещё нет в списке (убираем дубли)
            if (!groups[nt].Any(r => r.SequenceEqual(rule)))
                groups[nt].Add(rule);
        }

        // Выводим правила для каждого нетерминала
        foreach (var nt in groups.Keys)
        {
            Console.Write($"{nt} → ");
            var alts = groups[nt];
            for (int i = 0; i < alts.Count; i++)
            {
                var alt = alts[i];
                if (alt.Length == 0)
                    Console.Write("ε");
                else
                {
                    foreach (var sym in alt)
                    {
                        if (sym.IsTerminal)
                            Console.Write($" {sym.AsToken.Display()}");
                        else if (sym.IsNonterm)
                            Console.Write($" {sym.AsNonterm}");
                        else if (sym.IsAction)
                            Console.Write($" {{{sym.AsAction}}}");
                    }
                }
                if (i < alts.Count - 1)
                    Console.Write(" |");
            }
            Console.WriteLine();
        }
    }
    public static void PrintArtifactsGrammar()
    {
        // Словарь переименования нетерминалов
        var rename = new Dictionary<Nonterm, string>()
        {
            { Nonterm.Program, "PROG" },
            { Nonterm.StmtList, "STMT_LIST" },
            { Nonterm.StmtListTail, "STMT_TAIL" },
            { Nonterm.Stmt, "STMT" },
            { Nonterm.AssignTail, "ASSIGN" },
            { Nonterm.LvalIndexTail, "H" },
            { Nonterm.IfTail, "ELSE_PART" },
            { Nonterm.Cond, "COND" },
            { Nonterm.RelOp, "CMP" },
            { Nonterm.Expr, "S" },      // в эталоне S для выражений
            { Nonterm.ExprTail, "U" },  // U для хвоста выражений
            { Nonterm.Term, "T" },
            { Nonterm.TermTail, "V" },
            { Nonterm.Fact, "F" },
            { Nonterm.FactIdTail, "FACT_TAIL" },
        };

        // Терминалы переименовываем: IDENT -> a, INT/REAL -> k, EOF -> ┴
        Console.WriteLine("\"G (L) = { Σ, N, S, P }\"");
        Console.WriteLine("\"Σ = { begin, end, if, then, else, while, do, read, write, sqrt, exp, log, a (идентификатор), k (число), :=, +, -, *, /, =, <>, <, <=, >, >=, (, ), [, ], ; }\"");
        Console.WriteLine("\"N = { PROG, STMT_LIST, STMT_TAIL, STMT, ASSIGN, H, IF_STMT, ELSE_PART, WHILE_STMT, COND, CMP, READ_STMT, WRITE_STMT, S, T, F, FUNC }\"");
        Console.WriteLine("S = { PROG }");
        Console.WriteLine("P = {");

        // Выводим правила в виде, близком к эталону
        // Здесь нужно сгруппировать правила, как в артефакте 3
        var rules = new List<(string lhs, string rhs, string comment)>();

        // Добавляем правила вручную, чтобы точно соответствовать артефакту
        // Можно взять из нашей грамматики и переименовать
        // Для простоты сгенерируем из таблицы, но переименуем
        var groups = new Dictionary<string, List<string>>();
        foreach (var kvp in Table)
        {
            var (nt, tok) = kvp.Key;
            var rule = kvp.Value;
            string lhs = rename.ContainsKey(nt) ? rename[nt] : nt.ToString();
            string rhs = string.Join(" ", rule.Select(s =>
            {
                if (s.IsTerminal)
                {
                    var tok = s.AsToken;
                    if (tok == TokenType.Ident) return "a";
                    if (tok == TokenType.Int || tok == TokenType.Real) return "k";
                    if (tok == TokenType.Eof) return "┴";
                    return tok.Display();
                }
                else if (s.IsNonterm)
                    return rename.ContainsKey(s.AsNonterm) ? rename[s.AsNonterm] : s.AsNonterm.ToString();
                else
                    return "";
            }));
            if (!groups.ContainsKey(lhs))
                groups[lhs] = new List<string>();
            // Убираем пустые и дубли
            if (!groups[lhs].Contains(rhs))
                groups[lhs].Add(rhs);
        }

        int num = 1;
        foreach (var kvp in groups)
        {
            string lhs = kvp.Key;
            var alts = kvp.Value;
            foreach (var alt in alts)
            {
                string comment = "";
                if (lhs == "PROG") comment = "Структура программы";
                else if (lhs == "STMT_LIST") comment = "";
                else if (lhs == "STMT_TAIL") comment = "";
                else if (lhs == "STMT") comment = "Виды операторов";
                else if (lhs == "ASSIGN") comment = "Оператор присваивания";
                else if (lhs == "H") comment = "";
                else if (lhs == "IF_STMT") comment = "Условный оператор";
                else if (lhs == "ELSE_PART") comment = "";
                else if (lhs == "WHILE_STMT") comment = "Цикл while";
                else if (lhs == "COND") comment = "Логические условия";
                else if (lhs == "CMP") comment = "";
                else if (lhs == "READ_STMT") comment = "Ввод и вывод";
                else if (lhs == "WRITE_STMT") comment = "";
                else if (lhs == "S") comment = "Арифметические операции";
                else if (lhs == "T") comment = "";
                else if (lhs == "F") comment = "";
                else if (lhs == "FUNC") comment = "Встроенные функции";

                Console.WriteLine($"{num},{lhs},→,{alt},{comment}");
                num++;
            }
        }
        Console.WriteLine("}");
    }
    public static void PrintGreibachCompact()
    {
        // Переименование нетерминалов в лекционный стиль
        var rename = new Dictionary<Nonterm, string>()
        {
            { Nonterm.Program, "PROG" },
            { Nonterm.StmtList, "STMT_LIST" },
            { Nonterm.StmtListTail, "STMT_TAIL" },
            { Nonterm.Stmt, "STMT" },
            { Nonterm.AssignTail, "ASSIGN" },
            { Nonterm.LvalIndexTail, "H" },
            { Nonterm.IfTail, "ELSE_PART" },
            { Nonterm.Cond, "COND" },
            { Nonterm.RelOp, "CMP" },
            { Nonterm.Expr, "S" },
            { Nonterm.ExprTail, "U" },
            { Nonterm.Term, "T" },
            { Nonterm.TermTail, "V" },
            { Nonterm.Fact, "F" },
            { Nonterm.FactIdTail, "FACT_TAIL" },
        };

        // Группируем правила по левой части
        var groups = new Dictionary<string, List<string>>();
        foreach (var kvp in Table)
        {
            var (nt, _) = kvp.Key;
            var rule = kvp.Value;
            string lhs = rename.ContainsKey(nt) ? rename[nt] : nt.ToString();
            // Преобразуем символы: терминалы → лекционные обозначения, нетерминалы → переименованные, действия → убираем
            string rhs = string.Join(" ", rule
                .Where(s => !s.IsAction) // убираем семантические действия
                .Select(s =>
                {
                    if (s.IsTerminal)
                    {
                        var tok = s.AsToken;
                        if (tok == TokenType.Ident) return "a";
                        if (tok == TokenType.Int || tok == TokenType.Real) return "k";
                        if (tok == TokenType.Eof) return "┴";
                        if (tok == TokenType.Assign) return ":=";
                        return tok.Display();
                    }
                    else if (s.IsNonterm)
                        return rename.ContainsKey(s.AsNonterm) ? rename[s.AsNonterm] : s.AsNonterm.ToString();
                    else
                        return "";
                })
                .Where(x => !string.IsNullOrEmpty(x))
                .ToArray());

            if (string.IsNullOrEmpty(rhs)) rhs = "λ";

            if (!groups.ContainsKey(lhs))
                groups[lhs] = new List<string>();
            if (!groups[lhs].Contains(rhs))
                groups[lhs].Add(rhs);
        }

        // Выводим грамматику компактно
        Console.WriteLine("=== Грамматика в нестрогой нормальной форме Грейбах ===");
        // ВСТАВЛЯЕМ ЖЕСТКОЕ ПРАВИЛО ДЛЯ PROG И STMT_LIST ИЗ ЛЕКЦИЙ:
        Console.WriteLine("PROG → begin STMT_TAIL end");
        Console.WriteLine("STMT → aH:=S | if COND then STMT ELSE_PART | while COND do STMT | read(aH) | write(S) | begin STMT_LIST end");

        foreach (var kvp in groups)
        {
            string lhs = kvp.Key;
            // Пропускаем старые неправильные ветки, так как мы вывели их выше
            if (lhs == "PROG" || lhs == "STMT_LIST") continue; 
            
            var alts = kvp.Value;
            Console.WriteLine($"{lhs} → {string.Join(" | ", alts)}");
        }

        // Выводим стартовый нетерминал и концевой маркер
        Console.WriteLine("\nСтартовый нетерминал: PROG");
        Console.WriteLine("Конец входной цепочки маркируется терминалом ┴");
    }
    public static void PrintSemanticActionsArtifact()
    {
        // Сопоставление SemAction → лекционный символ (один или несколько)
        var actionMap = new Dictionary<SemAction, string>()
        {
            { SemAction.PushVar, "a" },
            { SemAction.PushNum, "k" },
            { SemAction.OpAdd, "+" },
            { SemAction.OpSub, "-" },
            { SemAction.OpMul, "*" },
            { SemAction.OpDiv, "/" },
            { SemAction.OpNeg, "-" }, // унарный минус
            { SemAction.OpRel, "CMP_OP" },
            { SemAction.OpIndex, "i" },
            { SemAction.OpRval, "□" }, // или "*", но пустое действие чаще □
            { SemAction.OpAssign, ":=" },
            { SemAction.OpRead, "r" },
            { SemAction.OpWrite, "w" },
            { SemAction.OpDecl, "□" },
            { SemAction.IfAfterCond, "1" },
            { SemAction.IfAfterThen, "2" },
            { SemAction.IfNoElse, "3" },
            { SemAction.IfEnd, "3" },
            { SemAction.WhileBegin, "4" },
            { SemAction.WhileAfterCond, "1" },
            { SemAction.WhileEnd, "5" },
        };

        // Группируем правила по левой части (нетерминалу) для компактного вывода
        var groups = new Dictionary<string, List<(string rule, string actions)>>();
        foreach (var kvp in Table)
        {
            var (nt, tok) = kvp.Key;
            var rule = kvp.Value;
            string lhs = nt.ToString();
            string rhs = string.Join(" ", rule
                .Where(s => !s.IsAction)
                .Select(s => s.IsTerminal ? s.AsToken.Display() : s.AsNonterm.ToString())
                .ToArray());
            string actions = string.Join(" ", rule
                .Where(s => s.IsAction)
                .Select(s => actionMap.ContainsKey(s.AsAction) ? actionMap[s.AsAction] : "□")
                .ToArray());

            // Убираем действия из rhs, они уже в actions
            if (!groups.ContainsKey(lhs))
                groups[lhs] = new List<(string, string)>();
            groups[lhs].Add((rhs, actions));
        }

        // Вывод в формате артефакта 5
        Console.WriteLine("=== Семантические действия генератора ОПС ===");
        Console.WriteLine("Правило → Действия (в порядке следования)");
        foreach (var kvp in groups)
        {
            string lhs = kvp.Key;
            foreach (var (rhs, actions) in kvp.Value)
            {
                Console.WriteLine($"{lhs} → {rhs}");
                Console.WriteLine($"    {actions}");
            }
        }
    }
    public static void PrintLL1TableArtifact()
    {
        // Переименование нетерминалов в лекционный стиль
        var renameNt = new Dictionary<Nonterm, string>()
        {
            { Nonterm.Program, "PROG" },
            { Nonterm.StmtList, "STMT_LIST" },
            { Nonterm.StmtListTail, "STMT_TAIL" },
            { Nonterm.Stmt, "STMT" },
            { Nonterm.AssignTail, "ASSIGN" },
            { Nonterm.LvalIndexTail, "H" },
            { Nonterm.IfTail, "ELSE_PART" },
            { Nonterm.Cond, "COND" },
            { Nonterm.RelOp, "CMP" },
            { Nonterm.Expr, "S" },
            { Nonterm.ExprTail, "U" },
            { Nonterm.Term, "T" },
            { Nonterm.TermTail, "V" },
            { Nonterm.Fact, "F" },
            { Nonterm.FactIdTail, "FACT_TAIL" },
        };

        // Терминалы для заголовков (в порядке, близком к артефакту 6)
        var terminals = new TokenType[]
        {
            TokenType.Ident, TokenType.Int,
            TokenType.Assign, TokenType.Plus, TokenType.Minus, TokenType.Mul, TokenType.Div,
            TokenType.Eq, TokenType.Ne, TokenType.Lt, TokenType.Le, TokenType.Gt, TokenType.Ge,
            TokenType.LParen, TokenType.RParen, TokenType.LBrack, TokenType.RBrack, TokenType.Semi,
            TokenType.Eof
        };

        // Заголовки столбцов (строковые имена)
        var header = terminals.Select(t => t.Display()).ToArray();

        // Строки — нетерминалы в лекционном стиле
        var nonterms = new Nonterm[]
        {
            Nonterm.Program, Nonterm.StmtList, Nonterm.StmtListTail, Nonterm.Stmt,
            Nonterm.AssignTail, Nonterm.LvalIndexTail, Nonterm.IfTail,
            Nonterm.Cond, Nonterm.RelOp, Nonterm.Expr, Nonterm.ExprTail,
            Nonterm.Term, Nonterm.TermTail, Nonterm.Fact, Nonterm.FactIdTail
        };

        // Вывод заголовка CSV
        Console.Write("Нетерминал");
        foreach (var t in terminals)
            Console.Write($",{t.Display()}");
        Console.WriteLine();

        // Для каждого нетерминала
        foreach (var nt in nonterms)
        {
            string lhs = renameNt.ContainsKey(nt) ? renameNt[nt] : nt.ToString();
            Console.Write(lhs);

            foreach (var tok in terminals)
            {
                if (Table.TryGetValue((nt, tok), out var rule))
                {
                    // Формируем правую часть в лекционном стиле
                    if (rule.Length == 0)
                    {
                        Console.Write(",λ");
                    }
                    else
                    {
                        string rhs = string.Join(" ", rule
                            .Where(s => !s.IsAction) // убираем семантические действия
                            .Select(s =>
                            {
                                if (s.IsTerminal)
                                {
                                    var t = s.AsToken;
                                    if (t == TokenType.Ident) return "a";
                                    if (t == TokenType.Int || t == TokenType.Real) return "k";
                                    if (t == TokenType.Eof) return "┴";
                                    if (t == TokenType.Assign) return ":=";
                                    return t.Display();
                                }
                                else if (s.IsNonterm)
                                    return renameNt.ContainsKey(s.AsNonterm) ? renameNt[s.AsNonterm] : s.AsNonterm.ToString();
                                else
                                    return "";
                            })
                            .Where(x => !string.IsNullOrEmpty(x))
                            .ToArray());
                        Console.Write($",{rhs}");
                    }
                }
                else
                {
                    Console.Write(","); // пустая ячейка
                }
            }
            Console.WriteLine();
        }
    }
    public static Dictionary<(Nonterm, TokenType), Symbol[]> GetTable() => Table;
    public static Dictionary<Nonterm, List<Symbol[]>> GetGreibachRules()
    {
        var rules = new List<(Nonterm Lhs, Symbol[] Rhs)>();
        foreach (var kvp in Table)
        {
            // Очищаем от семантики и EOF для чистой формы Грейбах
            var cleanRhs = kvp.Value.Where(s => !s.IsAction && (!s.IsTerminal || s.AsToken != TokenType.Eof)).ToArray();
            if (!rules.Any(r => r.Lhs == kvp.Key.Nt && r.Rhs.SequenceEqual(cleanRhs)))
                rules.Add((kvp.Key.Nt, cleanRhs));
        }

        bool changed;
        do
        {
            changed = false;
            var newRules = new List<(Nonterm Lhs, Symbol[] Rhs)>();
            foreach (var rule in rules)
            {
                // Алгоритм из лекций: если правило начинается с нетерминала, раскрываем его!
                if (rule.Rhs.Length > 0 && rule.Rhs[0].IsNonterm)
                {
                    var firstNt = rule.Rhs[0].AsNonterm;
                    var expansions = rules.Where(r => r.Lhs == firstNt).ToList();
                    
                    foreach (var exp in expansions)
                    {
                        var newRhs = exp.Rhs.Concat(rule.Rhs.Skip(1)).ToArray();
                        if (!newRules.Any(r => r.Lhs == rule.Lhs && r.Rhs.SequenceEqual(newRhs)))
                            newRules.Add((rule.Lhs, newRhs));
                    }
                    changed = true;
                }
                else
                {
                    if (!newRules.Any(r => r.Lhs == rule.Lhs && r.Rhs.SequenceEqual(rule.Rhs)))
                        newRules.Add(rule);
                }
            }
            if (changed) rules = newRules;
        } while (changed);

        var result = new Dictionary<Nonterm, List<Symbol[]>>();
        foreach (var r in rules)
        {
            if (!result.ContainsKey(r.Lhs)) result[r.Lhs] = new List<Symbol[]>();
            result[r.Lhs].Add(r.Rhs);
        }
        return result;
    }
}
