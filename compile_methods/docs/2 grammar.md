# Грамматика MiniLang (C#-порт)

Этот документ описывает синтаксис языка **в том виде, как его разбирает
парсер из `src/MiniLang/Parsing/`**. Грамматика — приведена здесь для удобства.

## 1. Лексическая основа

Лексический уровень полностью отделён от синтаксического и описан
в [lexer.md](lexer.md). Парсер видит уже поток токенов:

```
IDENT     — идентификатор
INT       — целое число
REAL      — вещественное число
IF THEN ELSE END WHILE DO READ WRITE ARRAY  — ключевые слова
+ - * /              := < > <= >= = <>      — операторы
( ) [ ] ; ,                                 — пунктуация
EOF                                         — конец входа
```

## 2. Контекстно-свободная грамматика

Запись в нестрогой нормальной форме Грейбах: каждая правая часть начинается
либо с терминала, либо с ε. Это делает грамматику **LL(1)** —
для каждой пары (нетерминал, текущий токен) однозначно выбирается правило.

Семантические действия записаны в фигурных скобках `{ … }` — они
выполняются в момент, когда парсер встречает их в правой части,
и порождают элементы ОПС. Подробнее в [rpn.md](rpn.md).

```ebnf
Program        → StmtList EOF

StmtList       → Stmt StmtList'
StmtList'      → ';' StmtList'
               | Stmt StmtList'          // ε при ELSE, END, EOF
               | ε

Stmt           → IDENT {PUSH_VAR} AssignTail
               | READ '(' IDENT {PUSH_VAR} LvalIdxTail {READ} ')'
               | WRITE '(' Expr {WRITE} ')'
               | IF Cond {IF_AFTER_COND} THEN StmtList IfTail
               | WHILE {WHILE_BEGIN} Cond {WHILE_AFTER_COND}
                 DO StmtList END {WHILE_END}
               | ARRAY IDENT {PUSH_VAR} '[' Expr ']' {DECL}

AssignTail     → ':=' Expr {ASSIGN}
               | '[' Expr ']' {INDEX} ':=' Expr {ASSIGN}

LvalIdxTail    → '[' Expr ']' {INDEX}
               | ε

IfTail         → {IF_AFTER_THEN} ELSE StmtList END {IF_END}
               | END {IF_NO_ELSE}

Cond           → Expr RelOp Expr {REL}
RelOp          → '<' | '>' | '<=' | '>=' | '=' | '<>'

Expr           → Term Expr'
Expr'          → '+' Term {ADD} Expr'
               | '-' Term {SUB} Expr'
               | ε

Term           → Fact Term'
Term'          → '*' Fact {MUL} Term'
               | '/' Fact {DIV} Term'
               | ε

Fact           → '(' Expr ')'
               | INT  {PUSH_NUM}
               | REAL {PUSH_NUM}
               | IDENT {PUSH_VAR} FactIdTail
               | '-' Fact {NEG}

FactIdTail     → '[' Expr ']' {INDEX} {RVAL}
               | ε {RVAL}
```

## 3. Краткая семантика

| Конструкция                | Что делает                                                |
| -------------------------- | --------------------------------------------------------- |
| `x := expr`                | присваивает значение `expr` переменной `x`                |
| `a[i] := expr`             | присваивает значение элементу массива                     |
| `read(x)` / `read(a[i])`   | читает число из stdin                                     |
| `write(expr)`              | печатает число в stdout                                   |
| `array a[n]`               | объявляет массив `a` размера `n`                          |
| `if c then S end`          | условный оператор без `else`                              |
| `if c then S else T end`   | условный оператор с `else`                                |
| `while c do S end`         | цикл с предусловием                                       |

Точные типы значений и форматы вывода см. в [rpn.md](rpn.md) и
исходнике `src/MiniLang/Interpreting/Interpreter.cs`.

## 4. Откуда берётся таблица разбора

`src/MiniLang/Parsing/ParseTable.cs` строит словарь
`Dictionary<(Nonterm, TokenType), Symbol[]>` ровно по правилам выше.
Для каждого нетерминала перебираются токены из его **FIRST**-множества,
и для каждого варианта правой части записывается её представление
в виде массива `Symbol`. На ε-альтернативах используется
FOLLOW-множество соответствующего нетерминала.

Структура `Symbol` объединяет три вида символов в едином числовом
пространстве:

* `[0..100)`   — токены (`TokenType`),
* `[100..200)` — нетерминалы (`Nonterm`),
* `[200..)`    — семантические действия (`SemAction`).

Благодаря неявным конверсиям правила выглядят естественно:

```csharp
Add(Nonterm.AssignTail, TokenType.Assign, new Symbol[]
{
    TokenType.Assign, Nonterm.Expr, SemAction.OpAssign,
});
```

## 5. Разбор шаг за шагом

`Parser.Run()` поддерживает три стека:

* `_stack`   — стек символов (магазин LL(1)),
* `_semStack` — стек «отложенных» токенов IDENT/INT/REAL/RelOp,
* `_labels`  — стек адресов меток для back-patching `if`/`while`.

На каждой итерации снимаем верхний символ:

1. **Семантическое действие** — выполняем `ExecAction(...)`,
   которое дописывает элемент(ы) в `_rpn`.
2. **Терминал** — сравниваем с текущим токеном; при совпадении
   потребляем токен, при необходимости запоминаем его в `_semStack`.
3. **Нетерминал** — берём правило из `ParseTable` и кладём в стек
   справа налево.

Если для пары (нетерминал, токен) правила нет — кидаем
`ParseException` с координатами текущей лексемы. Это и есть
синтаксическая ошибка.
