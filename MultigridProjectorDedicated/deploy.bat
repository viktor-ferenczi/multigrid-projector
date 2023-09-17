@echo off
setlocal enabledelayedexpansion

REM Check if the required parameters are passed
REM (3rd param will be blank if there are not enough)
if "%~3" == "" (
    echo ERROR: Missing required parameters
    exit /b 1
)

REM Extract parameters and remove quotes
set NAME=%~1
set SOURCE=%~2
set DEDICATED64=%~3

REM Remove trailing backslash if applicable
if "%NAME:~-1%"=="\" set NAME=%NAME:~0,-1%
if "%SOURCE:~-1%"=="\" set SOURCE=%SOURCE:~0,-1%
if "%DEDICATED64:~-1%"=="\" set DEDICATED64=%DEDICATED64:~0,-1%

REM Get the plugin directory
set PLUGIN_DIR=%DEDICATED64%\Plugins

REM Create this directory if it does not exist
if not exist "%PLUGIN_DIR%" (
    echo Creating "Plugins\" folder in "%DEDICATED64%\"
    mkdir "%PLUGIN_DIR%" >NUL 2>&1
)

REM Copy the plugin into the plugin directory
echo Copying "%NAME%" to "%PLUGIN_DIR%\"

for /l %%i in (1, 1, 10) do (
    copy /y "%SOURCE%\%NAME%" "%PLUGIN_DIR%\"

    if !ERRORLEVEL! NEQ 0 (
        REM "timeout" requires input redirection which is not supported,
        REM so we use ping as a way to delay the script between retries.
        ping -n 2 127.0.0.1 >NUL 2>&1
    ) else (
        goto BREAK_LOOP_PLUGIN
    )
)

REM This part will only be reached if the loop has been exhausted
REM Any success would skip to the BREAK_LOOP_PLUGIN label below
echo ERROR: Could not copy "%NAME%".
exit /b 1

:BREAK_LOOP_PLUGIN

REM Copy Harmony into the plugin directory
echo Copying "0Harmony.dll" to "%PLUGIN_DIR%\"

for /l %%i in (1, 1, 10) do (
    copy /y "%SOURCE%\0Harmony.dll" "%PLUGIN_DIR%\"

    if !ERRORLEVEL! NEQ 0 (
        REM "timeout" requires input redirection which is not supported,
        REM so we use ping as a way to delay the script between retries.
        ping -n 2 127.0.0.1 >NUL 2>&1
    ) else (
        goto BREAK_LOOP_HARMONY
    )
)

REM This part will only be reached if the loop has been exhausted
REM Any success would skip to the BREAK_LOOP_HARMONY label below
echo ERROR: Could not copy "0Harmony.dll".
exit /b 1

:BREAK_LOOP_HARMONY
exit /b 0
