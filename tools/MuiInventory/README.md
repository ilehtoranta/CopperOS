# MUI inventory generator

This tool reads a locally installed MorphOS 3.20 SDK and compares its public MUI
ABI facts with the sibling CopperSharp68k SDK project. It emits identifiers and
ABI facts only; it does not copy SDK documentation or source text.

```powershell
python tools\MuiInventory\generate_inventory.py `
  --sdk-include D:\path\to\Development\gg\os-include `
  --sdk-autodoc D:\path\to\Development\Autodoc\MUI `
  --coppersharp D:\path\to\CopperSharp68k `
  --output docs\Libraries\MorphOs320Mui\abi-inventory.json
python -m unittest discover -s tests\Inventory -p "test_*.py" -v
```

The generated summary is a gate: unverified and missing rows are retained for
MG01 rather than silently discarded.


After regenerating the ledger, refresh the shared SDK-owned ABI sources and
exhaustive tests:

```powershell
python tools\MuiInventory\generate_csharp_constants.py `
  --inventory docs\Libraries\MorphOs320Mui\abi-inventory.json `
  --constants-output D:\path\to\CopperSharp68k\Sdk.Amiga\MUIMaster\MUIConstants.Generated.cs `
  --tests-output D:\path\to\CopperSharp68k\Compiler.Tests\MuiConstantAbiTests.Generated.cs
python tools\MuiInventory\generate_csharp_messages.py `
  --inventory docs\Libraries\MorphOs320Mui\abi-inventory.json `
  --structures-output D:\path\to\CopperSharp68k\Sdk.Amiga\MUIMaster\Messages.Generated.cs `
  --tests-output D:\path\to\CopperSharp68k\Compiler.Tests\MuiMessageAbiTests.Generated.cs
python tools\MuiInventory\generate_csharp_vector_tests.py `
  --inventory docs\Libraries\MorphOs320Mui\abi-inventory.json `
  --output D:\path\to\CopperSharp68k\Compiler.Tests\MuiVectorAbiTests.Generated.cs
```

Generated files contain public identifiers and fixed-width ABI facts only.
They do not embed SDK documentation, comments, examples, or implementation
text. The acceptance suite rejects missing, conflicting, heuristic, and
unverified comparison states.
