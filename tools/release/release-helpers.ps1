Set-StrictMode -Version Latest

function Invoke-ReleaseNativeCommand {
    param(
        [Parameter(Mandatory = $true)][string]$FilePath,
        [Parameter(Mandatory = $true)][string[]]$Arguments,
        [Parameter(Mandatory = $true)][string]$Description,
        [string]$WorkingDirectory = (Get-Location).Path
    )

    Write-Host "> $FilePath $([string]::Join(' ', $Arguments))"
    Push-Location -LiteralPath $WorkingDirectory
    try {
        $output = @(& $FilePath @Arguments 2>&1)
        $exitCode = if ($null -eq $LASTEXITCODE) { 0 } else { [int]$LASTEXITCODE }
    }
    finally {
        Pop-Location
    }

    $output | ForEach-Object { Write-Host $_ }
    if ($exitCode -ne 0) {
        throw "$Description failed with exit code $exitCode."
    }

    return $output
}

function Test-SemVerPrerelease {
    param([Parameter(Mandatory = $true)][string]$Version)

    if ($Version -notmatch '^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)-([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)$') {
        return $false
    }

    foreach ($identifier in $Matches[4].Split('.')) {
        if ($identifier -match '^[0-9]+$' -and $identifier.Length -gt 1 -and $identifier.StartsWith('0', [StringComparison]::Ordinal)) {
            return $false
        }
    }

    return $true
}

function Invoke-ReleaseGit {
    param(
        [Parameter(Mandatory = $true)][string]$RepoRoot,
        [Parameter(Mandatory = $true)][string[]]$Arguments,
        [Parameter(Mandatory = $true)][string]$Description
    )

    $output = @(& git -C $RepoRoot @Arguments 2>&1)
    $exitCode = if ($null -eq $LASTEXITCODE) { 0 } else { [int]$LASTEXITCODE }
    if ($exitCode -ne 0) {
        throw "$Description failed with exit code $exitCode. $($output -join ' ')"
    }

    return ($output -join "`n").Trim()
}

function Assert-CleanTaggedPrerelease {
    param(
        [Parameter(Mandatory = $true)][string]$RepoRoot,
        [Parameter(Mandatory = $true)][string]$Version,
        [string]$ExpectedCommit = ''
    )

    if (-not (Test-SemVerPrerelease -Version $Version)) {
        throw "Version '$Version' is not a SemVer prerelease such as 1.0.0-beta.1."
    }

    $resolvedRepoRoot = [IO.Path]::GetFullPath($RepoRoot).TrimEnd('\', '/')
    $gitRootText = Invoke-ReleaseGit -RepoRoot $RepoRoot -Arguments @('rev-parse', '--show-toplevel') -Description 'Resolving repository root'
    $gitRoot = [IO.Path]::GetFullPath($gitRootText).TrimEnd('\', '/')
    if (-not [string]::Equals($resolvedRepoRoot, $gitRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Release script must run against the repository root '$resolvedRepoRoot'; git resolved '$gitRoot'."
    }

    $status = Invoke-ReleaseGit -RepoRoot $RepoRoot -Arguments @('status', '--porcelain=v1', '--untracked-files=all') -Description 'Checking repository cleanliness'
    if (-not [string]::IsNullOrWhiteSpace($status)) {
        throw "Release requires a clean worktree, including no untracked files. Commit or remove these entries first:`n$status"
    }

    $headCommit = Invoke-ReleaseGit -RepoRoot $RepoRoot -Arguments @('rev-parse', 'HEAD') -Description 'Resolving HEAD'
    if (-not [string]::IsNullOrWhiteSpace($ExpectedCommit) -and
        -not [string]::Equals($headCommit, $ExpectedCommit, [StringComparison]::OrdinalIgnoreCase)) {
        throw "HEAD changed after validation. Expected $ExpectedCommit but found $headCommit."
    }

    $tagName = "v$Version"
    $tagCommit = Invoke-ReleaseGit -RepoRoot $RepoRoot -Arguments @('rev-parse', "refs/tags/$tagName^{commit}") -Description "Resolving tag $tagName"
    if (-not [string]::Equals($headCommit, $tagCommit, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Release tag '$tagName' does not point at HEAD $headCommit."
    }

    $tagsAtHeadText = Invoke-ReleaseGit -RepoRoot $RepoRoot -Arguments @('tag', '--points-at', 'HEAD') -Description 'Checking tags at HEAD'
    $tagsAtHead = @($tagsAtHeadText -split "`n" | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    if ($tagName -notin $tagsAtHead) {
        throw "HEAD is not exactly tagged '$tagName'."
    }

    return $headCommit
}

function Test-FullPathWithinRoot {
    param(
        [Parameter(Mandatory = $true)][string]$Candidate,
        [Parameter(Mandatory = $true)][string]$Root
    )

    $candidatePath = [IO.Path]::GetFullPath($Candidate).TrimEnd('\', '/')
    $rootPath = [IO.Path]::GetFullPath($Root).TrimEnd('\', '/')
    if ([string]::Equals($candidatePath, $rootPath, [StringComparison]::OrdinalIgnoreCase)) {
        return $true
    }

    $rootPrefix = $rootPath + [IO.Path]::DirectorySeparatorChar
    return $candidatePath.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)
}

function Get-ValidatedFfmpegManifest {
    param([Parameter(Mandatory = $true)][string]$ManifestPath)

    if (-not (Test-Path -LiteralPath $ManifestPath -PathType Leaf)) {
        throw "Tracked FFmpeg manifest is missing: $ManifestPath"
    }

    try {
        $manifest = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json
    }
    catch {
        throw "Tracked FFmpeg manifest is invalid JSON: $($_.Exception.Message)"
    }

    if ([int]$manifest.schemaVersion -ne 1) {
        throw "Unsupported FFmpeg manifest schemaVersion '$($manifest.schemaVersion)'."
    }

    if ([string]::IsNullOrWhiteSpace([string]$manifest.buildVersion)) {
        throw 'FFmpeg manifest buildVersion is required.'
    }

    $entries = @($manifest.files)
    if ($entries.Count -eq 0) {
        throw 'FFmpeg manifest must contain at least one file.'
    }

    $requiredNames = @('avcodec-62.dll', 'avformat-62.dll', 'avutil-60.dll', 'swresample-6.dll')
    $manifestNames = @($entries | ForEach-Object { [string]$_.fileName } | Sort-Object)
    $nameDifference = @(Compare-Object -ReferenceObject ($requiredNames | Sort-Object) -DifferenceObject $manifestNames)
    if ($nameDifference.Count -ne 0) {
        $differenceText = (($nameDifference | ForEach-Object { $_.InputObject + $_.SideIndicator }) -join ', ')
        throw "FFmpeg manifest must contain the exact supported DLL set: $differenceText"
    }

    $seenNames = @{}
    foreach ($entry in $entries) {
        $fileName = [string]$entry.fileName
        if ([string]::IsNullOrWhiteSpace($fileName) -or
            -not [string]::Equals([IO.Path]::GetFileName($fileName), $fileName, [StringComparison]::Ordinal)) {
            throw "FFmpeg manifest fileName must be a leaf filename: '$fileName'."
        }
        if ($seenNames.ContainsKey($fileName)) {
            throw "FFmpeg manifest contains duplicate filename '$fileName'."
        }
        $seenNames[$fileName] = $true

        if ([string]::IsNullOrWhiteSpace([string]$entry.productVersion) -or
            [string]::IsNullOrWhiteSpace([string]$entry.fileVersion)) {
            throw "FFmpeg manifest versions are required for '$fileName'."
        }
        if (-not [string]::Equals([string]$entry.productVersion, [string]$manifest.buildVersion, [StringComparison]::Ordinal)) {
            throw "FFmpeg productVersion for '$fileName' must equal buildVersion '$($manifest.buildVersion)'."
        }
        if ([string]$entry.sha256 -notmatch '^[0-9A-Fa-f]{64}$') {
            throw "FFmpeg manifest SHA-256 is invalid for '$fileName'."
        }
    }

    return $manifest
}

function Assert-FfmpegRuntimeMatchesManifest {
    param(
        [Parameter(Mandatory = $true)][string]$RuntimeDirectory,
        [Parameter(Mandatory = $true)]$Manifest
    )

    if (-not (Test-Path -LiteralPath $RuntimeDirectory -PathType Container)) {
        throw "FFmpeg runtime directory is missing: $RuntimeDirectory"
    }

    $entries = @($Manifest.files)
    $expectedNames = @($entries | ForEach-Object { [string]$_.fileName } | Sort-Object)
    $actualNames = @(Get-ChildItem -LiteralPath $RuntimeDirectory -Filter '*.dll' -File | ForEach-Object Name | Sort-Object)
    $difference = @(Compare-Object -ReferenceObject $expectedNames -DifferenceObject $actualNames)
    if ($difference.Count -ne 0) {
        $differenceText = (($difference | ForEach-Object { $_.InputObject + $_.SideIndicator }) -join ', ')
        throw "FFmpeg runtime DLL set does not match the tracked manifest: $differenceText"
    }

    foreach ($entry in $entries) {
        $fileName = [string]$entry.fileName
        $path = Join-Path $RuntimeDirectory $fileName
        $file = Get-Item -LiteralPath $path
        $actualProductVersion = [string]$file.VersionInfo.ProductVersion
        $actualFileVersion = [string]$file.VersionInfo.FileVersion
        $actualHash = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash

        if (-not [string]::Equals($actualProductVersion, [string]$entry.productVersion, [StringComparison]::Ordinal)) {
            throw "FFmpeg product version mismatch for '$fileName': expected '$($entry.productVersion)', found '$actualProductVersion'."
        }
        if (-not [string]::Equals($actualFileVersion, [string]$entry.fileVersion, [StringComparison]::Ordinal)) {
            throw "FFmpeg file version mismatch for '$fileName': expected '$($entry.fileVersion)', found '$actualFileVersion'."
        }
        if (-not [string]::Equals($actualHash, [string]$entry.sha256, [StringComparison]::OrdinalIgnoreCase)) {
            throw "FFmpeg SHA-256 mismatch for '$fileName': expected '$($entry.sha256)', found '$actualHash'."
        }
    }
}

function Resolve-ArtifactSigningPrerequisites {
    param(
        [Parameter(Mandatory = $true)][string]$RepoRoot,
        [Parameter(Mandatory = $true)][string]$SignToolPath,
        [Parameter(Mandatory = $true)][string]$AzureCodeSigningDlibPath,
        [Parameter(Mandatory = $true)][string]$ArtifactSigningMetadataPath,
        [string]$AzureCliPath = 'az'
    )

    $resolvedSignTool = [IO.Path]::GetFullPath($SignToolPath)
    if (-not (Test-Path -LiteralPath $resolvedSignTool -PathType Leaf)) {
        throw "SignTool was not found at '$resolvedSignTool'. Install the Windows SDK signing tools and pass -SignToolPath explicitly."
    }

    $resolvedDlib = [IO.Path]::GetFullPath($AzureCodeSigningDlibPath)
    if (-not (Test-Path -LiteralPath $resolvedDlib -PathType Leaf)) {
        throw "Azure Artifact Signing Dlib was not found at '$resolvedDlib'. Install the Microsoft signing client and pass -AzureCodeSigningDlibPath explicitly."
    }
    if (-not [string]::Equals([IO.Path]::GetFileName($resolvedDlib), 'Azure.CodeSigning.Dlib.dll', [StringComparison]::OrdinalIgnoreCase)) {
        throw "Azure signing library must be Azure.CodeSigning.Dlib.dll; received '$resolvedDlib'."
    }

    $resolvedMetadata = [IO.Path]::GetFullPath($ArtifactSigningMetadataPath)
    if (-not (Test-Path -LiteralPath $resolvedMetadata -PathType Leaf)) {
        throw "Azure Artifact Signing metadata was not found at '$resolvedMetadata'. Create it outside the repository and pass -ArtifactSigningMetadataPath."
    }
    if (Test-FullPathWithinRoot -Candidate $resolvedMetadata -Root $RepoRoot) {
        throw 'Azure Artifact Signing metadata must remain outside the repository so it cannot be committed or packaged.'
    }
    try {
        $signingMetadata = Get-Content -LiteralPath $resolvedMetadata -Raw | ConvertFrom-Json
    }
    catch {
        throw "Azure Artifact Signing metadata is invalid JSON: $($_.Exception.Message)"
    }

    foreach ($requiredProperty in @('Endpoint', 'CodeSigningAccountName', 'CertificateProfileName')) {
        $property = $signingMetadata.PSObject.Properties[$requiredProperty]
        if ($null -eq $property -or [string]::IsNullOrWhiteSpace([string]$property.Value)) {
            throw "Azure Artifact Signing metadata property '$requiredProperty' is required."
        }
    }

    $excludedCredentialsProperty = $signingMetadata.PSObject.Properties['ExcludeCredentials']
    $excludedCredentials = if ($null -eq $excludedCredentialsProperty) { @() } else { @($excludedCredentialsProperty.Value) }
    $requiredCredentialExclusions = @(
        'EnvironmentCredential',
        'ManagedIdentityCredential',
        'WorkloadIdentityCredential',
        'SharedTokenCacheCredential',
        'VisualStudioCredential',
        'VisualStudioCodeCredential',
        'AzurePowerShellCredential',
        'AzureDeveloperCliCredential',
        'InteractiveBrowserCredential'
    )
    foreach ($credential in $requiredCredentialExclusions) {
        if ($credential -notin $excludedCredentials) {
            throw "Artifact Signing metadata must exclude '$credential' so the authenticated Azure CLI identity is the signing principal."
        }
    }
    if ('AzureCliCredential' -in $excludedCredentials) {
        throw "Artifact Signing metadata must not exclude AzureCliCredential."
    }

    $azureCommand = Get-Command $AzureCliPath -ErrorAction SilentlyContinue
    if ($null -eq $azureCommand) {
        throw "Azure CLI '$AzureCliPath' was not found. Install/update Azure CLI, run 'az login', and retry."
    }
    $resolvedAzureCli = $azureCommand.Source
    $null = Invoke-ReleaseNativeCommand -FilePath $resolvedAzureCli -Arguments @('version', '--output', 'json') -Description 'Azure CLI version check' -WorkingDirectory $RepoRoot
    $null = Invoke-ReleaseNativeCommand -FilePath $resolvedAzureCli -Arguments @('account', 'show', '--only-show-errors', '--output', 'none') -Description "Azure CLI authentication check; run 'az login' with an identity authorized for the Artifact Signing profile" -WorkingDirectory $RepoRoot

    return [pscustomobject]@{
        SignToolPath = $resolvedSignTool
        DlibPath = $resolvedDlib
        MetadataPath = $resolvedMetadata
        AzureCliPath = $resolvedAzureCli
    }
}

function Assert-RequiredPackageFiles {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string[]]$RelativePaths
    )

    foreach ($relativePath in $RelativePaths) {
        $path = Join-Path $Root $relativePath
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            throw "Required package file is missing: $relativePath"
        }
    }
}

function Invoke-ArtifactSigning {
    param(
        [Parameter(Mandatory = $true)]$Prerequisites,
        [Parameter(Mandatory = $true)][string[]]$Files,
        [string]$TimestampUrl = 'http://timestamp.acs.microsoft.com'
    )

    foreach ($file in $Files) {
        if (-not (Test-Path -LiteralPath $file -PathType Leaf)) {
            throw "First-party file selected for signing is missing: $file"
        }

        $null = Invoke-ReleaseNativeCommand `
            -FilePath $Prerequisites.SignToolPath `
            -Arguments @(
                'sign',
                '/v',
                '/debug',
                '/fd', 'SHA256',
                '/tr', $TimestampUrl,
                '/td', 'SHA256',
                '/dlib', $Prerequisites.DlibPath,
                '/dmdf', $Prerequisites.MetadataPath,
                $file
            ) `
            -Description "Azure Artifact Signing for '$file'"
    }
}

function Assert-AuthenticodeSignatures {
    param(
        [Parameter(Mandatory = $true)]$Prerequisites,
        [Parameter(Mandatory = $true)][string[]]$Files
    )

    foreach ($file in $Files) {
        $null = Invoke-ReleaseNativeCommand `
            -FilePath $Prerequisites.SignToolPath `
            -Arguments @('verify', '/pa', '/all', '/v', '/debug', '/tw', $file) `
            -Description "Authenticode verification for '$file'"
    }
}

function Get-SafeZipEntryName {
    param([Parameter(Mandatory = $true)]$Entry)

    $entryName = $Entry.FullName.Replace('\', '/')
    if ($entryName.StartsWith('/', [StringComparison]::Ordinal) -or
        $entryName -match '(^|/)\.\.(/|$)' -or
        $entryName -match '^[A-Za-z]:') {
        throw "ZIP contains an unsafe entry path: $entryName"
    }

    return $entryName
}

function Assert-ZipContainsFiles {
    param(
        [Parameter(Mandatory = $true)][string]$ZipPath,
        [Parameter(Mandatory = $true)][string[]]$RelativePaths
    )

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [IO.Compression.ZipFile]::OpenRead($ZipPath)
    try {
        $entryNames = @{}
        foreach ($entry in $archive.Entries) {
            $entryName = Get-SafeZipEntryName -Entry $entry
            $entryNames[$entryName] = $true
        }

        foreach ($relativePath in $RelativePaths) {
            $expectedName = $relativePath.Replace('\', '/')
            if (-not $entryNames.ContainsKey($expectedName)) {
                throw "ZIP is missing required entry: $expectedName"
            }
        }
    }
    finally {
        $archive.Dispose()
    }
}

function Assert-ZipMatchesDirectory {
    param(
        [Parameter(Mandatory = $true)][string]$ZipPath,
        [Parameter(Mandatory = $true)][string]$StagingDirectory
    )

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $stagingRoot = [IO.Path]::GetFullPath($StagingDirectory).TrimEnd('\', '/')
    $stagingPrefix = $stagingRoot + [IO.Path]::DirectorySeparatorChar
    $expectedNames = @(Get-ChildItem -LiteralPath $stagingRoot -File -Recurse | ForEach-Object {
        $_.FullName.Substring($stagingPrefix.Length).Replace('\', '/')
    } | Sort-Object)

    $archive = [IO.Compression.ZipFile]::OpenRead($ZipPath)
    try {
        $seenNames = @{}
        $actualNames = @()
        foreach ($entry in $archive.Entries) {
            $entryName = Get-SafeZipEntryName -Entry $entry
            if ([string]::IsNullOrEmpty($entry.Name)) {
                continue
            }
            if ($seenNames.ContainsKey($entryName)) {
                throw "ZIP contains a duplicate file entry: $entryName"
            }
            $seenNames[$entryName] = $true
            $actualNames += $entryName
        }

        $difference = @(Compare-Object -ReferenceObject $expectedNames -DifferenceObject @($actualNames | Sort-Object))
        if ($difference.Count -ne 0) {
            $differenceText = (($difference | ForEach-Object { $_.InputObject + $_.SideIndicator }) -join ', ')
            throw "ZIP file inventory differs from staging: $differenceText"
        }
    }
    finally {
        $archive.Dispose()
    }
}
