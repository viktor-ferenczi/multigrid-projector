:: This script creates a symlink to the game binaries to account for different installation directories on different systems.

@echo off

:Again2
set /p path="Please enter the folder location of your Torch.Server.exe: "
cd %~dp0
mklink /J TorchDir "%path%"
if errorlevel 1 goto Error2
goto End2
:Error2
echo An error occured creating the symlink.
goto Again2
:End2

:Again3
set /p path="Please enter the folder location of your SpaceEngineers.exe: "
cd %~dp0
mklink /J GameBin64 "%path%"
if errorlevel 1 goto Error3
echo Done! - You can now open the solution without issue.
goto End3
:Error3
echo An error occured creating the symlink.
goto Again3
:End3
pause
