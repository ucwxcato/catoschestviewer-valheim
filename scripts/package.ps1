[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$project = Join-Path $repoRoot 'src\CatosChestViewer\CatosChestViewer.csproj'
$dllPath = Join-Path $repoRoot 'src\CatosChestViewer\bin\Release\net48\net48\CatosChestViewer.dll'
$manifestPath = Join-Path $repoRoot 'thunderstore\manifest.json'
$iconPath = Join-Path $repoRoot 'thunderstore\icon.png'
$screenshotPath = Join-Path $repoRoot 'thunderstore\catoschestviewer.png'
$readmePath = Join-Path $repoRoot 'thunderstore\README.md'
$changelogPath = Join-Path $repoRoot 'CHANGELOG.md'
$artifactsRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot 'artifacts'))

$manifest = Get-Content -Raw -LiteralPath $manifestPath | ConvertFrom-Json
if ($manifest.name -ne 'CatosChestViewer') {
    throw "Unexpected package name '$($manifest.name)'."
}
if ($manifest.version_number -notmatch '^\d+\.\d+\.\d+$') {
    throw 'Manifest version_number must use MAJOR.MINOR.PATCH format.'
}
if ([string]::IsNullOrWhiteSpace($manifest.description) -or $manifest.description.Length -gt 250) {
    throw 'Manifest description must contain 1-250 characters.'
}
if ($null -eq $manifest.dependencies -or
    $manifest.dependencies -notcontains 'denikson-BepInExPack_Valheim-5.4.2350') {
    throw 'Manifest must declare BepInExPack Valheim 5.4.2350.'
}

foreach ($requiredPath in @($project, $manifestPath, $iconPath, $screenshotPath, $readmePath, $changelogPath)) {
    if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
        throw "Required package file is missing: $requiredPath"
    }
}

Add-Type -AssemblyName System.Drawing
$icon = [System.Drawing.Image]::FromFile($iconPath)
try {
    if ($icon.Width -ne 256 -or $icon.Height -ne 256) {
        throw "Thunderstore icon must be 256x256 pixels; found $($icon.Width)x$($icon.Height)."
    }
}
finally {
    $icon.Dispose()
}

$screenshot = [System.Drawing.Image]::FromFile($screenshotPath)
try {
    if ($screenshot.Width -lt 1 -or $screenshot.Height -lt 1) {
        throw 'Gameplay screenshot must have non-zero dimensions.'
    }
}
finally {
    $screenshot.Dispose()
}

dotnet build $project -c Release
if ($LASTEXITCODE -ne 0) {
    throw "CatosChestViewer build failed with exit code $LASTEXITCODE."
}

$assemblyVersion = [System.Reflection.AssemblyName]::GetAssemblyName($dllPath).Version
$manifestVersion = [System.Version]::Parse($manifest.version_number)
if ($assemblyVersion.Major -ne $manifestVersion.Major -or
    $assemblyVersion.Minor -ne $manifestVersion.Minor -or
    $assemblyVersion.Build -ne $manifestVersion.Build) {
    throw "DLL version $assemblyVersion does not match manifest version $manifestVersion."
}

$packageRoot = [System.IO.Path]::GetFullPath((Join-Path $artifactsRoot "package-$($manifest.version_number)"))
$outputPath = [System.IO.Path]::GetFullPath((Join-Path $artifactsRoot "CatosChestViewer-$($manifest.version_number).zip"))
$artifactsPrefix = $artifactsRoot.TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
foreach ($targetPath in @($packageRoot, $outputPath)) {
    if (-not $targetPath.StartsWith($artifactsPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Unsafe package path: $targetPath"
    }
}

New-Item -ItemType Directory -Path $artifactsRoot -Force | Out-Null
if (Test-Path -LiteralPath $packageRoot) {
    Remove-Item -LiteralPath $packageRoot -Recurse -Force
}
if (Test-Path -LiteralPath $outputPath) {
    Remove-Item -LiteralPath $outputPath -Force
}
New-Item -ItemType Directory -Path $packageRoot | Out-Null

Copy-Item -LiteralPath $dllPath -Destination (Join-Path $packageRoot 'CatosChestViewer.dll')
Copy-Item -LiteralPath $manifestPath -Destination (Join-Path $packageRoot 'manifest.json')
Copy-Item -LiteralPath $iconPath -Destination (Join-Path $packageRoot 'icon.png')
Copy-Item -LiteralPath $screenshotPath -Destination (Join-Path $packageRoot 'catoschestviewer.png')
Copy-Item -LiteralPath $readmePath -Destination (Join-Path $packageRoot 'README.md')
Copy-Item -LiteralPath $changelogPath -Destination (Join-Path $packageRoot 'CHANGELOG.md')

$expectedFiles = @(
    'CatosChestViewer.dll',
    'manifest.json',
    'icon.png',
    'catoschestviewer.png',
    'README.md',
    'CHANGELOG.md'
)
$stagedFiles = @(Get-ChildItem -LiteralPath $packageRoot -File | ForEach-Object { $_.Name })
if (@(Compare-Object -ReferenceObject $expectedFiles -DifferenceObject $stagedFiles).Count -ne 0 -or
    @(Get-ChildItem -LiteralPath $packageRoot -Directory).Count -ne 0) {
    throw 'Package staging directory contains unexpected files.'
}

Compress-Archive -Path (Join-Path $packageRoot '*') -DestinationPath $outputPath -CompressionLevel Optimal -Force

$archive = Get-Item -LiteralPath $outputPath
$hash = Get-FileHash -LiteralPath $outputPath -Algorithm SHA256
Write-Host "Created $($archive.FullName)"
Write-Host "Size: $($archive.Length) bytes"
Write-Host "SHA256: $($hash.Hash)"
Write-Host "Contents: $($expectedFiles -join ', ')"
