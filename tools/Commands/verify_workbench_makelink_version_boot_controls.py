"""Run semantic and binding corruptions through the complete Version boot verifier.

Semantic controls rewrite the captured TRX JSON to agree with the changed
observation, so they cannot pass merely by triggering a TRX/JSON mismatch.
Original observations, TRX, private media and binaries remain read-only.
"""
import argparse
from copy import deepcopy
import json
from pathlib import Path
from tempfile import TemporaryDirectory
from types import SimpleNamespace
import xml.etree.ElementTree as ET

from verify_workbench_makelink_version_boot import verify, digest


def event(trace, name, text=None, last=False):
    found = [v for v in trace["observedDosCalls"] if v["Name"] == name and
             (text is None or v.get("Text") == text)]
    if not found:
        raise ValueError(f"Control setup missing {name}/{text}")
    return found[-1] if last else found[0]


def version_run(trace):
    load = event(trace, "LoadSeg", "C:Version")
    return next(v for v in trace["observedDosCalls"] if v["Name"] == "RunCommand" and v["D1"] == load["ReturnedD0"])


def makelink_run(trace):
    segment = trace["copyInvocationOwnership"][0]["Segment"]
    return next(v for v in trace["observedDosCalls"] if v["Name"] == "RunCommand" and v["D1"] == segment)


def read_byte(trace, name):
    events = trace["observedDosCalls"]
    opened = event(trace, "Open", name)
    return next(v for v in events[events.index(opened) + 1:] if v["Name"] == "FGetC" and
                v["Task"] == opened["Task"] and v["D1"] == opened["ReturnedD0"])


def run_controls(args):
    positive = verify(args)
    original = json.loads(args.observations.read_text(encoding="utf-8-sig"))
    original_trx = ET.parse(args.trx)
    controls = [
        ("wrong-version-return", lambda trace: version_run(trace).__setitem__("ReturnedD0", 5)),
        ("wrong-version-file-target", lambda trace: event(trace, "Open", "C:Ed").__setitem__("Text", "C:Other")),
        ("wrong-version-file-mode", lambda trace: event(trace, "Open", "C:Ed").__setitem__("D2", 1006)),
        ("wrong-version-command", lambda trace: event(trace, "LoadSeg", "C:Version").__setitem__("Text", "C:Other")),
        ("wrong-inspection-caller", lambda trace: event(trace, "LoadSeg", "C:Ed", last=True).__setitem__("CallerPc", 0x1000)),
        ("wrong-inspection-loaded-bytes", lambda trace: trace.__setitem__("loadedCopyImageSha256", "0" * 64)),
        ("changed-version-rendered-byte", lambda trace: read_byte(trace, "out-version").__setitem__("ReturnedD0", ord("X"))),
        ("incomplete-version-readback", lambda trace: trace["observedDosCalls"].remove(read_byte(trace, "out-version"))),
        ("changed-alias-content", lambda trace: read_byte(trace, "first").__setitem__("ReturnedD0", ord("X"))),
        ("changed-makelink-final-error", lambda trace: makelink_run(trace).__setitem__("ReturnedIoErr", 999)),
        ("changed-resident-image", lambda trace: trace["copyCpuImageWrites"].append({"Address": 0x1234})),
        ("changed-execution-bound", lambda trace: trace.__setitem__("maximumChunks", 64)),
    ]
    results = []
    prefix = "DOS passive boot readiness: "
    with TemporaryDirectory(prefix="makelink-version-controls-") as directory:
        directory = Path(directory)
        for index, (name, mutate) in enumerate(controls):
            trace = deepcopy(original)
            mutate(trace)
            selected = SimpleNamespace(**vars(args))
            selected.observations = directory / f"{index}.json"
            selected.trx = directory / f"{index}.trx"
            selected.observations.write_text(json.dumps(trace), encoding="utf-8")
            tree = deepcopy(original_trx)
            replaced = 0
            for node in tree.iter():
                if node.tag.endswith("}StdOut"):
                    lines = []
                    for line in (node.text or "").splitlines():
                        if line.startswith(prefix):
                            line = prefix + json.dumps(trace)
                            replaced += 1
                        lines.append(line)
                    node.text = "\n".join(lines)
            if replaced != 1:
                raise ValueError("Expected one TRX capture for semantic control")
            tree.write(selected.trx, encoding="utf-8", xml_declaration=True)
            try:
                verify(selected)
            except ValueError as error:
                reason = str(error)
                if reason == "TRX/observation mismatch":
                    raise ValueError("Semantic control failed only at capture binding") from error
                results.append({"name": name, "rejected": True, "reason": reason, "trx_observations_consistent": True})
            else:
                raise ValueError("Control was not rejected: " + name)
        # These two additional controls specifically exercise terminal result and
        # capture binding, independently of the semantic mutations above.
        for name in ("failed-terminal-result", "stale-capture"):
            selected = SimpleNamespace(**vars(args))
            selected.trx = directory / (name + ".trx")
            tree = deepcopy(original_trx)
            if name == "failed-terminal-result":
                result = next(node for node in tree.iter() if node.tag.endswith("}UnitTestResult"))
                result.set("outcome", "Failed")
            else:
                selected.observations = directory / "stale.json"
                trace = deepcopy(original)
                trace["maximumChunks"] = 33
                selected.observations.write_text(json.dumps(trace), encoding="utf-8")
            tree.write(selected.trx, encoding="utf-8", xml_declaration=True)
            try:
                verify(selected)
            except ValueError as error:
                results.append({"name": name, "rejected": True, "reason": str(error), "trx_observations_consistent": name != "stale-capture"})
            else:
                raise ValueError("Binding control was not rejected: " + name)
    paths = {name: getattr(args, name) for name in ("observations", "media", "trx", "startup", "test_assembly", "rom")}
    paths.update(controls=Path(__file__), verifier=Path(__file__).with_name("verify_workbench_makelink_version_boot.py"))
    return {"status": "passed", "positive_status": positive["status"], "controls": results,
            "rejected": len(results), "shipping_qualified": False, "pure_admitted": False,
            "scope": "Twelve semantic corruptions through the complete verifier with mutually consistent TRX/observations, plus failed-terminal and stale-capture controls; no new guest cases.",
            "evidence": {name: {"path": str(path.resolve()), "sha256": digest(path)} for name, path in paths.items()}}


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ("observations", "media", "trx", "startup", "test_assembly", "rom"):
        parser.add_argument(name, type=Path)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    if args.output.exists():
        parser.error("Use a new output path")
    report = run_controls(args)
    with args.output.open("x", encoding="utf-8", newline="\n") as stream:
        json.dump(report, stream, indent=2)
        stream.write("\n")
    print(f"Passed positive verifier and rejected all {report['rejected']} controls")
