"""Independently check the same staged mathtrans library on two booted ROMs."""
import argparse
import hashlib
import importlib.util
import json
import struct
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'DiskBuilder'))
from verify_image import Adf


def sha(data):
    return hashlib.sha256(data).hexdigest()


def check(image, directory):
    receipt = json.loads((directory / 'qualification.json').read_text())
    assert receipt['inputImageSha256'] == sha(image)
    result = (directory / 'result.adf').read_bytes()
    helper = (directory / 'helper.adf').read_bytes()
    assert receipt['resultImageSha256'] == sha(result)
    assert receipt['helperImageSha256'] == sha(helper)
    helper_disk = Adf(helper)
    assert set(helper_disk.walk()) == {'p'}
    source = Path(__file__).resolve().parents[1] / 'MathTrans.Workbench' / 'make_edge_probe.py'
    spec = importlib.util.spec_from_file_location('expected_math_probe', source)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    expected = module.build(40, 'SYS:Libs/mathtrans.library', 'SYS:T/mathresult', full=True)
    assert helper_disk.read_file(helper_disk.walk()['p']['block']) == expected
    disk = Adf(result)
    entries = disk.walk()
    assert disk.read_file(entries['t/exact']['block']) == b'ok\n'
    data = disk.read_file(entries['t/mathresult']['block'])
    assert len(data) == 124 and data[:4] == bytes(4)
    assert data[4:].hex() == receipt['mathtransSamplesHex']
    original = Adf(image)
    library = original.read_file(original.walk()['libs/mathtrans.library']['block'])
    assert disk.read_file(entries['libs/mathtrans.library']['block']) == library
    assert receipt['librarySha256'] == sha(library)
    return receipt, data, helper


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('image', 'kickstart-result', 'copperstart-result', 'kickstart-rom',
                 'copperstart-rom', 'extension-rom', 'copperstart-ram', 'output'):
        parser.add_argument('--' + name, type=Path, required=True)
    args = parser.parse_args()
    image = args.image.read_bytes()
    ks, ks_data, ks_helper = check(image, args.kickstart_result)
    cu, cu_data, cu_helper = check(image, args.copperstart_result)
    assert ks_data == cu_data, 'Numerical results or defined condition codes differ'
    assert ks_helper == cu_helper, 'The guest probes differ'
    assert ks['romSha256'] == sha(args.kickstart_rom.read_bytes())
    rom = args.copperstart_rom.read_bytes()
    extension = args.extension_rom.read_bytes()
    assert cu['romSha256'] == sha(rom)
    assert cu['extensionRomSha256'] == sha(extension)
    assert ks['emulatorSha256'] == cu['emulatorSha256']
    assert ks['probeSha256'] == cu['probeSha256']
    ram = args.copperstart_ram.read_bytes()

    def read(address, count):
        for base, data in ((0, ram), (0xE00000, extension), (0xF80000, rom)):
            if base <= address and address + count <= base + len(data):
                return data[address-base:address-base+count]
        raise ValueError(f'Unmapped observation {address:08x}')

    def u32(address):
        return struct.unpack('>I', read(address, 4))[0]

    def u16(address):
        return struct.unpack('>H', read(address, 2))[0]

    def in_rom_address(address):
        return 0xE00000 <= address < 0xE80000 or 0xF80000 <= address < 0x1000000

    exec_base = u32(4)
    tail = exec_base + 378 + 4
    node = u32(exec_base + 378)
    seen = set()
    library = 0
    while node != tail:
        assert node and node not in seen
        seen.add(node)
        name = u32(node + 10)
        if read(name, 16) == b'mathffp.library\0':
            library = node
        node = u32(node)
    assert library, 'No boot-installed mathffp.library'
    assert in_rom_address(u32(20)), 'No ROM divide exception entry'
    assert u16(library + 16) == 96 and u16(library + 18) == 84
    assert u16(library + 20) == 40

    def in_rom(address):
        return (0xE00000 <= address < 0xE80000 or
                0xF80000 <= address < 0x1000000)

    vectors = []
    for index in range(1, 17):
        slot = library - index * 6
        assert u16(slot) == 0x4EF9
        target = u32(slot + 2)
        assert in_rom(target), 'FFP executable adapter is outside ROM'
        vectors.append(f'{target:08x}')
    kernels = [u32(library + 34 + i * 4) for i in range(12)]
    assert all(in_rom(address) for address in kernels)
    receipt = {
        'kind': 'normal-image-mathtrans-two-rom-acceptance', 'passed': True,
        'imageSha256': sha(image), 'librarySha256': cu['librarySha256'],
        'kickstartRomSha256': ks['romSha256'],
        'copperstartRomSha256': cu['romSha256'],
        'extensionRomSha256': cu['extensionRomSha256'],
        'mathtransVectors': 17, 'identicalNumericalAndCcrSamples': 20,
        'overflowAndTrapStages': 6,
        'lifecycle': ['version rejection', 'multiple opens', 'expunge and reload'],
        'copperstartFfpBase': f'{library:08x}',
        'ffpRomVectors': vectors,
        'divideExceptionRomEntry': f'{u32(20):08x}',
        'ffpRomKernels': [f'{a:08x}' for a in kernels],
        'ramObservationSha256': sha(ram),
        'scope': 'Unmodified normal CopperOS ADF, keyboard-driven Shell, read-only DF1 probe; 68000, 2 MiB Chip RAM. This does not requalify the complete Shell or claim physical-hardware coverage.'
    }
    args.output.write_text(json.dumps(receipt, indent=2) + '\n')
    print(json.dumps(receipt))


if __name__ == '__main__':
    main()
