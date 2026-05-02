# Quick launcher for Sussudio
# Opens in Visual Studio and starts debugging

$vsPath = "C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\devenv.exe"
$solutionPath = "C:\Users\crest\source\repos\Sussudio\Sussudio.slnx"

Write-Host "Launching Sussudio in Visual Studio..." -ForegroundColor Cyan

# Launch VS with the solution
& $vsPath $solutionPath

Write-Host "Visual Studio opened. Press F5 in VS to run the app." -ForegroundColor Green
