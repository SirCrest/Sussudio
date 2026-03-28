# Preview Animation Specification

## Thematic Goal

The preview is the most important element on screen — a live video feed the user
watches while they play. It behaves like a physical object with weight and
inertia. It never snaps or jumps. It doesn't instantly respond to layout changes.

Floating UI elements (settings panel, flashback timeline, stats dock) slide into
position **over** the preview first. The preview then **reacts** — as if noticing
it's being covered — and smoothly reshapes itself to fit into the remaining
visible space. The impression is that the preview yields to the panel, not that
the layout pushes it.

## Design Language: Staggered Motion

The staggered timing between panel and preview is not a technical detail — it is
a deliberate motion design choice borrowed from premium motion graphics. In
professional motion design, elements that animate in perfect sync look
mechanical and cheap. Elements that move **out of sync** — where one leads and
another follows — instantly read as polished and intentional.

This rolling/staggered pattern is a core design language of the app:
- The panel is the **initiator** — it moves first, with confidence.
- The preview is the **responder** — it notices, yields, and settles with weight.
- The temporal gap between them communicates that these are independent,
  physically-modeled elements, not cells in a spreadsheet recalculating.

This principle applies everywhere: any time a UI element appears, disappears, or
changes in a way that affects another element's space, the affected element
responds with its own independent, delayed animation — never as an instant
layout side-effect.

## Interaction Model

### Panel Opens (e.g. Settings)
1. The settings panel slides up from the bottom edge (250ms, ease-out).
   During this slide, the preview remains at full size — the panel overlaps it.
2. Once the panel is in position (or after a short delay), the preview begins
   its transition: it shrinks and repositions to fit the space above the panel.
3. The preview transition uses a 700ms cubic bezier ease with slight overshoot
   — control points (0.05, 0.7) and (0.1, 1.0).
4. The drop shadow tracks the preview exactly, same easing, same timing.

### Panel Closes (e.g. Settings)
1. The preview begins expanding back toward full size.
2. The settings panel slides out downward (200ms, ease-in) concurrently or
   slightly after the preview begins moving.
3. The preview fills the full stage area once the transition completes.

### Key Principle
The preview does **not** respond instantly to the panel appearing. The panel
moves first, overlapping the preview briefly. Then the preview reacts. This
creates the feeling of a responsive, weighted element rather than a layout
recalculation.

## Animation Mechanics

### Mechanism
Compositor Scale + Offset on the VideoContent handoff visual, driven by a
single Progress property via expression animations.

- **Scale** handles the size change (shrink/grow).
- **Offset** handles the position change (reposition to center of available space).
- **CenterPoint** is set to the center of the element so it scales symmetrically.
- **Offset at Progress=0** compensates for the CenterPoint displacement so the
  preview starts at its exact current position.

### Formula
At any progress value `p`:
```
visualPos = layoutPos + Offset(p) + CenterPoint * (1 - Scale(p))
visualSize = elementSize * Scale(p)
```

At Progress=0 (start):
- Scale = (1, 1) or (oldW/newW, oldH/newH) depending on direction
- Offset cancels CenterPoint displacement → preview at old position/size

At Progress=1 (end):
- Scale = target ratio
- Offset = translate delta
- Preview at new position/size

### Shadow
The shadow visual (`_videoShadowVisual`) is a SpriteVisual with DropShadow.
Its Offset and Size are animated via expression animations bound to the same
Progress property, guaranteeing per-frame lockstep with the preview.

### Easing
Cubic bezier: control points (0.05, 0.7) and (0.1, 1.0).
Duration: 700ms.
Character: fast initial response, gentle overshoot, smooth settle.

### Completion
When the animation completes (batch.Completed), commit the final XAML state
via `UpdateVideoContentOverlays()` — sets Width, Height, TranslateTransform,
SwapChainPanel size, PreviewImage size, and shadow position.

## Guard Rails

### Re-entrancy Protection
`_isVideoTransitioning` is set to `true` before any XAML mutations that could
trigger SizeChanged handlers. Both `OnVideoContentSizeChanged` and
`OnStageGridSizeChanged` check this flag and bail early during transitions.

### Mid-flight Interruption
If a new transition starts while one is running, `CaptureVideoVisualRect()`
freezes the in-flight state, resets the compositor to identity, commits the
frozen visual rect to XAML, and the new transition starts from that state.

### Dual-panel Overlap
If both settings and timeline toggle simultaneously, the second
`AnimateVideoTransitionDeferred` call freezes the first via
`CaptureVideoVisualRect()` before starting.

## What This Is NOT

- Not a XAML layout animation. XAML Width/Height do not change during the
  transition — they are committed only on completion.
- Not a per-frame property animation. The compositor thread drives Scale/Offset
  at display refresh rate, independent of the UI thread.
- Not a clip or crop. The entire video content scales uniformly — every
  intermediate frame shows the complete video at the correct in-between size.
