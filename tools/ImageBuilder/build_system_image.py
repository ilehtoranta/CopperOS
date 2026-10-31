"""Compose an original CopperOS ADF or FFS HDF development boot proof (never shipping).

Only hash-bound native artifacts from build_system_proof.ps1 are
admitted. No loose commands, reference OS files, ROMs or command-release records
are imported. This does not bypass the separate command distribution preflight.
"""
import argparse
import hashlib
import json
import struct
import importlib.util
from pathlib import Path


def digest(data):
    return hashlib.sha256(data).hexdigest()


def pair_id(manifest_bytes):
    return 'a500-copperos-cli-boot-proof-dev.1.' + digest(manifest_bytes)[:12]


class Disk:
    def __init__(self, blocks=1760):
        # This image producer uses the root's 25 direct bitmap pointers.
        if blocks < 1760 or blocks > 25 * 4064 + 2 or blocks % 2:
            raise ValueError('Image requires an even block count from 1760 to 101602')
        self.blocks = blocks
        self.root = blocks // 2
        self.bitmaps = list(range(self.root + 1, self.root + 1 + (blocks - 2 + 4063) // 4064))
        self.data = bytearray(blocks * 512)
        self.used = {0, 1, self.root, *self.bitmaps}
        self.next = 2
        self.files = []
        self.put(0, 0, 0x444F5301)
        self.put(0, 8, self.root)
        # Original DOS resident boot block: FindResident; return rt_Init.
        words = [0x43FA, 24, 0x4EAE, 0xFFA0, 0x4A80, 0x670A,
                 0x2040, 0x2068, 0x0016, 0x7000, 0x4E75, 0x70FF, 0x4E75]
        code = struct.pack('>' + 'H' * len(words), *words) + b'dos.library\0'
        self.data[12:12 + len(code)] = code
        total = 0
        for at in range(0, 1024, 4):
            value = total + struct.unpack_from('>I', self.data, at)[0]
            total = (value & 0xFFFFFFFF) + (value >> 32)
        self.put(0, 4, ~total & 0xFFFFFFFF)
        self.header(self.root, 0, 'CopperOS', 1)
        self.put(self.root, 12, 72)
        self.put(self.root, 312, 0xFFFFFFFF)
        for index, bitmap in enumerate(self.bitmaps):
            self.put(self.root, 316 + index * 4, bitmap)
        self.checksum(self.root)

    def put(self, block, offset, value):
        struct.pack_into('>I', self.data, block * 512 + offset, value)

    def get(self, block, offset):
        return struct.unpack_from('>I', self.data, block * 512 + offset)[0]

    def allocate(self):
        while self.next in self.used:
            self.next += 1
        if self.next >= self.blocks:
            raise ValueError('Original system payload does not fit the image')
        result = self.next
        self.used.add(result)
        self.next += 1
        return result

    def checksum(self, block, offset=20):
        self.put(block, offset, 0)
        total = sum(struct.unpack_from('>128I', self.data, block * 512))
        self.put(block, offset, -total & 0xFFFFFFFF)

    def header(self, block, parent, name, kind):
        encoded = name.encode('ascii')
        if not encoded or len(encoded) > 30 or '/' in name or ':' in name:
            raise ValueError('Invalid original image name')
        self.put(block, 0, 2)
        self.put(block, 4, 0 if kind == 1 else block)
        self.put(block, 500, parent)
        self.put(block, 508, kind & 0xFFFFFFFF)
        self.data[block * 512 + 432] = len(encoded)
        self.data[block * 512 + 433:block * 512 + 433 + len(encoded)] = encoded

    def link(self, parent, block, name):
        value = len(name)
        for c in name.upper().encode('ascii'):
            value = (value * 13 + c) & 0x7FF
        offset = 24 + value % 72 * 4
        self.put(block, 496, self.get(parent, offset))
        self.put(parent, offset, block)
        self.checksum(block)
        self.checksum(parent)

    def directory(self, parent, name):
        block = self.allocate()
        self.header(block, parent, name, 2)
        self.link(parent, block, name)
        return block

    def file(self, parent, path, payload):
        name = path.rsplit('/', 1)[-1]
        header = self.allocate()
        self.header(header, parent, name, -3)
        self.put(header, 324, len(payload))
        table = header
        for index, at in enumerate(range(0, len(payload), 512)):
            if index and index % 72 == 0:
                extension = self.allocate()
                self.put(table, 504, extension)
                self.checksum(table)
                table = extension
                self.put(table, 0, 16)
                self.put(table, 4, table)
                self.put(table, 500, header)
                self.put(table, 508, 0xFFFFFFFD)
            block = self.allocate()
            self.put(table, 8, index % 72 + 1)
            self.put(table, 308 - index % 72 * 4, block)
            if not index:
                self.put(header, 16, block)
            data = payload[at:at + 512]
            self.data[block * 512:block * 512 + len(data)] = data
        self.checksum(table)
        self.link(parent, header, name)
        self.files.append(dict(path=path, bytes=len(payload), sha256=digest(payload), protection=0))

    def finish(self):
        for block in range(2, self.blocks):
            if block not in self.used:
                bitmap = self.bitmaps[(block - 2) // 4064]
                bit = (block - 2) % 4064
                offset = 4 + bit // 32 * 4
                self.put(bitmap, offset, self.get(bitmap, offset) | (1 << (bit % 32)))
        for bitmap in self.bitmaps:
            self.checksum(bitmap, 0)
        return bytes(self.data)


def build(manifest_path, blocks=1760):
    manifest_bytes = manifest_path.read_bytes()
    manifest = json.loads(manifest_bytes.decode('utf-8-sig'))
    if manifest['kind'] != 'original-system-boot-proof' or manifest['shipping'] is not False:
        raise ValueError('Only explicit nonshipping proof inputs are accepted')
    artifacts = manifest['artifacts']
    if len({f['name'] for f in artifacts}) != len(artifacts) or {f['name'] for f in artifacts} not in ({'Shell-Seg', 'BootShell', 'Demo', 'mathtrans.library'}, {'Shell-Seg', 'BootShell', 'Demo', 'mathtrans.library', 'Storage'}):
        raise ValueError('Original Shell/Boot/Demo, staged mathtrans.library and optional native Storage proof artifacts are required')
    binaries = {}
    for item in artifacts:
        path = Path(item['path'])
        if path.is_absolute() or len(path.parts) != 1 or '..' in path.parts:
            raise ValueError('Proof input must be a sibling filename')
        payload = (manifest_path.parent / path).read_bytes()
        if len(payload) != item['bytes'] or digest(payload) != item['sha256'] or payload[:4] != b'\0\0\x03\xf3':
            raise ValueError('Missing, altered or non-HUNK proof input: ' + item['name'])
        binaries[item['name']] = payload
    disk = Disk(blocks)
    dirs = {name: disk.directory(disk.root, name) for name in ['C', 'L', 'Libs', 'S']}
    disk.file(dirs['C'], 'C/BootShell', binaries['BootShell'])
    disk.file(dirs['C'], 'C/Demo', binaries['Demo'])
    if 'Storage' in binaries:
        disk.file(dirs['C'], 'C/Storage', binaries['Storage'])
    disk.file(dirs['L'], 'L/Shell-Seg', binaries['Shell-Seg'])
    disk.file(dirs['Libs'], 'Libs/mathtrans.library', binaries['mathtrans.library'])
    disk.file(dirs['S'], 'S/Startup-Sequence', b'; Original CopperOS development boot proof\nC:BootShell\n')
    identity = dict(schemaVersion=1, version=manifest['version'], pairId=pair_id(manifest_bytes),
                    shipping=False, producerManifestSha256=digest(manifest_bytes),
                    nativeArtifacts={f['name']: f['sha256'] for f in artifacts})
    disk.file(dirs['S'], 'S/CopperOS-Release.json', (json.dumps(identity, sort_keys=True, indent=2) + '\n').encode('ascii'))
    return disk.finish(), disk.files


def verify(image, files):
    # Preserve the distribution verifier's nonbootable admission boundary.
    # Only a private copy's reserved boot code/checksum is zeroed for its
    # independent filesystem check; the original boot checksum is checked here.
    total = 0
    for value in struct.unpack_from('>256I', image):
        total += value
        total = (total & 0xFFFFFFFF) + (total >> 32)
    if total != 0xFFFFFFFF:
        raise ValueError('Invalid original boot-block checksum')
    path = Path(__file__).resolve().parents[1] / 'DiskBuilder/verify_image.py'
    spec = importlib.util.spec_from_file_location('independent_system_filesystem_reader', path)
    reader = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(reader)
    normalized = bytearray(image)
    normalized[4:8] = bytes(4)
    normalized[12:1024] = bytes(1012)
    records = [dict(installed_path=f['path'], bytes=f['bytes'], sha256=f['sha256'], amiga_protection=f['protection']) for f in files]
    result = reader._verify(bytes(normalized), records, 'CopperOS')
    return dict(originalBootChecksum='passed', originalImageSha256=digest(image),
                filesystemCheckOnPrivateBootNormalizedCopy=result,
                normalization='Only reserved boot checksum [4:8] and boot code [12:1024]; filesystem bytes unmodified',
                runtimeQualified=False)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--manifest', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--receipt', type=Path, required=True)
    parser.add_argument('--format', choices=['adf', 'hdf'], default='adf',
                        help='DD ADF (default), or 8 MiB explicit FFS partition HDF')
    args = parser.parse_args()
    if args.output.exists() or args.receipt.exists():
        raise ValueError('Preserve existing image/receipt; choose new paths')
    manifest_bytes = args.manifest.read_bytes()
    manifest = json.loads(manifest_bytes.decode('utf-8-sig'))
    image, files = build(args.manifest.resolve(strict=True), 16384 if args.format == 'hdf' else 1760)
    if args.manifest.read_bytes() != manifest_bytes:
        raise ValueError('Proof manifest changed during image composition')
    verification = verify(image, files)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.receipt.parent.mkdir(parents=True, exist_ok=True)
    with args.output.open('xb') as f:
        f.write(image)
    receipt = dict(schemaVersion=1, kind='original-system-boot-proof', version=manifest['version'], pairId=pair_id(manifest_bytes),
                   shipping=False, runtimeQualified=False, bytes=len(image), sha256=digest(image),
                   imageFormat=args.format, filesystem='DOS1', volume='CopperOS', dates='Amiga epoch', files=files,
                   inputManifestSha256=digest(manifest_bytes), verification=verification)
    if args.format == 'hdf':
        receipt['mount'] = dict(mode='partition', geometry='DH0:2:32:0:255', blockBytes=512,
                               reservedBlocks=2, rom='CopperStart standalone A500')
    with args.receipt.open('x', encoding='utf-8') as f:
        json.dump(receipt, f, indent=2)
        f.write('\n')
    print(f'Created original nonshipping system proof: {args.output}, SHA-256 {digest(image)}')
