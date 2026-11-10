#!/usr/bin/env python3
"""Check physical-pixel layout math using .NET 8, without Unity or NuGet."""
import argparse
import json
from pathlib import Path
import shutil
import subprocess

p=argparse.ArgumentParser()
p.add_argument('--dotnet',default=shutil.which('dotnet'))
a=p.parse_args()
if not a.dotnet: raise SystemExit('Install .NET 8 SDK or pass --dotnet /path/to/dotnet')
dotnet=Path(a.dotnet).resolve()
root=Path(__file__).resolve().parents[1]
csc=sorted((dotnet.parent/'sdk').glob('8.*/Roslyn/bincore/csc.dll'))[-1]
refs=sorted((dotnet.parent/'packs/Microsoft.NETCore.App.Ref').glob('8.*/ref/net8.0'))[-1]
out=root/'Tests/Arvito.PixelChecks/bin/standalone'
out.mkdir(parents=True,exist_ok=True)
sources=[root/'Tests/Arvito.PixelChecks/Program.cs',root/'Unity/Arvito/Assets/Arvito/Runtime/PixelArtGeometry.cs']
subprocess.run([str(dotnet),str(csc),'-nologo','-target:exe','-out:'+str(out/'PixelChecks.dll')]
    +['-r:'+str(f) for f in refs.glob('*.dll')]+list(map(str,sources)),check=True)
(out/'PixelChecks.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':'net8.0','framework':{'name':'Microsoft.NETCore.App','version':'8.0.0'}}}))
subprocess.run([str(dotnet),str(out/'PixelChecks.dll')],check=True)
