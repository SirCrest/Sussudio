param(
    [string[]]$Configurations = @("Debug", "Release")
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "Sussudio\Sussudio.csproj"
$targetFramework = "net8.0-windows10.0.19041.0"
$rid = "win-x64"

function Invoke-RobocopySync {
    param(
        [Parameter(Mandatory = $true)][string]$Source,
        [Parameter(Mandatory = $true)][string]$Destination
    )

    if (!(Test-Path $Source)) {
        throw "Source folder does not exist: $Source"
    }

    New-Item -ItemType Directory -Path $Destination -Force | Out-Null

    $null = & robocopy $Source $Destination /MIR /NFL /NDL /NJH /NJS /NP
    $code = $LASTEXITCODE
    if ($code -ge 8) {
        throw "robocopy failed with exit code $code syncing '$Source' -> '$Destination'"
    }
}

if (!(Test-Path $projectPath)) {
    throw "Project file not found: $projectPath"
}

foreach ($configuration in $Configurations) {
    Write-Host "=== Building x64 $configuration ==="
    & dotnet build $projectPath -c $configuration -p:Platform=x64 -v minimal
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed for configuration: $configuration"
    }

    $source = Join-Path $repoRoot "Sussudio\bin\x64\$configuration\$targetFramework\$rid"
    $dest = Join-Path $repoRoot "builds\$rid\$configuration"

    Write-Host "=== Staging $configuration build to $dest ==="
    Invoke-RobocopySync -Source $source -Destination $dest
}

Write-Host ""
Write-Host "Staged builds:"
foreach ($configuration in $Configurations) {
    Write-Host " - $(Join-Path $repoRoot "builds\$rid\$configuration\Sussudio.exe")"
}
