@echo off

git fetch origin
git pull
git submodule init
git submodule update

dotnet build /p:Configuration=Release /p:Platform="Any CPU"
