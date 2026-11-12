#!/usr/bin/env python3
"""Static C# API check against an installed Unity Editor. This is NOT an Editor/Play Mode test."""
import argparse
from pathlib import Path
import subprocess

p = argparse.ArgumentParser()
p.add_argument('--editor-data', type=Path, required=True)
p.add_argument('--dotnet', type=Path, required=True)
p.add_argument('--output', type=Path, default=Path('/tmp/arvito-api-check'))
a = p.parse_args()
root = Path(__file__).resolve().parents[1]
a.output.mkdir(parents=True, exist_ok=True)
csc = sorted((a.dotnet.parent / 'sdk').glob('*/Roslyn/bincore/csc.dll'))[-1]
refs = list((a.editor_data / 'Managed/UnityEngine').glob('*.dll')) + list((a.editor_data / 'Managed/UnityEditor').glob('*.dll'))
mono = a.editor_data / 'MonoBleedingEdge/lib/mono/4.8-api'
refs += list(mono.glob('*.dll')) + list((mono / 'Facades').glob('*.dll'))
libcache = a.editor_data / 'Resources/PackageManager/ProjectTemplates/libcache'
for name in ['UnityEngine.UI.dll', 'Unity.TextMeshPro.dll', 'Unity.InputSystem.dll']:
    matches = list(libcache.glob('*/ScriptAssemblies/' + name))
    if not matches: raise SystemExit('Missing real Unity reference: ' + name)
    refs.append(matches[0])
sources = list((root / 'Unity/Arvito/Assets/Arvito').rglob('*.cs'))
sources += list((root / 'Unity/Arvito/Tools').glob('PackageRepair.cs'))
for mode in ['player', 'web', 'editor']:
    defines = ('UNITY_WEBGL' if mode == 'web' else 'UNITY_STANDALONE_WIN') + ',ENABLE_INPUT_SYSTEM,UNITY_6000_0_OR_NEWER'
    if mode == 'editor': defines += ',UNITY_EDITOR'
    result = subprocess.run([str(a.dotnet), str(csc), '-nologo', '-nostdlib', '-langversion:9', '-target:library',
        '-define:' + defines, '-out:' + str(a.output / ('Arvito-' + mode + '.dll'))] + ['-r:' + str(f) for f in refs] + list(map(str, sources)), capture_output=True, text=True)
    (a.output / (mode + '.log')).write_text(result.stdout + result.stderr)
    print(result.stdout + result.stderr, end='')
    if result.returncode: raise SystemExit(result.returncode)
    print('PASS: ' + mode + ' C# source compiled against Unity reference assemblies.')
print('Editor asset import, rendering and player build still require a licensed Unity Editor.')
