set -e

dotnet pack R3D-cs/R3D-cs.csproj \
  -c Release \
  -p:Version=0.12.0-alpha.1 \
  -p:PackageVersion=0.12.0-alpha.1

dotnet nuget push \
  R3D-cs/bin/Release/Meatcorps.R3D-cs.0.12.0-alpha.1.nupkg \
  --source /Users/sinsys/NuGetLocal