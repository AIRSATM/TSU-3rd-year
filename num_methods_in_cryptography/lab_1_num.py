def to_digits(number, base=10):
    """Переводит целое число в список цифр (старший разряд первый)."""
    if number == 0:
        return [0]
    digits = []
    while number > 0:
        number, remainder = divmod(number, base)
        digits.append(remainder)
    # Разворачиваем, потому что цифры накапливались с младшего разряда
    return digits[::-1]


def from_digits(digits, base=10):
    """Преобразует список цифр (старший разряд первый) обратно в число."""
    value = 0
    for d in digits:
        value = value * base + d
    return value


def fast_square_verbose(digits_first, base=10):
    """
    Быстрое возведение в квадрат длинного числа.
    
    Параметры:
      digits_first – список цифр числа,
          записанных от старшего разряда к младшему (как обычно пишем число).
      base – основание системы счисления.
    
    Возвращает:
      Список цифр квадрата числа, старший разряд первый.
    """
    # Для удобства внутри будем работать с цифрами, хранящимися
    # от младшего разряда к старшему (индекс 0 – единицы).
    x = digits_first[::-1]
    n = len(x)

    # Результат может содержать до 2n+1 цифры. Создадим массив с запасом.
    # Каждая ячейка будет накапливать значение соответствующего разряда.
    y = [0] * (2 * n + 1)

    # Основной цикл: перебираем все пары (i, j) с i <= j,
    # учитывая, что 2*x_i*x_j при i < j – это и есть перекрёстные члены.
    for i in range(n):
        # 1. Квадрат текущей цифры (i = j)
        digit_square = x[i] * x[i]
        pos = 2 * i               # разряд, в который попадает x_i^2 * base^{2i}
        sum_val = y[pos] + digit_square
        y[pos] = sum_val % base
        carry = sum_val // base    # перенос в старший разряд

        # 2. Перекрёстные произведения с более старшими цифрами
        for j in range(i + 1, n):
            cross_term = 2 * x[i] * x[j]
            pos = i + j           # разряд для 2*x_i*x_j * base^{i+j}
            sum_val = y[pos] + cross_term + carry
            y[pos] = sum_val % base
            carry = sum_val // base

        # 3. Если после обработки всех j остался перенос,
        #    помещаем его в следующий свободный разряд.
        #    Он попадает в позицию i + n (после последнего j = n-1 имеем pos = i+(n-1),
        #    а перенос идёт на разряд выше, т.е. i+n).
        if carry > 0:
            # Если переносов несколько, продвигаем их, пока не исчерпаем
            k = i + n
            while carry > 0:
                sum_val = y[k] + carry
                y[k] = sum_val % base
                carry = sum_val // base
                k += 1

    # Теперь y содержит цифры квадрата, начиная с младшего разряда.
    # Убираем возможные ведущие нули (в старших разрядах они находятся в конце массива).
    while len(y) > 1 and y[-1] == 0:
        y.pop()

    # Возвращаем список цифр в привычном порядке: старший разряд первый.
    return y[::-1]


# -------------------------------------------------------------------
# Пример использования и проверка
if __name__ == "__main__":
    # Тест 1: десятичное число
    original = 1234
    base10 = 10
    x_digits = to_digits(original, base10)
    result_digits = fast_square_verbose(x_digits, base10)
    result_num = from_digits(result_digits, base10)

    print(f"Исходное число: {original}")
    print(f"Его цифры (старший разряд первый): {x_digits}")
    print(f"Квадрат (алгоритм): {result_num}")
    print(f"Проверка встроенным умножением: {original ** 2}")

    # Тест 2: двоичное число
    print("\n" + "=" * 40)
    base2 = 2
    bin_digits = [1, 1, 0, 1]   # 13 в десятичной системе
    original_bin = from_digits(bin_digits, base2)
    square_bin_digits = fast_square_verbose(bin_digits, base2)
    square_bin_value = from_digits(square_bin_digits, base2)

    print(f"Двоичное число: {bin_digits} = {original_bin}")
    print(f"Квадрат в двоичном виде: {square_bin_digits} = {square_bin_value}")
    print(f"Проверка: {original_bin}^2 = {original_bin ** 2}")