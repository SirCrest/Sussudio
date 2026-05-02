# Sussudio Design System

Adapted from [shadcn/ui](https://ui.shadcn.com/) principles for WinUI 3 / XAML.
Retains: drop shadows, teal accent color system. Eliminates: stock Fluent feel,
ripple/reveal effects, overly generous spacing.

## Principles

1. **Restraint over decoration.** Components should look like content until
   interacted with. No gratuitous borders, fills, or visual noise.
2. **Shadows for depth, borders for boundaries.** Drop shadows stay (they
   provide spatial hierarchy). Use 1px borders only to separate adjacent
   regions, not to outline every control.
3. **Accent sparingly.** Teal (`#00CED1`) appears on active/selected states,
   toggle indicators, and key interactive elements. Most of the UI is neutral.
4. **Dense but not cramped.** 14px base font. Tighter spacing than Fluent
   defaults. 4px grid for all spacing values.
5. **Flat interactions.** No Fluent reveal highlight, no ripple effects.
   Hover = subtle background fill change. Press = slight opacity reduction.

---

## Color Tokens (Dark Theme)

### Surfaces
| Token | Value | Usage |
|---|---|---|
| `Background` | `#0A0A0A` | Window/page background (Mica) |
| `Surface` | `#141414` | Cards, panels, control bar |
| `SurfaceHover` | `#1E1E1E` | Elevated surface on hover |
| `SurfaceRaised` | `#202020` | Timeline panel, popovers |
| `Overlay` | `#0A0A0A` at 80% | Acrylic panels (settings, stats) |

### Text
| Token | Value | Usage |
|---|---|---|
| `TextPrimary` | `#FFFFFF` | Headings, active labels |
| `TextSecondary` | `#A0A0A0` | Body text, descriptions |
| `TextMuted` | `#707070` | Inactive labels, placeholders |
| `TextDisabled` | `#3A3A3A` | Disabled controls |

### Accent (Teal)
| Token | Value | Usage |
|---|---|---|
| `Accent` | `#00CED1` | Active toggles, selected items, live edge |
| `AccentHover` | `#3FE8E8` | Accent element hover |
| `AccentPressed` | `#00A5A8` | Accent element press |
| `AccentSubtle` | `#1800CED1` | Checked toggle background fill |
| `AccentGlow` | `#3000CED1` | Selection regions, emphasis areas |

### Interactive (Ghost/Neutral)
| Token | Value | Usage |
|---|---|---|
| `GhostHover` | `#12FFFFFF` | Button/item hover fill |
| `GhostPressed` | `#08FFFFFF` | Button/item press fill |
| `GhostBorder` | `Transparent` | Default button border |

### Borders
| Token | Value | Usage |
|---|---|---|
| `BorderSubtle` | `#10FFFFFF` | Panel edges, separators |
| `BorderMedium` | `#20FFFFFF` | Input fields, active panels |
| `BorderStrong` | `#30FFFFFF` | Dividers, toolbar separators |

### Semantic
| Token | Value | Usage |
|---|---|---|
| `Destructive` | `#E81123` | Record button, stop, errors |
| `DestructiveHover` | `#FF4D5E` | Record hover |
| `Warning` | `#F5C542` | Disk warnings, clip alert |
| `Gold` | `#FFD700` | In/out point markers |

---

## Typography

Use Segoe UI Variable (ships with Windows 11) as primary. Consolas for
monospace data values (timecodes, stats, bitrates).

| Role | Size | Weight | Tracking | Font |
|---|---|---|---|---|
| `HeadingLg` | 20px | SemiBold (600) | -20 | Segoe UI Variable |
| `HeadingSm` | 14px | SemiBold (600) | -10 | Segoe UI Variable |
| `Label` | 13px | Medium (500) | 0 | Segoe UI Variable |
| `Body` | 13px | Regular (400) | 0 | Segoe UI Variable |
| `BodySmall` | 12px | Regular (400) | 0 | Segoe UI Variable |
| `Caption` | 11px | Regular (400) | 0 | Segoe UI Variable |
| `DataValue` | 12px | Regular (400) | 0 | Consolas |
| `DataValueLg` | 13px | Medium (500) | 0 | Consolas |
| `TimecodeLabel` | 11px | Regular (400) | 0 | Consolas |
| `ButtonLabel` | 12px | Medium (500) | 0 | Segoe UI Variable |

### Rules
- Headings use **negative CharacterSpacing** (-20 or -10) for the tight,
  premium feel. Body and smaller text use 0.
- **SemiBold (600)** for section headers, never Bold (700) except the record
  indicator.
- **Medium (500)** for interactive text (buttons, labels, active values).
- **Regular (400)** for passive/informational text.

---

## Spacing (4px Grid)

All spacing values are multiples of 4.

| Token | Value | Usage |
|---|---|---|
| `SpaceXs` | 4px | Inline gaps, icon-to-text |
| `SpaceSm` | 8px | Between related controls, row spacing |
| `SpaceMd` | 12px | Panel padding, section gaps |
| `SpaceLg` | 16px | Panel margins, major grouping |
| `SpaceXl` | 24px | Section separation |
| `Space2xl` | 32px | Page-level spacing |

### Panel Padding
| Element | Padding | Margin |
|---|---|---|
| Control bar | `16,12` | `16,8,16,16` |
| Timeline panel | `12,10` | `16,0,16,8` |
| Settings panel | `12,8` | `0` |
| Stats dock | `12` | `0,8,12,16` |
| Action buttons | `8,0` horizontal | internal `4` spacing |

---

## Corner Radii

Single global radius variable with derivations to prevent double-rounding.

| Token | Value | Usage |
|---|---|---|
| `RadiusContainer` | 12px | Control bar, settings panel |
| `RadiusPanel` | 8px | Timeline panel, stats dock, cards |
| `RadiusControl` | 6px | Buttons, inputs, dropdowns |
| `RadiusSmall` | 4px | Track backgrounds, indicators, badges |
| `RadiusPill` | 9999px | Pill shapes, record button |
| `RadiusCircle` | 50% | Playhead handle, dots |
| `RadiusMin` | 2px | Progress bars, thin elements |

### Nesting Rule
When a control sits inside a container, its radius should be
`container_radius - padding_between / 2` or the next smaller token.
A button (6px) inside a panel (8px) with 12px padding looks correct.
A button (8px) inside a panel (8px) looks double-rounded — avoid.

---

## Shadows

Shadows provide spatial hierarchy. Three tiers:

| Tier | BlurRadius | Color | Offset Y | Usage |
|---|---|---|---|---|
| `ShadowGround` | 8px | `rgba(0,0,0,0.3)` | 1px | Subtle ground plane (timeline track) |
| `ShadowMid` | 12px | `rgba(0,0,0,0.47)` | 1px | Floating panels (control bar) |
| `ShadowHigh` | 16px | `rgba(0,0,0,0.63)` | 2px | Top-level elements (video frame) |

### Rules
- Every shadow has a small Y offset (1-2px) — pure centered shadows look
  unnatural.
- Shadow alpha increases with elevation tier.
- Only surfaces that float above the background get shadows. Inline controls
  (buttons, inputs) never have shadows.
- Shadows animate in/out with their parent element (opacity fade, not
  blur/offset animation).

---

## Interactive States

### Ghost Button (default control bar style)
| State | Background | Foreground |
|---|---|---|
| Rest | Transparent | `TextMuted` (#707070) |
| Hover | `GhostHover` (#12FFFFFF) | `TextPrimary` (#FFFFFF) |
| Pressed | `GhostPressed` (#08FFFFFF) | `TextSecondary` (#A0A0A0) |
| Disabled | Transparent | `TextDisabled` (#3A3A3A) |

### Accent Toggle (checked state)
| State | Background | Foreground |
|---|---|---|
| Checked | `AccentSubtle` (#1800CED1) | `Accent` (#00CED1) |
| Checked+Hover | `#2400CED1` | `AccentHover` (#3FE8E8) |
| Checked+Pressed | `#1000CED1` | `AccentPressed` (#00A5A8) |

### Action Button (timeline In/Out/Export)
| State | Background | Border | Foreground |
|---|---|---|---|
| Rest | Transparent | `BorderSubtle` | `TextSecondary` |
| Hover | `GhostHover` | `BorderMedium` | `TextPrimary` |
| Pressed | `GhostPressed` | `BorderSubtle` | `TextSecondary` |
| Disabled | Transparent | Transparent | `TextDisabled` |

### Rules
- **No ripple or reveal effects.** Hover = instant background fill.
- **No border changes on hover** for ghost buttons. Only background.
- **Action buttons** (outline style) can brighten their border on hover.
- **Disabled = 50% opacity** on the entire control, not individual property
  changes.
- **Focus ring**: 2px solid `Accent` with 2px offset from element edge.

---

## Animation Timing

| Category | Duration | Easing |
|---|---|---|
| Hover state transition | 100ms | Linear |
| Panel open/close | 250ms | CubicEase EaseOut |
| Fade in (content) | 200-350ms | CubicEase EaseOut |
| Fade out (content) | 150-200ms | CubicEase EaseIn |
| Scale entrance | 250ms, 0.97→1.0 | CubicEase EaseOut |
| Scale exit | 200ms, 1.0→0.97 | CubicEase EaseIn |
| Stagger per item | 50ms delay | Same as parent |
| Shadow fade | Matches parent opacity | Linear |
| Value transitions (sliders, meters) | 80ms | Linear |
| Playhead position (live mode) | Per-frame, no easing | Immediate |
| Playhead position (snap/seek) | 150ms | CubicEase EaseOut |

### Rules
- Exits are faster than entrances (200ms out vs 250-350ms in).
- Scale animations are subtle: 0.97-1.0 range only. Never scale below 0.95.
- Stagger delays are 50ms per item, max 5 items (250ms total stagger).
- Live-updating values (meters, playhead in live mode) update immediately
  with no easing — easing causes perceived lag.
- Seek/snap operations use a short ease-out to feel responsive but not
  jarring.

---

## Flashback Timeline — Design Spec

### Track
- Background: `Surface` (#141414), CornerRadius `RadiusSmall` (4px)
- Height: 32px (visual track), hit area extends 8px above/below
- Ground shadow beneath the track (ShadowGround tier)

### Playhead / Needle
- White vertical line, 2px width, full track height
- Circular handle on top: 10px diameter, white fill, 1px `#40000000` border
- Time label floats above handle: `TimecodeLabel` font, `Surface` background
  at 80% opacity, `RadiusSmall` corners, 4px horizontal padding
- In live mode: updates position per-frame, no transition animation
- On seek/snap: 150ms CubicEase EaseOut

### Buffer Visualization
- Filled region from oldest segment to live edge
- Fill color: `GhostHover` (#12FFFFFF) — subtle presence
- Live edge: 2px `Accent` line at the right edge of filled region
- As new content arrives while paused: filled region extends right,
  playhead stays put, time label counts relative to live edge

### In/Out Selection
- Region fill: `AccentGlow` (#3000CED1)
- Markers: 2px `Gold` (#FFD700) lines at in/out positions
- Markers visible once set, hidden by default

### Duration Display
- Bottom-left of timeline panel: elapsed/total as `TimecodeLabel`
- Format: `-MM:SS` (negative = behind live) or `MM:SS / MM:SS` (in/out range)
- `TextMuted` color, transitions to `TextSecondary` on timeline hover

### Action Buttons
- Arranged horizontally below the track with `SpaceXs` (4px) spacing
- Style: Action Button (outline variant from Interactive States above)
- Grouped with `BorderStrong` vertical separators between logical groups:
  [In | Out | Clear] | [Play/Pause | Go Live] | [Export | Save Last 5m]
- "Go Live" uses `Accent` foreground (teal text, no fill)

### Panel Container
- Background: `SurfaceRaised` (#202020) at 90% opacity
- Border: 1px `BorderMedium`
- CornerRadius: `RadiusPanel` (8px)
- Shadow: `ShadowGround` tier
- Margin: `16,0,16,8` (sits between preview and control bar)

### State-Dependent Behavior
| State | Needle | Track | Duration Label |
|---|---|---|---|
| Flashback OFF | Hidden | Hidden | Hidden |
| Live (Flashback ON) | Right edge, live updates | Shows buffer fill | Shows buffer duration |
| Paused from live | Stays put, buffer grows right | Buffer extends | Counts relative to live |
| Scrubbing | Follows pointer | Shows position in buffer | Shows scrub position |
| Playing | Moves right at playback rate | Shows playback position | Counts up/relative |
| At live edge | Snaps to live mode | Full fill | Shows buffer duration |

---

## Migration Plan

1. **Flashback timeline panel** — apply new tokens, fix UX issues
2. **Control bar** — align button styles, spacing, shadows
3. **Settings panel** — typography hierarchy, input styles, spacing
4. **Stats dock** — consistent with new tokens
5. **Global resources** — move inline colors to AppResources.xaml tokens
