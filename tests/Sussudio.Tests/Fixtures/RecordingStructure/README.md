# Recording structure fixtures

These small files exercise the actual `InProcessRecordingStructureVerifier` in `InProcessRecordingStructureVerifierTests.cs`. They contain generated black video and sine tones, with no captured or third-party media. Ordinary tests read these committed files and the app-bundled native libav runtime; they do not launch FFmpeg, require capture hardware, or use GPU encoders.

The 16 files total 146,491 bytes. `manifest.json` records their hashes and independently observed stream metadata. Every valid video is 64x64 at 50 fps. The tests copy each input to an isolated directory, check its hash against this manifest, invoke the compiled production verifier, assert result and track evidence, and verify that the file remains unchanged and its handle is released.

## Corpus and contracts

| Fixtures | Content and purpose |
|---|---|
| `h264-video.mp4` | 2 s H.264 with no audio; valid baseline, mismatched codec/dimensions, missing audio, and 0.25 s shortfall boundary |
| `h264-program.mp4` | 2 s H.264 plus one 2 s AAC tone; program audio, microphone-only, and topology mismatches |
| `h264-dual.mp4` | 2 s H.264 plus two separate 2 s AAC tones (440/880 Hz); program/microphone topology |
| `hevc-hdr-explicit.mp4` | 2 s HEVC Main10 with BT.2020 primaries, SMPTE 2084 transfer and BT.2020 nonconstant luminance matrix |
| `hevc-sdr.mp4` | 2 s HEVC without HDR metadata; valid SDR and rejected HDR request |
| `av1-video.mp4` | 2 s AV1; supported codec baseline |
| `h264-floor-boundary.mp4` | 0.1 s video; expected recording interval at 0.25 s |
| `h264-percent-boundary.mp4` | 9.5 s video; expected intervals 10.0 s and 10.0001 s bracket the 5% shortfall boundary |
| `h264-cap-boundary.mp4` | 42 s video; expected intervals 44.0 s and 44.0001 s bracket the 2 s maximum shortfall |
| `h264-audio-short-boundary.mp4`, `h264-audio-too-short.mp4` | 2 s video with 1.5 s and 1.48 s audio; shorter-audio tolerance |
| `h264-audio-long-boundary.mp4`, `h264-audio-too-long.mp4` | 2 s video with 2.5 s and 2.52 s audio; longer-audio tolerance |
| `empty.mp4`, `garbage.mp4` | Empty output and plain text masquerading as an MP4 |
| `header-without-packets.mp4` | Fast-start H.264 metadata preserved through the mdat header; all packet payloads removed |

The ordinary 2 s fixture uses expected intervals 2.25 s and 2.2501 s for the minimum shortfall allowance. These are literal contract examples; the test does not duplicate the verifier's tolerance formula.

Requested audio labels follow the recording context and observed stream order. A one-audio-track fixture can represent program audio or microphone-only topology. These cases do not prove physical source identity. Unexpected audio is tested by observed cardinality rather than freezing an inferred device/microphone label.

## Provenance and regeneration

Generated and validated on 2026-09-07 using:

- FFmpeg/ffprobe `2026-01-29-git-c898ddb8fe-essentials_build-www.gyan.dev`.
- CPU encoders `libx264`, `libx265`, `libaom-av1`, and `aac`.
- Actual verification with app-bundled libav build `N-123250-gb8a4d8a18d-20260307`; all four library hashes matched `Sussudio/ffmpeg/manifest.json`.

From the repository root, generate into a new empty scratch directory:

```powershell
& 'tests/Sussudio.Tests/Fixtures/RecordingStructure/Generate-Fixtures.ps1' -OutputDirectory 'artifacts/recording-structure-fixtures' -Ffmpeg 'C:/WINDOWS/ffmpeg.exe' -Ffprobe 'C:/WINDOWS/ffprobe.exe'
```

The script refuses to overwrite a nonempty directory, records the tool versions and exact argument arrays in `generation.json`, records fresh hashes and stream metadata, and fully decodes each valid fixture. It does not run the intentionally invalid files through the positive decode gate. Review the generated files and metadata before replacing the committed corpus; encoder-version changes can change bytes without changing the intended media contract. Update the manifest together with any accepted fixture changes and rerun the native verifier tests.

Explicit x265 `colorprim`, `transfer` and `colormatrix` parameters are required in the generator. On the validated FFmpeg build, generic color options alone did not preserve primaries and transfer metadata, and the verifier correctly rejected that trial fixture.

These tests prove saved-output structure checks and input ownership. They do not prove end-to-end recording finalization timing, audible output, HDR display appearance, or full production-file decode validity. The verifier checks required readable packets and metadata; full fixture decode is a separate provenance check.
