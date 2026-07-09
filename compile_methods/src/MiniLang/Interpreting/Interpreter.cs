using System.Globalization;
using MiniLang.Parsing;

namespace MiniLang.Interpreting;

// =============================================================================
// ИНТЕРПРЕТАТОР ОПС
// =============================================================================
//
// Интерпретатор последовательно проходит по массиву RpnItem и поддерживает
// стек значений. Значения на стеке могут быть:
//   - числом (double)
//   - lvalue (ссылкой на переменную) — для := и READ
//   - lvalue-addr (ссылкой на элемент массива arr[i])
// =============================================================================

/// <summary>Ошибка времени выполнения с указанием строки/колонки.</summary>
public sealed class RuntimeException : Exception
{
    public int Line { get; }
    public int Col { get; }

    public RuntimeException(int line, int col, string message)
        : base(FormatHeader(line, col, message))
    {
        Line = line;
        Col = col;
    }

    private static string FormatHeader(int line, int col, string msg)
        => line > 0
            ? $"Ошибка выполнения [строка {line}, символ {col}]: {msg}"
            : $"Ошибка выполнения: {msg}";
}

internal enum ValueKind
{
    Num,
    LVal,
    LAddr,
}

internal readonly struct RtValue
{
    public ValueKind Kind { get; init; }
    public double Num { get; init; }
    public string Name { get; init; }
    public int Idx { get; init; }

    public static RtValue MakeNum(double n) => new() { Kind = ValueKind.Num, Num = n, Name = string.Empty };
    public static RtValue MakeLVal(string name) => new() { Kind = ValueKind.LVal, Name = name };
    public static RtValue MakeLAddr(string name, int idx) => new() { Kind = ValueKind.LAddr, Name = name, Idx = idx };
}

public sealed class Interpreter
{
    private readonly Dictionary<string, double> _vars = new();
    private readonly Dictionary<string, double[]> _arrays = new();

    /// <summary>Запустить ОПС на исполнение.</summary>
    public static void Run(IReadOnlyList<RpnItem> rpn, TextReader input, TextWriter output)
        => new Interpreter().Execute(rpn, input, output);

    private void Execute(IReadOnlyList<RpnItem> rpn, TextReader input, TextWriter output)
    {
        var stack = new Stack<RtValue>(64);
        var reader = new NumberReader(input);

        int ip = 0;
        while (ip < rpn.Count)
        {
            var it = rpn[ip];

            switch (it.Kind)
            {
                case RpnKind.Num:
                    stack.Push(RtValue.MakeNum(it.Num));
                    ip++;
                    break;

                case RpnKind.Var:
                    stack.Push(RtValue.MakeLVal(it.Name));
                    ip++;
                    break;

                case RpnKind.Lbl:
                    stack.Push(RtValue.MakeNum(it.Addr));
                    ip++;
                    break;

                case RpnKind.Op:
                    ip = ExecOp(it, stack, reader, output, ip);
                    break;
            }
        }
    }

    private int ExecOp(RpnItem it, Stack<RtValue> stack, NumberReader reader, TextWriter output, int ip)
    {
        switch (it.Op)
        {
            case OpCode.Halt:
                return int.MaxValue; // прервёт внешний while

            case OpCode.Add: case OpCode.Sub: case OpCode.Mul: case OpCode.Div:
            {
                var bv = stack.Pop();
                var av = stack.Pop();
                double a = ResolveNum(av, it);
                double b = ResolveNum(bv, it);
                double r = it.Op switch
                {
                    OpCode.Add => a + b,
                    OpCode.Sub => a - b,
                    OpCode.Mul => a * b,
                    OpCode.Div => b == 0
                        ? throw new RuntimeException(it.Line, it.Col, "деление на ноль")
                        : a / b,
                    _ => 0,
                };
                stack.Push(RtValue.MakeNum(r));
                return ip + 1;
            }

            case OpCode.Neg:
            {
                var av = stack.Pop();
                stack.Push(RtValue.MakeNum(-ResolveNum(av, it)));
                return ip + 1;
            }

            case OpCode.Lt: case OpCode.Gt: case OpCode.Le:
            case OpCode.Ge: case OpCode.Eq: case OpCode.Ne:
            {
                var bv = stack.Pop();
                var av = stack.Pop();
                double a = ResolveNum(av, it);
                double b = ResolveNum(bv, it);
                bool ok = it.Op switch
                {
                    OpCode.Lt => a < b,
                    OpCode.Gt => a > b,
                    OpCode.Le => a <= b,
                    OpCode.Ge => a >= b,
                    OpCode.Eq => a == b,
                    OpCode.Ne => a != b,
                    _ => false,
                };
                stack.Push(RtValue.MakeNum(ok ? 1 : 0));
                return ip + 1;
            }

            case OpCode.Index:
            {
                var idxV = stack.Pop();
                var baseV = stack.Pop();
                if (baseV.Kind != ValueKind.LVal)
                    throw new RuntimeException(it.Line, it.Col, "индексация не по имени массива");
                int idx = (int)ResolveNum(idxV, it);
                stack.Push(RtValue.MakeLAddr(baseV.Name, idx));
                return ip + 1;
            }

            case OpCode.Rval:
            {
                var v = stack.Pop();
                stack.Push(RtValue.MakeNum(ResolveNum(v, it)));
                return ip + 1;
            }

            case OpCode.Assign:
            {
                var rhsV = stack.Pop();
                var targetV = stack.Pop();
                double rhs = ResolveNum(rhsV, it);
                StoreNum(targetV, rhs, it);
                return ip + 1;
            }

            case OpCode.Decl:
            {
                var sizeV = stack.Pop();
                var nameV = stack.Pop();
                if (nameV.Kind != ValueKind.LVal)
                    throw new RuntimeException(it.Line, it.Col, "DECL: ожидалось имя массива");
                double sz = ResolveNum(sizeV, it);
                if (sz <= 0)
                {
                    throw new RuntimeException(it.Line, it.Col,
                        $"неположительный размер массива \"{nameV.Name}\": {sz.ToString("G", CultureInfo.InvariantCulture)}");
                }
                _arrays[nameV.Name] = new double[(int)sz];
                return ip + 1;
            }

            case OpCode.Read:
            {
                var targetV = stack.Pop();
                if (!reader.TryReadNumber(out double x, out string? err))
                    throw new RuntimeException(it.Line, it.Col, "ошибка ввода: " + err);
                StoreNum(targetV, x, it);
                return ip + 1;
            }

            case OpCode.Write:
            {
                var v = stack.Pop();
                output.Write(FormatNumber(ResolveNum(v, it)) + " ");
                return ip + 1;
            }

            case OpCode.Jmp:
            {
                var addrV = stack.Pop();
                return (int)addrV.Num;
            }

            case OpCode.Jz:
            {
                var addrV = stack.Pop();
                var condV = stack.Pop();
                double cond = ResolveNum(condV, it);
                return cond == 0 ? (int)addrV.Num : ip + 1;
            }

            default:
                throw new RuntimeException(it.Line, it.Col, $"неизвестная операция {it.Op}");
        }
    }

    private double ResolveNum(RtValue v, RpnItem ctx)
    {
        switch (v.Kind)
        {
            case ValueKind.Num:
                return v.Num;
            case ValueKind.LVal:
                if (!_vars.TryGetValue(v.Name, out var x))
                    throw new RuntimeException(ctx.Line, ctx.Col,
                        $"переменная \"{v.Name}\" не инициализирована");
                return x;
            case ValueKind.LAddr:
                if (!_arrays.TryGetValue(v.Name, out var arr))
                    throw new RuntimeException(ctx.Line, ctx.Col,
                        $"массив \"{v.Name}\" не объявлен");
                if (v.Idx < 0 || v.Idx >= arr.Length)
                    throw new RuntimeException(ctx.Line, ctx.Col,
                        $"индекс {v.Idx} вне границ массива \"{v.Name}\" (размер {arr.Length})");
                return arr[v.Idx];
        }
        throw new RuntimeException(ctx.Line, ctx.Col, "внутренняя ошибка: неизвестный вид значения");
    }

    private void StoreNum(RtValue target, double x, RpnItem ctx)
    {
        switch (target.Kind)
        {
            case ValueKind.LVal:
                _vars[target.Name] = x;
                break;
            case ValueKind.LAddr:
                if (!_arrays.TryGetValue(target.Name, out var arr))
                    throw new RuntimeException(ctx.Line, ctx.Col,
                        $"массив \"{target.Name}\" не объявлен");
                if (target.Idx < 0 || target.Idx >= arr.Length)
                    throw new RuntimeException(ctx.Line, ctx.Col,
                        $"индекс {target.Idx} вне границ массива \"{target.Name}\" (размер {arr.Length})");
                arr[target.Idx] = x;
                break;
            default:
                throw new RuntimeException(ctx.Line, ctx.Col, "присваивание не в lvalue");
        }
    }

    private static string FormatNumber(double x)
    {
        if (x > -1e18 && x < 1e18 && x == Math.Truncate(x))
            return ((long)x).ToString(CultureInfo.InvariantCulture);
        return x.ToString("G", CultureInfo.InvariantCulture);
    }
}

/// <summary>Чтение чисел из потока ввода (целых или вещественных).</summary>
internal sealed class NumberReader
{
    private readonly TextReader _reader;
    public NumberReader(TextReader reader) => _reader = reader;

    public bool TryReadNumber(out double value, out string? error)
    {
        value = 0;
        error = null;

        // Пропускаем пробельные символы.
        int ch;
        do
        {
            ch = _reader.Read();
            if (ch == -1) { error = "ожидалось число"; return false; }
        } while (ch == ' ' || ch == '\t' || ch == '\n' || ch == '\r');

        var buf = new System.Text.StringBuilder();
        buf.Append((char)ch);
        while (true)
        {
            int next = _reader.Peek();
            if (next == -1) break;
            if (next == ' ' || next == '\t' || next == '\n' || next == '\r') break;
            buf.Append((char)_reader.Read());
        }

        string s = buf.ToString();
        if (!double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
        {
            error = $"не число: \"{s}\"";
            return false;
        }
        return true;
    }
}
