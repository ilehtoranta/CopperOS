import importlib.util
import hashlib
from pathlib import Path
import struct
import unittest


path = Path(__file__).resolve().parents[1] / 'wrap_shell_stack.py'
spec = importlib.util.spec_from_file_location('wrap_shell_stack', path)
stack_gate = importlib.util.module_from_spec(spec)
spec.loader.exec_module(stack_gate)


def word(data, offset):
    return struct.unpack_from('>I', data, offset)[0]


def input_hunk(offsets=(0, 8)):
    code = struct.pack('>3I', 4, 0x4e714e71, 0)
    return (struct.pack('>8I', 0x3f3, 0, 1, 0, 0, 3, 0x3e9, 3) + code +
            struct.pack('>3I', 0x3ec, len(offsets), 0) +
            b''.join(struct.pack('>I', off) for off in offsets) +
            struct.pack('>2I', 0, 0x3f2))


class ShellStackGateTests(unittest.TestCase):
    def test_relocates_gate_and_original_code_for_multiple_load_addresses(self):
        result = stack_gate.wrap(input_hunk())
        total = word(result, 28) * 4
        gate_length = total - 12
        self.assertEqual(0, gate_length % 4)
        self.assertEqual(0x6000002a, word(result, 32))
        self.assertEqual(0x43534846, word(result, 36))
        self.assertEqual(1, word(result, 40))
        self.assertEqual(hashlib.sha256(input_hunk()).digest(), result[44:76])
        self.assertEqual(total, word(result, 20) * 4)
        self.assertEqual(0x3ec, word(result, 32 + total))
        count = word(result, 36 + total)
        self.assertEqual(4, count)
        offsets = [word(result, 44 + total + i * 4) for i in range(count)]
        self.assertEqual([gate_length, gate_length + 8], offsets[2:])
        self.assertEqual(0x4e714e71, word(result, 32 + gate_length + 4))
        for address in (0x10000, 0x340000):
            memory = bytearray(result[32:32 + total])
            for site in offsets:
                struct.pack_into('>I', memory, site, word(memory, site) + address)
            self.assertEqual(address + gate_length + 4, word(memory, gate_length))
            self.assertEqual(address + gate_length, word(memory, gate_length + 8))
            self.assertEqual(address + gate_length, word(memory, offsets[0]))
            self.assertEqual(address + gate_length, word(memory, offsets[1]))

    def test_rejects_truncation_and_unsafe_relocations(self):
        for data in (input_hunk()[:-4], input_hunk((0, 0)),
                     input_hunk((2, 4)), input_hunk((10,))):
            with self.subTest(data=data):
                with self.assertRaises(ValueError):
                    stack_gate.wrap(data)


if __name__ == '__main__':
    unittest.main()
