# ============================================================
#  Лабораторная работа №2
#  Интервальные оценки параметров нормального распределения
# ============================================================

# ── Параметры задачи ─────────────────────────────────────────
set.seed(42)          # фиксируем зерно для воспроизводимости
a      <- 5           # истинное математическое ожидание
sigma  <- 2           # истинное СКО (σ)
alpha  <- 0.05        # уровень значимости
N_vals <- c(100, 500, 1000)   # объёмы выборок

cat("═══════════════════════════════════════════════════\n")
cat(sprintf("  Лаб. работа №2  |  N( a=%g, σ=%g )  |  α=%.2f\n", a, sigma, alpha))
cat("═══════════════════════════════════════════════════\n\n")


# ============================================================
# ЗАДАНИЕ 1.  Генерация выборок
# ============================================================
# Генерируем три независимые выборки из N(a, sigma)
samples <- setNames(
  lapply(N_vals, function(n) rnorm(n, mean = a, sd = sigma)),
  paste0("N", N_vals)
)
cat("Задание 1: выборки сгенерированы.\n\n")


# ============================================================
# ЗАДАНИЕ 2.  Гистограммы и Q-Q графики
# ============================================================
# Размещаем 6 графиков в сетке 3×2: для каждого N –
#   левый столбец = гистограмма, правый = Q-Q график
dev.new(width = 11, height = 13, noRStudioGD = TRUE)
par(mfrow = c(3, 2),
    mar   = c(4, 4, 3.2, 1.5),
    oma   = c(0, 0, 3, 0))

for (i in seq_along(N_vals)) {
  n    <- N_vals[i]
  samp <- samples[[i]]   # используем samp, а не x, чтобы не конфликтовать с curve()

  # ── Гистограмма ──────────────────────────────────────────
  hist(samp,
       breaks = "Sturges",
       freq   = FALSE,          # нормированная (плотность, а не частоты)
       col    = "lightsteelblue",
       border = "white",
       main   = bquote(bold("Гистограмма,") ~ N == .(n)),
       xlab   = "x",
       ylab   = "Плотность")
  # Накладываем теоретическую плотность N(a, σ)
  # В curve() переменная x — это аргумент функции, а не внешний вектор
  curve(dnorm(x, mean = a, sd = sigma),
        add = TRUE, col = "firebrick", lwd = 2.5)
  legend("topright",
         legend = c("Теоретич. плотность"),
         col    = "firebrick", lwd = 2.5,
         cex    = 0.82, bty = "n")

  # ── Q-Q график (квантиль–квантиль) ───────────────────────
  # Если точки ложатся на прямую — данные нормальны
  qqnorm(samp,
         main = bquote(bold("Q-Q график,") ~ N == .(n)),
         col  = "steelblue", pch = 20, cex = 0.55)
  qqline(samp, col = "firebrick", lwd = 2)
}
mtext("Задание 2: гистограммы и Q-Q графики",
      outer = TRUE, font = 2, cex = 1.25)


# ============================================================
# ЗАДАНИЕ 3.  Точечные оценки числовых характеристик
# ============================================================
# Вычисляем выборочное среднее, дисперсию (несмещённую, n-1) и СКО
cat("─────────────────────────────────────────────────────────\n")
cat("Задание 3: Точечные оценки\n")
cat(sprintf("  Истинные параметры: a = %g   σ² = %g   σ = %g\n\n", a, sigma^2, sigma))

stats_list <- lapply(seq_along(N_vals), function(i) {
  samp <- samples[[i]]
  list(n    = N_vals[i],
       xbar = mean(samp),        # точечная оценка МО
       s2   = var(samp),         # несмещённая оценка дисперсии
       s    = sd(samp))          # СКО = sqrt(s²)
})

cat(sprintf("  %-7s  %-12s  %-12s  %-10s\n", "N", "x̄ (МО)", "s² (дисп.)", "s (СКО)"))
cat(sprintf("  %s\n", strrep("-", 47)))
for (st in stats_list) {
  cat(sprintf("  N=%-5d  %-12.4f  %-12.4f  %-10.4f\n",
              st$n, st$xbar, st$s2, st$s))
}
cat("\n")


# ============================================================
# ЗАДАНИЕ 4.  ДИ для МО при ИЗВЕСТНОЙ дисперсии (z-интервал)
# ============================================================
#
#   ДИ: [x̄ − z · σ/√n ;  x̄ + z · σ/√n]
#
#   Откуда берётся формула?
#   Случайная величина Z = (x̄ − a) / (σ/√n) ~ N(0,1).
#   Из условия P(−z ≤ Z ≤ z) = 1 − α  →  z = z_{1−α/2}.
#
z_crit <- qnorm(1 - alpha / 2)   # z_{0.975} ≈ 1.9600

cat("─────────────────────────────────────────────────────────\n")
cat("Задание 4: ДИ (ИЗВЕСТНАЯ дисперсия, z-интервал)\n")
cat(sprintf("  z_{1-α/2} = z_{%.3f} = %.4f\n\n", 1 - alpha / 2, z_crit))

CI_z <- lapply(stats_list, function(st) {
  half_width <- z_crit * sigma / sqrt(st$n)   # полуширина интервала
  list(n     = st$n,
       xbar  = st$xbar,
       lower = st$xbar - half_width,
       upper = st$xbar + half_width,
       width = 2 * half_width)
})

cat(sprintf("  %-7s  %-10s  %-12s  %-12s  %-10s\n",
            "N", "x̄", "Нижн. граница", "Верхн. граница", "Ширина"))
cat(sprintf("  %s\n", strrep("-", 57)))
for (ci in CI_z) {
  cat(sprintf("  N=%-5d  %-10.4f  %-13.4f  %-13.4f  %-10.4f\n",
              ci$n, ci$xbar, ci$lower, ci$upper, ci$width))
}
cat(sprintf("\n  ВЫВОД: при увеличении N в 10 раз ширина ДИ уменьшается в √10 ≈ %.2f раз.\n\n",
            sqrt(10)))


# ============================================================
# ЗАДАНИЕ 5.  ДИ для МО при НЕИЗВЕСТНОЙ дисперсии (t-интервал)
# ============================================================
#
#   ДИ: [x̄ − t · s/√n ;  x̄ + t · s/√n]
#
#   Откуда берётся формула?
#   Когда σ неизвестен, заменяем его на s = √(s²).
#   Статистика T = (x̄ − a) / (s/√n) ~ t_{n−1}  (Стьюдент).
#   Из условия P(−t ≤ T ≤ t) = 1 − α  →  t = t_{1−α/2, n−1}.
#   При n → ∞  t_{n−1} → z, поэтому оба интервала сходятся.
#
cat("─────────────────────────────────────────────────────────\n")
cat("Задание 5: ДИ (НЕИЗВЕСТНАЯ дисперсия, t-интервал)\n\n")

CI_t <- lapply(stats_list, function(st) {
  t_crit     <- qt(1 - alpha / 2, df = st$n - 1)   # квантиль Стьюдента
  half_width <- t_crit * st$s / sqrt(st$n)
  list(n      = st$n,
       xbar   = st$xbar,
       lower  = st$xbar - half_width,
       upper  = st$xbar + half_width,
       width  = 2 * half_width,
       t_crit = t_crit)
})

cat(sprintf("  %-7s  %-12s  %-10s  %-12s  %-12s  %-10s\n",
            "N", "t_{1-α/2,n-1}", "x̄", "Нижн.", "Верхн.", "Ширина"))
cat(sprintf("  %s\n", strrep("-", 67)))
for (ci in CI_t) {
  cat(sprintf("  N=%-5d  %-12.4f  %-10.4f  %-12.4f  %-12.4f  %-10.4f\n",
              ci$n, ci$t_crit, ci$xbar, ci$lower, ci$upper, ci$width))
}
cat(sprintf("\n  ВЫВОД: при больших n t_{n-1} ≈ z (например, t_{999} ≈ %.4f ≈ z = %.4f).\n\n",
            qt(0.975, df = 999), z_crit))


# ============================================================
# ЗАДАНИЕ 6.  График: точечные оценки и границы ДИ vs N
# ============================================================
xbar_v  <- sapply(stats_list, `[[`, "xbar")
lower_z <- sapply(CI_z, `[[`, "lower")
upper_z <- sapply(CI_z, `[[`, "upper")
lower_t <- sapply(CI_t, `[[`, "lower")
upper_t <- sapply(CI_t, `[[`, "upper")

# Диапазон оси Y с небольшим отступом
y_all <- c(lower_z, upper_z, lower_t, upper_t, xbar_v, a)
y_pad <- diff(range(y_all)) * 0.18

dev.new(width = 10, height = 6, noRStudioGD = TRUE)
par(mar = c(5, 4.5, 4, 2))

# Точечные оценки МО
plot(N_vals, xbar_v,
     type = "b", pch = 16, lwd = 2.5, col = "black",
     ylim = range(y_all) + c(-y_pad, y_pad),
     xaxt = "n",
     xlab = "Объём выборки  N",
     ylab = "Значение",
     main = "Задание 6: точечные оценки и доверительные интервалы для МО")
axis(1, at = N_vals, labels = N_vals)

# Истинное МО — пунктирная горизонтальная линия
abline(h = a, lty = 2, col = "grey40", lwd = 1.5)

# Границы ДИ при ИЗВЕСТНОЙ дисперсии (синий, lty=2)
lines(N_vals, lower_z, type = "b", pch = 25, lty = 2,
      col = "steelblue", lwd = 1.8, bg = "steelblue")
lines(N_vals, upper_z, type = "b", pch = 24, lty = 2,
      col = "steelblue", lwd = 1.8, bg = "steelblue")

# Границы ДИ при НЕИЗВЕСТНОЙ дисперсии (красный, lty=3)
lines(N_vals, lower_t, type = "b", pch = 25, lty = 3,
      col = "firebrick", lwd = 1.8, bg = "firebrick")
lines(N_vals, upper_t, type = "b", pch = 24, lty = 3,
      col = "firebrick", lwd = 1.8, bg = "firebrick")

# Лёгкое заполнение областей ДИ (для наглядности)
polygon(c(N_vals, rev(N_vals)), c(lower_z, rev(upper_z)),
        col = adjustcolor("steelblue", alpha.f = 0.10), border = NA)
polygon(c(N_vals, rev(N_vals)), c(lower_t, rev(upper_t)),
        col = adjustcolor("firebrick", alpha.f = 0.08), border = NA)

legend("topright",
       legend = c("x̄  (точечная оценка)",
                  "Нижн./верхн. граница (изв. σ)",
                  "Нижн./верхн. граница (неизв. σ)",
                  paste0("Истинное a = ", a)),
       col    = c("black",    "steelblue",  "firebrick",  "grey40"),
       lty    = c(1,           2,            3,            2),
       pch    = c(16,          25,           25,           NA),
       pt.bg  = c("black",    "steelblue",  "firebrick",   NA),
       lwd    = c(2.5,         1.8,          1.8,          1.5),
       cex    = 0.88,
       bty    = "n")

cat("─────────────────────────────────────────────────────────\n")
cat("Задание 6: график построен.\n\n")
cat("Общий вывод:\n")
cat("  • С ростом N ширина ДИ убывает пропорционально 1/√N.\n")
cat("  • t-интервал (неизв. σ) шире z-интервала (изв. σ),\n")
cat("    но при N ≥ 500 разница практически незаметна.\n")
cat("  • При N=1000 оба интервала почти совпадают.\n")