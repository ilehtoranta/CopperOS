"""Independently read the boot-proof ADF and check its staged mathtrans bytes."""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument('--image', type=Path, required=True)
parser.add_argument('--receipt', type=Path, required=True)
parser.add_argument('--staged-library', type=Path, required=True)
args = parser.parse_args()
image = args.image.read_bytes()
receipt = json.loads(args.receipt.read_text(encoding='utf-8-sig'))
staged = args.staged_library.read_bytes()
digest = lambda value: hashlib.sha256(value).hexdigest()
if receipt.get('kind') != 'original-system-boot-proof' or receipt.get('imageFormat') != 'adf' or receipt.get('shipping') is not False:
    raise ValueError('Expected a nonshipping ADF boot-proof receipt')
if receipt.get('sha256') != digest(image) or receipt.get('bytes') != len(image):
    raise ValueError('Boot-proof image differs from its receipt')
records = [f for f in receipt.get('files', []) if f.get('path') == 'Libs/mathtrans.library']
if len(records) != 1 or records[0]['sha256'] != digest(staged) or records[0]['bytes'] != len(staged):
    raise ValueError('Staged mathtrans library differs from the image receipt')
path = Path(__file__).resolve().parents[1] / 'Commands/Inventory/inventory.py'
spec = importlib.util.spec_from_file_location('proof_adf_reader', path)
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)
disk = module.Adf(image)
entry = disk.walk().get('libs/mathtrans.library')
if not entry or entry['secondary_type'] != -3 or disk.read_file(entry['block']) != staged:
    raise ValueError('Boot-proof ADF does not contain the staged mathtrans bytes')
print(json.dumps({'imageSha256': digest(image), 'librarySha256': digest(staged), 'volume': disk.volume}))
