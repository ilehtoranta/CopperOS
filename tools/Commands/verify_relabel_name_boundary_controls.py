"""Challenge boundary arguments, intermediate disk state, faults and recovery."""
import argparse
import copy
import json
from pathlib import Path
import struct
from prepare_relabel_boot import NAME_BOUNDARIES
from verify_relabel_boot import digest, require
from verify_relabel_name_boundaries import observe


def call(report, name, index=0):
    row = report["copyInvocationOwnership"][index]
    return next(e for e in report["observedDosCalls"] if e["Name"] == name and
                e["Task"] == row["Task"] and row["EntryCycle"] < e["EntryRetireCycle"] < row["ReturnCycle"])


def runs(report):
    return [e for e in report["observedDosCalls"] if "DataRootBlockHex" in e]


def alter_root(report, offset):
    entry = runs(report)[1]
    data = bytearray.fromhex(entry["DataRootBlockHex"])
    data[offset] ^= 1
    struct.pack_into(">I", data, 20, 0)
    struct.pack_into(">I", data, 20, -sum(struct.unpack(">128I", data)) & 0xffffffff)
    entry["DataRootBlockHex"] = data.hex()


def alter_diagnostic(report):
    events = report["observedDosCalls"]
    opened = next(e for e in events if e["Name"] == "Open" and e.get("Text") == "rn01")
    byte = next(e for e in events[events.index(opened)+1:] if e["Name"] == "FGetC" and
                e["D1"] == opened["ReturnedD0"] and e["Task"] == opened["Task"])
    byte["ReturnedD0"] ^= 1


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--directory", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    require(not args.output.exists(), "Fresh controls output")
    reports, media, bindings = {}, {}, {}
    for case in NAME_BOUNDARIES:
        reports[case], media[case], bindings[case] = {}, {}, {}
        results = []
        for role in ["reference", "candidate"]:
            p = args.directory / case
            r, m = p / "qualified" / (role + "-observations.json"), p / "media" / (role + "-media.json")
            reports[case][role], media[case][role] = json.loads(r.read_text(encoding="utf-8")), json.loads(m.read_text(encoding="utf-8"))
            bindings[case][role] = {"observationsSha256": digest(r), "mediaSha256": digest(m)}
            results.append(observe(reports[case][role], media[case][role], role == "candidate"))
        require(results[0] == results[1], "Positive pair " + case)
    controls = [
        ("name30", "missing-recovery", lambda r: r["copyInvocationOwnership"].pop()),
        ("name30", "wrong-recovery-error", lambda r: r["copyInvocationOwnership"][1].update(ReturnIoErr=210)),
        ("name30", "lost-boundary-label", lambda r: runs(r)[1].update(DataRootBlockHex=runs(r)[0]["DataRootBlockHex"])),
        ("name30", "non-root-write", lambda r: runs(r)[1].update(DataNonRootSha256="0" * 64)),
        ("name30", "root-table-write-valid-checksum", lambda r: alter_root(r, 24)),
        ("name30", "root-label-write-valid-checksum", lambda r: alter_root(r, 433)),
        ("name30", "wrong-private-free", lambda r: call(r, "CopyFreeMem").update(A1=1)),
        ("name30", "missing-parser-release", lambda r: call(r, "FreeArgs").update(D1=1)),
        ("name31", "frontend-truncation", lambda r: call(r, "Relabel").update(DestinationText="N" * 30)),
        ("name31", "incorrect-fault-number", lambda r: call(r, "PrintFault").update(D1=205)),
        ("name31", "changed-fault-text", alter_diagnostic),
        ("name31", "no-handler-error", lambda r: call(r, "Relabel").update(ReturnedIoErr=0)),
        ("name-empty", "invented-name", lambda r: call(r, "Relabel").update(DestinationText="Untitled")),
        ("name-empty", "wrong-failure-level", lambda r: r["copyInvocationOwnership"][0].update(ReturnCode=5)),
        ("name-slash", "frontend-path-rewrite", lambda r: call(r, "Relabel").update(DestinationText="Left")),
        ("name-slash", "wrong-final-export", lambda r: r.update(dataDiskFinalSha256="0" * 64)),
    ]
    rejected = []
    for case, name, change in controls:
        report = copy.deepcopy(reports[case]["candidate"])
        change(report)
        try:
            observe(report, media[case]["candidate"], True)
        except ValueError as error:
            rejected.append({"case": case, "control": name, "rejected": True, "reason": str(error)})
        else:
            raise ValueError("Corruption accepted: " + name)
    args.output.write_text(json.dumps({"status": "passed", "positivePairs": 4, "inputs": bindings,
        "rejections": rejected, "verifierSha256": digest(Path(__file__).with_name("verify_relabel_name_boundaries.py")),
        "controlsSha256": digest(__file__), "shippingOrPureApproval": False}, indent=2) + "\n", encoding="utf-8")
    print(args.output)


if __name__ == "__main__":
    main()
