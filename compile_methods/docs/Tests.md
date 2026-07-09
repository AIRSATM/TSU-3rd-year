## 🧪 Все 34 теста — команды для запуска

```bash
dotnet test MiniLang.sln -c Release
```

#### LexerTests (9 тестов)

1. `dotnet test MiniLang.sln --filter "Name=Empty"`
2. `dotnet test MiniLang.sln --filter "Name=Operators"`
3. `dotnet test MiniLang.sln --filter "Name=Keywords"`
4. `dotnet test MiniLang.sln --filter "Name=IntAndReal"`
5. `dotnet test MiniLang.sln --filter "Name=IdentifiersAndUnderscore"`
6. `dotnet test MiniLang.sln --filter "Name=Comment"`
7. `dotnet test MiniLang.sln --filter "Name=LineAndCol"`
8. `dotnet test MiniLang.sln --filter "Name=LexicalErrorReportsPosition"`
9. `dotnet test MiniLang.sln --filter "Name=RealWithoutFractionFails"`

---

#### ParserTests (7 тестов)

10. `dotnet test MiniLang.sln --filter "Name=Precedence"`
11. `dotnet test MiniLang.sln --filter "FullyQualifiedName=MiniLang.Tests.ParserTests.UnaryMinus"`
12. `dotnet test MiniLang.sln --filter "Name=IfWithoutElseHasJzPatched"`
13. `dotnet test MiniLang.sln --filter "Name=WhileEmitsJmpBack"`
14. `dotnet test MiniLang.sln --filter "Name=SyntaxErrorMissingOperand"`
15. `dotnet test MiniLang.sln --filter "Name=ArrayDeclaration"`
16. `dotnet test MiniLang.sln --filter "Name=RvalForIdentifierInExpression"`

---

#### InterpreterTests (18 тестов, включая Theory)

17. `dotnet test MiniLang.sln --filter "Name=Simple"`
18. `dotnet test MiniLang.sln --filter "Name=PrecedenceAndParens"`
19. `dotnet test MiniLang.sln --filter "Name=ReadWrite"`
20. `dotnet test MiniLang.sln --filter "Name=RealNumbers"`
21. `dotnet test MiniLang.sln --filter "FullyQualifiedName=MiniLang.Tests.InterpreterTests.UnaryMinus"`
22. `dotnet test MiniLang.sln --filter "Name=IfThen"` (3 набора данных → 3 теста)
23. `dotnet test MiniLang.sln --filter "Name=IfElse"` (2 набора данных → 2 теста)
24. `dotnet test MiniLang.sln --filter "Name=WhileSum"`
25. `dotnet test MiniLang.sln --filter "Name=ArrayBasic"`
26. `dotnet test MiniLang.sln --filter "Name=ArraySort"`
27. `dotnet test MiniLang.sln --filter "Name=DivisionByZeroThrows"`
28. `dotnet test MiniLang.sln --filter "Name=IndexOutOfBoundsThrows"`
29. `dotnet test MiniLang.sln --filter "Name=UninitializedVarThrows"`
30. `dotnet test MiniLang.sln --filter "Name=Factorial"`
31. `dotnet test MiniLang.sln --filter "Name=Gcd"`

---

- **Лексическая ошибка с координатами** → `dotnet test MiniLang.sln --filter "Name=LexicalErrorReportsPosition"`
- **Синтаксическая ошибка** → `dotnet test MiniLang.sln --filter "Name=SyntaxErrorMissingOperand"`
- **Ошибка выполнения (деление на ноль)** → `dotnet test MiniLang.sln --filter "Name=DivisionByZeroThrows"`
- **Сортировка массива (главный тест)** → `dotnet test MiniLang.sln --filter "Name=ArraySort"`

---

# 1. Показать саму таблицу переходов ДКА (HTML-автомат)
dotnet run --project src/MiniLang.Cli -c Release -- --lex-table

# 2. Показать, как лексер разбивает конкретный файл на токены (список лексем)
dotnet run --project src/MiniLang.Cli -c Release -- --tokens examples/01_formulas.ml

# 3. Сгенерировать красивый HTML-отчет со списком токенов
dotnet run --project src/MiniLang.Cli -c Release -- --artifacts-lexer-html

# 4. Показать исходную чистую КС-грамматику (без привязки к токенам C#)
dotnet run --project src/MiniLang.Cli -c Release -- --artifacts-grammar-html

# 5. Показать полную грамматику с привязкой к типам токенов
dotnet run --project src/MiniLang.Cli -c Release -- --artifacts-grammar

# 7. Сгенерировать HTML со сводной таблицей LL(1)-анализатора
dotnet run --project src/MiniLang.Cli -c Release -- --artifacts-lltable-html

# 8. Сгенерировать HTML-файл с грамматикой в форме Грейбах
dotnet run --project src/MiniLang.Cli -c Release -- --artifacts-greibach-html

# 9. Показать правила грамматики со встроенными семантическими действиями (те самые "квадратики" из лекций)
dotnet run --project src/MiniLang.Cli -c Release -- --sem-actions

# 10. Показать список поддерживаемых кодов операций ОПС (OpCodes)
dotnet run --project src/MiniLang.Cli -c Release -- --opcodes

# 11. Показать сгенерированную ОПС для конкретной программы (без выполнения)
dotnet run --project src/MiniLang.Cli -c Release -- --rpn examples/03_factorial.ml

# 12. Сгенерировать HTML-документ со списком операций ОПС
dotnet run --project src/MiniLang.Cli -c Release -- --artifacts-ops-html

# 13. Запуск программы в режиме "всё включено" (печать токенов + ОПС + выполнение)
dotnet run --project src/MiniLang.Cli -c Release -- --all examples/03_factorial.ml

# 14. Обычный запуск программы (только ввод/вывод результата)
dotnet run --project src/MiniLang.Cli -c Release -- examples/02_sort.ml

# 15. Запуск всех автоматических тестов (показать, что всё успешно написано и протестировано)
dotnet test MiniLang.sln -c Release

# 16. Запустить тест конкретно на проверку синтаксической ошибки
dotnet test MiniLang.sln --filter "Name=ParserThrows_OnInvalidSyntax" -v n

# Ручные тесты:
C:\Users\Пользователь\Desktop\MiniLang.CSharp\examples\01_formulas.ml:
// Пример 1: проверка сложных формул с вводом значений
// и выводом результата.
//
// Вычисление: ((a + b) * c - d) / 2
//
read(a);
read(b);
read(c);
read(d);

x := ((a + b) * c - d) / 2;
write(x);

// Вычисление дискриминанта: D = b^2 - 4*a*c
read(qa);
read(qb);
read(qc);
d2 := qb * qb - 4 * qa * qc;
write(d2);

C:\Users\Пользователь\Desktop\MiniLang.CSharp\examples\02_sort.ml:
// Пример 2: тест из задания — ввод n, ввод n элементов массива,
// упорядочение массива (сортировка пузырьком), вывод массива.

read(n);
array a[1000];

// читаем n элементов
i := 0;
while i < n do
    read(a[i]);
    i := i + 1
end;

// сортировка пузырьком по возрастанию
i := 0;
while i < n - 1 do
    j := 0;
    while j < n - 1 - i do
        if a[j] > a[j + 1] then
            t := a[j];
            a[j] := a[j + 1];
            a[j + 1] := t
        end;
        j := j + 1
    end;
    i := i + 1
end;

// выводим отсортированный массив
i := 0;
while i < n do
    write(a[i]);
    i := i + 1
end;

C:\Users\Пользователь\Desktop\MiniLang.CSharp\examples\03_factorial.ml:
// Пример 3: вычисление факториала n.
// Демонстрирует цикл while с условием и накопление произведения.

read(n);

f := 1;
i := 2;
while i <= n do
    f := f * i;
    i := i + 1
end;

write(f);

C:\Users\Пользователь\Desktop\MiniLang.CSharp\examples\04_gcd.ml:
// Пример 4: НОД двух чисел по алгоритму Евклида.
// Демонстрирует if/else в теле цикла.

read(a);
read(b);

while a <> b do
    if a > b then
        a := a - b
    else
        b := b - a
    end
end;

write(a);

C:\Users\Пользователь\Desktop\MiniLang.CSharp\examples\05_error_syntax.ml:
// Пример 5: ошибочная программа — отсутствует правый операнд.
// Транслятор должен выдать диагностику с № строки и № символа.

x := 1;
y := x + ;
write(y);

C:\Users\Пользователь\Desktop\MiniLang.CSharp\examples\06_error_lexical.ml:
// Пример 6: лексическая ошибка — недопустимый символ '@'.
// Транслятор должен указать строку и колонку.

x := 1;
y := x @ 2;
write(y);

C:\Users\Пользователь\Desktop\MiniLang.CSharp\examples\07_error_runtime.ml:
// Пример 7: ошибка времени выполнения — выход за границы массива.

array a[5];
a[0] := 10;
write(a[10]);

C:\Users\Пользователь\Desktop\MiniLang.CSharp\examples\test_expr.ml:
x := 1 + 2 * 3;
