"""Build a tiny LIST command for the native global-listing probe."""
import argparse
import importlib.util
from pathlib import Path

root = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location('amiga_assembler', root / 'tools/Locale.Workbench/make_probe.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


def build(result, argument_path):
    if result not in (0, 5, 10):
        raise ValueError('Fixture result must be 0, 5 or 10')
    a = module.Assembler()
    def call(lvo): a.words(0x4eae, lvo)
    # Save the command tail before opening DOS. D4 = length, D5 = pointer.
    a.words(0x48e7, 0x3f3e, 0x2800, 0x2a08, 0x2c78, 4)
    a.address(1, 'dos'); a.d(0, 37); call(-552)
    a.words(0x2e00, 0x4a80); a.branch(0x6700, 'fail')
    a.words(0x2c47); a.address(0, 'args'); a.words(0x2208); a.d(2, 1006); call(-30)
    a.words(0x2c00, 0x4a80); a.branch(0x6700, 'close')
    a.words(0x2206, 0x2405, 0x2604); call(-48)
    a.words(0x2206); call(-36)
    call(-60); a.words(0x2200); a.address(0, 'message'); a.words(0x2408); a.d(3, 10); call(-48)
    # The outer SetEnv/UnsetEnv must not publish this child secondary error.
    a.d(1, 205); call(-462)
    a.label('close'); a.words(0x2247, 0x2c78, 4); call(-414)
    a.words(0x7000 | result); a.branch(0x6000, 'return')
    a.label('fail'); a.words(0x7014)
    a.label('return'); a.words(0x4cdf, 0x7cfc, 0x4e75)
    a.text('dos', 'dos.library'); a.text('args', argument_path); a.text('message', 'MOCK LIST\n')
    return a.finish()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('output', type=Path)
    parser.add_argument('--result', type=int, choices=(0, 5, 10), required=True)
    parser.add_argument('--argument-path', default='DH0:listargs')
    args = parser.parse_args()
    args.output.write_bytes(build(args.result, args.argument_path))


if __name__ == '__main__':
    main()
