# =============================================================================
# Лабораторная работа №1 — Задание 2
# Непрерывные распределения: Экспоненциальное и Гамма
# =============================================================================

set.seed(42)
dir.create("plots", showWarnings = FALSE)

# =============================================================================
# ЧАСТЬ 1: ЭКСПОНЕНЦИАЛЬНОЕ РАСПРЕДЕЛЕНИЕ  X ~ Exp(λ)
# =============================================================================
# Плотность: f(x) = λ * e^(-λx),  x >= 0
# MX = 1/λ,   DX = 1/λ²

lambda <- 2.0   # параметр интенсивности
N_exp  <- 150   # объём выборки

# rexp(N, rate=λ) — генерирует N значений экспоненциального распределения
sample_exp <- rexp(N_exp, rate = lambda)

cat("============================================\n")
cat("  ЭКСПОНЕНЦИАЛЬНОЕ РАСПРЕДЕЛЕНИЕ Exp(λ=2)\n")
cat("============================================\n\n")

# --- Шаг 3: Числовые характеристики выборки ---

x_bar_exp  <- mean(sample_exp)
var_exp    <- var(sample_exp)
sd_exp     <- sd(sample_exp)
median_exp <- median(sample_exp)

# Для непрерывного распределения мода вычисляется как середина самого
# частого бина гистограммы
hist_exp  <- hist(sample_exp, plot=FALSE)
mode_exp  <- hist_exp$mids[which.max(hist_exp$counts)]

m3e <- mean((sample_exp - x_bar_exp)^3)
m2e <- mean((sample_exp - x_bar_exp)^2)
skew_exp <- m3e / (m2e^(3/2))
kurt_exp <- mean((sample_exp - x_bar_exp)^4) / (m2e^2) - 3

cat("--- Выборочные характеристики ---\n")
cat(sprintf("Среднее  (x̄)      : %.4f\n", x_bar_exp))
cat(sprintf("Дисперсия (s²)    : %.4f\n", var_exp))
cat(sprintf("СКО (s)           : %.4f\n", sd_exp))
cat(sprintf("Мода (по гист.)   : %.4f\n", mode_exp))
cat(sprintf("Медиана           : %.4f\n", median_exp))
cat(sprintf("Асимметрия        : %.4f\n", skew_exp))
cat(sprintf("Эксцесс           : %.4f\n", kurt_exp))
cat("\nСправка: для Exp(λ) теоретическая асимметрия = 2, эксцесс = 6\n")

# --- Шаг 4: Теоретические характеристики ---
mx_exp_theor <- 1 / lambda
dx_exp_theor <- 1 / lambda^2

cat("\n--- Теоретические характеристики ---\n")
cat(sprintf("MX = 1/λ = 1/%.1f = %.4f\n", lambda, mx_exp_theor))
cat(sprintf("DX = 1/λ² = 1/%.2f = %.4f\n", lambda^2, dx_exp_theor))
cat(sprintf("\nОтклонение среднего:    %.4f (%.2f%%)\n",
            abs(x_bar_exp - mx_exp_theor), abs(x_bar_exp - mx_exp_theor)/mx_exp_theor*100))
cat(sprintf("Отклонение дисперсии:   %.4f (%.2f%%)\n",
            abs(var_exp - dx_exp_theor), abs(var_exp - dx_exp_theor)/dx_exp_theor*100))

# --- Шаг 5: Оценка параметра λ ---
# Метод моментов: MX = 1/λ  =>  λ̂ = 1/x̄
# Это одновременно и оценка максимального правдоподобия
lambda_hat <- 1 / x_bar_exp
cat("\n--- Оценка параметров ---\n")
cat(sprintf("Оценка λ: λ̂ = 1/x̄ = 1/%.4f = %.4f (истинное λ = %.1f)\n",
            x_bar_exp, lambda_hat, lambda))

# --- Шаг 6: Критерий χ² для непрерывного распределения ---
# Разбиваем ось x на интервалы (бины). Количество бинов выбираем по правилу
# Стёрджеса: k ≈ 1 + log2(N)
k_bins <- ceiling(1 + log2(N_exp))    # ≈ 8 интервалов
breaks_exp <- quantile(sample_exp, probs=seq(0,1,length.out=k_bins+1))
breaks_exp[1] <- 0  # начало носителя

obs_exp <- hist(sample_exp, breaks=breaks_exp, plot=FALSE)$counts
exp_prob_exp <- diff(pexp(breaks_exp, rate=lambda))  # P(x_i < X < x_{i+1})
exp_freq_exp <- exp_prob_exp * N_exp

chi2_exp  <- sum((obs_exp - exp_freq_exp)^2 / exp_freq_exp)
df_exp    <- k_bins - 1 - 1  # -1 за оценённый параметр λ
p_val_exp <- pchisq(chi2_exp, df=df_exp, lower.tail=FALSE)

cat("\n--- Критерий χ² (Хи-квадрат) ---\n")
cat(sprintf("χ² наблюдаемое : %.4f\n", chi2_exp))
cat(sprintf("Степени свободы: %d\n",   df_exp))
cat(sprintf("p-значение      : %.4f\n", p_val_exp))
if (p_val_exp > 0.05) {
  cat("Вывод: H0 НЕ ОТВЕРГАЕТСЯ (p > 0.05) — данные согласуются с Exp(2)\n")
} else {
  cat("Вывод: H0 ОТВЕРГАЕТСЯ (p < 0.05)\n")
}

# --- Шаг 2: Гистограмма + теоретическая плотность + ядерная оценка ---
png("plots/task2_exponential.png", width=1200, height=500, res=120)
par(mfrow=c(1,2), mar=c(4,4,3,1))

# Гистограмма относительных частот: частота / (N * ширина бина) = оценка плотности
hist(sample_exp, freq=FALSE, col="lightblue", border="white",
     xlab="x", ylab="Плотность",
     main="Гистограмма и плотность: Exp(λ=2)",
     breaks=15)
# Теоретическая плотность f(x) = λ·e^{-λx}
curve(dexp(x, rate=lambda), add=TRUE, col="firebrick", lwd=2.5)
# Ядерная оценка плотности (KDE) — непараметрическая оценка
lines(density(sample_exp), col="darkgreen", lwd=2, lty=2)
legend("topright", legend=c("Теория", "KDE"),
       col=c("firebrick","darkgreen"), lwd=2, lty=c(1,2))

# Эмпирическая функция распределения
plot(ecdf(sample_exp), col="steelblue", lwd=2,
     xlab="x", ylab="F(x)",
     main="ЭФР vs Теоретическая ФР: Exp(λ=2)")
curve(pexp(x, rate=lambda), add=TRUE, col="firebrick", lwd=2, lty=2)
legend("bottomright", legend=c("ЭФР", "Теория"),
       col=c("steelblue","firebrick"), lwd=2, lty=c(1,2))

dev.off()
cat("\nГрафики сохранены: plots/task2_exponential.png\n")


# =============================================================================
# ЧАСТЬ 2: ГАММА-РАСПРЕДЕЛЕНИЕ  X ~ Γ(α, β)
# =============================================================================
# Плотность: f(x) = x^(α-1) * e^(-βx) * β^α / Γ(α),  x >= 0
# MX = α/β,   DX = α/β²
#
# ВАЖНО: в R функция rgamma использует параметры shape=α и rate=β (= 1/scale),
# что совпадает с нотацией в задании.

alpha <- 3.0   # параметр формы (shape)
beta  <- 1.5   # параметр скорости (rate)
N_gam <- 150

sample_gam <- rgamma(N_gam, shape=alpha, rate=beta)

cat("\n\n============================================\n")
cat("  ГАММА-РАСПРЕДЕЛЕНИЕ Γ(α=3, β=1.5)\n")
cat("============================================\n\n")

# --- Числовые характеристики ---
x_bar_gam  <- mean(sample_gam)
var_gam    <- var(sample_gam)
sd_gam     <- sd(sample_gam)
median_gam <- median(sample_gam)

hist_gam <- hist(sample_gam, plot=FALSE)
mode_gam <- hist_gam$mids[which.max(hist_gam$counts)]

m3g2 <- mean((sample_gam - x_bar_gam)^3)
m2g2 <- mean((sample_gam - x_bar_gam)^2)
skew_gam <- m3g2 / (m2g2^(3/2))
kurt_gam <- mean((sample_gam - x_bar_gam)^4) / (m2g2^2) - 3

cat("--- Выборочные характеристики ---\n")
cat(sprintf("Среднее  (x̄)      : %.4f\n", x_bar_gam))
cat(sprintf("Дисперсия (s²)    : %.4f\n", var_gam))
cat(sprintf("СКО (s)           : %.4f\n", sd_gam))
cat(sprintf("Мода (по гист.)   : %.4f\n", mode_gam))
cat(sprintf("Медиана           : %.4f\n", median_gam))
cat(sprintf("Асимметрия        : %.4f\n", skew_gam))
cat(sprintf("Эксцесс           : %.4f\n", kurt_gam))
cat(sprintf("\nСправка: теоретическая асимметрия = 2/√α = %.4f, эксцесс = 6/α = %.4f\n",
            2/sqrt(alpha), 6/alpha))

# --- Теоретические характеристики ---
mx_gam_theor <- alpha / beta
dx_gam_theor <- alpha / beta^2

cat("\n--- Теоретические характеристики ---\n")
cat(sprintf("MX = α/β = %.1f/%.1f = %.4f\n", alpha, beta, mx_gam_theor))
cat(sprintf("DX = α/β² = %.1f/%.2f = %.4f\n", alpha, beta^2, dx_gam_theor))
cat(sprintf("\nОтклонение среднего:    %.4f (%.2f%%)\n",
            abs(x_bar_gam - mx_gam_theor), abs(x_bar_gam - mx_gam_theor)/mx_gam_theor*100))
cat(sprintf("Отклонение дисперсии:   %.4f (%.2f%%)\n",
            abs(var_gam - dx_gam_theor), abs(var_gam - dx_gam_theor)/dx_gam_theor*100))

# --- Оценка параметров α и β по методу моментов ---
# MX = α/β  =>  α/β = x̄
# DX = α/β² =>  α/β² = s²
# Из двух уравнений: β̂ = x̄/s²,  α̂ = x̄·β̂ = x̄²/s²
beta_hat  <- x_bar_gam / var_gam      # оценка β
alpha_hat <- x_bar_gam^2 / var_gam    # оценка α

cat("\n--- Оценка параметров (метод моментов) ---\n")
cat(sprintf("Оценка α: α̂ = x̄²/s² = %.4f²/%.4f = %.4f (истинное α = %.1f)\n",
            x_bar_gam, var_gam, alpha_hat, alpha))
cat(sprintf("Оценка β: β̂ = x̄/s²  = %.4f/%.4f  = %.4f (истинное β = %.1f)\n",
            x_bar_gam, var_gam, beta_hat, beta))

# --- Критерий χ² ---
k_bins_g  <- ceiling(1 + log2(N_gam))
breaks_gam <- quantile(sample_gam, probs=seq(0,1,length.out=k_bins_g+1))
breaks_gam[1] <- 0

obs_gam <- hist(sample_gam, breaks=breaks_gam, plot=FALSE)$counts
exp_prob_gam <- diff(pgamma(breaks_gam, shape=alpha, rate=beta))
exp_freq_gam <- exp_prob_gam * N_gam

chi2_gam  <- sum((obs_gam - exp_freq_gam)^2 / exp_freq_gam)
df_gam    <- k_bins_g - 1 - 2  # -2 за два оценённых параметра (α и β)
p_val_gam <- pchisq(chi2_gam, df=df_gam, lower.tail=FALSE)

cat("\n--- Критерий χ² (Хи-квадрат) ---\n")
cat(sprintf("χ² наблюдаемое : %.4f\n", chi2_gam))
cat(sprintf("Степени свободы: %d\n",   df_gam))
cat(sprintf("p-значение      : %.4f\n", p_val_gam))
if (p_val_gam > 0.05) {
  cat("Вывод: H0 НЕ ОТВЕРГАЕТСЯ (p > 0.05) — данные согласуются с Γ(3, 1.5)\n")
} else {
  cat("Вывод: H0 ОТВЕРГАЕТСЯ (p < 0.05)\n")
}

# --- Графики ---
png("plots/task2_gamma.png", width=1200, height=500, res=120)
par(mfrow=c(1,2), mar=c(4,4,3,1))

hist(sample_gam, freq=FALSE, col="lightgreen", border="white",
     xlab="x", ylab="Плотность",
     main="Гистограмма и плотность: Γ(3, 1.5)",
     breaks=15)
curve(dgamma(x, shape=alpha, rate=beta), add=TRUE, col="firebrick", lwd=2.5)
lines(density(sample_gam), col="purple", lwd=2, lty=2)
legend("topright", legend=c("Теория", "KDE"),
       col=c("firebrick","purple"), lwd=2, lty=c(1,2))

plot(ecdf(sample_gam), col="darkgreen", lwd=2,
     xlab="x", ylab="F(x)",
     main="ЭФР vs Теоретическая ФР: Γ(3, 1.5)")
curve(pgamma(x, shape=alpha, rate=beta), add=TRUE, col="firebrick", lwd=2, lty=2)
legend("bottomright", legend=c("ЭФР", "Теория"),
       col=c("darkgreen","firebrick"), lwd=2, lty=c(1,2))

dev.off()
cat("\nГрафики сохранены: plots/task2_gamma.png\n")
cat("\n=== Задание 2 завершено ===\n")
