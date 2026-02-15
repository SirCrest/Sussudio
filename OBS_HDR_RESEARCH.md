# OBS HDR Handling Research (Elgato 4K S / 4K X / 4K Pro)

Date: 2026-02-14  
Scope baseline: OBS latest release + `master`  
Latest release at time of research: OBS Studio `32.0.4` (released 2025-12-13)

## 1) Executive Conclusion

OBS handles HDR ingest from Elgato capture cards through the generic Windows DirectShow source path, not a vendor-specific HDR path. The key gate is 10-bit input (`P010`) plus Rec.2100 color configuration.  

For your requested models:
- `4K X` and `4K Pro` are explicitly listed by OBS as HDR-capable in the official KB.
- `4K S` is not named in that OBS KB entry, but Elgato documentation describes Windows HDR capture behavior and 10-bit HDR capture constraints for that card.

## 2) What OBS Is Doing (Fact vs Inference)

### Facts (directly documented in OBS sources)

1. HDR ingest support for video capture devices in OBS is Windows-only and requires a 10-bit capture format such as `P010`.
- Source: https://obsproject.com/kb/video-capture-devices-with-hdr-support

2. OBS explicitly lists these Elgato HDR-capable devices in that KB: `4K Pro`, `4K X`, `4K60 S+` (plus others).
- Source: https://obsproject.com/kb/video-capture-devices-with-hdr-support

3. In OBS code, `win-dshow` includes `VideoFormat::P010`, maps it to `VIDEO_FORMAT_P010`, and outputs frames via `obs_source_output_video2`.
- Source: https://github.com/obsproject/obs-studio/blob/master/plugins/win-dshow/win-dshow.cpp#L1535
- Source: https://github.com/obsproject/obs-studio/blob/master/plugins/win-dshow/win-dshow.cpp#L1162

4. `win-dshow` resolves color space/range from source settings (including `2100PQ` and `2100HLG`), and applies transfer/range/matrix info on frames before output.
- Source: https://github.com/obsproject/obs-studio/blob/master/plugins/win-dshow/win-dshow.cpp#L646
- Source: https://github.com/obsproject/obs-studio/blob/master/plugins/win-dshow/win-dshow.cpp#L663
- Source: https://github.com/obsproject/obs-studio/blob/master/plugins/win-dshow/win-dshow.cpp#L543
- Source: https://github.com/obsproject/obs-studio/blob/master/plugins/win-dshow/win-dshow.cpp#L545

5. HDR metadata channels are represented in `obs_source_frame2` (`range`, `trc`, `color_matrix`, range bounds).
- Source: https://github.com/obsproject/obs-studio/blob/master/libobs/obs.h#L64
- Source: https://github.com/obsproject/obs-studio/blob/master/libobs/obs.h#L86

6. libobs source/filter color-space negotiation uses `video_get_color_space`.
- Source: https://github.com/obsproject/obs-studio/blob/master/libobs/obs-source.h#L241
- Source: https://github.com/obsproject/obs-studio/wiki/High-precision-color-spaces-(including-HDR)

7. OBS HDR/precision wiki states:
- Rec.2100 options only exist with 10-bit formats (`P010`/`I010`).
- SDR white level and HDR nominal peak level are central to conversions/metadata behavior.
- Source: https://github.com/obsproject/obs-studio/wiki/High-precision-color-spaces-(including-HDR)

8. OBS tone-map filter behavior is implemented in `hdr-tonemap-filter.c` and uses SDR white level in shader parameterization.
- Source: https://github.com/obsproject/obs-studio/blob/master/plugins/obs-filters/hdr-tonemap-filter.c#L281
- Source: https://github.com/obsproject/obs-studio/blob/master/plugins/obs-filters/hdr-tonemap-filter.c#L264

9. Known P010 caveat: scaling filters/Luma Wipe can produce darkening artifacts for P010 sources under SDR workflows.
- Source: https://github.com/obsproject/obs-studio/issues/7675

10. OBS 28 release notes formally introduced HDR and documented encoder/feature limitations in that initial rollout.
- Source: https://obsproject.com/blog/obs-studio-28-release-notes

11. OBS 30.1 release notes added HDR support for Elgato HD60 X Rev.2.
- Source: https://obsproject.com/blog/obs-studio-30-1-release-notes

### Inferences (derived from source + docs)

1. OBS HDR ingest for Elgato is primarily "format/capability driven" (P010 + Rec.2100 path), not a device-brand custom HDR engine.

2. Elgato-specific code in `win-dshow` is focused on buffering behavior (`IsDelayedDevice` / `SetupBuffering`), not HDR color pipeline rules.
- Source: https://github.com/obsproject/obs-studio/blob/master/plugins/win-dshow/win-dshow.cpp#L673
- Source: https://github.com/obsproject/obs-studio/blob/master/plugins/win-dshow/win-dshow.cpp#L731

3. For parity, our app should model "HDR eligibility = negotiated 10-bit source + valid color metadata + compatible encoder path", not just a toggle.

## 3) Release (32.0.4) vs `master` Check

Baseline release date/source:
- https://obsproject.com/download

Checked post-2025-12-13 history for relevant files via GitHub API:
- `plugins/win-dshow/win-dshow.cpp`
- `plugins/win-dshow/dshow-base.cpp`
- `plugins/win-dshow/dshow-formats.cpp`
- `plugins/obs-filters/hdr-tonemap-filter.c`
- `libobs/obs-source.h`
- `libobs/obs.h`
- `libobs/obs-video.c`

Notable findings:
1. `win-dshow.cpp` changed after 32.0.4, but inspected commit is buffering-related (async/unbuffered handling), not HDR transfer/matrix logic.
- Source: https://github.com/obsproject/obs-studio/commit/d8d14ec69516fa7466cf5d16e5a40ec587453d7b

2. `dshow-base.cpp` post-release commit reviewed is warning/lint related, not HDR behavior.
- Source: https://github.com/obsproject/obs-studio/commit/e8aac2f79f5d177f185ace4f21417f5975cc6d8c

3. `hdr-tonemap-filter.c` post-release touches are non-behavioral (CI/translations), based on inspected commits.
- Source: https://github.com/obsproject/obs-studio/commit/472ea622db8bc9ba6fa816ea39f755f72d0f5f76
- Source: https://github.com/obsproject/obs-studio/commit/e5f28a5c658f67a43709338869c8ffd57afe9d18

4. No evidence found of a material post-32.0.4 redesign of the DirectShow HDR ingest fundamentals.

## 4) Model Mapping: 4K S / 4K X / 4K Pro

1. `4K X` and `4K Pro`:
- Explicitly in OBS HDR-capable KB list.
- Sources:
  - https://obsproject.com/kb/video-capture-devices-with-hdr-support
  - https://help.elgato.com/hc/en-us/articles/23658118721421-Elgato-Game-Capture-4K-X-Technical-Specifications
  - https://help.elgato.com/hc/en-us/articles/23710571317517-Elgato-Game-Capture-4K-Pro-Technical-Specifications

2. `4K S`:
- Not explicitly listed on the OBS KB page above.
- Elgato publishes Windows HDR capture constraints and limits for 4K S (including HDR capture cap).
- Source:
  - https://help.elgato.com/hc/en-us/articles/37196014096017-Elgato-Game-Capture-4K-S-Technical-Specifications
  - https://help.elgato.com/hc/en-us/articles/37618531304721-Elgato-Studio-1-0-1-Release-Notes

## 5) ElgatoCapture Parity Matrix (Current vs OBS)

## 5.1 Must

1. Add explicit HDR color mode model (PQ/HLG) in app settings and runtime contracts.
- OBS: supports both Rec.2100 PQ and HLG settings path.
- Current app: `HdrOutputMode` has `Off` and `Hdr10Pq` only.
- Local refs:
  - `ElgatoCapture/Models/CaptureSettings.cs:25`
  - `ElgatoCapture/Models/CaptureSettings.cs:44`

2. Track and propagate explicit color metadata (primaries/transfer/colorspace/range) from capture negotiation through runtime snapshot, not just pixel format.
- OBS: frame-level metadata fields are first-class (`range`, `trc`, matrix).
- Current app: strong pixel-format checks (`P010`) and ffprobe output checks, but no first-class capture-time transfer/range enum contract.
- Local refs:
  - `ElgatoCapture/Services/CaptureService.cs:842`
  - `ElgatoCapture/Services/CaptureService.cs:1179`
  - `ElgatoCapture/Services/RecordingVerifier.cs:641`
  - `ElgatoCapture/Models/AutomationContracts.cs:266`

3. Align HDR policy gate behavior with user intent and diagnostics clarity.
- OBS: HDR availability depends on valid format/colorspace combinations.
- Current app: HDR pipeline can be force-disabled via environment overrides, which can differ from user expectation.
- Local refs:
  - `ElgatoCapture/Services/HdrOutputPolicy.cs:8`
  - `ElgatoCapture/Services/HdrOutputPolicy.cs:15`
  - `ElgatoCapture/Services/HdrOutputPolicy.cs:22`

4. Preserve strict "invalid combination" blocking semantics for HDR mode.
- OBS: Rec.2100 only with 10-bit format.
- Current app: partially enforced through format and codec checks; should be unified as one explicit decision tree.
- Local refs:
  - `ElgatoCapture/ViewModels/MainViewModel.cs:664`
  - `ElgatoCapture/ViewModels/MainViewModel.cs:717`
  - `ElgatoCapture/Services/FFmpegEncoderService.cs:660`
  - `ElgatoCapture/Services/FFmpegEncoderService.cs:678`

## 5.2 Should

1. Formalize preview tone-map mode semantics.
- Current app reports `None/Auto/Unavailable` based largely on HDR detection + GPU active, not explicit transform mode.
- Local ref: `ElgatoCapture/Services/AutomationDiagnosticsHub.cs:305`

2. Mirror OBS-style source capability negotiation semantics in diagnostics.
- Current app does subtype checks and runtime fallback; add explicit "chosen HDR mode + reason + fallback reason code" struct for parity-grade debugging.
- Local refs:
  - `ElgatoCapture/Services/CaptureService.cs:1246`
  - `ElgatoCapture/Services/CaptureService.cs:1363`

## 5.3 Optional

1. Add richer user-facing warnings for filter/processing operations that are known to behave differently with 10-bit HDR sources.
- Relevant OBS caveat source: https://github.com/obsproject/obs-studio/issues/7675

## 6) Implementation Checklist (Actionable)

1. **Settings/Contracts**
- Add `HdrColorTransferMode` enum (`Pq`, `Hlg`) and `HdrColorRangeMode` enum (`Tv`, `Full`, `Auto`).
- Add runtime snapshot fields: `NegotiatedColorPrimaries`, `NegotiatedColorTransfer`, `NegotiatedColorSpace`, `NegotiatedColorRange`.

2. **Capture negotiation**
- When HDR requested, require negotiated 10-bit source and resolve transfer mode from negotiated subtype/properties.
- Persist resolved mode in capture runtime state and automation snapshot.

3. **Encoder signaling**
- Keep existing FFmpeg metadata args, but bind them to resolved runtime mode rather than hardcoded PQ assumptions.
- For HLG path, emit HLG transfer metadata when encoder supports it.

4. **Unified HDR decision tree**
- Create one method that returns `HdrDecision` with:
  - `IsHdrActive`
  - `ResolvedTransfer` (`PQ`/`HLG`/`None`)
  - `ResolvedInputPixelFormat`
  - `FailureReasonCode`
- Use this in ViewModel validation, CaptureService startup, and diagnostics.

5. **Verification**
- Extend verifier to assert expected transfer mode (`smpte2084` for PQ, `arib-std-b67` for HLG where applicable).
- Keep existing `bt2020` primaries/space checks.

6. **Diagnostics UX**
- Show one-line HDR state summary in UI/logs:
  - `requested -> negotiated -> encoder-signaled -> verified`

## 7) Validation Scenarios

1. 4K X HDR input:
- Validate negotiated 10-bit input, active HDR pipeline, and recorded transfer/primaries metadata.

2. 4K Pro HDR input:
- Same validation as above.

3. 4K S HDR input:
- Validate within card's documented HDR capture limits (Windows + capture resolution constraints).

4. Negative matrix:
- HDR requested + non-10-bit negotiated source -> fail fast or auto-downgrade with explicit reason.
- HDR requested + incompatible encoder -> explicit blocking error.

5. Regression matrix:
- SDR mode unchanged.
- HDR mode with HEVC and AV1.
- Verify output metadata and cadence checks remain green.

## 8) Source Index

OBS official:
- https://obsproject.com/download
- https://obsproject.com/kb/video-capture-devices-with-hdr-support
- https://obsproject.com/blog/obs-studio-28-release-notes
- https://obsproject.com/blog/obs-studio-30-1-release-notes
- https://github.com/obsproject/obs-studio/wiki/High-precision-color-spaces-(including-HDR)
- https://github.com/obsproject/obs-studio/blob/master/plugins/win-dshow/win-dshow.cpp
- https://github.com/obsproject/obs-studio/blob/master/libobs/obs.h
- https://github.com/obsproject/obs-studio/blob/master/libobs/obs-source.h
- https://github.com/obsproject/obs-studio/blob/master/plugins/obs-filters/hdr-tonemap-filter.c
- https://github.com/obsproject/obs-studio/issues/7675

Release-vs-main evidence:
- https://api.github.com/repos/obsproject/obs-studio/commits?path=plugins/win-dshow/win-dshow.cpp&since=2025-12-13T00:00:00Z
- https://api.github.com/repos/obsproject/obs-studio/commits?path=plugins/win-dshow/dshow-base.cpp&since=2025-12-13T00:00:00Z&per_page=1
- https://api.github.com/repos/obsproject/obs-studio/commits?path=plugins/obs-filters/hdr-tonemap-filter.c&since=2025-12-13T00:00:00Z
- https://github.com/obsproject/obs-studio/commit/d8d14ec69516fa7466cf5d16e5a40ec587453d7b
- https://github.com/obsproject/obs-studio/commit/e8aac2f79f5d177f185ace4f21417f5975cc6d8c
- https://github.com/obsproject/obs-studio/commit/472ea622db8bc9ba6fa816ea39f755f72d0f5f76
- https://github.com/obsproject/obs-studio/commit/e5f28a5c658f67a43709338869c8ffd57afe9d18

Elgato model context:
- https://help.elgato.com/hc/en-us/articles/37196014096017-Elgato-Game-Capture-4K-S-Technical-Specifications
- https://help.elgato.com/hc/en-us/articles/23658118721421-Elgato-Game-Capture-4K-X-Technical-Specifications
- https://help.elgato.com/hc/en-us/articles/23710571317517-Elgato-Game-Capture-4K-Pro-Technical-Specifications
- https://help.elgato.com/hc/en-us/articles/37618531304721-Elgato-Studio-1-0-1-Release-Notes


