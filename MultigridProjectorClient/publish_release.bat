mkdir "%AppData%\SpaceEngineers\Mods" 2>&1 >NUL
mkdir "%AppData%\SpaceEngineers\Mods\Multigrid Projector" 2>&1 >NUL
copy /y Mod\*.* "%AppData%\SpaceEngineers\Mods\Multigrid Projector\"
copy /y bin\Release\MultigridProjectorClient.pdb "%AppData%\SpaceEngineers\Mods\Multigrid Projector\Multigrid_Projector.pdb"
copy /y bin\Release\MultigridProjectorClient.dll "%AppData%\SpaceEngineers\Mods\Multigrid Projector\Multigrid_Projector.plugin"
