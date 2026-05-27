class BigNumber:
    BASE = 1 << 30      # основание системы счисления (2^30)
    BASE_SIZE = 30      # количество бит в одном слове

    def __init__(self, value):
        """
        value может быть:
          - целым числом Python
          - списком 30-битных слов (младший разряд в начале)
        """
        if isinstance(value, int):
            if value == 0:
                self.coef = [0]
            else:
                coef = []
                while value > 0:
                    coef.append(value & (self.BASE - 1))   # value % BASE
                    value >>= self.BASE_SIZE
                self.coef = coef
        elif isinstance(value, list):
            # предполагаем, что передан корректный список слов
            self.coef = value[:]
            self._strip()
        else:
            raise TypeError("Значение должно быть int или list")

    def _strip(self):
        """Удаляет старшие нулевые слова (ведущие нули)."""
        while len(self.coef) > 1 and self.coef[-1] == 0:
            self.coef.pop()

    def __int__(self):
        """Преобразование обратно в целое Python (для тестирования)."""
        res = 0
        for word in reversed(self.coef):
            res = (res << self.BASE_SIZE) | word
        return res

    def __repr__(self):
        return f"BigNumber({self.coef})"

    def __eq__(self, other):
        return self.coef == other.coef

    # ---------- Статические методы для работы с битами ----------
    @staticmethod
    def bit_length(num):
        """Возвращает количество значащих бит в числе (аналог int.bit_length())."""
        if len(num.coef) == 1 and num.coef[0] == 0:
            return 0
        msb_word = len(num.coef) - 1
        # старшее слово гарантированно не ноль (благодаря _strip)
        val = num.coef[msb_word]
        bits_in_word = val.bit_length()   # встроенный метод для int
        return msb_word * BigNumber.BASE_SIZE + bits_in_word

    @staticmethod
    def get_bit(num, pos):
        """Извлекает бит на позиции pos (0 = младший)."""
        if pos < 0:
            return -1
        word_idx = pos // BigNumber.BASE_SIZE
        bit_idx = pos % BigNumber.BASE_SIZE
        if word_idx >= len(num.coef):
            return 0
        return (num.coef[word_idx] >> bit_idx) & 1

    # ---------- Арифметические операции ----------
    def __mul__(self, other):
        """Умножение двух длинных чисел «в столбик»."""
        n = len(self.coef)
        m = len(other.coef)
        result = [0] * (n + m)
        for i in range(n):
            carry = 0
            for j in range(m):
                val = result[i + j] + self.coef[i] * other.coef[j] + carry
                result[i + j] = val % self.BASE
                carry = val // self.BASE
            result[i + m] += carry
        res = BigNumber(result)
        res._strip()
        return res

    def fast_square(self):
        """
        Быстрое возведение в квадрат.
        Использует симметрию: (x_i * x_j) считается один раз и удваивается.
        """
        x = self.coef
        n = len(x)
        y = [0] * (2 * n + 1)
        for i in range(n):
            # квадрат x_i
            val = y[2 * i] + x[i] * x[i]
            y[2 * i] = val % self.BASE
            carry = val // self.BASE

            # перекрёстные члены 2 * x_i * x_j, i < j
            for j in range(i + 1, n):
                val = y[i + j] + 2 * x[i] * x[j] + carry
                y[i + j] = val % self.BASE
                carry = val // self.BASE

            # размещение оставшегося переноса
            k = i + n
            while carry > 0:
                val = y[k] + carry
                y[k] = val % self.BASE
                carry = val // self.BASE
                k += 1

        res = BigNumber(y)
        res._strip()
        return res

    # ---------- Дихотомический алгоритм возведения в степень ----------
    def exponentiation_t(self, exponent):
        """
        Быстрое возведение self в степень exponent (дихотомический метод).
        exponent – объект BigNumber.
        """
        # Обработка особых случаев
        zero = BigNumber([0])
        one = BigNumber([1])

        # 0^0 = 1 (по соглашению, часто принимают)
        if self == zero and exponent == zero:
            return one
        # 0^a = 0 (a > 0)
        if self == zero:
            return zero
        # a^0 = 1
        if exponent == zero:
            return one
        # a^1 = a
        if exponent == one:
            return BigNumber(self.coef[:])
        # 1^a = 1
        if self == one:
            return one

        # Основной цикл: бинарное возведение в степень
        bits = BigNumber.bit_length(exponent)
        z = BigNumber([1])          # результат
        q = BigNumber(self.coef[:]) # текущая степень основания

        for i in range(bits):
            if BigNumber.get_bit(exponent, i) == 1:
                z = z * q
            q = q.fast_square()

        return z

    # для удобства можно добавить __pow__, но оставим как есть

if __name__ == "__main__":
    # Проверка на встроенных целых
    a = BigNumber(12345678901234567890)
    exp = BigNumber(100)

    res = a.exponentiation_t(exp)
    print(int(res))  # огромное число

    # Сравним с встроенным pow (вычисления без модуля)
    expected = pow(12345678901234567890, 100)
    print(int(res) == expected)  # True