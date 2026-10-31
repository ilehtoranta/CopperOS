"""Test-only HUNKs that inject a command through the guest input device."""
import importlib.util
from pathlib import Path
import struct

path = Path(__file__).resolve().parents[1] / 'Locale.Workbench' / 'make_probe.py'
spec = importlib.util.spec_from_file_location('locale_probe', path)
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


def parent():
    a = module.Assembler()
    def call(lvo): a.words(0x4eae, lvo)
    a.words(0x48e7, 0x3f3e, 0x2c78, 4)
    a.address(1, 'dos'); a.d(0, 37); call(-552)
    a.words(0x2a40, 0x4a80); a.branch(0x6700, 'fail')
    a.words(0x2c4d); a.address(0, 'child'); a.words(0x2208); call(-150)
    a.words(0x2c00, 0x4a80); a.branch(0x6700, 'close')
    a.address(4, 'tags'); a.words(0x2946, 4, 0x220c); call(-498)
    a.words(0x4a80); a.branch(0x6600, 'close')
    a.words(0x2206); call(-156)
    a.label('close'); a.words(0x224d, 0x2c78, 4); call(-414)
    a.words(0x7000); a.branch(0x6000, 'return')
    a.label('fail'); a.words(0x7014)
    a.label('return'); a.words(0x4cdf, 0x7cfc, 0x4e75)
    a.text('dos', 'dos.library'); a.text('child', 'C:KeyChild')
    if len(a.code) & 1: a.code.append(0)
    a.label('tags')
    a.code += struct.pack('>8I', 0x800003e9, 0, 0x800003ea, 1,
                          0x800003f3, 8192, 0, 0)
    return a.finish()


def child(commands=None):
    # Amiga US raw-key codes and qualifiers for two interactive commands.
    raw = {'a': 0x20, 'b': 0x35, 'c': 0x33, 'd': 0x22, 'e': 0x12,
           'f': 0x23, 'g': 0x24, 'h': 0x25, 'i': 0x17, 'k': 0x27,
           'l': 0x28, 'm': 0x37, 'n': 0x36, 'o': 0x18, 'p': 0x19,
           'r': 0x13, 's': 0x21, 't': 0x14, 'u': 0x16, 'v': 0x34,
           'x': 0x32, '1': 0x01, '2': 0x02, ' ': 0x40, '.': 0x39, '\n': 0x44}
    keys = bytearray()
    if commands is None:
        commands = ('echo >interactive.txt hello\nmakedir interactiveprobe\n'
                    'eval 1 + 2 >eval.txt\nversion c:eval file >version.txt\n'
                    'dir interactiveprobe >dir.txt\ncopperosnosuchcommand\n'
                    'shellstatus\necho >>interactive.txt again\n'
                    'makedir removeme\ndir >before.txt\ndelete removeme\n'
                    'echo >finish.txt done\n')
    for c in commands:
        keys.extend((0x39, 1) if c == '>' else (0x0c, 1) if c == '+' else
                    (0x29, 1) if c == ':' else (raw[c], 0))
    keys.extend((0xff, 0))
    a = module.Assembler()
    def call(lvo): a.words(0x4eae, lvo)
    a.words(0x48e7, 0x3f3e, 0x2c78, 4)
    a.address(1, 'dos'); a.d(0, 37); call(-552)
    a.words(0x2e00, 0x4a80); a.branch(0x6700, 'fail'); a.words(0x7c00)
    a.words(0x2c47); a.d(1, 500); call(-198)  # Allow normal BootShell to start.
    a.words(0x2c78, 4); a.address(1, 'shell_task'); call(-294)
    a.words(0x4a80); a.branch(0x6700, 'create_port')
    a.words(0x2c47); a.address(0, 'present'); a.words(0x2208); a.d(2, 1006); call(-30)
    a.words(0x2a40, 0x4a80); a.branch(0x6700, 'create_port')
    a.words(0x220d); a.address(0, 'present_text'); a.words(0x2408); a.d(3, 8); call(-48)
    a.words(0x220d); call(-36)
    # If the first boot's injector receipt exists, record a second boot.
    a.address(0, 'sent'); a.words(0x2208); a.d(2, 1005); call(-30)
    a.words(0x4a80); a.branch(0x6700, 'create_port')
    a.words(0x2200); call(-36)
    a.address(0, 'second'); a.words(0x2208); a.d(2, 1006); call(-30)
    a.words(0x2a40, 0x4a80); a.branch(0x6700, 'create_port')
    a.words(0x220d); a.address(0, 'second_text'); a.words(0x2408); a.d(3, 6); call(-48)
    a.words(0x220d); call(-36)
    a.label('create_port'); a.words(0x2c78, 4); call(-666)  # CreateMsgPort
    a.words(0x2840, 0x4a80); a.branch(0x6700, 'close_dos')
    a.words(0x204c); a.d(0, 48); call(-654)   # CreateIORequest
    a.words(0x2a40, 0x4a80); a.branch(0x6700, 'delete_port')
    a.address(0, 'device'); a.words(0x224d, 0x7000, 0x7200); call(-444)
    a.words(0x4a80); a.branch(0x6600, 'delete_request')
    a.address(2, 'keys'); a.address(3, 'event')
    a.label('next'); a.words(0x7000, 0x101a, 0x0c00, 0x00ff)
    a.branch(0x6700, 'complete')
    a.words(0x7200, 0x121a, 0x2a00, 0x2801); a.branch(0x6100, 'send')
    a.words(0x2005, 0x2204, 0x0040, 0x0080); a.branch(0x6100, 'send')
    a.words(0x2c47, 0x7201); call(-198)        # One tick between keys.
    a.words(0x2c78, 4); a.branch(0x6000, 'next')
    a.label('send')
    a.words(0x4293, 0x377c, 0x0100, 4, 0x3740, 6, 0x3741, 8,
            0x42ab, 10, 0x42ab, 14, 0x42ab, 18)
    a.words(0x3b7c, 11, 28, 0x2b7c, 0, 22, 36, 0x2b4b, 40, 0x224d)
    call(-456); a.words(0x4e75)
    a.label('complete'); a.words(0x7c01)
    a.label('close_device'); a.words(0x224d); call(-450)
    a.label('delete_request'); a.words(0x204d); call(-660)
    a.label('delete_port'); a.words(0x204c); call(-672)
    a.words(0x4a86); a.branch(0x6700, 'close_dos')
    a.words(0x2c47); a.address(0, 'sent'); a.words(0x2208); a.d(2, 1006); call(-30)
    a.words(0x2a40, 0x4a80); a.branch(0x6700, 'close_dos_restore')
    a.words(0x220d); a.address(0, 'sent_text'); a.words(0x2408); a.d(3, 5); call(-48)
    a.words(0x220d); call(-36)
    a.label('close_dos_restore'); a.words(0x2c78, 4)
    a.label('close_dos'); a.words(0x2247); call(-414)
    a.words(0x7000); a.branch(0x6000, 'return')
    a.label('fail'); a.words(0x7014)
    a.label('return'); a.words(0x4cdf, 0x7cfc, 0x4e75)
    a.text('dos', 'dos.library'); a.text('device', 'input.device')
    a.text('sent', 'SYS:input-sent.txt'); a.text('sent_text', 'SENT\n')
    a.text('shell_task', 'CopperOS Shell'); a.text('present', 'SYS:shell-present.txt')
    a.text('present_text', 'PRESENT\n')
    a.text('second', 'SYS:second-boot.txt'); a.text('second_text', 'AGAIN\n')
    if len(a.code) & 1: a.code.append(0)
    a.label('event'); a.code += bytes(22)
    a.label('keys'); a.code += keys
    return a.finish()
