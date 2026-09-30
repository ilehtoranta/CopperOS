import importlib.util
import sys
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
GENERATOR_PATH = ROOT / "tools" / "MuiInventory" / "generate_csharp_messages.py"
SPEC = importlib.util.spec_from_file_location("generate_csharp_messages", GENERATOR_PATH)
GENERATOR = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
sys.modules[SPEC.name] = GENERATOR
SPEC.loader.exec_module(GENERATOR)


class MuiMessageStructureTests(unittest.TestCase):
    def test_public_source_uses_struct_fields_without_parallel_layout_constants(self):
        fields = [
            GENERATOR.Field("MethodID", "MethodID", "uint", 0, None, False),
            GENERATOR.Field("value", "value", "APTR", 4, None, False),
        ]

        source = GENERATOR.generate_structures([("MUIP_Example", fields, 8)])

        self.assertIn("public struct MUIP_Example", source)
        self.assertIn("public uint MethodID;", source)
        self.assertIn("public APTR value;", source)
        self.assertNotIn("MUIMessageLayout", source)

    def test_flexible_array_tail_keeps_only_its_required_offset(self):
        fields = [
            GENERATOR.Field("MethodID", "MethodID", "uint", 0, None, False),
            GENERATOR.Field("data", "data", "byte", 4, None, True),
        ]

        source = GENERATOR.generate_structures([("MUIP_Flexible", fields, 4)])

        self.assertIn("public const uint dataOffset = 4;", source)
        self.assertNotIn("MUIMessageLayout", source)


if __name__ == "__main__":
    unittest.main()
