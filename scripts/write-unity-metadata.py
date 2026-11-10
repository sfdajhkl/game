#!/usr/bin/env python3
"""Give new source assets stable GUIDs before the first licensed Editor import. Never replaces metadata."""
from pathlib import Path
import uuid

root = Path(__file__).resolve().parents[1] / 'Unity/Arvito/Assets'
count = 0
for asset in sorted(root.rglob('*')):
    if asset.suffix == '.meta': continue
    meta = Path(str(asset) + '.meta')
    if meta.exists(): continue
    guid = uuid.uuid5(uuid.NAMESPACE_URL, 'arvito/' + asset.relative_to(root).as_posix()).hex
    text = 'fileFormatVersion: 2\nguid: ' + guid + '\n'
    if asset.is_dir():
        text += 'folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
    elif asset.suffix == '.cs':
        text += 'MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
    elif asset.suffix == '.asmdef':
        text += 'AssemblyDefinitionImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
    elif asset.suffix in ('.txt', '.json'):
        text += 'TextScriptImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
    # Texture/TTF importers are populated by Unity; ProjectSetup applies their import settings.
    meta.write_text(text)
    count += 1
print('Created', count, 'stable metadata files.')
