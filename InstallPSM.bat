@echo off

if exist .git\ (
  rem nothing here
) else (
  git init
  git remote add origin git@github.com:AffectedArc07/PSM.git
)

git fetch --all
git reset --hard
git switch master
git pull

git submodule init
git submodule update

dotnet build /p:Configuration=Release /p:Platform="Any CPU"
