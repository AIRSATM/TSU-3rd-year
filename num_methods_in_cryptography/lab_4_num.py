import random

def phi(n: int) -> int:
    
    f = n
    if n % 2 == 0:
        while n % 2 == 0:
            n //= 2
        f //= 2

    i = 3
    while i * i <= n:
        if n % i == 0:
            while n % i == 0:
                n //= i
            f = f // i * (i - 1)
        i += 2

    if n > 1:
        f = f // n * (n - 1)
    return f

def estimate_error(n: int, t: int) -> float:

    eps = phi(n) / n
    return eps ** t

def test_ferma(n: int, t: int) -> bool:

    if n <= 1:
        return False
    if n == 2 or n == 3:
        return True
    if n % 2 == 0:
        return False 

    for _ in range(t):
        a = random.randint(2, n - 2)
        
        r = pow(a, n - 1, n)

        if r != 1:
            return False

    return True

def test_ferma_wr(n: int, t: int):
    if n < 3 or n % 2 == 0:
        if n == 2:
             print(f"простое - {n}")
        else:
             print(f"составное (или недопустимое) - {n}")
        return

    is_prime = test_ferma(n, t)

    if not is_prime:
        print(f"составное - {n}")
    else:
        print(f"простое - {n}")
        error_prob = estimate_error(n, t)
        print(f"  Вероятность ошибки <= {error_prob:.6g}")

def test():
    # числа Кармайкла
    test_data = [2, 3, 5, 7, 11, 15, 561, 8911, 10585, 15841, 29341, 41041]
    t = 12
    
    print(f"Запуск тестов (параметр надежности t = {t})")
    for i in test_data:
        test_ferma_wr(i, t)
    print("-" * 50)

def manual_mode():
    try:
        n = int(input("Введите целое число для проверки: "))
        t = int(input("Введите параметр надёжности (количество итераций): "))
        test_ferma_wr(n, t)
    except ValueError:
        print("Ошибка: введите целые числа.")
    
def random_mode():
    try:
        count = int(input("Сколько случайных чисел сгенерировать? (по умолчанию 5): ") or "5")
        min_val = int(input("Минимальное значение (по умолчанию 2): ") or "2")
        max_val = int(input("Максимальное значение (по умолчанию 100000): ") or "100000")
        t = int(input("Параметр надёжности t (по умолчанию 10): ") or "10")
    except ValueError:
        print("Ошибка ввода, используются значения по умолчанию: count=5, min=2, max=100000, t=10")
        count, min_val, max_val, t = 5, 2, 100000, 10

    print(f"\nТестирование {count} случайных чисел в диапазоне [{min_val}, {max_val}] с t={t}")
    for _ in range(count):
        n = random.randint(min_val, max_val)
        print(f"\nЧисло: {n}")
        test_ferma_wr(n, t)

def main():
    while True:
        print("Тест Ферма на простоту чисел")
        print("1. Ручной ввод")
        print("2. Случайные числа")
        print("3. Предопределенные тесты")
        print("4. Выход")
        choice = input("Выберите режим (1-4): ")
        
        if choice == '1':
            manual_mode()
        elif choice == '2':
            random_mode()
        elif choice == '3':
            test()
        elif choice == '4':
            print("Выход из программы.")
            break
        else:
            print("Неверный выбор")    

if __name__ == "__main__":
    main()