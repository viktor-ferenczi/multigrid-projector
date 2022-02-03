mkdir ..\GameBin64\Plugins\Local\ 2>&1 >NUL
copy /y bin\Release\MultigridProjectorClient.pdb ..\GameBin64\Plugins\Local\
copy /y bin\Release\MultigridProjectorClient.dll ..\GameBin64\Plugins\Local\

REM Mod to release on the Steam Workshop in binary form
mkdir "%AppData%\SpaceEngineers\Mods" 2>&1 >NUL
del /f /s /q "%AppData%\SpaceEngineers\Mods\Multigrid Projector" 2>&1 >NUL
rd /s /q "%AppData%\SpaceEngineers\Mods\Multigrid Projector" 2>&1 >NUL
mkdir "%AppData%\SpaceEngineers\Mods\Multigrid Projector" 2>&1 >NUL
xcopy /s /e /y Mod\ "%AppData%\SpaceEngineers\Mods\Multigrid Projector\"
copy /y bin\Release\MultigridProjectorClient.pdb "%AppData%\SpaceEngineers\Mods\Multigrid Projector\Multigrid_Projector.pdb"
copy /y bin\Release\MultigridProjectorClient.dll "%AppData%\SpaceEngineers\Mods\Multigrid Projector\Multigrid_Projector.plugin"