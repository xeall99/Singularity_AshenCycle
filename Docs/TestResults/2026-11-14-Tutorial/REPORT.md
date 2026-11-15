# Tutorial / Field Guide — 14 November 2026

## Design

The project had no implemented player tutorial. This milestone adds an optional, always-available six-page Field Guide from Main Menu. It explains only current Standard Run behavior and does not interrupt gameplay or mutate run data. Full design and acceptance criteria are in `Docs/Tutorial_DesignPlan.md`.

Research informed the contextual, concise chapter structure: in-game tutorial research reports value in timely/contextual access, practice and feedback, while cautioning against repetitive or unnecessary instructions ([review and pilot study](https://pmc.ncbi.nlm.nih.gov/articles/PMC9676530/), [tutorial assessment approach](https://www.mdpi.com/2227-9709/10/1/6)). For this first implementation, practice/learning effectiveness is not claimed. Existing uGUI/Canvas patterns were retained; Unity documentation notes UI Toolkit is recommended for new UI development while uGUI remains appropriate for existing projects ([Unity UI systems](https://docs.unity3d.com/2023.2/Documentation/Manual/UIElements.html)).

## Implementation

- `Assets/Project/Scripts/UI/TutorialUI.cs` and its `.meta`: runtime Field Guide modal, six authored chapters, chapter counter, Previous/Next boundaries, Escape and Close, keyboard focus restoration, responsive anchored card.
- `Assets/Project/Scripts/UI/MainMenuUI.cs`: adds **FIELD GUIDE** in both generated and existing-scene Main Menu paths.
- `Assets/Tests/PlayMode/BattleSnapshotTests.cs`: Main Menu presence and navigation/focus/no-TimeScale-change regression (`TC_MENU_006`), and existing menu identity/layout test now includes the guide entry.
- `Docs/Tutorial_DesignPlan.md`: feature research, chapter/content plan, interaction/data boundaries, acceptance criteria and verification plan.
- `Docs/SYNGRAVA_Codex_Context_Pack/AGENTS.md` and repository `CLAUDE.md`: owner-requested per-feature design/research, baseline, implementation, and three-consecutive-Play-Mode workflow.
- `Docs/SYNGRAVA_Codex_Context_Pack/Docs/PROJECT_CONTEXT.md`: durable implementation and verification status.

No scene, Inspector, package, SQLite schema, save format, or runtime data migration was needed.

## Verification

Unity 6000.6.0f1 full `BattleSnapshotTests` Play Mode suite, on one isolated test project/profile, three consecutive runs:

| Pass | Result | Passed | Failed | Skipped | Duration |
|---|---|---:|---:|---:|---:|
| 1 | Passed | 152 | 0 | 0 | 134.8828407 s |
| 2 | Passed | 152 | 0 | 0 | 122.4540518 s |
| 3 | Passed | 152 | 0 | 0 | 121.4792892 s |

Each run produced its own XML and log:

- `D:\Akmal\SyngravaTaskTemp\syngrava-battle-tests-b8edb7640456402b91570461ce9f6c33\playmode-results.xml`, `unity-playmode.log`
- `D:\Akmal\SyngravaTaskTemp\syngrava-battle-tests-b8edb7640456402b91570461ce9f6c33\playmode-results-2.xml`, `unity-playmode-2.log`
- `D:\Akmal\SyngravaTaskTemp\syngrava-battle-tests-b8edb7640456402b91570461ce9f6c33\playmode-results-3.xml`, `unity-playmode-3.log`

`TC_MENU_006_FieldGuideNavigatesAndRestoresMenuFocus` passed all three times. The test exercises open, page count, next/previous, last-page boundary, Escape handling, Close button, menu focus restoration, body overflow, and unchanged Time Scale.

The pre-implementation baseline was attempted but did not yield a usable report: the sandboxed Temp copy failed, and the isolated retry did not complete due to runner/licensing startup. An initial post-implementation attempt found a deprecated TextMeshPro wrapping property and a test assembly visibility issue; both were corrected before the three consecutive passing runs. `git diff --check` reported no whitespace errors (Git emitted existing LF-to-CRLF notices).

## Limitations

- No manual playtest or visual capture was requested or performed. Tests do not evaluate the readability of the final art treatment or actual player learning.
- Current content is an optional field guide rather than a playable practice encounter. Future work could add contextual in-battle hints/practice after separate design approval.
- No standalone Windows build was produced for this milestone.
