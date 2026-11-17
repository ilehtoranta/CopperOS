"""Build a diagnostic HUNK that records DOS RunCommand's raw return value.

The fixture is a separate HUNK on DF1:. The wrapper writes the returned D0 as
one big-endian long to SYS:T/raw-run-result and returns success itself. It is
intended for disposable guest images and does not qualify the complete Shell.
"""
import argparse
from pathlib import Path

from make_kickstart_shell_probe import module


def build():
    a = module.Assembler()
    def call(lvo): a.words(0x4eae, lvo)

    a.words(0x48e7, 0x3f3e, 0x2c78, 4)
    a.address(1, 'dos'); a.d(0, 37); call(-552)
    a.words(0x2a40, 0x4a80); a.branch(0x6700, 'fail')
    a.words(0x2c4d); a.address(0, 'fixture'); a.words(0x2208); call(-150)
    a.words(0x2c00, 0x4a80); a.branch(0x6700, 'close_fail')
    a.words(0x2206); a.d(2, 65536); a.address(0, 'arguments')
    a.words(0x2608, 0x7801); call(-504)
    a.words(0x2e00); a.address(4, 'result'); a.words(0x2887)
    a.address(0, 'result_path'); a.words(0x2208); a.d(2, 1006); call(-30)
    a.words(0x2840, 0x4a80); a.branch(0x6700, 'unload_fail')
    a.words(0x220c); a.address(0, 'result'); a.words(0x2408); a.d(3, 4); call(-48)
    a.words(0x0c80, 0, 4); a.branch(0x6600, 'close_file_fail')
    a.words(0x220c); call(-36)
    a.words(0x2206); call(-156)
    a.words(0x224d, 0x2c78, 4); call(-414)
    a.words(0x7000); a.branch(0x6000, 'return')
    a.label('close_file_fail'); a.words(0x220c); call(-36)
    a.label('unload_fail'); a.words(0x2206); call(-156)
    a.label('close_fail'); a.words(0x224d, 0x2c78, 4); call(-414)
    a.label('fail'); a.words(0x7014)
    a.label('return'); a.words(0x4cdf, 0x7cfc, 0x4e75)
    for name, value in (('dos', 'dos.library'), ('fixture', 'DF1:fixture'),
                        ('result_path', 'SYS:T/raw-run-result'), ('arguments', '\n')):
        a.text(name, value)
    if len(a.code) & 1: a.code.append(0)
    a.label('result'); a.code.extend(b'\0' * 4)
    return a.finish()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('output', type=Path)
    args = parser.parse_args()
    args.output.write_bytes(build())


if __name__ == '__main__':
    main()
