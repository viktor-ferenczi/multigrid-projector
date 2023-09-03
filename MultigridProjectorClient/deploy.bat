@echo off
setlocal enabledelayedexpansion

:: Check if the required parameters are passed
:: (3rd param will be blank if there are not enough)
if "%~3" == "" (
    echo ERROR: Missing required parameters
        endlocal
    exit /b 1
)

:: Extract parameters and remove quotes
set NAME=%~1
set SOURCE=%~2
set BIN64=%~3

:: Remove trailing backslash if applicable
if "%NAME:~-1%"=="\" set NAME=%NAME:~0,-1%
if "%SOURCE:~-1%"=="\" set SOURCE=%SOURCE:~0,-1%
if "%BIN64:~-1%"=="\" set BIN64=%BIN64:~0,-1%

:: Get the plugin directory
set PLUGIN_DIR=%BIN64%\Plugins\Local

:: Create this directory if it does not exist
if not exist "%PLUGIN_DIR%" (
    echo Creating "Local\" folder in "%BIN64%\Plugins\"
    mkdir "%PLUGIN_DIR%" >NUL 2>&1
)

:: Copy the plugin into the plugin directory
echo Copying "%NAME%" to "%PLUGIN_DIR%\"

for /l %%i in (1, 1, 10) do (
    copy /y "%SOURCE%\%NAME%" "%PLUGIN_DIR%\"

    if !ERRORLEVEL! NEQ 0 (
        :: "timeout" requires input redirection which is not supported,
        :: so we use ping as a way to delay the script between retries.
        ping -n 2 127.0.0.1 >NUL 2>&1
    ) else (
        goto BREAK_LOOP_PLUGIN
    )
)

:: This part will only be reached if the loop has been exhausted
:: Any success would skip to the BREAK_LOOP_PLUGIN label below
echo ERROR: Could not copy "%NAME%".
endlocal
exit /b 1

:BREAK_LOOP_PLUGIN

:: Copy Harmony into the plugin directory
echo Copying "0Harmony.dll" to "%PLUGIN_DIR%\"

for /l %%i in (1, 1, 10) do (
    copy /y "%SOURCE%\0Harmony.dll" "%PLUGIN_DIR%\"

    if !ERRORLEVEL! NEQ 0 (
        :: "timeout" requires input redirection which is not supported,
        :: so we use ping as a way to delay the script between retries.
        ping -n 2 127.0.0.1 >NUL 2>&1
    ) else (
        goto BREAK_LOOP_HARMONY
    )
)

:: This part will only be reached if the loop has been exhausted
:: Any success would skip to the BREAK_LOOP_HARMONY label below
echo ERROR: Could not copy "0Harmony.dll".
endlocal
exit /b 1

:BREAK_LOOP_HARMONY

endlocal
exit /b 0
