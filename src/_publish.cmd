@echo off

cd /d %~dp0

REM Completely delete publish folder (Warning: all contents will be deleted)
rmdir /s /q publish

REM Publish SyncBridge related projects

echo Publishing SyncBridgeServer...
dotnet publish .\Aloe\Apps\SyncBridge\Aloe.Apps.SyncBridgeServer\Aloe.Apps.SyncBridgeServer.csproj -c Release -r win-x64 --self-contained true -o .\publish\SyncBridgeServer

echo Publishing SyncBridgeClient...
dotnet publish .\Aloe\Apps\SyncBridge\Aloe.Apps.SyncBridge\Aloe.Apps.SyncBridgeClient\Aloe.Apps.SyncBridgeClient.csproj -c Release -r win-x64 --self-contained true -o .\publish\SyncBridgeClient

echo Publishing DummyService...
dotnet publish .\Aloe\Apps\SyncBridge\Aloe.Apps.DummyService\Aloe.Apps.DummyService.csproj -c Release -r win-x64 --self-contained true -o .\publish\DummyService

echo.
echo Completed.
pause
