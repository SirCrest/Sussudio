# Quick launcher for ElgatoCapture
# Opens in Visual Studio and starts debugging

$vsPath = "C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\devenv.exe"
$solutionPath = "C:\Users\crest\source\repos\ElgatoCapture\ElgatoCapture.slnx"

Write-Host "Launching ElgatoCapture in Visual Studio..." -ForegroundColor Cyan

# Launch VS with the solution
& $vsPath $solutionPath

Write-Host "Visual Studio opened. Press F5 in VS to run the app." -ForegroundColor Green
