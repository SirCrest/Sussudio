# Automation snapshot compatibility fixtures

These expected results were captured from the `BuildAutomationSnapshot` implementation in commit `db57ef5b`, before removing its copy-only projections.

- App DLL SHA-256: `22F943F4009EB12C1C6367726F88D047E9874143C9276295C2492244C3BDAC29`.
- Snapshot projection source SHA-256: `7921C8D90EEB76C5F9754C561359AEAD43784CE225E9A70A0458966A64EB0691`.
- Flashback projection source SHA-256: `F98CEAA14440B7DE7BD765A91C9D96F8B38AF8CB7D70E25EE60029C4D9B7B90F`.

`AutomationSnapshotRegressionFixture.BuildResult` supplies all 18 captured inputs and an uninitialized hub with explicit verification/threshold state; no providers, timers or devices start. `populated.json` records all 807 fields with distinct metric values, varied booleans, two-element arrays, normalization/fallback inputs and a fixed reported telemetry age. `defaults.json` records all 807 fields with default raw inputs and nullable members, including computed defaults that differ from DTO initializers. Repeating each capture produced identical output.

The tests compare every serialized field by name, JSON kind and value, including nested records and ordered arrays. Change expected data only for an intentional contract change after reviewing the field differences; running the tests never regenerates these files.
