# MiniLang.CSharp

Учебный транслятор-интерпретатор языка **MiniLang**, сделанный на C# (.NET 8).
В папке: исходники, тесты, документация и примеры.

## Что внутри

```
MiniLang.CSharp/
├── MiniLang.sln                — решение для трёх проектов
├── global.json                 — пин .NET SDK 8.0.x
├── src/
│   ├── MiniLang/               — библиотека: лексер, парсер, интерпретатор
│   │   ├── Lexing/             — Token.cs, Lexer.cs
│   │   ├── Parsing/            — Rpn.cs, Symbols.cs, ParseTable.cs, Parser.cs
│   │   └── Interpreting/       — Interpreter.cs
│   └── MiniLang.Cli/           — консольная программа `MiniLang.Cli`
├── tests/
│   └── MiniLang.Tests/         — xUnit-тесты (лексер, парсер, интерпретатор)
├── examples/                   — *.ml образцы
└── docs/                       — описание языка и реализации
    ├── grammar.md
    ├── lexer.md
    └── rpn.md
```

## Требования

* .NET SDK **8.0.x** (проверено на 8.0.400).
* `global.json` фиксирует версию, чтобы более новые SDK не мешали резолверу
  имён проектов.

## Сборка и запуск

```bash
cd MiniLang.CSharp

# Сборка
dotnet build MiniLang.sln -c Release

# Все тесты (43 шт.)
dotnet test MiniLang.sln -c Release
dotnet test MiniLang.sln --logger "console;verbosity=detailed"

# Запуск программы
dotnet run --project src/MiniLang.Cli -c Release -- examples/03_factorial.ml
# (введите 6, нажмите Enter — получите 720)

# Просто показать лексемы
dotnet run --project src/MiniLang.Cli -c Release -- --tokens examples/01_formulas.ml

# Просто показать ОПС, не выполняя:
dotnet run --project src/MiniLang.Cli -c Release -- --rpn examples/01_formulas.ml

# Все стадии: лексемы → ОПС → выполнение
echo "10 32" | dotnet run --project src/MiniLang.Cli -c Release -- --all examples/01_formulas.ml

# Тест сортировки массива
dotnet test MiniLang.sln --filter "Name=ArraySort" -v d

echo "5 3 1 4 1 5" | dotnet run --project src/MiniLang.Cli -c Release -- examples/02_sort.ml

# 1. Чистая КС-грамматика
dotnet run --project src/MiniLang.Cli -c Release -- --raw-grammar

# 3. ОПС для примера
echo "x := 1 + 2 * 3;" > test.ml
dotnet run --project src/MiniLang.Cli -c Release -- --rpn test.ml

# 4. Список операций ОПС (если добавили флаг)
dotnet run --project src/MiniLang.Cli -c Release -- --opcodes

# 5. ОПС с метками (if)
echo "if 1 < 2 then write(1) end;" > test_if.ml
dotnet run --project src/MiniLang.Cli -c Release -- --rpn test_if.ml

# 7. Ключевой тест с деталями
dotnet test MiniLang.sln --filter "Name=ArraySort" -v d

# 8. Тест на диагностику ошибки
dotnet test MiniLang.sln --filter "Name=LexicalErrorReportsPositionAndMessage" -v d

# 9. Тест на синтаксическую ошибку
dotnet run --project src/MiniLang.Cli -c Release -- examples/05_error_syntax.ml

# 10. Тест на лексическую ошибку
dotnet run --project src/MiniLang.Cli -c Release -- examples/06_error_lexical.ml

# 11. Тест на ошибку выполнения
dotnet run --project src/MiniLang.Cli -c Release -- examples/07_error_runtime.ml

# Артефакты
dotnet run --project src/MiniLang.Cli -c Release -- --artifacts-lexer
dotnet run --project src/MiniLang.Cli -c Release -- --artifacts-transition
dotnet run --project src/MiniLang.Cli -c Release -- --artifacts-grammar
dotnet run --project src/MiniLang.Cli -c Release -- --artifacts-greibach-compact
dotnet run --project src/MiniLang.Cli -c Release -- --artifacts-semantics
dotnet run --project src/MiniLang.Cli -c Release -- --artifacts-lltable
dotnet run --project src/MiniLang.Cli -c Release -- --artifacts-ops
```

## Флаги CLI

| Флаг             | Действие                                              |
| -----------      | ----------------------------------------------------- |
| (без флага)      | прогнать лексер → парсер → интерпретатор              |
| `--tokens`       | напечатать список лексем и выйти                      |
| `--rpn`          | напечатать ОПС и выйти                                |
| `--all`          | напечатать лексемы и ОПС, затем выполнить программу   |
| `--help`         | показать справку                                      |
| `--lex-table`    | показать таблицу переходов                            |
| `--raw-grammar`  | показать кс-грамматики                                |
| `--sem-actions`  | показать семантические действия ОПС                   |
| `--greibach`     | показать кс-грамматику в форме Грейбаха               |
| `--opcodes`      | показать операции ОПС                                 |

## Архитектура

| Слой         | Класс/файл                                       | Что делает                                                            |
| ------------ | ------------------------------------------------ | -------------------------------------------------------------------   |
| Лексер       | `MiniLang.Lexing.Lexer`                          | Табличный ДКА: текст → поток `Token`                                  |
| Парсер       | `MiniLang.Parsing.Parser` + `ParseTable`         | LL(1) с магазином; правые части содержат семантические действия       |
| Семантика    | `SemAction` + `ExecAction` в `Parser`            | Генерация элементов ОПС (`RpnItem`), back-patching меток для if/while |
| Интерпретатор| `MiniLang.Interpreting.Interpreter`              | Стековая виртуальная машина по массиву `RpnItem`                      |

Каждый слой описан в отдельном документе в `docs/`.


## Тесты

```bash
dotnet test MiniLang.sln -c Release
```

Покрытие:

* `LexerTests` — операторы, ключевые слова, числа, идентификаторы,
  комментарии, точные координаты лексем, лексические ошибки.
* `ParserTests` — приоритет операций, унарный минус, оператор `array`,
  корректность меток в `if`/`while`, генерация `RVAL`, синтаксические ошибки.
* `InterpreterTests` — формулы, ввод/вывод, ветвления, циклы, массивы,
  сортировка пузырьком, факториал, НОД, обнаружение деления на ноль,
  выход за границы, использование неинициализированной переменной.
