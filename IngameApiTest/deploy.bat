@echo off
if [%1] == [] goto EOF

echo Parameters: %*

set SRC=%1

echo.
echo Deploying Multigrid Projector Ingame API Test:
echo.

mkdir "%AppData%\SpaceEngineers\IngameScripts\local" 2>&1 >NUL
set "SCRIPT_DIR=%AppData%\SpaceEngineers\IngameScripts\local\Multigrid Projector Ingame API Test"

del /f /s /q "%SCRIPT_DIR%" 2>&1 >NUL
rd /s /q "%SCRIPT_DIR%" 2>&1 >NUL
mkdir "%SCRIPT_DIR%" 2>&1 >NUL

xcopy /s /e /y "%SRC%\Script\" "%SCRIPT_DIR%"

echo.
echo Done
echo.
exit 0

:EOF