Arvito Unity C# PC migration — 2026-11-10

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
