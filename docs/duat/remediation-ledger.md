# DUAT remediation ledger

This ledger records decisions and closure evidence for the immutable review at
[`reports/2026-09-02-2149.md`](reports/2026-09-02-2149.md). Corrections and
remediations belong here; do not rewrite the dated report after publication.

## High-priority decisions

| Report item | Decision | Status | Closure evidence required |
|---|---|---|---|
| #1 Automation pipe identity | Accepted risk — local hobby application; hostile same-user automation is outside project scope. Keep the existing optional-token and local named-pipe behavior unchanged. | Accepted risk | No protocol or security changes are planned. Revisit only if the product threat model changes. |
| #2 Recording finalization truth | Use typed Recording/Finalizing/Idle state plus Saved/Failed outcome; require drain, native close, and in-process structural verification before success; persist active and unresolved recovery journals. | Implemented; exact-commit closure pending | `RecordingFinalizationTruthTests`, `RecordingFinalizationFailureInjectionTests`, `RecordingFinalizationWatchdogTests`, `RecordingStructureVerifierTests`, and automation snapshot contract tests. |
| #3 FFmpeg/D3D COM ownership | Retain owned D3D11 device/context references for FFmpeg hardware contexts and release them with the enclosing native lifetime. | Implemented; exact-commit closure pending | `FfmpegD3D11OwnershipTests` and D3D source-contract coverage for initialization rollback, recording, Flashback, and reinitialization. |
| #4 Invalid device-audio mode | Strictly parse HDMI/Analog before runtime or settings mutation and return canonical values. | Implemented; exact-commit closure pending | `DeviceAudioModeParserTests` and automation dispatcher non-mutation tests. |
| #5 Missing requested microphone track | Resolve requested program and microphone inputs before recording commit; fail on initialization, pre-start disappearance, runtime loss, or missing observed stream. | Implemented; exact-commit closure pending | `RecordingAudioPreflightTests`, runtime audio-failure tests, and structural topology verification tests. |
| #6 WASAPI fallback format | Preserve float32/48-kHz/stereo internally; retain the negotiated render format and convert to supported float/PCM mono, stereo, or channel-mask-aware multichannel output with continuous resampling and pooled buffers. | Implemented; exact-commit closure pending | `WasapiRenderFormatConverterTests`, format-policy tests, continuity/drift tests, and unsupported-format failure tests. |
| #7 WASAPI worker teardown | Make workers own native resources through terminal cleanup; quarantine after a five-second timeout, block restart, preserve recording recovery, and begin bounded acknowledged app closure. | Implemented; exact-commit closure pending | `WasapiWorkerQuarantineTests`, ownership source-contract tests, and emergency-close UI/lifecycle tests. |
| #8 False-green release gate | Run and count real xUnit tests, keep the offline harness separate, run package contracts, verify FFmpeg identity and signatures, and require the gate before staging. | Workflow implemented; signed artifact evidence pending | Canonical Debug/Release gates, fresh nonzero TRX, release-helper tests, and FFmpeg manifest verification. Final closure additionally requires a clean tagged commit and successful signed ZIP plus checksum. |

## Release-policy decisions

- The supported current artifact is a signed Windows x64 prerelease ZIP.
- GitHub releases produced by the release script must be marked prerelease.
- MSIX packaging and Stream Deck integration remain future roadmap work.
- First-party binaries use Azure Artifact Signing with SHA-256 and a Microsoft
  timestamp. External signing metadata is never committed or packaged.
- Third-party FFmpeg DLLs retain their own identity and are accepted only when
  filename, build/product version, file version, and SHA-256 match
  `Sussudio/ffmpeg/manifest.json`.

No remediation commit was created as part of this working-tree implementation.
Statuses change to **Remediated** only after the listed evidence is captured on
the exact reviewed commit. A passing source build alone is not closure.
