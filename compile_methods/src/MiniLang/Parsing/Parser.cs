using MiniLang.Lexing;

namespace MiniLang.Parsing;

// =============================================================================
// PARSER — магазинный автомат, генерирующий ОПС
// =============================================================================
//
// Алгоритм:
//
//   1. На стек кладётся стартовый символ Program.
//   2. В цикле:
//        - снимаем верхний символ X
//        - если X — терминал и X совпадает с текущим токеном — потребляем токен
//        - если X — нетерминал — по ParseTable[X][curTok] получаем правую
//          часть и кладём её в стек в обратном порядке
//        - если X — семантическое действие — выполняем его
//          (генерируем элемент(ы) ОПС)
//   3. Завершаемся, когда стек пуст.
//
// Параллельно ведётся стек семантических значений semStack — на нём
// сохраняются токены IDENT/INT/REAL/RelOp до момента, когда соответствующее
// действие PushVar/PushNum/OpRel выполнится.
//
// Стек меток labels используется для ветвлений (back-patching).
// =============================================================================

public sealed class Parser
{
    private readonly IReadOnlyList<Token> _tokens;
    private int _pos;
    private readonly Stack<Symbol> _stack = new();
    private readonly Stack<Token> _semStack = new();
    private readonly Stack<int> _labels = new();
    private readonly List<RpnItem> _rpn = new();

    private Parser(IReadOnlyList<Token> tokens) => _tokens = tokens;

    /// <summary>Главная функция. Принимает все токены, возвращает ОПС.</summary>
    public static IReadOnlyList<RpnItem> Parse(IReadOnlyList<Token> tokens)
    {
        var p = new Parser(tokens);
        return p.Run();
    }
    public static void PrintSemanticActionsArtifact() => ParseTable.PrintSemanticActionsArtifact();
    public static void PrintRawGrammar() => ParseTable.PrintRawGrammar();
    public static void PrintArtifactsGrammar() => ParseTable.PrintArtifactsGrammar();
    public static void PrintGreibachCompact() => ParseTable.PrintGreibachCompact();
    public static void PrintLL1TableArtifact() => ParseTable.PrintLL1TableArtifact();
    
    private IReadOnlyList<RpnItem> Run()
    {
        _stack.Push(TokenType.Eof);
        _stack.Push(Nonterm.Program);

        while (_stack.Count > 0)
        {
            var top = _stack.Pop();
            var curTok = Current();

            if (top.IsAction)
            {
                ExecAction(top.AsAction, curTok);
                continue;
            }

            if (top.IsTerminal)
            {
                var tt = top.AsToken;
                if (tt != curTok.Type)
                {
                    throw new ParseException(curTok.Line, curTok.Col,
                        $"ожидался {tt.Display()}, получен {curTok.Type.Display()} (\"{curTok.Lexeme}\")");
                }

                if (NeedsSemValue(tt))
                    _semStack.Push(curTok);

                Advance();
                continue;
            }

            // Нетерминал — берём правило из таблицы.
            if (!ParseTable.TryGet(top.AsNonterm, curTok.Type, out var rule))
            {
                throw new ParseException(curTok.Line, curTok.Col,
                    $"неожиданный {curTok.Type.Display()} (\"{curTok.Lexeme}\") при разборе {top.AsNonterm}");
            }

            // Кладём в обратном порядке.
            for (int i = rule.Length - 1; i >= 0; i--)
                _stack.Push(rule[i]);
        }

        // HALT, чтобы интерпретатор остановился.
        _rpn.Add(RpnItem.MakeOp(OpCode.Halt, default));
        return _rpn;
    }

    private Token Current()
    {
        if (_pos >= _tokens.Count)
            return new Token(TokenType.Eof, "", 0, 0, 0);
        return _tokens[_pos];
    }

    private void Advance() => _pos++;

    private static bool NeedsSemValue(TokenType t) => t switch
    {
        TokenType.Ident or TokenType.Int or TokenType.Real
            or TokenType.Lt or TokenType.Gt or TokenType.Le
            or TokenType.Ge or TokenType.Eq or TokenType.Ne => true,
        _ => false,
    };

    private int Emit(RpnItem item)
    {
        _rpn.Add(item);
        return _rpn.Count - 1;
    }

    // =========================================================================
    // Семантические действия
    // =========================================================================
    private void ExecAction(SemAction act, Token curTok)
    {
        switch (act)
        {
            case SemAction.PushNum:
            {
                var t = _semStack.Pop();
                Emit(RpnItem.MakeNum(t.Value, t));
                break;
            }
            case SemAction.PushVar:
            {
                var t = _semStack.Pop();
                Emit(RpnItem.MakeVar(t.Lexeme, t));
                break;
            }
            case SemAction.OpAdd:    Emit(RpnItem.MakeOp(OpCode.Add, curTok)); break;
            case SemAction.OpSub:    Emit(RpnItem.MakeOp(OpCode.Sub, curTok)); break;
            case SemAction.OpMul:    Emit(RpnItem.MakeOp(OpCode.Mul, curTok)); break;
            case SemAction.OpDiv:    Emit(RpnItem.MakeOp(OpCode.Div, curTok)); break;
            case SemAction.OpNeg:    Emit(RpnItem.MakeOp(OpCode.Neg, curTok)); break;
            case SemAction.OpIndex:  Emit(RpnItem.MakeOp(OpCode.Index, curTok)); break;
            case SemAction.OpRval:   Emit(RpnItem.MakeOp(OpCode.Rval, curTok)); break;
            case SemAction.OpAssign: Emit(RpnItem.MakeOp(OpCode.Assign, curTok)); break;
            case SemAction.OpRead:   Emit(RpnItem.MakeOp(OpCode.Read, curTok)); break;
            case SemAction.OpWrite:  Emit(RpnItem.MakeOp(OpCode.Write, curTok)); break;
            case SemAction.OpDecl:   Emit(RpnItem.MakeOp(OpCode.Decl, curTok)); break;

            case SemAction.OpRel:
            {
                var t = _semStack.Pop();
                Emit(RpnItem.MakeOp(RelOpFor(t.Type), t));
                break;
            }

            // ---- if ----
            case SemAction.IfAfterCond:
            {
                // эмитим L(?) JZ — адрес метки подставится позже
                int m1 = Emit(RpnItem.MakeLbl(-1));
                Emit(RpnItem.MakeOp(OpCode.Jz, curTok));
                _labels.Push(m1);
                break;
            }
            case SemAction.IfAfterThen:
            {
                // есть ELSE: после THEN-блока выдать L(m2) JMP, потом m1 ← здесь
                int m2 = Emit(RpnItem.MakeLbl(-1));
                Emit(RpnItem.MakeOp(OpCode.Jmp, curTok));
                int m1 = _labels.Pop();
                _rpn[m1].Addr = _rpn.Count;
                _labels.Push(m2);
                break;
            }
            case SemAction.IfNoElse:
            {
                int m1 = _labels.Pop();
                _rpn[m1].Addr = _rpn.Count;
                break;
            }
            case SemAction.IfEnd:
            {
                int m2 = _labels.Pop();
                _rpn[m2].Addr = _rpn.Count;
                break;
            }

            // ---- while ----
            case SemAction.WhileBegin:
                _labels.Push(_rpn.Count);
                break;
            case SemAction.WhileAfterCond:
            {
                int end = Emit(RpnItem.MakeLbl(-1));
                Emit(RpnItem.MakeOp(OpCode.Jz, curTok));
                _labels.Push(end);
                break;
            }
            case SemAction.WhileEnd:
            {
                int end = _labels.Pop();
                int begin = _labels.Pop();
                Emit(RpnItem.MakeLbl(begin));
                Emit(RpnItem.MakeOp(OpCode.Jmp, curTok));
                _rpn[end].Addr = _rpn.Count;
                break;
            }

            default:
                throw new InvalidOperationException($"неизвестное семантическое действие {act}");
        }
    }

    private static OpCode RelOpFor(TokenType t) => t switch
    {
        TokenType.Lt => OpCode.Lt,
        TokenType.Gt => OpCode.Gt,
        TokenType.Le => OpCode.Le,
        TokenType.Ge => OpCode.Ge,
        TokenType.Eq => OpCode.Eq,
        TokenType.Ne => OpCode.Ne,
        _ => OpCode.Eq,
    };
}
