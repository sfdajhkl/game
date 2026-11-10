#!/usr/bin/env python3
"""Run the dependency-free C# rule checks with .NET 8; avoids NuGet/network access."""
import argparse
import json
from pathlib import Path
import shutil
import subprocess

p = argparse.ArgumentParser()
p.add_argument('--dotnet', default=shutil.which('dotnet'))
a = p.parse_args()
if not a.dotnet: raise SystemExit('Install .NET 8 SDK or pass --dotnet /path/to/dotnet')
dotnet = Path(a.dotnet).resolve()
root = Path(__file__).resolve().parents[1]
csc = sorted((dotnet.parent / 'sdk').glob('8.*/Roslyn/bincore/csc.dll'))
refs = list((dotnet.parent / 'packs/Microsoft.NETCore.App.Ref').glob('8.*/ref/net8.0'))
if not csc or not refs: raise SystemExit('.NET 8 SDK reference assemblies are required.')
out = root / 'Tests/Arvito.CoreChecks/bin/standalone'
out.mkdir(parents=True, exist_ok=True)
sources = list((root / 'Unity/Arvito/Assets/Arvito/Core').glob('*.cs')) + [root / 'Tests/Arvito.CoreChecks/Program.cs']
subprocess.run([str(dotnet), str(csc[-1]), '-nologo', '-target:exe', '-out:' + str(out / 'CoreChecks.dll')]
    + ['-r:' + str(f) for f in sorted(refs)[-1].glob('*.dll')] + list(map(str, sources)), check=True)
(out / 'CoreChecks.runtimeconfig.json').write_text(json.dumps({'runtimeOptions': {'tfm': 'net8.0', 'framework': {'name': 'Microsoft.NETCore.App', 'version': '8.0.0'}}}))
subprocess.run([str(dotnet), str(out / 'CoreChecks.dll'), str(root / 'Unity/Arvito/Assets/Resources/Arvito/catalog.json')], check=True)
