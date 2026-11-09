"""Source/data smoke checks; this does NOT compile VB.NET or run the GUI."""
from pathlib import Path
import re
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parents[1]
project = ET.parse(root / 'src/NSRCGII.vbproj').getroot()
properties = {child.tag: child.text for prop in project.findall('PropertyGroup') for child in prop}
assert properties['TargetFramework'] == 'net10.0-windows'
assert properties['UseWindowsForms'] == 'true'
assert properties['OutputType'] == 'WinExe'

source = '\n'.join(p.read_text() for p in (root / 'src').glob('*.vb'))
assert 'ImportLegacy' in source
assert 'WriteHtml' in source and 'WritePdf' in source
assert 'HighDpiMode.PerMonitorV2' in source
assert 'Ally Spirit creator' not in source
assert 'Export Excel' not in source

banned = re.compile(r'(?i)(^|[^a-z0-9])(sr1|sr2|cb1|cb2|cb3|cb4|cp|pw|n/sl|nagee\d*|adh|bjf|ray|r3)(?=\.|\b)')
files = list((root / 'LegacyData').glob('*.DAT')) + list((root / 'LegacyData').glob('*.dat'))
assert len(files) >= 10
for file in files:
    for i, line in enumerate(file.read_text('latin1').splitlines(), 1):
        assert not banned.search(line), f'Excluded source in {file.name}:{i}'

legacy = (root / 'samples/archStreetSamurai.sr3').read_text('latin1')
for part in ('[Attributes]', '[Skills]', '[Cyberware]', '[Gear]', '[General]'):
    assert part in legacy
print(f'Smoke checks passed: {len(files)} catalog files, project and sample validated. VB.NET compile not run.')
