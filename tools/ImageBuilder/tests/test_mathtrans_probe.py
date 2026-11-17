"""Regression for 68000 word alignment in pathname-based math probes."""
import importlib.util
from pathlib import Path
import unittest


class MathProbeAlignmentTests(unittest.TestCase):
    def test_absolute_library_path_keeps_writable_words_even(self):
        source = Path(__file__).resolve().parents[2] / 'MathTrans.Workbench' / 'make_edge_probe.py'
        spec = importlib.util.spec_from_file_location('math_edge_alignment', source)
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        assembler = module.Assembler
        labels = {}

        class Inspect(assembler):
            def finish(self):
                labels.update(self.labels)
                return super().finish()

        module.Assembler = Inspect
        for name in ('mathtrans.library', 'SYS:Libs/mathtrans.library'):
            with self.subTest(name=name):
                image = module.build(40, name, 'SYS:T/mathresult', full=True)
                self.assertEqual(image[:4], bytes.fromhex('000003f3'))
                for label in ('samples', 'status', 'cosine', 'traps', 'wrong', 'trap'):
                    self.assertEqual(labels[label] & 1, 0, label)
                self.assertIn(name.encode() + b'\0', image)


if __name__ == '__main__':
    unittest.main()
