@echo off
cd /d "%~dp0"

REM
where pio >nul 2>nul
if %errorlevel% neq 0 (
    set "PATH=%PATH%;%USERPROFILE%\.platformio\penv\Scripts"
)

echo Compilando firmware...
pio run -s

if %ERRORLEVEL% equ 0 (
    echo [EXITO] Compilacion completada.
    timeout /t 3 >nul
) else (
    echo [ERROR] Fallo al compilar.
    pause
)
