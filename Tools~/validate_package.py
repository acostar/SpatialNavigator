"""Structural validation only; run Unity Test Runner separately."""
from pathlib import Path
import json, re
root = Path(__file__).resolve().parents[1]
manifest = json.loads((root / 'package.json').read_text())
assert manifest['name'] == 'com.acostar.spatialNavigator'
assert re.fullmatch(r'\d+\.\d+\.\d+', manifest['version'])
for sample in manifest['samples']:
    assert (root / sample['path']).is_dir()
assemblies = {json.loads(p.read_text())['name']: json.loads(p.read_text()) for p in root.rglob('*.asmdef')}
for assembly in assemblies.values():
    for reference in assembly.get('references', []):
        assert reference in assemblies, reference
assert assemblies['Acostar.SpatialNavigator.Tests']['includePlatforms'] == ['Editor']
guids = {}
for p in root.rglob('*.meta'):
    match = re.search(r'^guid: ([a-f0-9]{32})$', p.read_text(), re.M)
    assert match, p
    assert match[1] not in guids, p
    guids[match[1]] = p
    assert p.with_suffix('').exists(), p
for p in root.rglob('*'):
    rel = p.relative_to(root)
    if any(part.startswith('.') for part in rel.parts): continue
    if p.suffix == '.meta': continue
    if rel.parts[0] in ('Documentation~', 'Tools~'): continue
    if rel.as_posix() == 'Samples~': continue
    assert Path(str(p) + '.meta').exists(), p
for scene in root.rglob('*.unity'):
    for value in re.findall(r'guid: ([a-f0-9]{32})', scene.read_text()):
        assert value in guids, (scene, value)
for p in (root / 'Runtime').rglob('*.cs'):
    source = p.read_text()
    for forbidden in ('Octant_v2', 'GameManager.', 'PriorityQueue<', 'UnityEditor', 'NUnit', 'FindWithTag'):
        assert forbidden not in source, (p, forbidden)
print('PASS: manifest, samples, assembly references, GUIDs, metadata and runtime dependency checks.')
print('Unity compilation, Play Mode, Test Runner and player builds are separate required checks.')
