import importlib.util
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
GENERATOR_PATH = ROOT / "tools" / "MuiInventory" / "generate_csharp_constants.py"
SPEC = importlib.util.spec_from_file_location("generate_csharp_constants", GENERATOR_PATH)
GENERATOR = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
SPEC.loader.exec_module(GENERATOR)


class MuiConstantDocumentationTests(unittest.TestCase):
    def test_attribute_documentation_uses_structured_facts(self):
        source = GENERATOR.generate_source([(
            "MUIA_Application_Title",
            0x804281B8,
            {
                "name": "MUIA_Application_Title",
                "introduced_version": 4,
                "access": ["initialize", "get"],
                "value_kind": "string-pointer",
            },
        )])

        self.assertIn("MUI attribute identifier for <c>Application.Title</c>.", source)
        self.assertIn("for initialization and reading", source)
        self.assertIn("guest string pointer", source)
        self.assertIn("since MUI version 4", source)
        self.assertNotIn("DESCRIPTION", source)

    def test_profile_documentation_explains_resolution(self):
        source = GENERATOR.generate_source([(
            "MUIA_Urltext_Active",
            1,
            {
                "name": "MUIA_Urltext_Active",
                "profile_choice": "USE_HYPERLINK_MCC",
            },
        )])

        self.assertIn("through Hyperlink.mcc compatibility", source)


if __name__ == "__main__":
    unittest.main()
