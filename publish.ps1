$ErrorActionPreference = "Stop"
dotnet restore
dotnet publish .\FactorialApp.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o .\publish
Write-Host "Published to .\publish"
