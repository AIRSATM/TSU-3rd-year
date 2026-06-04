set.seed(42)

dir.create("plots", showWarnings = FALSE)

n_bin <- 20
p_bin <- 0.4
q_bin <- 1 - p_bin
N_bin <- 150

# сколько успехов из n испытаний при вероятности p
sample_bin <- rbinom(N_bin, size = n_bin, prob = p_bin)

cat("--- биномиальное распределение Bin(20, 0.4)\n")

x_bar_bin  <- mean(sample_bin)             
var_bin    <- var(sample_bin)              
sd_bin     <- sd(sample_bin)             
mode_bin   <- as.integer(names(sort(table(sample_bin), decreasing=TRUE)[1]))
median_bin <- median(sample_bin)

m3 <- mean((sample_bin - x_bar_bin)^3)
m2 <- mean((sample_bin - x_bar_bin)^2)
skew_bin <- m3 / (m2^(3/2))

kurt_bin <- mean((sample_bin - x_bar_bin)^4) / (m2^2) - 3

cat("Выборочные характеристики\n")
cat(sprintf("Среднее  (x̄)     : %.4f\n", x_bar_bin))
cat(sprintf("Дисперсия (s²)   : %.4f\n", var_bin))
cat(sprintf("СКО (s)          : %.4f\n", sd_bin))
cat(sprintf("Мода             : %d\n",   mode_bin))
cat(sprintf("Медиана          : %.1f\n", median_bin))
cat(sprintf("Асимметрия       : %.4f\n", skew_bin))
cat(sprintf("Эксцесс          : %.4f\n", kurt_bin))

# Для Bin(n, p): MX = n*p,  DX = n*p*q

mx_bin_theor <- n_bin * p_bin
dx_bin_theor <- n_bin * p_bin * q_bin

cat("\n--- Теоретические характеристики ---\n")
cat(sprintf("MX = n*p = %d * %.1f = %.4f\n", n_bin, p_bin, mx_bin_theor))
cat(sprintf("DX = n*p*q = %d * %.1f * %.1f = %.4f\n", n_bin, p_bin, q_bin, dx_bin_theor))
cat(sprintf("\nОтклонение среднего от MX:        %.4f (%.2f%%)\n",
            abs(x_bar_bin - mx_bin_theor), abs(x_bar_bin - mx_bin_theor)/mx_bin_theor*100))
cat(sprintf("Отклонение дисперсии от DX:       %.4f (%.2f%%)\n",
            abs(var_bin - dx_bin_theor), abs(var_bin - dx_bin_theor)/dx_bin_theor*100))

# Метод моментов: MX = n*p  =>  vp = lx / n
p_hat_bin <- x_bar_bin / n_bin
cat("\n--- Оценка параметров ---\n")
cat(sprintf("Оценка p: p̂ = x̄/n = %.4f (истинное p = %.1f)\n", p_hat_bin, p_bin))


obs_freq <- table(factor(sample_bin, levels = 0:n_bin))  
theor_prob <- dbinom(0:n_bin, size = n_bin, prob = p_bin) 

exp_freq_full <- theor_prob * N_bin

valid <- exp_freq_full >= 5
left_cutoff  <- min(which(valid))    
right_cutoff <- max(which(valid))     

# Группируем: всё левее left_cutoff — в одну ячейку, правее right_cutoff — в другую
groups_obs <- c(
  sum(obs_freq[1:left_cutoff]),
  obs_freq[(left_cutoff+1):(right_cutoff-1)],
  sum(obs_freq[right_cutoff:length(obs_freq)])
)
groups_exp <- c(
  sum(exp_freq_full[1:left_cutoff]),
  exp_freq_full[(left_cutoff+1):(right_cutoff-1)],
  sum(exp_freq_full[right_cutoff:length(exp_freq_full)])
)

chi2_bin <- sum((groups_obs - groups_exp)^2 / groups_exp)
df_bin   <- length(groups_obs) - 1 - 1  
p_val_bin <- pchisq(chi2_bin, df = df_bin, lower.tail = FALSE)

cat("\nКритерий Хи-квадрат\n")
cat(sprintf("Хи-квадрат наблюдаемое : %.4f\n", chi2_bin))
cat(sprintf("Степени свободы: %d\n",   df_bin))
cat(sprintf("p-значение      : %.4f\n", p_val_bin))
if (p_val_bin > 0.05) {
  cat("Вывод: H0 НЕ ОТВЕРГАЕТСЯ (p > 0.05) — данные согласуются с Bin(20, 0.4)\n")
} else {
  cat("Вывод: H0 ОТВЕРГАЕТСЯ (p < 0.05)\n")
}

png("plots/task1_binomial.png", width=1200, height=500, res=120)
par(mfrow=c(1,2), mar=c(4,4,3,1))

freq_table_bin <- table(sample_bin) / N_bin
plot(as.integer(names(freq_table_bin)), as.numeric(freq_table_bin),
     type="b", pch=19, col="steelblue", lwd=2,
     xlab="Значение X", ylab="Относительная частота",
     main="Полигон частот: Bin(20, 0.4)",
     xlim=c(0, n_bin), ylim=c(0, max(freq_table_bin)*1.2))
lines(0:n_bin, dbinom(0:n_bin, n_bin, p_bin),
      type="b", pch=1, col="firebrick", lwd=1.5, lty=2)
legend("topright", legend=c("Выборка", "Теория"),
       col=c("steelblue","firebrick"), lwd=2, lty=c(1,2), pch=c(19,1))

plot(ecdf(sample_bin), col="steelblue", lwd=2,
     xlab="x", ylab="F(x)",
     main="ЭФР vs Теоретическая ФР: Bin(20, 0.4)")
x_grid <- 0:n_bin
lines(x_grid, pbinom(x_grid, n_bin, p_bin),
      type="s", col="firebrick", lwd=2, lty=2)
legend("bottomright", legend=c("ЭФР", "Теория"),
       col=c("steelblue","firebrick"), lwd=2, lty=c(1,2))

dev.off()
cat("\nГрафики сохранены: plots\n")

p_geom <- 0.3
q_geom <- 1 - p_geom
N_geom <- 150

# rgeom в R реализует именно "число неудач до первого успеха"
sample_geom <- rgeom(N_geom, prob = p_geom)

cat("  геометрическое распределение Geom(0.3)\n")

x_bar_geom  <- mean(sample_geom)
var_geom    <- var(sample_geom)
sd_geom     <- sd(sample_geom)
mode_geom   <- as.integer(names(sort(table(sample_geom), decreasing=TRUE)[1]))
median_geom <- median(sample_geom)

m3g <- mean((sample_geom - x_bar_geom)^3)
m2g <- mean((sample_geom - x_bar_geom)^2)
skew_geom <- m3g / (m2g^(3/2))
kurt_geom <- mean((sample_geom - x_bar_geom)^4) / (m2g^2) - 3

cat("Выборочные характеристики\n")
cat(sprintf("Среднее  (x)     : %.4f\n", x_bar_geom))
cat(sprintf("Дисперсия (s)   : %.4f\n", var_geom))
cat(sprintf("СКО (s)          : %.4f\n", sd_geom))
cat(sprintf("Мода             : %d\n",   mode_geom))
cat(sprintf("Медиана          : %.1f\n", median_geom))
cat(sprintf("Асимметрия       : %.4f\n", skew_geom))
cat(sprintf("Эксцесс          : %.4f\n", kurt_geom))

mx_geom_theor <- q_geom / p_geom
dx_geom_theor <- q_geom / p_geom^2

cat("\n--- Теоретические характеристики ---\n")
cat(sprintf("MX = q/p = %.1f/%.1f = %.4f\n", q_geom, p_geom, mx_geom_theor))
cat(sprintf("DX = q/p² = %.1f/%.2f = %.4f\n", q_geom, p_geom^2, dx_geom_theor))
cat(sprintf("\nОтклонение среднего:    %.4f (%.2f%%)\n",
            abs(x_bar_geom - mx_geom_theor), abs(x_bar_geom - mx_geom_theor)/mx_geom_theor*100))
cat(sprintf("Отклонение дисперсии:   %.4f (%.2f%%)\n",
            abs(var_geom - dx_geom_theor), abs(var_geom - dx_geom_theor)/dx_geom_theor*100))

p_hat_geom <- 1 / (x_bar_geom + 1)
cat("\n--- Оценка параметров ---\n")
cat(sprintf("Оценка p: p̂ = 1/(x̄+1) = 1/(%.4f+1) = %.4f (истинное p = %.1f)\n",
            x_bar_geom, p_hat_geom, p_geom))

max_k <- qgeom(0.99, p_geom)  # охватываем 99% вероятности
k_vals <- 0:max_k

obs_geom <- table(factor(sample_geom, levels=c(as.character(k_vals), "tail")))
obs_geom["tail"] <- sum(sample_geom > max_k)

theor_prob_geom <- c(dgeom(k_vals, p_geom), pgeom(max_k, p_geom, lower.tail=FALSE))
exp_geom <- theor_prob_geom * N_geom

valid_g <- exp_geom >= 5
obs_valid <- as.numeric(obs_geom)[valid_g]
exp_valid <- exp_geom[valid_g]

chi2_geom <- sum((obs_valid - exp_valid)^2 / exp_valid)
df_geom   <- length(obs_valid) - 1 - 1
p_val_geom <- pchisq(chi2_geom, df=df_geom, lower.tail=FALSE)

cat("\nКритерий Хи-квадрат\n")
cat(sprintf("Хи-квадрат наблюдаемое : %.4f\n", chi2_geom))
cat(sprintf("Степени свободы: %d\n",   df_geom))
cat(sprintf("p-значение      : %.4f\n", p_val_geom))
if (p_val_geom > 0.05) {
  cat("Вывод: H0 НЕ ОТВЕРГАЕТСЯ (p > 0.05) — данные согласуются с Geom(0.3)\n")
} else {
  cat("Вывод: H0 ОТВЕРГАЕТСЯ (p < 0.05)\n")
}

png("plots/task1_geometric.png", width=1200, height=500, res=120)
par(mfrow=c(1,2), mar=c(4,4,3,1))

freq_table_geom <- table(sample_geom) / N_geom
plot(as.integer(names(freq_table_geom)), as.numeric(freq_table_geom),
     type="b", pch=19, col="darkgreen", lwd=2,
     xlab="Значение X (число неудач)", ylab="Относительная частота",
     main="Полигон частот: Geom(0.3)")
k_range <- 0:max(sample_geom)
lines(k_range, dgeom(k_range, p_geom),
      type="b", pch=1, col="firebrick", lwd=1.5, lty=2)
legend("topright", legend=c("Выборка", "Теория"),
       col=c("darkgreen","firebrick"), lwd=2, lty=c(1,2), pch=c(19,1))

plot(ecdf(sample_geom), col="darkgreen", lwd=2,
     xlab="x", ylab="F(x)",
     main="ЭФР vs Теоретическая ФР: Geom(0.3)")
lines(k_range, pgeom(k_range, p_geom),
      type="s", col="firebrick", lwd=2, lty=2)
legend("bottomright", legend=c("ЭФР", "Теория"),
       col=c("darkgreen","firebrick"), lwd=2, lty=c(1,2))

dev.off()
cat("\nГрафики сохранены: plots/task1_geometric.png\n")
