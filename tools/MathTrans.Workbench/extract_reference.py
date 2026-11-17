"""Extract a locally owned mathtrans.library from an Amiga Workbench ADF/ZIP."""
import argparse
import hashlib
import importlib.util
from pathlib import Path
import zipfile

parser = argparse.ArgumentParser()
parser.add_argument('media', type=Path)
parser.add_argument('output', type=Path)
args = parser.parse_args()
if args.output.exists():
    raise SystemExit(f'refusing to overwrite {args.output}')
source = Path(__file__).resolve().parents[1] / 'Commands' / 'Inventory' / 'inventory.py'
spec = importlib.util.spec_from_file_location('inventory', source)
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)
if zipfile.is_zipfile(args.media):
    with zipfile.ZipFile(args.media) as archive:
        adfs = [n for n in archive.namelist() if n.lower().endswith('.adf')]
        if len(adfs) != 1:
            raise SystemExit('expected exactly one ADF in the ZIP')
        data = archive.read(adfs[0])
else:
    data = args.media.read_bytes()
disk = module.Adf(data)
entry = disk.walk().get('libs/mathtrans.library')
if not entry or entry['secondary_type'] != -3:
    raise SystemExit('ordinary Libs/mathtrans.library not found')
library = disk.read_file(entry['block'])
args.output.parent.mkdir(parents=True, exist_ok=True)
with args.output.open('xb') as output:
    output.write(library)
print(f'{disk.volume}: {entry["path"]}, {len(library)} bytes, SHA-256 {hashlib.sha256(library).hexdigest()}')
