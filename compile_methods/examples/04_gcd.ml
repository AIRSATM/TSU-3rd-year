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
