import json
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
INVENTORY = ROOT / "docs" / "Libraries" / "MorphOs320Mui" / "abi-inventory.json"


class MuiInventoryTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.data = json.loads(INVENTORY.read_text(encoding="utf-8"))

    def test_profile_and_public_vectors_are_frozen(self):
        self.assertEqual("MorphOs320M68k", self.data["profile"])
        vectors = self.data["vectors"]
        self.assertEqual(27, len(vectors))
        self.assertTrue(all(row["comparison"] == "match" for row in vectors))
        request_object = next(row for row in vectors if row["name"] == "MUI_RequestObjectA")
        self.assertEqual(-756, request_object["lvo"])
        self.assertEqual(["D0", "D1", "D2", "A0", "A1", "A2", "A3", "A4"], request_object["registers"])

    def test_official_class_inventory_and_packaging_are_complete(self):
        classes = self.data["classes"]
        self.assertEqual(69, len(classes))
        self.assertEqual(69, len({row["class"] for row in classes}))
        external = [row for row in classes if row["packaging"] == "external-component"]
        self.assertEqual(["Listtree"], [row["class"] for row in external])
        self.assertEqual("Listtree.mcc", external[0]["component"])
        self.assertEqual(68, sum(row["packaging"] == "loader-transparent-packaging-unverified" for row in classes))

    def test_base_header_category_counts_match_frozen_sdk(self):
        rows = [row for row in self.data["macros"] if row["source"] == "libraries/mui.h"]
        expected = {"muic": 104, "muia": 566, "muim": 259, "muiv": 302,
                    "muio": 25, "muii": 77, "muix": 9, "muif": 7}
        for category, count in expected.items():
            self.assertEqual(count, sum(row["category"] == category for row in rows), category)

    def test_every_declaration_has_an_explicit_comparison_disposition(self):
        macros = self.data["macros"]
        records = self.data["structures_and_messages"]
        self.assertEqual(2158, len(macros))
        self.assertEqual(371, len(records))
        self.assertTrue(all(row.get("comparison") for row in macros))
        self.assertTrue(all(row.get("comparison") for row in records))
        unresolved = {
            "missing", "conflict", "unverified", "present-value-unmapped",
            "present-value-unverified", "missing-helper-or-unverified",
            "declaration-marker-unverified",
        }
        self.assertEqual([], [
            row["name"] for row in macros if row["comparison"] in unresolved
        ])
        self.assertEqual(1923, sum(row["comparison"] == "match" for row in macros))
        self.assertEqual(19, sum(row["comparison"] == "typed-helper-tested" for row in macros))
        self.assertEqual([], [row["name"] for row in macros if row["comparison"] == "missing"])

        maxmax = next(row for row in macros if row["name"] == "MUI_MAXMAX")
        self.assertEqual("match", maxmax["comparison"])
        self.assertEqual("MUIConstants.MUI_MAXMAX", maxmax["csharp_member"])

        foundational = {
            "MUI_Command", "MUI_Palette_Entry", "MUI_InputHandlerNode",
            "MUI_EventHandlerNode", "MUI_List_TestPos_Result", "MUI_RGBColor",
            "MUI_GlobalInfo", "MUI_NotifyData", "MUI_MinMax", "MUI_LayoutMsg",
            "MUI_AreaData", "MUI_DragImage", "MUI_RenderInfo", "MUI_PenSpec",
            "MUI_CustomClass",
        }
        present = {row["name"] for row in records if row["comparison"] == "present"}
        self.assertTrue(foundational.issubset(present))
        self.assertEqual(371, len(present))
        self.assertEqual(0, sum(row["comparison"] == "missing" for row in records))
        call_hook = next(row for row in records if row["name"] == "MUIP_CallHook")
        self.assertEqual("present", call_hook["comparison"])

    def test_inventory_does_not_embed_sdk_prose(self):
        serialized = INVENTORY.read_text(encoding="utf-8")
        for forbidden in ("DESCRIPTION", "PURPOSE", "SEE ALSO", "Copyright c 2002-2020"):
            self.assertNotIn(forbidden, serialized)

    def test_attribute_documentation_is_structured(self):
        macros = {row["name"]: row for row in self.data["macros"]}
        title = macros["MUIA_Application_Title"]
        self.assertEqual(4, title["introduced_version"])
        self.assertEqual(["initialize", "get"], title["access"])
        self.assertEqual("string-pointer", title["value_kind"])

        opened = macros["MUIA_Window_Open"]
        self.assertEqual(["set", "get"], opened["access"])
        self.assertEqual("boolean", opened["value_kind"])

        active = macros["MUIA_List_Active"]
        self.assertEqual(["initialize", "set", "get"], active["access"])
        self.assertEqual("signed-integer", active["value_kind"])

    def test_profile_documentation_is_explicit(self):
        macros = {row["name"]: row for row in self.data["macros"]}
        self.assertEqual(
            "USE_HYPERLINK_MCC",
            macros["MUIA_Urltext_Active"]["profile_choice"],
        )


if __name__ == "__main__":
    unittest.main()
