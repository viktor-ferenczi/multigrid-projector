@echo off
if [%2] == [] goto EOF

echo Parameters: %*

set SRC=%~p1
set NAME=%~2

echo.
echo Deploying Multigrid Projector Mod API Test:
echo.

mkdir "%AppData%\SpaceEngineers\Mods" 2>&1 >NUL
set "MOD_DIR=%AppData%\SpaceEngineers\Mods\Multigrid Projector Mod API Test"

del /f /s /q "%MOD_DIR%" 2>&1 >NUL
rd /s /q "%MOD_DIR%" 2>&1 >NUL
mkdir "%MOD_DIR%" 2>&1 >NUL

xcopy /s /e /y "%SRC%\ApiTest\" "%MOD_DIR%"

set "API_SRC=%SRC%\..\MultigridProjector\Api"
set "API_DST=%MOD_DIR%\Data\Scripts\MultigridProjector\Api"
 
copy /y "%API_SRC%\BlockLocation.cs" "%API_DST%\"
copy /y "%API_SRC%\BlockState.cs" "%API_DST%\"
copy /y "%API_SRC%\IMultigridProjectorApi.cs" "%API_DST%\"
copy /y "%API_SRC%\MultigridProjectorModAgent.cs" "%API_DST%\"

echo.
echo Done
echo.
exit 0

:EOF