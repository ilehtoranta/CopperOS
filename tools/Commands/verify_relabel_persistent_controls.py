"""Challenge persistent-volume qualification with damaged media and traces."""
import argparse
import copy
import json
from pathlib import Path
import struct
from verify_relabel_boot import digest, require
from verify_relabel_persistent_boot import observe, verify_sectors


def event(report, name):
    row = report["copyInvocationOwnership"][0]
    return next(e for e in report["observedDosCalls"] if e["Name"] == name and
                (name == "SegmentFreeMem" or row["EntryCycle"] < e["EntryRetireCycle"] < row["ReturnCycle"]))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--directory", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    require(not args.output.exists(), "Fresh controls output")
    reports, media, bindings = {}, {}, {}
    for role in ["reference", "candidate"]:
        report = args.directory / "qualified" / (role + "-observations.json")
        receipt = args.directory / "media" / (role + "-media.json")
        reports[role], media[role] = json.loads(report.read_text()), json.loads(receipt.read_text())
        bindings[role] = {"observationsSha256": digest(report), "mediaSha256": digest(receipt)}
    require(observe(reports["reference"], media["reference"], False) ==
            observe(reports["candidate"], media["candidate"], True), "Positive original/replacement pair")
    rejected = []
    def reject(name, action):
        try:
            action()
        except ValueError as error:
            rejected.append({"control": name, "rejected": True, "reason": str(error)})
        else:
            raise ValueError("Corruption accepted: " + name)
    before = (args.directory / "data" / "initial.adf").read_bytes()
    after = (args.directory / "data" / "candidate-after.adf").read_bytes()
    reject("unchanged-media", lambda: verify_sectors(before, before))
    # A changed label with a broken checksum is not a persisted valid volume.
    damaged = bytearray(after)
    damaged[880 * 512 + 20] ^= 1
    reject("bad-root-checksum", lambda: verify_sectors(before, damaged))
    damaged_file = bytearray(after)
    damaged_file[900 * 512] ^= 1
    reject("write-outside-root", lambda: verify_sectors(before, damaged_file))
    damaged_root = bytearray(after)
    damaged_root[880 * 512 + 24] ^= 1
    struct.pack_into(">I", damaged_root, 880 * 512 + 20, 0)
    total = sum(struct.unpack_from(">128I", damaged_root, 880 * 512))
    struct.pack_into(">I", damaged_root, 880 * 512 + 20, -total & 0xffffffff)
    reject("changed-root-hash-table-with-valid-checksum", lambda: verify_sectors(before, damaged_root))
    mutations = [
        ("missing-call", lambda r: r["copyInvocationOwnership"].pop()),
        ("wrong-drive", lambda r: event(r, "Relabel").update(Text="RAM:")),
        ("failed-handler", lambda r: event(r, "Relabel").update(ReturnedD0=0)),
        ("wrong-volume-lookup", lambda r: event(r, "FindDosEntry").update(LookupText="RAM")),
        ("missing-parser-release", lambda r: event(r, "FreeArgs").update(D1=1)),
        ("storage-release-wrong-owner", lambda r: event(r, "CopyFreeMem").update(Task=1)),
        ("premature-image-release", lambda r: event(r, "SegmentFreeMem").update(EntryRetireCycle=1)),
        ("export-hash-mismatch", lambda r: r.update(dataDiskFinalSha256="0" * 64)),
    ]
    for name, mutate in mutations:
        report = copy.deepcopy(reports["candidate"])
        mutate(report)
        reject(name, lambda: observe(report, media["candidate"], True))
    result = {"status": "passed", "positivePairs": 1, "inputs": bindings,
              "rejections": rejected, "shippingOrPureApproval": False,
              "verifierSha256": digest(Path(__file__).with_name("verify_relabel_persistent_boot.py")),
              "controlSourceSha256": digest(__file__)}
    args.output.write_text(json.dumps(result, indent=2) + "\n")
    print(args.output)


if __name__ == "__main__":
    main()
