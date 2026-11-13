[CmdletBinding()]
param(
    [string]$GameDirectory = 'D:\Steam\steamapps\common\Slay the Spire 2',
    [string]$SourceDirectory = (Join-Path $PSScriptRoot '..\..\TestTheSpire')
)

$ErrorActionPreference = 'Stop'
$workspace = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$source = (Resolve-Path -LiteralPath $SourceDirectory).Path
$staging = Join-Path $workspace 'tmp\testthespire-113'
New-Item -ItemType Directory -Path $staging -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $source 'TestTheSpire.csproj') -Destination $staging -Force

foreach ($file in (Get-ChildItem -LiteralPath (Join-Path $source 'Framework') -Recurse -File)) {
    $relativePath = [System.IO.Path]::GetRelativePath($source, $file.FullName)
    $destination = Join-Path $staging $relativePath
    New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
    Copy-Item -LiteralPath $file.FullName -Destination $destination -Force
}

$previousFallback = $env:NUGET_FALLBACK_PACKAGES
try {
    $env:NUGET_FALLBACK_PACKAGES = Join-Path $workspace 'packages'
    & dotnet build (Join-Path $staging 'TestTheSpire.csproj') `
        "-p:Sts2DataDir=$(Join-Path $GameDirectory 'data_sts2_windows_x86_64')" `
        "-p:RestorePackagesPath=$(Join-Path $workspace 'packages')" -v:minimal
    if ($LASTEXITCODE -ne 0) {
        throw "TestTheSpire 0.113 rebuild failed with exit code $LASTEXITCODE."
    }
}
finally {
    $env:NUGET_FALLBACK_PACKAGES = $previousFallback
}

Write-Output (Join-Path $staging 'bin\Debug\net9.0\TestTheSpire.dll')
