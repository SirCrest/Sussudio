# Quick launcher for Sussudio
# Opens in Visual Studio and starts debugging

$vsPath = "C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\devenv.exe"
$solutionPath = Join-Path $PSScriptRoot "Sussudio.slnx"

Write-Host "Launching Sussudio in Visual Studio..." -ForegroundColor Cyan

# Launch VS with the solution
& $vsPath (Resolve-Path $solutionPath)

Write-Host "Visual Studio opened. Press F5 in VS to run the app." -ForegroundColor Green
