<#
.SYNOPSIS
Runs the Sussudio validation sequence and writes a machine-readable result.

.DESCRIPTION
Run from the repository root. This is the single validation entry point for
agents: it builds the solution, runs the xUnit suite, performs the
assembly-load smoke check, and checks the working tree for whitespace
damage, then writes artifacts/validation.json describing exactly what ran,
what passed, and what the run does NOT prove.

Exit code is 0 only when every executed step succeeded. The JSON is the
authoritative result; the console summary is a convenience.

The smoke step loads the built app assembly. It executes zero tests and must
never be read as regression coverage, so it is reported as its own step kind
rather than folded into the test totals.

Use -Restore only outside a confined agent sandbox. NuGet writes to the
global package cache, which a workspace-scoped sandbox denies silently.

A running copy of the app or of McpServer locks the binaries those projects
copy into their own output directories, which fails the build with MSB3027
rather than anything wrong in the source. Pass -ClearToolLocks to stop known
lockers first; the JSON records what was stopped. The Codex desktop app
relaunches McpServer whenever it opens, so this recurs.
#>

param(
    [string]$Root = (Get-Location).Path,
    [string]$Platform = "x64",
    [string]$Configuration = "Debug",
    [string]$Output = "artifacts/validation.json",
    [switch]$Restore,
    [switch]$SkipBuild,
    [switch]$ClearToolLocks
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Reusable MSBuild worker nodes outlive the build and hold the log files, which
# breaks the next run on Windows. Validation is not a hot path.
$env:MSBUILDDISABLENODEREUSE = "1"

$Root = (Resolve-Path -LiteralPath $Root).Path
$solution = Join-Path $Root "Sussudio.slnx"
$testProject = Join-Path $Root "tests\Sussudio.Tests\Sussudio.Tests.csproj"
$appDll = Join-Path $Root "Sussudio\bin\$Platform\$Configuration\net8.0-windows10.0.19041.0\win-x64\Sussudio.dll"

# The test project honours -p:Platform, so its output lands under bin\<Platform>
# when the solution is built that way. Prefer that path and only fall back to
# the platform-less one, so the smoke step cannot silently load a stale build.
$harnessCandidates = @(
    (Join-Path $Root "tests\Sussudio.Tests\bin\$Platform\$Configuration\net8.0\Sussudio.Tests.dll"),
    (Join-Path $Root "tests\Sussudio.Tests\bin\$Configuration\net8.0\Sussudio.Tests.dll")
)
$harnessDll = $harnessCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $harnessDll) { $harnessDll = $harnessCandidates[0] }

if (-not (Test-Path -LiteralPath $solution)) { throw "Solution not found: $solution" }

# The automated run exercises deterministic code. These behaviors are only
# provable on real hardware and stay explicitly unverified here.
$notCovered = @(
    "live capture from a capture device",
    "HDR display output correctness",
    "audible preview audio output",
    "recording playback of a produced file",
    "preview pacing under a real compositor"
)

$script:logDirectory = Join-Path $Root "artifacts\validation-logs"

function Invoke-Step {
    param(
        [string]$Name,
        [string]$Kind,
        [string[]]$Command
    )

    # Native stderr is captured through files rather than 2>&1: Windows
    # PowerShell 5.1 turns a native command's stderr into a terminating
    # NativeCommandError while $ErrorActionPreference is Stop, which would
    # abort the run on the first line xUnit writes to stderr.
    if (-not (Test-Path -LiteralPath $script:logDirectory)) {
        New-Item -ItemType Directory -Force -Path $script:logDirectory | Out-Null
    }

    $stdoutPath = Join-Path $script:logDirectory "$Name.out.log"
    $stderrPath = Join-Path $script:logDirectory "$Name.err.log"
    $exe = (Get-Command $Command[0] -ErrorAction Stop).Source
    $arguments = @($Command | Select-Object -Skip 1 | ForEach-Object {
        if ($_ -match '\s') { '"{0}"' -f $_ } else { $_ }
    })
    $started = [System.Diagnostics.Stopwatch]::StartNew()
    $exit = 1

    Push-Location $Root
    try {
        # Run through cmd.exe with native redirection. Two PowerShell-only
        # approaches both failed here:
        #   & exe 2>file   promotes native stderr to a terminating
        #                  NativeCommandError while ErrorActionPreference is Stop
        #   Start-Process  -Wait waits for the whole process tree, and MSBuild
        #                  leaves a persistent Roslyn compiler server behind, so
        #                  it never returns; without -Wait, ExitCode comes back
        #                  null.
        $quoted = @($arguments | ForEach-Object {
            if ($_ -match '[\s"]') { '"' + ($_ -replace '"', '\"') + '"' } else { $_ }
        })
        $commandLine = '"{0}" {1} 1> "{2}" 2> "{3}"' -f $exe, ($quoted -join ' '), $stdoutPath, $stderrPath
        & cmd.exe /c $commandLine
        $exit = $LASTEXITCODE
    }
    catch {
        Set-Content -LiteralPath $stderrPath -Value $_.Exception.Message -Encoding utf8
        $exit = 1
    }
    finally {
        Pop-Location
        $started.Stop()
    }

    $stdout = if (Test-Path -LiteralPath $stdoutPath) { Get-Content -LiteralPath $stdoutPath -Raw } else { "" }
    $stderr = if (Test-Path -LiteralPath $stderrPath) { Get-Content -LiteralPath $stderrPath -Raw } else { "" }
    $text = (@($stdout, $stderr) | Where-Object { $_ }) -join "`n"

    # A locked output binary is an environment problem, not a source problem.
    # Report it as its own flag so nobody debugs the code for it.
    $lockDetected = $text -match 'MSB3027|MSB3021|MSB3026' -or
        $text -match 'because it is being used by another process'

    [pscustomobject]@{
        name = $Name
        kind = $Kind
        command = ($Command -join " ")
        exitCode = $exit
        durationMs = [int]$started.ElapsedMilliseconds
        succeeded = ($exit -eq 0)
        lockDetected = $lockDetected
        output = $text.Trim()
    }
}

# Stops processes known to lock this repository's build outputs and returns a
# record of what was stopped. Only ever called when the caller passes
# -ClearToolLocks, because stopping a tool can interrupt whoever is using it.
function Clear-KnownOutputLockers {
    param([string]$RepoRoot)

    $targets = @('McpServer', 'Sussudio')
    $stopped = @()
    foreach ($name in $targets) {
        $processes = @(Get-Process -Name $name -ErrorAction SilentlyContinue)
        foreach ($process in $processes) {
            # Only stop processes whose image lives under this repository.
            $path = $null
            try { $path = $process.MainModule.FileName } catch { }
            if ($path -and -not $path.StartsWith($RepoRoot, [StringComparison]::OrdinalIgnoreCase)) { continue }

            $stopped += "$name($($process.Id))"
            try { Stop-Process -Id $process.Id -Force -ErrorAction Stop } catch { }
        }
    }

    if ($stopped.Count -gt 0) { Start-Sleep -Seconds 2 }

    # Report anything that came straight back, so a respawning launcher is
    # visible instead of showing up later as a mysterious build failure.
    $respawned = @()
    foreach ($name in $targets) {
        foreach ($process in @(Get-Process -Name $name -ErrorAction SilentlyContinue)) {
            $path = $null
            try { $path = $process.MainModule.FileName } catch { }
            if ($path -and $path.StartsWith($RepoRoot, [StringComparison]::OrdinalIgnoreCase)) {
                $respawned += "$name($($process.Id))"
            }
        }
    }

    return [pscustomobject]@{
        stopped = $stopped
        respawned = $respawned
    }
}

function Get-TestTotals {
    param([string]$Text)

    $match = [regex]::Match(
        $Text,
        '(Passed|Failed)!\s+-\s+Failed:\s+(\d+),\s+Passed:\s+(\d+),\s+Skipped:\s+(\d+),\s+Total:\s+(\d+)')
    if (-not $match.Success) { return $null }

    return [pscustomobject]@{
        failed = [int]$match.Groups[2].Value
        passed = [int]$match.Groups[3].Value
        skipped = [int]$match.Groups[4].Value
        total = [int]$match.Groups[5].Value
    }
}

function Get-FailedTestNames {
    param([string]$Text)

    # Parameterized test names contain spaces and parentheses, so match up to
    # the trailing [duration] marker rather than the first whitespace. xUnit
    # reports long-running tests in seconds, so accept both units.
    return @([regex]::Matches($Text, '(?m)^\s*Failed\s+(Sussudio\.Tests\..+?)\s+\[\d+(?:\.\d+)?\s*(?:ms|s)\]') |
        ForEach-Object { $_.Groups[1].Value.Trim() } |
        Sort-Object -Unique)
}

$steps = @()
$lockClearing = $null

if ($ClearToolLocks) {
    $lockClearing = Clear-KnownOutputLockers -RepoRoot $Root
    if ($lockClearing.stopped.Count -gt 0) {
        Write-Host ("cleared build-output locks: {0}" -f ($lockClearing.stopped -join ", "))
    }
    if ($lockClearing.respawned.Count -gt 0) {
        Write-Host ("WARNING: these came straight back and will still lock outputs: {0}" -f ($lockClearing.respawned -join ", "))
    }
}


if ($Restore) {
    $steps += Invoke-Step -Name "restore" -Kind "build" -Command @(
        "dotnet", "restore", $solution)
}

if (-not $SkipBuild) {
    $steps += Invoke-Step -Name "build" -Kind "build" -Command @(
        "dotnet", "build", $solution, "-p:Platform=$Platform", "--no-restore", "-v", "minimal")
}

$steps += Invoke-Step -Name "tests" -Kind "tests" -Command @(
    "dotnet", "test", $testProject, "-p:Platform=$Platform", "--no-restore", "-v", "minimal")

if (Test-Path -LiteralPath $harnessDll) {
    $steps += Invoke-Step -Name "assembly-load-smoke" -Kind "smoke" -Command @(
        "dotnet", "exec", $harnessDll, $appDll)
}
else {
    $steps += [pscustomobject]@{
        name = "assembly-load-smoke"
        kind = "smoke"
        command = "dotnet exec $harnessDll $appDll"
        exitCode = 1
        durationMs = 0
        succeeded = $false
        output = "Harness assembly not built: $harnessDll"
    }
}

$steps += Invoke-Step -Name "whitespace" -Kind "diff" -Command @(
    "git", "diff", "--check")

$testStep = $steps | Where-Object { $_.name -eq "tests" } | Select-Object -First 1
$totals = Get-TestTotals -Text $testStep.output
$failedTests = Get-FailedTestNames -Text $testStep.output

function Get-GitValue {
    param([string[]]$Arguments)
    $value = (& git -C $Root @Arguments 2>&1 | Out-String).Trim()
    if ($LASTEXITCODE -ne 0) { return $null }
    return $value
}

$assemblyHash = $null
if (Test-Path -LiteralPath $appDll) {
    $assemblyHash = (Get-FileHash -LiteralPath $appDll -Algorithm SHA256).Hash
}

$status = Get-GitValue -Arguments @("status", "--porcelain")

$result = [pscustomobject]@{
    schemaVersion = 1
    timestampUtc = (Get-Date).ToUniversalTime().ToString("o")
    gitHead = Get-GitValue -Arguments @("rev-parse", "HEAD")
    gitBranch = Get-GitValue -Arguments @("rev-parse", "--abbrev-ref", "HEAD")
    workingTreeClean = [string]::IsNullOrWhiteSpace($status)
    targetAssemblySha256 = $assemblyHash
    succeeded = (@($steps | Where-Object { -not $_.succeeded }).Count -eq 0)
    environmentLockDetected = (@($steps | Where-Object { $_.lockDetected }).Count -gt 0)
    lockClearing = $lockClearing
    tests = $totals
    failedTests = $failedTests
    notCovered = $notCovered
    steps = @($steps | ForEach-Object {
        [pscustomobject]@{
            name = $_.name
            kind = $_.kind
            command = $_.command
            exitCode = $_.exitCode
            durationMs = $_.durationMs
            succeeded = $_.succeeded
            lockDetected = $_.lockDetected
        }
    })
}

$outputPath = Join-Path $Root $Output
$outputDir = Split-Path -Parent $outputPath
if ($outputDir -and -not (Test-Path -LiteralPath $outputDir)) {
    New-Item -ItemType Directory -Force -Path $outputDir | Out-Null
}

$result | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $outputPath -Encoding utf8

# Console summary. Failures print their captured output so an agent does not
# have to re-run a step to see the error.
foreach ($step in $steps) {
    $marker = if ($step.succeeded) { "PASS" } else { "FAIL" }
    Write-Host ("[{0}] {1} ({2} ms)" -f $marker, $step.name, $step.durationMs)
    if (-not $step.succeeded) {
        Write-Host $step.output
    }
}

if ($totals) {
    Write-Host ("tests: passed={0} failed={1} skipped={2} total={3}" -f `
        $totals.passed, $totals.failed, $totals.skipped, $totals.total)
    foreach ($name in $failedTests) { Write-Host ("  failed: {0}" -f $name) }
}
else {
    Write-Host "tests: no summary line parsed"
}

Write-Host ("result: {0}" -f $outputPath)
Write-Host ("not covered by this run: {0}" -f ($notCovered -join "; "))

if ($result.environmentLockDetected) {
    Write-Host ""
    Write-Host "A build output was locked by a running tool, which is an environment problem and not a source problem."
    Write-Host "Re-run with -ClearToolLocks to stop it first, or close the Codex desktop app, which relaunches McpServer."
}

if (-not $result.succeeded) { exit 1 }
exit 0
