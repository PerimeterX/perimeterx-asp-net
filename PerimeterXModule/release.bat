@echo off

del *.nupkg
nuget pack PerimeterXModule.csproj -Prop Configuration=Release
rem nuget push *.nupkg -Source https://www.nuget.org/api/v2/package

del *.nupkg
nuget pack PerimeterXModule.csproj -Symbols
rem nuget push *.nupkg -Source https://nuget.smbsrc.net/

