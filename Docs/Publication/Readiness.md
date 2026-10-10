# LoopShift publication readiness

This is a release acceptance record, not publication approval. The endless-room runtime, visual authoring tools and separate distributions are implemented. Automated qualification is complete for the implementation described below. Native editor usability review, store media and publisher decisions remain open.

## Publisher details

- Author: Estrix Studio.
- Support: contact@estrixstudio.com and https://estrixstudio.com, supplied by the user and included in the package metadata/README. HTTPS reachability was verified with status 200 and an Estrix Studio page title on 2026-10-10; LoopShift-SupportWebsite.json records the scoped check.
- Price, final product name and marketing approval: pending.
- Proposed category: Tools/Level Design, used for local validation; publisher selection pending.
- No public submission or upload has been performed. Listing: [Listing-Draft.md](Listing-Draft.md).

## Final distribution identities

| Format | SHA-256 |
| --- | --- |
| UPM | `74D47E6B8624C4F36B4C44906044954FEB1DC9A034D90D54721246DB046ADDB4` |
| Native Unity package | `2256C34715D1E48213060C80575CA9282AE1F6E4D2AEA816200649AC5A5BCDD0` |

The final rebuild changed only README.md, Authoring.md and UPM publisher metadata. A complete file comparison in LoopShift-PublisherMetadata-ArchiveAudit.json proves that code, tests, sample assets and Unity .meta files are identical to the qualified base archives. No serialized sample reference points to a missing package asset. Runtime test reports retain their original archive hashes; the comparison establishes the final archives' unchanged implementation rather than pretending those tests ran again.

## Qualification evidence

| Scope | Result and evidence |
| --- | --- |
| Unity 2022.3.62f3 UPM | 50 editor, 28 runtime (27 package plus consumer sample), two reload-mode tests, Windows build and player smoke passed; LoopShift-PreloadFix-Unity2022.json |
| Unity 2022.3.62f3 native package | Clean import, consumer sample traversal, two reload-mode tests, Windows build and player smoke passed; LoopShift-PreloadFix-NativeUnity2022.json. This format intentionally excludes package tests |
| Unity 6000.3.25f1 / URP 17.3.0 UPM | 50 editor, 28 runtime, two reload-mode tests, Windows build and player smoke passed; LoopShift-PreloadFix-Unity6URP.json |
| Built-in and URP rendering | Current implementation rendered in both pipelines with zero magenta error pixels; Rendering-PreloadFix-Unity6 contains images/metrics. The URP image was inspected. This is camera-render evidence, not native editor inspection |
| Asset Store Tools 12.0.5 | Final native archive reimported and passed all 36 checks without warnings; LoopShift-AssetStore-PublisherMetadata-Identity.json records its exact hash |
| Host project | 100 project checks, 50 editor tests and 27 runtime tests passed; the preload regression has before/after evidence |
| Stress | 768 square-route crossings across preload radii 0–2 passed, alongside lifecycle, ownership and allocation checks. This is bounded coverage, not a universal performance guarantee |
| Game integration | 198 assertions and 17 local physical doorway crossings passed; LoopShift-PhysicalProgression.json records probe/graph identities and scope |
| Distribution isolation | No game artwork or scripts shipped; stage inventory has no executables, DLLs or cache directories. Export preserves saved sample content |

The Unity 6 consumer initially failed during package resolution, then crashed during import with an out-of-memory exception. Resuming after other editor jobs had stopped succeeded. A URP film-grain texture retained a failed import; explicitly reimporting that texture succeeded, and a subsequent build and player run completed without that error. The recovery and recovered build hash are retained in the Unity 6 evidence. No package code workaround was introduced.

Base archives are preserved locally in Build/LoopShift/qualified-preload-20261009. Their SHA-256 values are recorded by the archive audit. Historical results for older candidates remain evidence only for their recorded content.

## Remaining release gates

- Native visual/usability review of the graph canvas, inspectors, scene handles and sample controls. The computer-use runtime still fails during sandbox initialization; the 2026-10-10 retry also failed before any screenshots or interactions.
- Full player-input game navigation. The automated game tour positions the player near doorways and uses authored interaction callbacks. Walls remain enabled; enemy collisions are ignored only for the route tour and are tested separately for loss. It does not prove room-interior routes or keyboard/controller usability.
- Original editor/sample screenshots and demonstration media reviewed for accuracy and readability.
- Publisher approval of listing, compatibility fields, AI-assistance disclosure, category and price; confirm support website suitability.
- Final visual/documentation consistency pass alongside the actual editor workflow.

Windows standalone is qualified; other player platforms and HDRP sample materials are not. Unity 6000.7.0b1 is the host development editor and is not the sole basis for compatibility claims.

The [Unity submission guidelines](https://assetstore.unity.com/publishing/submission-guidelines) were rechecked on 2026-10-09 (page last updated 2026-05-20). Local validator success is not Unity approval. Consumer probes live under Tools/LoopShiftValidation and are not shipped as sample gameplay. Evidence names above refer to Docs/Evidence.
## Original cover concept and remaining review

[The native review/capture checklist](Native-Review-and-Media.md) defines the concrete remaining scenarios and product captures. The original generated cover is saved under Media with its prompt and identity. The publisher requested that the concept remain unchanged; it has not been resized or cropped and is not yet a size-qualified store image. No generated image is used as a product screenshot.