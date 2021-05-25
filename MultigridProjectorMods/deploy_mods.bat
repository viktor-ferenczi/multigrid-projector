mkdir "%AppData%\SpaceEngineers\Mods" 2>&1 >NUL

REM MGP Mod API Test
del /f /s /q "%AppData%\SpaceEngineers\Mods\Multigrid Projector Mod API Test" 2>&1 >NUL
rd /s /q "%AppData%\SpaceEngineers\Mods\Multigrid Projector Mod API Test" 2>&1 >NUL
mkdir "%AppData%\SpaceEngineers\Mods\Multigrid Projector Mod API Test" 2>&1 >NUL
xcopy /s /e /y ApiTest\ "%AppData%\SpaceEngineers\Mods\Multigrid Projector Mod API Test\"
mkdir "%AppData%\SpaceEngineers\Mods\Multigrid Projector Mod API Test\Data\Scripts\MultigridProjector\Api"
xcopy /s /e /y ..\MultigridProjector\Api\ "%AppData%\SpaceEngineers\Mods\Multigrid Projector Mod API Test\Data\Scripts\MultigridProjector\Api\"

REM MGP Extra
del /f /s /q "%AppData%\SpaceEngineers\Mods\Multigrid Projector Extra" 2>&1 >NUL
rd /s /q "%AppData%\SpaceEngineers\Mods\Multigrid Projector Extra" 2>&1 >NUL
mkdir "%AppData%\SpaceEngineers\Mods\Multigrid Projector Extra" 2>&1 >NUL
xcopy /s /e /y Extra\ "%AppData%\SpaceEngineers\Mods\Multigrid Projector Extra\"