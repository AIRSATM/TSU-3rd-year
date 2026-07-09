# Защита лабораторной работы — MiniLang.CSharp
## Полное руководство: Теория → Код → Команды → Тесты

> **Все команды запускать из корневой папки проекта (где лежит `MiniLang.sln`).**

---

## Блок 0. Структура проекта

```
MiniLang.CSharp/
├── src/
│   ├── MiniLang/
│   │   ├── Lexing/         ← Token.cs, Lexer.cs
│   │   ├── Parsing/        ← Rpn.cs, Symbols.cs, ParseTable.cs, Parser.cs
│   │   └── Interpreting/   ← Interpreter.cs
│   └── MiniLang.Cli/       ← Program.cs (точка входа CLI)
├── tests/
│   └── MiniLang.Tests/     ← 43 xUnit-теста
├── examples/               ← *.ml файлы программ
└── docs/                   ← lexer.md, grammar.md, rpn.md
```

**Архитектурная цепочка:**

```
Исходный текст
     ↓  Lexer.cs         — Табличный ДКА → поток Token
     ↓  Parser.cs        — LL(1) магазинный автомат → ОПС (массив RpnItem)
     ↓  Interpreter.cs   — Стековая виртуальная машина → результат
```

---

## Блок 1. Лексический анализатор — Лекция 2

### Теория: состояния и классы символов

| `LexState`    | `CharClass`                         |
|---------------|-------------------------------------|
| `Start`       | `Letter` — `a..z A..Z _`           |
| `InIdent`     | `Digit` — `0..9`                   |
| `InInt`       | `Dot` — `'.'`                      |
| `InReal`      | `Op` — `+ - * / < > = !`          |
| `InOpStart`   | `Lparen` — `'('`                   |
| `InColon`     | `Rparen` — `')'`                   |
| `InComment`   | `Lbrack` — `'['`, `Rbrack` — `']'` |
|               | `Semi` — `';'`, `Comma` — `','`    |
|               | `Colon` — `':'`, `Slash` — `'/'`   |
|               | `Eq` — `'='`, `Lt` — `'<'`, `Gt` — `'>'` |
|               | `Ws` — пробел, таб, перевод строки |
|               | `Eof`, `Other` — всё прочее → ошибка |

Ячейка таблицы: `Cell { NextState, Action }`, где `Action` одно из:

| Действие | Значение                                      |
|----------|-----------------------------------------------|
| `Accum`  | Добавить символ в накопитель и сдвинуться     |
| `Skip`   | Пропустить символ, накопитель не трогать      |
| `Emit`   | Выдать токен, символ не потреблять            |
| `EmitC`  | Выдать токен и потребить текущий символ       |
| `Error`  | Лексическая ошибка                            |

### Что распознаётся

- **Идентификаторы:** `[A-Za-z_][A-Za-z_0-9]*`
- **Целые числа:** цепочка цифр → `long` → `double`
- **Вещественные:** `цифры '.' цифры` (точка с обязательной дробной частью)
- **Ключевые слова:** через словарь `Keywords` (поэтому `ifx` → `IDENT`)
- **Операторы:** `+ - * / := < > <= >= = <>`; двухсимвольные собираются через состояния `InColon`, `InOpStart`
- **Комментарии:** `//` до конца строки (состояние `InComment`)
- **Координаты:** `Advance()` ведёт `_line`/`_col`; фиксируются при старте токена

### Ошибки лексера

`LexException(line, col, message)` → `«Лексическая ошибка [строка L, символ C]: <причина>»`

Примеры: `«недопустимый символ '@'»`, `«ожидалась цифра после '.'»`

### Где показать в коде

Файл: `src/MiniLang/Lexing/Lexer.cs`

1. Двумерный массив `TransitionTable[state, charClass]`
2. Метод `Classify()` — классификатор символов
3. Метод `NextToken()` — цикл по таблице, переходы между состояниями
4. Метод `BuildTransitionTable()` — инициализация (один раз)

### Команды

```bash
# Таблица переходов ДКА в консоли
dotnet run --project src/MiniLang.Cli -c Release -- --lex-table

# Разбить файл на токены (список лексем)
dotnet run --project src/MiniLang.Cli -c Release -- --tokens examples/01_formulas.ml

# HTML-артефакт: список лексем
dotnet run --project src/MiniLang.Cli -c Release -- --artifacts-lexer-html
```

---

## Блок 2. Грамматика и LL(1)-анализатор — Лекции 3, 8

### КС-грамматика (форма Грейбах, с семантическими действиями `{}`)

```
Program     → StmtList EOF

StmtList    → Stmt StmtList'
StmtList'   → ';' StmtList' | Stmt StmtList' | ε

Stmt        → IDENT {PUSH_VAR} AssignTail
            | READ '(' IDENT {PUSH_VAR} LvalIdxTail {READ} ')'
            | WRITE '(' Expr {WRITE} ')'
            | IF Cond {IF_AFTER_COND} THEN StmtList IfTail
            | WHILE {WHILE_BEGIN} Cond {WHILE_AFTER_COND} DO StmtList END {WHILE_END}
            | ARRAY IDENT {PUSH_VAR} '[' Expr ']' {DECL}

AssignTail  → ':=' Expr {ASSIGN}
            | '[' Expr ']' {INDEX} ':=' Expr {ASSIGN}

LvalIdxTail → '[' Expr ']' {INDEX} | ε
IfTail      → {IF_AFTER_THEN} ELSE StmtList END {IF_END}
            | END {IF_NO_ELSE}

Cond        → Expr RelOp Expr {REL}
RelOp       → '<' | '>' | '<=' | '>=' | '=' | '<>'

Expr        → Term Expr'
Expr'       → '+' Term {ADD} Expr' | '-' Term {SUB} Expr' | ε

Term        → Fact Term'
Term'       → '*' Fact {MUL} Term' | '/' Fact {DIV} Term' | ε

Fact        → '(' Expr ')'
            | INT  {PUSH_NUM}
            | REAL {PUSH_NUM}
            | IDENT {PUSH_VAR} FactIdTail
            | '-' Fact {NEG}

FactIdTail  → '[' Expr ']' {INDEX} {RVAL} | ε {RVAL}
```

### Как строится таблица разбора

`ParseTable.cs` строит словарь `Dictionary<(Nonterm, TokenType), Symbol[]>`:
- Для нетерминала + FIRST-множество → правило записывается как `Symbol[]`
- Для ε-альтернатив используется FOLLOW-множество
- `Symbol` = токен `[0..100)` ∪ нетерминал `[100..200)` ∪ `SemAction [200..)`

```csharp
// Пример записи в коде:
Add(Nonterm.AssignTail, TokenType.Assign, new Symbol[]
{
    TokenType.Assign, Nonterm.Expr, SemAction.OpAssign,
});
```

### Как работает магазинный автомат (`Parser.Run()`)

Три стека:

| Стек        | Назначение                                         |
|-------------|----------------------------------------------------|
| `_stack`    | Стек символов (магазин LL(1))                      |
| `_semStack` | Стек отложенных токенов `IDENT`/`INT`/`REAL`/`RelOp` |
| `_labels`   | Стек адресов меток для back-patching (`if`/`while`) |

На каждой итерации снимаем верхний символ:
1. **Семантическое действие** → `ExecAction()` → дописываем в `_rpn`
2. **Терминал** → сравниваем с токеном; при совпадении потребляем
3. **Нетерминал** → берём правило из `ParseTable`, кладём в стек **справа налево**

Нет правила для `(нетерминал, токен)` → `ParseException` с координатами.

### Где показать в коде

- `src/MiniLang/Parsing/ParseTable.cs` — метод `Build()`, словарь `Table`
- `src/MiniLang/Parsing/Parser.cs` — метод `Run()`, стеки `_stack` / `_labels`

### Команды

```bash
# Исходная чистая КС-грамматика (ОБЯЗАТЕЛЬНО показать!)
dotnet run --project src/MiniLang.Cli -c Release -- --raw-grammar

# Полная грамматика с типами токенов
dotnet run --project src/MiniLang.Cli -c Release -- --grammar

# Грамматика в форме Грейбах (устранена левая рекурсия)
dotnet run --project src/MiniLang.Cli -c Release -- --greibach

# HTML: таблица LL(1)-анализатора
dotnet run --project src/MiniLang.Cli -c Release -- --artifacts-lltable-html

# HTML: грамматика в форме Грейбах
dotnet run --project src/MiniLang.Cli -c Release -- --artifacts-greibach-html
```

---

## Блок 3. Семантика, ОПС и интерпретатор — Лекции 4, 5, 6, 7

### Структура элемента ОПС (`RpnItem`)

| `RpnKind` | Значение                                        |
|-----------|-------------------------------------------------|
| `Num`     | Числовая константа                              |
| `Var`     | Имя переменной (lvalue)                         |
| `Op`      | Операция                                        |
| `Lbl`     | Метка перехода (`Addr` изменяемый — для back-patching) |

### Операции (`OpCode`)

| `OpCode`              | Символ             | Семантика                                              |
|-----------------------|--------------------|--------------------------------------------------------|
| `Add Sub Mul Div`     | `+ - * /`          | Арифметика (`Div` → ошибка при делении на 0)           |
| `Neg`                 | `@-`               | Унарный минус                                          |
| `Lt Gt Le Ge Eq Ne`   | `< > <= >= = <>`   | Сравнения: возвращают `1.0` / `0.0`                    |
| `Assign`              | `:=`               | Снять `(value, lvalue)`, записать                      |
| `Index`               | `[]`               | `(lval, num)` → адрес элемента массива                 |
| `Rval`                | `RVAL`             | lvalue → число (разыменование)                         |
| `Decl`                | `DECL`             | `array a[n]`: аллоцировать `double[n]`                 |
| `Read Write`          | `READ WRITE`       | Ввод / вывод                                           |
| `Jmp`                 | `JMP`              | Безусловный переход                                    |
| `Jz`                  | `JZ`               | Переход если 0                                         |
| `Halt`                | `HALT`             | Конец программы                                        |

### Семантические действия → ОПС

| Действие          | Что порождает в ОПС                                                      |
|-------------------|--------------------------------------------------------------------------|
| `PushNum`         | `Num(значение INT/REAL)`                                                 |
| `PushVar`         | `Var(имя IDENT)`                                                         |
| `OpAdd/Sub/...`   | Соответствующий `Op`                                                     |
| `OpRel`           | `Op(< или > или ...)` — по типу запомненного `RelOp`                     |
| `IfAfterCond`     | `Lbl(?=-1) Op(Jz)` — адрес ещё неизвестен                               |
| `IfAfterThen`     | `Lbl(?=-1) Op(Jmp)` + правка `?1` (JZ-метки) на текущий адрес           |
| `IfNoElse`        | Правка JZ-метки на текущий адрес                                         |
| `IfEnd`           | Правка JMP-метки на текущий адрес                                        |
| `WhileBegin`      | Запомнить `begin` = текущий адрес                                        |
| `WhileAfterCond`  | `Lbl(?=-1) Op(Jz)`                                                       |
| `WhileEnd`        | `Lbl(begin) Op(Jmp)` + правка JZ-метки на текущий адрес                 |

### Пример ОПС: `x := 2 + 3 * 4`

```
0: x     ← PushVar
1: 2     ← PushNum
2: 3
3: 4
4: *     ← OpMul  (сначала умножение — приоритет!)
5: +     ← OpAdd
6: :=    ← OpAssign
7: HALT
```

### Пример: `if x > 0 then write(1) else write(2) end`

| Шаг              | Действие                        | ОПС добавляет                          |
|------------------|---------------------------------|----------------------------------------|
| разбор `Cond`    | `PushVar PushNum OpRel`         | `x  0  >`                              |
| `IfAfterCond`    | `Lbl(?1) Jz`                    | `L(-1)  JZ`                            |
| ветка `THEN`     | `write(1)`                      | `1  WRITE`                             |
| `IfAfterThen`    | `Lbl(?2) Jmp` + правка `?1`     | `L(-1)  JMP`; `?1` ← текущий адрес    |
| ветка `ELSE`     | `write(2)`                      | `2  WRITE`                             |
| `IfEnd`          | правка `?2`                     | `?2` ← текущий адрес                   |

### Где показать в коде

- `src/MiniLang/Parsing/ParseTable.cs` — `SemAction` в правых частях правил
- `src/MiniLang/Parsing/Parser.cs` — метод `ExecAction()`, стек `_labels`, `switch` по `SemAction`
- `src/MiniLang/Interpreting/Interpreter.cs` — интерпретатор ОПС

### Команды

```bash
# Правила грамматики со встроенными семантическими действиями (те самые □)
dotnet run --project src/MiniLang.Cli -c Release -- --sem-actions

# Список поддерживаемых кодов операций ОПС
dotnet run --project src/MiniLang.Cli -c Release -- --opcodes

# Показать сгенерированную ОПС для программы (без выполнения)
dotnet run --project src/MiniLang.Cli -c Release -- --rpn examples/03_factorial.ml

# HTML: список операций ОПС
dotnet run --project src/MiniLang.Cli -c Release -- --artifacts-ops-html

# HTML: семантические действия
dotnet run --project src/MiniLang.Cli -c Release -- --artifacts-semantics-html
```

---

## Блок 4. Диагностика ошибок — Лекции 5, 8

### Как передаются координаты

| Класс             | Поля / роль                                          |
|-------------------|------------------------------------------------------|
| `Token.cs`        | `Line`, `Col` — фиксируются лексером при старте токена |
| `RpnItem`         | `Line`, `Col` — копируются из токена при генерации ОПС |
| `LexException`    | `«Лексическая ошибка [строка L, символ C]: ...»`    |
| `ParseException`  | `«Синтаксическая ошибка [строка L, символ C]: ...»` |
| `RuntimeException`| `«Ошибка выполнения [строка L, символ C]: ...»`     |

### Где показать в коде

- `src/MiniLang/Lexing/Token.cs` — поля `Line`, `Col`
- `src/MiniLang/Lexing/Lexer.cs` — `Advance()`, `_line`, `_col`
- `src/MiniLang/Parsing/Rpn.cs` — класс `RpnItem` с полями `Line`, `Col`
- `src/MiniLang/Interpreting/Interpreter.cs` — `RuntimeException` с координатами

### Команды

```bash
# Лексическая ошибка: символ '@' — недопустимый
dotnet run --project src/MiniLang.Cli -c Release -- examples/06_error_lexical.ml

# Синтаксическая ошибка: пропущен операнд (y := x + ;)
dotnet run --project src/MiniLang.Cli -c Release -- examples/05_error_syntax.ml

# Ошибка выполнения: выход за границы массива (a[10] при размере 5)
dotnet run --project src/MiniLang.Cli -c Release -- examples/07_error_runtime.ml
```

---

## Блок 5. Тесты — xUnit (43 штуки, все проходят)

### Покрытие тестов

| Набор              | Что проверяется                                                                                      |
|--------------------|------------------------------------------------------------------------------------------------------|
| `LexerTests`       | Операторы, ключевые слова, числа, идентификаторы, комментарии, точные координаты лексем, лексические ошибки |
| `ParserTests`      | Приоритет операций, унарный минус, `array`, метки в `if`/`while`, генерация `RVAL`, синтаксические ошибки |
| `InterpreterTests` | Формулы, ввод/вывод, ветвления, циклы, массивы, сортировка пузырьком, факториал, НОД, деление на ноль, выход за границы, неинициализированная переменная |

### Как читать тест (показываем на примере)

Открыть: `tests/MiniLang.Tests/InterpreterTests.cs`

**Метод `Factorial()`:**
```csharp
// src — исходный код на MiniLang
// "6"  — входные данные (stdin)
Assert.Equal("720", Run(src, "6").Trim());
// Ожидаем 720 → если совпало, тест зелёный
```

**Метод `DivisionByZeroThrows_WithMessageAndPosition()`:**
```csharp
// Специально пишем x := 1 / 0;
Assert.Throws<RuntimeException>(...);
// Проверяем: exception.Line == 1, в сообщении «деление на ноль»
// → координаты дошли от лексера через ОПС до интерпретатора
```

### Команды

```bash
# Запустить все 43 теста (сводка: Passed/Failed)
dotnet test MiniLang.sln -c Release

# Запустить с подробностями (каждый тест по имени)
dotnet test MiniLang.sln --logger "console;verbosity=detailed"

# Конкретный тест: деление на ноль
dotnet test MiniLang.sln --filter "Name=DivisionByZeroThrows_WithMessageAndPosition" -v n

# Тест сортировки массива (с деталями)
dotnet test MiniLang.sln --filter "Name=ArraySort" -v d

# Тест лексической ошибки
dotnet test MiniLang.sln --filter "Name=LexicalErrorReportsPositionAndMessage" -v d

# Тест синтаксической ошибки
dotnet test MiniLang.sln --filter "Name=ParserThrows_OnInvalidSyntax" -v n
```

---

## Блок 6. Ручные тесты на `.ml` файлах (Демонстрация требований ТЗ)

### Тест 1: Сложные формулы (ТЗ Пункт 1)

Вычисляет `((a+b)*c - d)/2` и дискриминант `b²-4ac`. Программа запросит `a, b, c, d, qa, qb, qc` по очереди.

```bash
# Интерактивный ввод
dotnet run --project src/MiniLang.Cli -c Release -- examples/01_formulas.ml

# Передать все значения сразу
echo "10 32 2 4 1 5 6" | dotnet run --project src/MiniLang.Cli -c Release -- examples/01_formulas.ml

# Полная трассировка: лексемы → ОПС → выполнение
echo "10 32 2 4 1 5 6" | dotnet run --project src/MiniLang.Cli -c Release -- --all examples/01_formulas.ml
```

### Тест 2: Сортировка массива (ТЗ Пункт 2)

Ввод N, ввод N элементов, сортировка пузырьком, вывод.

```bash
# Интерактивный ввод: 5 [Enter] 3 [Enter] 1 [Enter] 4 [Enter] 1 [Enter] 5 [Enter]
dotnet run --project src/MiniLang.Cli -c Release -- examples/02_sort.ml

# Одной строкой
echo "5 3 1 4 1 5" | dotnet run --project src/MiniLang.Cli -c Release -- examples/02_sort.ml
```

### Тест 3: Факториал — полная трассировка

```bash
# Только выполнение (ввести 6 → получить 720)
dotnet run --project src/MiniLang.Cli -c Release -- examples/03_factorial.ml

# Полная трассировка: лексемы → ОПС → выполнение
echo "6" | dotnet run --project src/MiniLang.Cli -c Release -- --all examples/03_factorial.ml
```

### Тест 4: НОД по алгоритму Евклида (`if/else` в цикле)

```bash
echo "12 8" | dotnet run --project src/MiniLang.Cli -c Release -- examples/04_gcd.ml
# Ожидаемый результат: 4
```

### Ошибки: Демонстрация диагностики

```bash
# 06: Лексическая ошибка — символ '@'
dotnet run --project src/MiniLang.Cli -c Release -- examples/06_error_lexical.ml

# 05: Синтаксическая ошибка — пропущен операнд: y := x + ;
dotnet run --project src/MiniLang.Cli -c Release -- examples/05_error_syntax.ml

# 07: Ошибка выполнения — выход за границы: array[5], обращение к [10]
dotnet run --project src/MiniLang.Cli -c Release -- examples/07_error_runtime.ml
```

---

## Блок 7. HTML-артефакты (вся теория в браузере)

```bash
# Лексер: список лексем
dotnet run --project src/MiniLang.Cli -c Release -- --artifacts-lexer-html

# Грамматика: форма Грейбах
dotnet run --project src/MiniLang.Cli -c Release -- --artifacts-greibach-html

# Парсер: таблица LL(1)
dotnet run --project src/MiniLang.Cli -c Release -- --artifacts-lltable-html

# ОПС: семантические действия (квадратики □)
dotnet run --project src/MiniLang.Cli -c Release -- --artifacts-semantics-html

# ОПС: список операций
dotnet run --project src/MiniLang.Cli -c Release -- --artifacts-ops-html
```

---

## Блок 8. Сценарий защиты — цепочка рассказа

> На каждом этапе подчёркивайте: **«всё реализовано как табличный автомат, как требует задание и лекции».**

| Шаг | Что делаем                                                                                                          |
|-----|---------------------------------------------------------------------------------------------------------------------|
| 1   | **Лексер.** Теория (Лекция 2) → `--lex-table` → `Lexer.cs`: `TransitionTable`                                     |
| 2   | **Грамматика.** `--raw-grammar` → `--greibach` → объясняем «устранили левую рекурсию» → `--artifacts-lltable-html` → `ParseTable.cs: Build()` |
| 3   | **ОПС и семантика.** `--sem-actions` («квадратики» из лекций) → `--artifacts-ops-html` → `Parser.cs: ExecAction(), _labels` → `--rpn examples/03_factorial.ml` |
| 4   | **Выполнение.** `--all examples/03_factorial.ml` → видна вся цепочка: лексемы → ОПС → результат                   |
| 5   | **Диагностика.** Три вида ошибок с указанием строки и символа: `06` → `05` → `07`                                  |
| 6   | **Тесты.** `dotnet test MiniLang.sln -c Release` → все 43 зелёных → показать `Factorial()`, `DivisionByZeroThrows_...()` |

---

## Блок 9. Аргументы «Код соответствует лекциям»

Если преподаватель спросит: *«Где здесь лекционный материал?»*

**Лексер (Лекция 2):**
> «Лекция 2 требует автоматных грамматик (А-грамматик) и ДКА. Вот массив `TransitionTable[state, charClass]` — никаких if/else. Символ → класс через `Classify()` → следующее состояние из таблицы.»

**Грамматика (Лекции 3, 8):**
> «`--raw-grammar`: вот исходная КС-грамматика. `--greibach`: вот она же после устранения левой рекурсии и факторизации. Форма Грейбах — каждая правая часть начинается с терминала. Это гарантирует LL(1) — детерминированный выбор по первому символу.»

**Магазинный автомат (Лекция 3):**
> «`Parser.Run()`: вот `Stack<Symbol> _stack`. Символ с вершины + текущий токен → правило из `ParseTable` → кладём в стек справа налево. Это классический алгоритм из лекции 3.»

**Семантика и ОПС (Лекции 4, 5):**
> «Семантические действия встроены в правые части правил (те самые □). `ExecAction()` в `Parser.cs`: switch по `SemAction`. Стек `_labels` — back-patching для `if`/`while`, как в лекции 5.»

**Диагностика (Лекции 5, 8):**
> «`RpnItem.Line/Col` — координаты из лексера дошли до интерпретатора. При любой ошибке (лексической, синтаксической, рантайм) выводится вид ошибки, строка и символ.»

---

## Блок 10. Полная таблица флагов CLI

| Флаг                       | Что делает                                                  |
|----------------------------|-------------------------------------------------------------|
| *(без флага)* `<файл.ml>`  | Запустить программу (лексер → парсер → интерпретатор)       |
| `--tokens <файл.ml>`       | Напечатать список лексем и выйти                            |
| `--rpn <файл.ml>`          | Напечатать ОПС и выйти                                      |
| `--all <файл.ml>`          | Лексемы + ОПС, затем выполнить программу                    |
| `--help`                   | Показать справку                                            |
| `--lex-table`              | Таблица переходов ДКА (в консоли)                           |
| `--raw-grammar`            | Исходная чистая КС-грамматика                               |
| `--grammar`                | Полная грамматика с типами токенов                          |
| `--greibach`               | КС-грамматика в форме Грейбах                               |
| `--sem-actions`            | Правила со встроенными семантическими действиями            |
| `--opcodes`                | Список кодов операций ОПС                                   |
| `--artifacts-lexer-html`   | HTML: список лексем                                         |
| `--artifacts-lltable-html` | HTML: таблица LL(1)-анализатора                             |
| `--artifacts-greibach-html`| HTML: грамматика в форме Грейбах                            |
| `--artifacts-semantics-html`| HTML: семантические действия                               |
| `--artifacts-ops-html`     | HTML: список операций ОПС                                   |

---

## Блок 11. Содержимое примеров (`.ml` файлы)

### `examples/01_formulas.ml`
```minilang
read(a); read(b); read(c); read(d);
x := ((a + b) * c - d) / 2;
write(x);
read(qa); read(qb); read(qc);
d2 := qb * qb - 4 * qa * qc;
write(d2);
```

### `examples/02_sort.ml`
```minilang
read(n);
array a[1000];
i := 0;
while i < n do read(a[i]); i := i + 1 end;
i := 0;
while i < n - 1 do
  j := 0;
  while j < n - 1 - i do
    if a[j] > a[j + 1] then
      t := a[j]; a[j] := a[j + 1]; a[j + 1] := t
    end;
    j := j + 1
  end;
  i := i + 1
end;
i := 0;
while i < n do write(a[i]); i := i + 1 end;
```

### `examples/03_factorial.ml` &nbsp;→ Ввод: `6`, Вывод: `720`
```minilang
read(n);
f := 1; i := 2;
while i <= n do f := f * i; i := i + 1 end;
write(f);
```

### `examples/04_gcd.ml` &nbsp;→ Ввод: `12 8`, Вывод: `4`
```minilang
read(a); read(b);
while a <> b do
  if a > b then a := a - b else b := b - a end
end;
write(a);
```

### `examples/05_error_syntax.ml` — синтаксическая ошибка
```minilang
x := 1;
y := x + ;   -- пропущен правый операнд
write(y);
```

### `examples/06_error_lexical.ml` — лексическая ошибка
```minilang
x := 1;
y := x @ 2;  -- недопустимый символ '@'
write(y);
```

### `examples/07_error_runtime.ml` — ошибка выполнения
```minilang
array a[5];
a[0] := 10;
write(a[10]);  -- выход за границы: индекс 10, размер 5
```

---

## Краткая шпаргалка

| Теория                        | Флаг / команда              | Файл в коде                              |
|-------------------------------|-----------------------------|------------------------------------------|
| ДКА лексера (Лекция 2)        | `--lex-table`               | `Lexer.cs`: `TransitionTable`, `NextToken()` |
| КС-грамматика                 | `--raw-grammar`             | `ParseTable.cs`: `Build()`, словарь `Table` |
| Форма Грейбах                 | `--greibach`                | `ParseTable.cs`: правые части `Symbol[]` |
| LL(1)-таблица                 | `--artifacts-lltable-html`  | `Parser.cs`: `Run()`, `_stack`           |
| Семантика / ОПС (Лекция 5)    | `--sem-actions`             | `Parser.cs`: `ExecAction()`, `_labels`   |
| Операции ОПС                  | `--opcodes`                 | `Rpn.cs`: `OpCode`, `RpnItem`            |
| Интерпретатор                 | `--all <файл>`              | `Interpreter.cs`: `Run()`                |

**Диагностика ошибок** (строка + символ): `Token.cs: Line/Col` → `RpnItem.Line/Col`

| Тип ошибки      | Файл                     | Класс исключения   |
|-----------------|--------------------------|--------------------|
| Лексическая     | `06_error_lexical.ml`    | `LexException`     |
| Синтаксическая  | `05_error_syntax.ml`     | `ParseException`   |
| Рантайм         | `07_error_runtime.ml`    | `RuntimeException` |

**Тесты:**
```bash
dotnet test MiniLang.sln -c Release   # 43/43 зелёных
```

**Демо (главные команды):**
```bash
echo "6" | dotnet run --project src/MiniLang.Cli -c Release -- --all examples/03_factorial.ml
echo "5 3 1 4 1 5" | dotnet run --project src/MiniLang.Cli -c Release -- examples/02_sort.ml
```
