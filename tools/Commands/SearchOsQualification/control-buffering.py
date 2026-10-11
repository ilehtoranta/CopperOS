"""Bind a failing old Search image to the corrected buffered-output fixture."""
import json
import pathlib
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1]))
from measure_c import identity
from qualify_sizes import run

repo = pathlib.Path(__file__).resolve().parents[3]
stage = pathlib.Path(sys.argv[1]).resolve()
runner = stage / "vector-runner/CopperOS.Commands.NativeExecution.dll"
image = repo / "artifacts/search-os-qualification-20261012/inputs/Search.candidate"
receipt = stage / "controls/old-candidate-buffered-check.json"
manifest = stage / "controls/buffering-control.json"
assert not receipt.exists() and not manifest.exists(), "Preserve previous controls; use a fresh stage."
before = [identity(path) for path in (runner, image)]
command = ["dotnet", str(runner), str(image), "68000", str(receipt),
           "wb31-search-native-entry-vector-fixture"]
code = run(command, receipt.with_suffix(".log"))
assert code != 0
result = json.loads(receipt.read_text(encoding="utf-8-sig"))
assert result["status"] == "failed" and "without flushing" in result["failure"]
assert result["imageSha256"] == before[1]["sha256"]
assert before == [identity(path) for path in (runner, image)]
manifest.write_text(json.dumps(dict(command=command, exitCode=code,
    runner=before[0], image=before[1], receipt=identity(receipt)), indent=2) + "\n")
print(result["failure"])
