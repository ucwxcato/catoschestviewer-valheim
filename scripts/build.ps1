[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot 'src/CatosChestViewer/CatosChestViewer.csproj'
dotnet build $project -c Release
if ($LASTEXITCODE -ne 0) {
    throw "CatosChestViewer build failed with exit code $LASTEXITCODE."
}
