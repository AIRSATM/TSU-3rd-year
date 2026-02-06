@echo off

echo ===== RUN TEST SET 1 =====
Testing.exe < input_tour.txt > act1.txt
Tester.exe tour_method_tests.txt act1.txt

echo.
echo ===== RUN TEST SET 2 =====
Testing.exe < input_w_method.txt > act2.txt
Tester.exe w_method_tests.txt act2.txt

del act1.txt
del act2.txt

echo.
echo Done.
pause
