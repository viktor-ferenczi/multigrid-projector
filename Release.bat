@echo off
if [%1]==[] goto usage

SET name=MultigridProjector
SET version=%1
SET p7z="C:\Program Files\7-Zip\7z.exe"

SET harmony=MultigridProjectorClient\bin\Release\0Harmony.dll

SET client_build_dir=MultigridProjectorClient\bin\Release
SET ds_build_dir=MultigridProjectorDedicated\bin\Release
SET torch_build_dir=MultigridProjectorServer\bin\Release

SET client_pkg=%name%-Client-%version%
SET ds_pkg=%name%-DedicatedServer-%version%
SET torch_pkg=%name%-Torch-%version%

mkdir "%client_pkg%"
mkdir "%ds_pkg%"
mkdir "%torch_pkg%"

copy /y "%harmony%" "%client_pkg%\"
copy /y "%client_build_dir%\%name%Client.dll" "%client_pkg%\"

copy /y "%harmony%" "%ds_pkg%\"
copy /y "%ds_build_dir%\%name%Dedicated.dll" "%ds_pkg%\"

copy /y "%harmony%" "%torch_pkg%\"
copy /y "%torch_build_dir%\%name%Server.dll" "%torch_pkg%\"
copy /y "%torch_build_dir%\manifest.xml" "%torch_pkg%\"

%p7z% a -tzip %name%-Client-%version%.zip "%client_pkg%"
%p7z% a -tzip %name%-DedicatedServer-%version%.zip "%ds_pkg%"
%p7z% a -tzip %name%-Torch-%version%.zip "%torch_pkg%"

cd "%torch_pkg%"
%p7z% a -tzip ..\%name%.zip *.*
cd ..

rd /s /q "%client_pkg%"
rd /s /q "%ds_pkg%"
rd /s /q "%torch_pkg%"

echo Done
goto :eof

:usage
@echo Usage: %0 VERSION

:eof
cd ..
pause