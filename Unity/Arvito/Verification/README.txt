Arvito Unity C# PC migration — 2026-11-10

Latest artwork update: event-illustrations.txt records 500 distinct event images,
their Unity UI/import integration, 9 pixel geometry checks and static C# compilation.
The older five-background count below describes the initial migration.

Passed:
- 24 C# rule checks in .NET 8 (core-checks.txt).
  Includes 30 seeded first-battle simulations, positional/target/resource gates,
  permanent companion death, aging across multi-year events, save RNG continuity,
  malformed combat snapshot rejection, and atomic-save backup recovery.
- Player and Editor source compiled against real Unity 6000.3.28f1 reference DLLs
  and bundled uGUI/TMP/Input System reference assemblies (unity-reference-compile.txt).
- Windows PowerShell build/install/uninstall scripts parsed without syntax errors
  using PowerShell 7.5.2 (installer-syntax.txt). Target usage is Windows PowerShell 5.1.
- Five illustration files; every event art reference resolves; source assets have
  stable GUID metadata; no JavaScript runtime files in Unity Assets.

Not completed:
- Unity Editor package import, generated scene/TMP assets, Play Mode rendering/input.
- Windows player build and playtest.
- IExpress installer execution and uninstall on Windows.

Actual Unity batch build attempt returned exit code 198 before project import:
  No valid Unity Editor license found. Please activate your license.
The installed Editor and Windows Mono build module are present. This is an account
license blocker, not a successful game build. No Arvito-Setup.exe is included.

The Universal 2D template (com.unity.template.universal-2d 6.1.6) was verified using
Unity CLI templates info, then its Assets/Packages/ProjectSettings were extracted
unchanged from the installed official template because CLI project creation failed
its temporary-log-directory check. ProjectVersion is pinned to 6000.3.28f1.
No package manifest dependency was hand-edited. ProjectSetup creates the real game
scene and font assets on first licensed import, and configures Windows settings.

Build on a Windows PC with activated Unity 6.3 LTS by running Build-Playtest.cmd.
The script produces the player first, then a ZIP and IExpress installer only after
successful Unity build. Any build errors remain in Builds/unity-build.log.

Launcher correction (2026-11-10):
- Removed the early abort based only on Temp/UnityLockfile existence. Unity now
  determines whether another Editor owns the project; no lock is removed.
- 3 dependency-free PowerShell regression checks passed with a mocked launcher:
  leftover lock permits launch, Unity's active-project error produces actionable
  guidance without packaging, and missing Editor reports a prelaunch error.
- PowerShell parsing passed. Actual Windows Unity locking remains untested.
- Run: pwsh -NoProfile -File Tests/BuildScripts/Build-Playtest.Tests.ps1

Project path correction (2026-11-10):
- User log showed a valid Unity Personal license and repository root game-main
  passed to -projectPath rather than the nested Unity/Arvito project.
- Launcher now validates project markers, resolves the project or repository-root
  layout, uses an absolute forward-slash -projectPath and the project as its
  WorkingDirectory, and invokes packaging from the resolved project's Tools.
- Added a repository-root Build-Playtest.cmd forwarding to Unity/Arvito.
- 5 mocked launcher checks passed, including nested project discovery with spaces
  and non-ASCII names, output/log/package placement, and invalid-project rejection.
- PowerShell syntax checks passed. Actual Unity on Windows remains untested here.

Live build progress (2026-11-10):
- Unity starts without a blocking -Wait; logs stream concurrently and elapsed time,
  latest observed stage and quiet time are printed every 5 seconds.
- Log reads share access with the writer, keep UTF-8 decoder/partial-line state,
  skip unchanged previous-run output and restart after truncation.
- The process is waited after exit, final output is drained, and the real exit
  code still prevents packaging on failure. The monitor never kills Unity.
- Watch-Build.cmd can watch an existing build log in a separate window.
- 6 mocked launcher checks and 4 progress checks passed. The latter include a real
  non-Unity child process, verifying output before exit and the final error/code.
- A null process exit code is treated as an error even if an older player exists.
- PowerShell parsing passed; actual Unity/Windows rendering/build remains untested.

Unity 6.3 dependency repair (2026-11-10):
- User's real Windows log identified old Input System1.12.0 referencing removed
  BuildTarget.ReservedCFE, and collab-proxy2.6.0 ObjectInfo ambiguity.
- Verified official Input System1.20.1 metadata (unity6000.0 minimum) and actual
  plugin-control source replacing that enum with (BuildTarget)49. Installed Unity
  6000.3 bundles URP17.3.0. collab-proxy is not needed by the game.
- Before compilation, the launcher now resolves those changes through Unity's
  asynchronous Client API in a disposable empty project, preserving other root
  requests, then validates/applies Client-generated manifest+lock with backups.
- The original source manifest remains the template's manifest until this repair
  runs on the user's licensed Editor. No manifest was hand-edited in this workspace.
- Bootstrap C# compiled against real Unity6.3 reference DLLs. PowerShell parsed.
- 5 mocked repair checks cover targeted changes, backup/apply, idempotence and
  preserving original files on failure/wrong resolution. 6 launcher checks passed.
- Native -createProject, UPM resolution and Windows game build require validation
  on the user's licensed Editor; those operations were not executed here.
