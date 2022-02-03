@echo off

REM Location of your Torch.Server.exe
mklink /J TorchDir "C:\Torch"

REM Location of your SpaceEngineers.exe
mklink /J GameBin64 "C:\Program Files (x86)\Steam\steamapps\common\SpaceEngineers\Bin64"

pause
