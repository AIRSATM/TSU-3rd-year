# ==========================================
# 0. Загрузка и подготовка данных
# ==========================================
# Читаем файл (учитываем разную длину столбцов с помощью fill = TRUE)
pulse_data <- read.table("pulse.txt", header = TRUE, sep = "\t", fill = TRUE)

# Разделяем на отдельные векторы и удаляем пропуски (NA)
CB <- na.omit(pulse_data$CB) # Пациенты ДО
EB <- na.omit(pulse_data$EB) # Здоровые ДО
CA <- na.omit(pulse_data$CA) # Пациенты ПОСЛЕ
EA <- na.omit(pulse_data$EA) # Здоровые ПОСЛЕ

# ==========================================
# 1. Проверка выборки на нормальность
# ==========================================
# Статистический тест Шапиро-Уилка
cat("--- Тест Шапиро-Уилка на нормальность ---\n")
print(shapiro.test(CB))
print(shapiro.test(CA))
print(shapiro.test(EB))
print(shapiro.test(EA))

# Графическая проверка (Квантильные графики QQ-plot)
par(mfrow = c(2, 2)) # Сетка 2х2 для графиков
qqnorm(CB, main = "QQ-plot: Пациенты ДО (CB)"); qqline(CB, col = "red")
qqnorm(CA, main = "QQ-plot: Пациенты ПОСЛЕ (CA)"); qqline(CA, col = "red")
qqnorm(EB, main = "QQ-plot: Здоровые ДО (EB)"); qqline(EB, col = "red")
qqnorm(EA, main = "QQ-plot: Здоровые ПОСЛЕ (EA)"); qqline(EA, col = "red")

# ==========================================
# 2. Сравнение данных «до» и «после» (Связанные выборки)
# ==========================================
cat("\n--- Сравнение 'до' и 'после' (Критерий Вилкоксона) ---\n")
# Используем парный критерий Вилкоксона, так как замеры сделаны на одних и тех же людях
wilcox_patients <- wilcox.test(CB, CA, paired = TRUE)
wilcox_healthy  <- wilcox.test(EB, EA, paired = TRUE)

print(paste("Пациенты (CB vs CA) p-value:", round(wilcox_patients$p.value, 4)))
print(paste("Здоровые (EB vs EA) p-value:", format(wilcox_healthy$p.value, scientific = TRUE)))

# Построение коробок с усами
dev.new() # Открыть новое окно для графиков
par(mfrow = c(1, 2))
boxplot(CB, CA, names = c("До (CB)", "После (CA)"), main = "Пациенты", col = c("#ADC6E8", "#A8E6CF"))
boxplot(EB, EA, names = c("До (EB)", "После (EA)"), main = "Здоровые", col = c("#ADC6E8", "#A8E6CF"))

# ==========================================
# 3. Сравнение «здоровых» и «пациентов» (Независимые выборки)
# ==========================================
cat("\n--- Сравнение Здоровых и Пациентов (Критерий Манна-Уитни) ---\n")
# Используем непарный критерий Манна-Уитни (Вилкоксона для независимых выборок)
wilcox_before <- wilcox.test(EB, CB, paired = FALSE)
wilcox_after  <- wilcox.test(EA, CA, paired = FALSE)

print(paste("Сравнение ДО (EB vs CB) p-value:", round(wilcox_before$p.value, 4)))
print(paste("Сравнение ПОСЛЕ (EA vs CA) p-value:", format(wilcox_after$p.value, scientific = TRUE)))

# Построение коробок с усами
dev.new()
par(mfrow = c(1, 2))
boxplot(EB, CB, names = c("Здоровые (EB)", "Пациенты (CB)"), main = "До применения", col = c("#FFD3B6", "#FFAAA6"))
boxplot(EA, CA, names = c("Здоровые (EA)", "Пациенты (CA)"), main = "После применения", col = c("#FFD3B6", "#FFAAA6"))

# ==========================================
# 0. Загрузка данных
# ==========================================
grades_data <- read.table("grades.txt", header = TRUE, sep = "\t", 
                          check.names = FALSE, fileEncoding = "UTF-16")

# Преобразуем данные из широкого формата в длинный (переменные Grade и Group)
grades_long <- stack(grades_data)
colnames(grades_long) <- c("Grade", "Group")

# ==========================================
# 1. Составление таблицы сопряженности
# ==========================================
contingency_table <- table(grades_long$Group, grades_long$Grade)

cat("--- Таблица сопряженности (Группа / Оценка) ---\n")
print(contingency_table)

# ==========================================
# 2. Проверка гипотезы и выбор критерия
# ==========================================
# Сначала выполним тест Хи-квадрат Пирсона
chi2_test <- chisq.test(contingency_table)

cat("\n--- Ожидаемые частоты (для проверки условий) ---\n")
print(chi2_test$expected)

cat("\n--- Результаты критерия Хи-квадрат Пирсона ---\n")
print(chi2_test)