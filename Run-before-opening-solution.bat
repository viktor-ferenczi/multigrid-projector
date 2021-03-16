:: This script creates a symlink to the game binaries to account for different installation directories on different systems.

@echo off

:Again1
set /p path="Please enter the folder location of your SpaceEngineersDedicated.exe: "
cd %~dp0
mklink /J DedicatedServerBinaries "%path%"
if errorlevel 1 goto Error1
goto End1
:Error1
echo An error occured creating the symlink.
goto Again1
:End1

:Again2
set /p path="Please enter the folder location of your Torch.Server.exe: "
cd %~dp0
mklink /J TorchBinaries "%path%"
if errorlevel 1 goto Error2
goto End2
:Error2
echo An error occured creating the symlink.
goto Again2
:End2

:Again3
set /p path="Please enter the folder location of your SpaceEngineers.exe: "
cd %~dp0
mklink /J InstalledGameBinaries "%path%"
if errorlevel 1 goto Error3
echo Done! - You can now open the solution without issue.
goto End3
:Error3
echo An error occured creating the symlink.
goto Again3
:End3
pause
