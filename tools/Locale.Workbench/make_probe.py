"""Original 68000 disk probe: read locale/ENV/Fault through public guest APIs.

Publishes a PA_IGNORE named MsgPort with owned, copied observations for passive
host inspection. It injects no host calls and returns to the real startup CLI.
"""
import argparse
from pathlib import Path
import struct


class Assembler:
    def __init__(self):
        self.code = bytearray()
        self.labels = {}
        self.patches = []

    def words(self, *values):
        self.code += struct.pack('>' + 'H' * len(values), *(v & 0xffff for v in values))

    def label(self, name):
        self.labels[name] = len(self.code)

    def branch(self, op, name):
        self.words(op)
        self.patches.append((len(self.code), name))
        self.words(0)

    def address(self, register, name):
        self.branch(0x41fa + (register << 9), name)

    def d(self, register, value):
        self.words(0x203c + (register << 9), value >> 16, value)

    def text(self, name, text):
        if len(self.code) & 1:
            self.code.append(0)
        self.label(name)
        self.code += text.encode('ascii') + b'\0'

    def finish(self):
        for at, name in self.patches:
            struct.pack_into('>h', self.code, at, self.labels[name] - at)
        while len(self.code) & 3:
            self.code.append(0)
        size = len(self.code) // 4
        return struct.pack('>8I', 1011, 0, 1, 0, 0, size, 1001, size) + self.code + struct.pack('>I', 1010)


def probe(name, delay=0, apply=None):
    a = Assembler()
    def call(lvo): a.words(0x4eae, lvo)
    def execbase(): a.words(0x2c78, 4)
    def dosbase(): a.words(0x2c4c)
    def localebase(): a.words(0x2c4d)
    def copy_to(offset):
        a.words(0x43ea, offset)
        a.branch(0x6100, 'copy')

    a.words(0x48e7, 0x3f3e)  # preserve D2-D7/A2-A6
    execbase(); a.address(1, 'dos'); a.d(0, 37); call(-552)
    a.words(0x2840, 0x4a80); a.branch(0x6700, 'fail')
    execbase(); a.d(0, 640); a.d(1, 0x10001); call(-198)
    a.words(0x2440, 0x4a80); a.branch(0x6700, 'close-dos')
    if delay:
        dosbase(); a.d(1, delay); call(-198)
    execbase(); a.address(1, 'locale'); a.d(0, 38); call(-552)
    a.words(0x2a40, 0x2540, 42, 0x4a80); a.branch(0x6700, 'publish')
    if apply:
        localebase(); a.address(0, 'prefs'); call(-156)
        a.words(0x2640, 0x4a80); a.branch(0x6700, 'close-locale-library')
        a.words(0x204b); call(-168)  # LocalePrefsUpdate(new)
        a.words(0x2040); call(-42)   # returned old locale
        a.words(0x204b); call(-42)   # our new handle; system retains its pin
    localebase(); a.words(0x91c8); call(-156)
    a.words(0x2640, 0x2540, 46, 0x4a80); a.branch(0x6700, 'close-locale-library')
    a.words(0x206b, 4); copy_to(448)  # public loc_LanguageName
    localebase(); a.words(0x204b); a.d(0, 15); call(-78)
    a.words(0x2040); copy_to(128)
    dosbase(); a.address(0, 'language'); a.words(0x2208, 0x41ea, 64, 0x2408)
    a.d(3, 63); a.d(4, 0x100); call(-906)
    for number, offset in ((205, 192), (103, 320)):
        dosbase(); a.d(1, number); a.d(2, 0); a.words(0x41ea, offset, 0x2608); a.d(4, 127); call(-468)
    execbase(); a.address(1, 'utility'); a.d(0, 37); call(-552)
    a.words(0x2040, 0x4a80); a.branch(0x6700, 'close-locale')
    a.words(0x7000, 0x1028, 34, 0x2540, 54, 0x2248); call(-414)
    a.label('close-locale'); localebase(); a.words(0x204b); call(-42)
    a.words(0x257c, 0, 1, 38)
    a.label('close-locale-library'); execbase(); a.words(0x224d); call(-414)
    a.label('publish')
    a.address(0, 'port-name'); copy_to(608)
    a.words(0x41ea, 608, 0x2548, 10, 0x157c, 4, 8, 0x157c, 2, 14)
    a.words(0x41ea, 24, 0x2548, 20, 0x41ea, 20, 0x2548, 28)
    a.words(0x257c, 0x4c50, 0x4631, 34)  # LPF1
    execbase(); a.words(0x224a); call(-354)
    a.label('close-dos'); execbase(); a.words(0x224c); call(-414)
    a.words(0x7000); a.branch(0x6000, 'return')
    a.label('fail'); a.words(0x7014)
    a.label('return'); a.words(0x4cdf, 0x7cfc, 0x4e75)
    a.label('copy'); a.words(0x12d8); a.branch(0x6600, 'copy'); a.words(0x4e75)
    for label, text in [('dos', 'dos.library'), ('locale', 'locale.library'), ('language', 'Language'), ('utility', 'utility.library'), ('port-name', name)]:
        a.text(label, text)
    if apply:
        a.text('prefs', apply)
    return a.finish()


if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--name', required=True)
    p.add_argument('--delay', type=int, default=0)
    p.add_argument('--apply', help='Diagnostic only: apply a named locale directly, without claiming IPrefs coverage')
    p.add_argument('--output', required=True, type=Path)
    a = p.parse_args()
    a.output.write_bytes(probe(a.name, a.delay, a.apply))
