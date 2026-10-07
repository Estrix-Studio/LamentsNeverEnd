# LoopShift publication readiness

This is an evidence checklist, not a release approval. Rechecked against the official [Unity Asset Store submission guidelines](https://assetstore.unity.com/publishing/submission-guidelines) on 2026-10-06.

## Publisher details

- Package author: Estrix Studio (existing manifest).
- Public support email: contact@estrixstudio.com (provided by the user).
- Public website: not supplied.
- Listing price/category and final marketing approval: not supplied.
- No upload or public submission has been authorized or performed.

## Submission requirements still requiring evidence

Unity requires accurate dependency and compatibility disclosures, comprehensive documentation, appropriate third-party notices, original/useful content, and disclosure of AI-assisted functional content. Its current guidelines require URP or HDRP support for submissions from Unity 6.5 onward and disabled-domain-reload support from 6.6. Publisher contact details must include an active email and maintained website. Package content must be organized under one root, avoid redundant files, and produce no undisclosed package-originating errors or warnings. Marketing must describe material limitations accurately. These checks do not replace Unity's review.

## Product-specific evidence and remaining work

| Requirement | Evidence or next verification |
| --- | --- |
| Standalone runtime/editor | Rebuilt UPM and unitypackage archives; fresh imports, builds and actual player runs passed for both formats |
| Visual authoring | Canvas, shared authoring operations, room handles and 50 passing host EditMode tests, including idle refresh and gesture behavior; visual inspection remains open |
| Runtime correctness | 25 host PlayMode tests plus 123 game assertions, including authored dialogue callbacks, torch state and physical enemy contact; physical doorway navigation and human-input dialogue checks remain open |
| Distribution documentation | Both archive formats now include authoring and lifecycle guides |
| Supported Unity editors | Earlier UPM candidate passed 48 editor, 26 runtime and two reload tests plus Windows player execution in 2022.3.62f3 and 6000.3.25f1 (Docs/Evidence/LoopShift-20261007-Unity2022.json and LoopShift-20261007-LTS6.json). The preceding archive also passed 49 editor, 26 runtime and two reload tests plus Windows player execution in 2022.3.62f3; exact identity: Docs/Evidence/LoopShift-20261007-Final-Unity2022.json |
| Render pipeline | Sample camera renders passed in Built-in and URP 17.7.0 on Unity 6000.7.0b1, and Built-in and URP 17.3.0 on Unity 6000.3.25f1. Preceding archive images inspected and retained under Docs/Evidence/Rendering-LTS6; interactive visual checks remain open |
| Fast Enter Play Mode | Two consumer tests passed per format for consecutive sessions with domain reload disabled, with and without scene reload |
| Standalone execution | Both built players passed 15 physical crossings, bounded room counts and restart using a consumer-only harness |
| Third-party notices | Dedicated dependency/content notices added and included in both exports; final publisher review remains open |
| Store media | Original editor and sample screenshots/video still required |
| Asset Store validator | Latest unitypackage: all 36 Asset Store Tools 12.0.5 checks pass without warnings in an isolated project; exact archive identity and results retained in Docs/Evidence/LoopShift-AssetStore-Gestures-Identity.json and LoopShift-AssetStore-Gestures.json |
| Public listing | Human-reviewed description, compatibility table, AI disclosure, support website and price remain open |

The player verification harness lives under Tools/LoopShiftValidation and is copied only into disposable consumer projects. It is not shipped inside the distributable package. Passing it proves tested runtime behavior; it does not prove visual quality.

## Changes after the verified archive

The graph editor now preserves refresh requests during a gesture and cancels an old drag when switching graphs. All 50 host editor tests passed, including the gesture regression test. These source changes are now exported: UPM SHA256 46CFF3ECC5614B20EC08813F49250B83326A6EB140327EE728647066BEEB6AF0 and unitypackage SHA256 BC2ECD6700D8A40B441DC60299D7C0AC24C76F3B92959939DE697A1BE0A51190. This candidate passed 50 editor tests, 26 runtime tests, two reload-mode tests and standalone player execution in Unity 6000.3.25f1 (Docs/Evidence/LoopShift-20261007-Gestures-LTS6.json). The unitypackage passed all 36 validator checks (Docs/Evidence/LoopShift-AssetStore-Gestures-Identity.json). Earlier compatibility and validator reports retain their preceding archive identities.

The preceding archive also completed Unity 6 LTS/URP standalone player verification and Unity 2022.3 unitypackage import, reload, sample and player checks. Exact archive identities are retained in Docs/Evidence/LoopShift-20261007-LTS6-URP.json and LoopShift-20261007-UnityPackage2022.json.
