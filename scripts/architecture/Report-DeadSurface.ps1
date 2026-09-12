<#
.SYNOPSIS
Reports production surface that nothing uses, so it can be deleted or annotated.

.DESCRIPTION
Run from the repository root. Emits artifacts/dead-surface.json plus a console
summary.

Policy: every reported item is either deleted, or annotated in place with a
comment explaining why it must stay. There is no third option - a report that
is read and then ignored is worse than no report, because the next agent
cannot tell a deliberate keeper from an oversight.

Detection is conservative: false negatives are acceptable, false positives are
not. A report that cries wolf gets ignored, which defeats the point. A `ref` or
`out` use counts as a read even when the callee only writes, so write-only
fields passed to Interlocked helpers are missed rather than misreported.

The corpus is scanned in single passes with precomputed counts. Rescanning it
once per symbol is quadratic and did not finish on this repository.

Categories:
  env-unset        environment variable read in production but never referenced
                   anywhere outside the production file that reads it
  field-unread     private field assigned but never read
  method-uncalled  private method with no call site
#>

param(
    [string]$Root = (Get-Location).Path,
    [string]$OutputJson = "artifacts/dead-surface.json"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$productionRoots = @("Sussudio", "Sussudio.Automation.Contracts", "tools")
$excludePattern = '\\(obj|bin|artifacts|node_modules)\\|\.desloppify\\'
$corpusExtensions = @('.cs', '.ps1', '.md', '.json', '.yml', '.yaml', '.xaml')

function Get-ProductionFiles {
    param([string]$RepoRoot)
    $results = @()
    foreach ($name in $productionRoots) {
        $path = Join-Path $RepoRoot $name
        if (-not (Test-Path -LiteralPath $path)) { continue }
        $results += Get-ChildItem -LiteralPath $path -Recurse -Include *.cs -File |
            Where-Object { $_.FullName -notmatch $excludePattern }
    }
    return $results
}

$production = Get-ProductionFiles -RepoRoot $Root

function Test-Annotated {
    # Policy: an item is deleted, or annotated in place with a `dead-surface:`
    # comment saying why it must stay. Honouring the marker is what lets the
    # report reach empty; without it an annotated keeper is reported forever and
    # the report stops being worth reading.
    param([string]$Text, [int]$Index, [int]$Length)

    $lineStart = $Index
    while ($lineStart -gt 0 -and $Text[$lineStart - 1] -ne "`n") { $lineStart-- }
    $lineEnd = $Index + $Length
    while ($lineEnd -lt $Text.Length -and $Text[$lineEnd] -ne "`n") { $lineEnd++ }
    if ($Text.Substring($lineStart, $lineEnd - $lineStart) -match 'dead-surface:') { return $true }

    # Look back over a short comment block, not just one line: an explanation
    # naturally runs to several lines above the declaration.
    for ($offset = 1; $offset -le 5 -and $lineStart -gt 0; $offset++) {
        $prevEnd = $lineStart - 1
        $prevStart = $prevEnd
        while ($prevStart -gt 0 -and $Text[$prevStart - 1] -ne "`n") { $prevStart-- }
        if ($Text.Substring($prevStart, $prevEnd - $prevStart) -match 'dead-surface:') { return $true }
        $lineStart = $prevStart
    }

    return $false
}

# Corpus: everything that could reference or set a symbol. Read once.
# docs/ matters: an environment knob documented in AGENT_MAP.md is a deliberate
# operator control, not dead surface. .xaml matters: a Click handler is wired by
# markup and has no C# call site.
$corpusFiles = @($production)
foreach ($extra in @("tests", "scripts", ".github", "docs", ".claude")) {
    $path = Join-Path $Root $extra
    if (-not (Test-Path -LiteralPath $path)) { continue }
    $corpusFiles += Get-ChildItem -LiteralPath $path -Recurse -File |
        Where-Object { $corpusExtensions -contains $_.Extension -and $_.FullName -notmatch $excludePattern }
}
# XAML lives inside the production roots, so pick those up too.
foreach ($name in $productionRoots) {
    $path = Join-Path $Root $name
    if (-not (Test-Path -LiteralPath $path)) { continue }
    $corpusFiles += Get-ChildItem -LiteralPath $path -Recurse -File -Include *.xaml |
        Where-Object { $_.FullName -notmatch $excludePattern }
}

$builder = [System.Text.StringBuilder]::new()
foreach ($f in $corpusFiles) { [void]$builder.AppendLine([System.IO.File]::ReadAllText($f.FullName)) }
$corpus = $builder.ToString()

# Single pass: how often does each identifier appear immediately before '(' ?
$callCounts = @{}
foreach ($m in [regex]::Matches($corpus, '([A-Za-z_]\w*)\s*\(')) {
    $key = $m.Groups[1].Value
    $callCounts[$key] = 1 + [int]$callCounts[$key]
}
# Single pass: identifiers referenced from XAML markup. Markup wires Click and
# similar handlers as Name="Handler", with no parentheses, so they never appear
# in the call counts above.
$xamlMentions = [System.Collections.Generic.HashSet[string]]::new()
foreach ($f in $corpusFiles) {
    if ($f.Extension -ne '.xaml') { continue }
    foreach ($m in [regex]::Matches([System.IO.File]::ReadAllText($f.FullName), '[A-Za-z_]\w*')) {
        [void]$xamlMentions.Add($m.Value)
    }
}

# Single pass: how often is each identifier declared as a member?
$declCounts = @{}
foreach ($m in [regex]::Matches($corpus, '(?m)^[ \t]*(?:private|internal|public|protected)[^\r\n=;]*?\b([A-Za-z_]\w*)\s*\(')) {
    $key = $m.Groups[1].Value
    $declCounts[$key] = 1 + [int]$declCounts[$key]
}
# Single pass: every identifier mention. A delegate subscription (+= Handler;)
# and a XAML wiring both reference a method without parentheses, so call counts
# alone under-report references.
$mentionCounts = @{}
foreach ($m in [regex]::Matches($corpus, '[A-Za-z_]\w*')) {
    $key = $m.Value
    $mentionCounts[$key] = 1 + [int]$mentionCounts[$key]
}

# Partial families: a private field declared in Foo.cs may be read in
# Foo.Bar.cs. Searching only the declaring file reports live fields as dead.
$familyText = @{}
foreach ($f in $production) {
    $base = $f.Name -replace '\..*$', ''
    $key = "$($f.DirectoryName)|$base"
    if (-not $familyText.ContainsKey($key)) { $familyText[$key] = [System.Text.StringBuilder]::new() }
    [void]$familyText[$key].AppendLine([System.IO.File]::ReadAllText($f.FullName))
}

# --- env-unset --------------------------------------------------------------
$envUnset = @()
$envReadPattern = 'GetEnvironmentVariable\(\s*"([A-Za-z_][A-Za-z0-9_]*)"|Get(?:Int|Bool|Double|String)FromEnv\(\s*"([A-Za-z_][A-Za-z0-9_]*)"'
$envReads = @{}
$envReadSites = @{}
foreach ($file in $production) {
    $text = [System.IO.File]::ReadAllText($file.FullName)
    $relative = $file.FullName.Substring($Root.Length + 1).Replace('\', '/')
    foreach ($m in [regex]::Matches($text, $envReadPattern)) {
        $name = if ($m.Groups[1].Success) { $m.Groups[1].Value } else { $m.Groups[2].Value }
        $envReads[$name] = 1 + [int]$envReads[$name]
        if (-not $envReadSites.ContainsKey($name)) { $envReadSites[$name] = @() }
        if ($envReadSites[$name] -notcontains $relative) { $envReadSites[$name] += $relative }
    }
}

if ($envReads.Count -gt 0) {
    # Longest name first: a shorter variable name that is a prefix of a longer
    # one would otherwise match first and shadow it, so the longer name looks
    # unmentioned no matter how often it appears.
    $alternation = ($envReads.Keys |
        Sort-Object -Property @{ Expression = { $_.Length }; Descending = $true }, @{ Expression = { $_ }; Descending = $false } |
        ForEach-Object { [regex]::Escape($_) }) -join '|'
    $corpusMentions = @{}
    foreach ($m in [regex]::Matches($corpus, $alternation)) {
        $corpusMentions[$m.Value] = 1 + [int]$corpusMentions[$m.Value]
    }
    foreach ($name in ($envReads.Keys | Sort-Object)) {
        # If the only mentions anywhere are the production reads, nothing sets it.
        if ([int]$corpusMentions[$name] -le [int]$envReads[$name]) {
            $envUnset += [pscustomobject]@{
                category = 'env-unset'
                name     = $name
                detail   = "read $($envReads[$name])x in production and mentioned nowhere else"
                files    = @($envReadSites[$name])
            }
        }
    }
}

# --- field-unread -----------------------------------------------------------
$fieldUnread = @()
foreach ($file in $production) {
    $text = [System.IO.File]::ReadAllText($file.FullName)
    $relative = $file.FullName.Substring($Root.Length + 1).Replace('\', '/')
    $familyKey = "$($file.DirectoryName)|$($file.Name -replace '\..*$', '')"
    $scope = $familyText[$familyKey].ToString()
    $declPattern = '(?m)^[ \t]*private\s+(?:static\s+)?(?:readonly\s+)?[\w\?<>\[\],\.]+\s+(_\w+)\s*(?:=[^;]*)?;'
    foreach ($decl in [regex]::Matches($text, $declPattern)) {
        $field = $decl.Groups[1].Value
        if (Test-Annotated -Text $text -Index $decl.Index -Length $decl.Length) { continue }

        # Event and delegate fields are read by the runtime when the event
        # fires, never by an explicit read in source. Reporting them is a false
        # positive, and a report that cries wolf gets ignored.
        $declarationText = $decl.Value
        if ($declarationText -match '\bevent\b' -or
            $declarationText -match 'EventHandler|Action\b|Func<|Delegate') {
            continue
        }

        # Remove the declaration itself, then judge the remaining uses. Index
        # comparison is invalid here because the scope spans sibling partials.
        $body = $scope.Replace($decl.Value, '')
        $uses = @([regex]::Matches($body, "\b$([regex]::Escape($field))\b"))
        if ($uses.Count -eq 0) {
            $fieldUnread += [pscustomobject]@{
                category = 'field-unread'
                name     = "$relative::$field"
                detail   = 'declared and never used at all'
                files    = @($relative)
            }
            continue
        }

        $isRead = $false
        foreach ($use in $uses) {
            $after = $body.Substring($use.Index + $use.Length)
            # Assignment targets are writes. Increment and decrement are NOT:
            # x++ reads the current value, so a counter that is only ever
            # incremented is live.
            if ($after -match '^\s*(?:=(?!=)|\+=|-=|\*=|/=|\|=|&=|\^=)') { continue }
            $isRead = $true
            break
        }
        if (-not $isRead) {
            $fieldUnread += [pscustomobject]@{
                category = 'field-unread'
                name     = "$relative::$field"
                detail   = "assigned but never read ($($uses.Count) textual uses)"
                files    = @($relative)
            }
        }
    }
}

# --- method-uncalled --------------------------------------------------------
$methodUncalled = @()
$seenMethods = [System.Collections.Generic.HashSet[string]]::new()
foreach ($file in $production) {
    $text = [System.IO.File]::ReadAllText($file.FullName)
    $relative = $file.FullName.Substring($Root.Length + 1).Replace('\', '/')
    $declPattern = '(?m)^[ \t]*private\s+(?:static\s+)?(?:async\s+)?[\w\?<>\[\],\.]+\s+(\w+)\s*\('
    foreach ($decl in [regex]::Matches($text, $declPattern)) {
        $method = $decl.Groups[1].Value
        if ($method -in @('Main', 'Dispose', 'GetHashCode', 'Equals', 'ToString')) { continue }
        if (Test-Annotated -Text $text -Index $decl.Index -Length $decl.Length) { continue }
        if ($xamlMentions.Contains($method)) { continue }
        if (-not $seenMethods.Add("$relative::$method")) { continue }

        $calls = [int]$callCounts[$method]
        $decls = [int]$declCounts[$method]
        $mentions = [int]$mentionCounts[$method]
        # Report only when the name appears nowhere but its own declarations:
        # a delegate subscription or XAML wiring references it without
        # parentheses, so call counts alone under-report real references.
        if ($mentions -le $decls) {
            $methodUncalled += [pscustomobject]@{
                category = 'method-uncalled'
                name     = "$relative::$method"
                detail   = "no call site ($mentions mentions, $decls declarations, $calls call-shaped)"
                files    = @($relative)
            }
        }
    }
}

$findings = @($envUnset) + @($fieldUnread) + @($methodUncalled)
$result = [pscustomobject]@{
    schemaVersion = 1
    generatedUtc  = (Get-Date).ToUniversalTime().ToString('o')
    gitHead       = (& git -C $Root rev-parse HEAD 2>$null)
    policy        = 'Delete each item, or annotate it in place with why it must stay.'
    counts        = [pscustomobject]@{
        envUnset       = @($envUnset).Count
        fieldUnread    = @($fieldUnread).Count
        methodUncalled = @($methodUncalled).Count
        total          = $findings.Count
    }
    findings      = $findings
}

$outPath = Join-Path $Root $OutputJson
$outDir = Split-Path -Parent $outPath
if ($outDir -and -not (Test-Path -LiteralPath $outDir)) { New-Item -ItemType Directory -Force -Path $outDir | Out-Null }
$result | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $outPath -Encoding utf8

"env-unset       : $(@($envUnset).Count)"
"field-unread    : $(@($fieldUnread).Count)"
"method-uncalled : $(@($methodUncalled).Count)"
"total           : $($findings.Count)"
"wrote           : $outPath"
""
"--- env-unset ---"
$envUnset | ForEach-Object { "  {0}  [{1}]" -f $_.name, ($_.files -join ', ') }
"--- field-unread (first 12) ---"
$fieldUnread | Select-Object -First 12 | ForEach-Object { "  {0}" -f $_.name }
"--- method-uncalled (first 12) ---"
$methodUncalled | Select-Object -First 12 | ForEach-Object { "  {0}" -f $_.name }
