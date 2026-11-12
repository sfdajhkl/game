# Web readiness checks

Run `node Tests/Web/check-web.cjs` with Playwright available. Set `ARVITO_PLAYWRIGHT` to its module path and `ARVITO_CHROMIUM` to the browser executable if needed.

This checks the real HTML template and `.jslib` against Chromium localStorage, with a test Unity loader and emulated UTF-8 memory helpers. It checks reload persistence, unavailable/quota-limited storage, loader errors, fullscreen dispatch, and two viewport widths. It does not run IL2CPP, WebAssembly, Unity rendering, or gameplay.

`python3 scripts/check-core.py --dotnet /path/to/dotnet` checks game rules and the browser save adapter, including validated backup recovery and failure atomicity. `scripts/check-unity-source.py` compiles desktop, WebGL-defined, and Editor C# paths against installed Unity references.

The required real-player acceptance steps are in `Unity/Arvito/Tools/WEB-PLAY.txt`.
