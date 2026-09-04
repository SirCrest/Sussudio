# Portable prerelease process

The supported release channel today is the signed, self-contained Windows x64
ZIP produced by `tools/package-github-release.ps1`. MSIX packaging and the
Stream Deck plugin are future roadmap items, not alternate current release
paths.

## Release contract

An official archive must come from a clean commit tagged exactly
`v<SemVer-prerelease>`, for example `v0.2.0-beta.1`. The package script runs the
canonical reliability gate before it publishes, stages, or signs anything, and
rechecks the commit immediately before signing. The gate:

1. builds the complete `Sussudio.slnx` solution for x64;
2. rebuilds `ssctl`, `McpServer`, `AutomationClient`, and
   `NativeXuAudioProbe`, whose shared-source inputs require explicit freshness;
3. runs the real xUnit suite, parses a fresh TRX file, and requires a nonzero
   total with zero failures;
4. runs the offline assembly/freshness harness as a separate stage; and
5. runs the package-contract helper tests; and
6. runs `git diff --check`.

The ZIP contains independent self-contained payloads for the app, `ssctl`, the
MCP server, and `AutomationClient`. Packaging verifies the complete ZIP entry
set against staging and emits a SHA-256 checksum after all signatures are
applied.

## Azure Artifact Signing prerequisites

Install or provide all of the following on the release machine:

- a current Azure CLI, authenticated with `az login`;
- an identity with the **Artifact Signing Certificate Profile Signer** role for
  an active, identity-validated certificate profile;
- x64 Windows SDK SignTool;
- the x64 `Azure.CodeSigning.Dlib.dll` from Microsoft's Artifact Signing
  client, with its sibling dependencies kept beside it; and
- an Artifact Signing metadata JSON file stored outside this repository.

The metadata file must name `Endpoint`, `CodeSigningAccountName`, and
`CertificateProfileName`. The release path deliberately uses the current Azure
CLI principal, so `ExcludeCredentials` must exclude the other supported
credential providers without excluding `AzureCliCredential`. A minimal policy
shape is:

```json
{
  "Endpoint": "https://REGION.codesigning.azure.net",
  "CodeSigningAccountName": "ACCOUNT",
  "CertificateProfileName": "PROFILE",
  "ExcludeCredentials": [
    "EnvironmentCredential",
    "ManagedIdentityCredential",
    "WorkloadIdentityCredential",
    "SharedTokenCacheCredential",
    "VisualStudioCredential",
    "VisualStudioCodeCredential",
    "AzurePowerShellCredential",
    "AzureDeveloperCliCredential",
    "InteractiveBrowserCredential"
  ]
}
```

Do not put credentials in this file. Do not copy it into the repository,
publish directories, staging directory, ZIP, logs, or release notes. The Dlib
uses the authenticated Azure identity; the first signing request remains the
authoritative RBAC/profile check.

## Build a prerelease

Restore the complete solution once on the prepared release machine (the
canonical gate deliberately uses `--no-restore` so validation cannot silently
change dependencies), then commit all intended changes, create the exact tag,
and run:

```powershell
dotnet restore Sussudio.slnx -p:Platform=x64
```

```powershell
tools\package-github-release.ps1 `
  -Version 0.2.0-beta.1 `
  -SignToolPath 'C:\Program Files (x86)\Windows Kits\10\bin\VERSION\x64\signtool.exe' `
  -AzureCodeSigningDlibPath 'C:\artifact-signing\bin\x64\Azure.CodeSigning.Dlib.dll' `
  -ArtifactSigningMetadataPath 'C:\release-secrets\sussudio-signing.json'
```

The script signs only an explicit first-party allowlist. It uses SHA-256 file
digests and the Microsoft RFC 3161 timestamp service, then runs SignTool
verification with timestamp warnings enabled for every first-party binary.
Third-party FFmpeg DLLs are never signed with the Sussudio publisher identity.
Microsoft documents this local Dlib/SignTool workflow and its required
timestamping in [Set up signing integrations](https://learn.microsoft.com/en-us/azure/artifact-signing/how-to-signing-integrations).

## FFmpeg identity

`Sussudio/ffmpeg/manifest.json` is a reviewed expected-input manifest, not a
manifest generated during packaging. It pins every shipped FFmpeg DLL by leaf
filename, product/build version, file version, and SHA-256. The package script
requires the source and staged DLL sets to match it exactly.

When intentionally updating FFmpeg, acquire the approved build, inspect its
version metadata, calculate each SHA-256 independently, update the tracked
manifest in the same review, and run the full reliability gate. Never update
the manifest merely to make an unexpected local binary pass.

## Publish on GitHub

Upload the generated ZIP and `.sha256.txt` file from `artifacts/releases/`.
Use the generated `.github-release.md` body and enable GitHub's **Set as a
pre-release** option. The ZIP checksum protects the final signed archive; the
Authenticode signatures independently bind first-party binaries to the
publisher identity.
