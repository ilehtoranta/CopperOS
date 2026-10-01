"""Validate the named guest-owned probe record using saved chip/slow RAM only.

Discovery follows Exec.PortList. No RAM scanning, ROM lookup, live guest access,
host DOS emulation or command-parity claim is involved. Exit zero means a bound,
complete probe record, not command return zero or compatibility qualification.
"""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
import struct
import sys

NAME = b"CopperOS.CommandProbe.v1\0"
NAME_BYTES = NAME.ljust(32, b"\0")
MAGIC = 0x43505242
HEADER_FIELDS = ("magic", "version", "token", "stage", "commandReturn", "postSystemIoErr",
                 "outputLength", "probeError", "currentProcess", "outputBuffer", "outputCapacity",
                 "publishedPort", "flags", "errorIoErr", "recordBytes", "reserved")
STAGES = {1: "Published", 2: "ArgumentsParsed", 3: "OutputOpened", 4: "SystemRunning",
          5: "SystemReturned", 6: "ReadingOutput", 100: "Complete", 200: "Failed"}
ERRORS = {0: "None", 1: "DosUnavailable", 2: "ReadArgsFailed", 3: "OutputAllocationFailed",
          4: "OutputOpenFailed", 5: "SystemFailed", 6: "SeekFailed", 7: "ReadFailed",
          8: "OutputTruncated", 9: "CloseFailed", 10: "InputOpenFailed", 11: "FlushFailed",
          12: "SelfTaskPlaceholderInvalid"}


class InvalidSnapshot(ValueError):
    pass


def require(condition, message):
    if not condition:
        raise InvalidSnapshot(message)


def identity(path):
    path = Path(path).resolve(strict=True)
    data = path.read_bytes()
    return {"path": str(path), "bytes": len(data), "sha256": hashlib.sha256(data).hexdigest()}


class Memory:
    def __init__(self, chip, slow):
        require(len(chip) == 0x80000 and len(slow) == 0x80000, "Wrong captured RAM extents")
        self.regions = ((0, chip), (0xc00000, slow))

    def read(self, address, count):
        require(isinstance(address, int) and isinstance(count, int) and count >= 0,
                "Invalid memory span")
        for base, data in self.regions:
            offset = address - base
            if 0 <= offset and offset + count <= len(data):
                return data[offset:offset + count]
        raise InvalidSnapshot(f"Uncaptured RAM span {address:08x}+{count}")

    def n(self, address, count=4):
        return int.from_bytes(self.read(address, count), "big")

    def pointer(self, address, count):
        require(address != 0 and address % 2 == 0, f"Null/odd public pointer {address:08x}")
        return self.read(address, count)


def probe_ports(memory, exec_base):
    memory.pointer(exec_base, 406)
    require(memory.n(exec_base + 8, 1) == 9, "ExecBase is not a library node")
    require(memory.n(exec_base + 20, 2) == 40, "ExecBase is not the expected v40 profile")
    address = exec_base + 392  # SDK ExecLayout.ExecBase.PortList
    require(memory.n(address + 4) == 0, "Public PortList tail is not null")
    previous, current = address, memory.n(address)
    seen, matches = set(), []
    while current != address + 4:
        require(current not in seen and len(seen) < 256, "Cyclic/oversized public PortList")
        memory.pointer(current, 34)
        require(memory.n(current + 4) == previous, "Public PortList predecessor mismatch")
        seen.add(current)
        name_pointer = memory.n(current + 10)
        try:
            name = memory.read(name_pointer, len(NAME)) if name_pointer else None
        except InvalidSnapshot:
            # Unrelated port names may live outside captured RAM. Never chase
            # them or infer that an inaccessible name is the requested probe.
            name = None
        if name == NAME:
            matches.append((current, name_pointer))
        previous, current = current, memory.n(current)
    require(memory.n(address + 8) == previous, "Public PortList tail predecessor mismatch")
    return matches


def decode_record(memory, port, record, expected_token):
    require(memory.n(port + 8, 1) == 4, "Probe node is not NT_MSGPORT")
    record_bytes = memory.pointer(record, 96)
    require(record_bytes[:32] == NAME_BYTES, "Probe name/padding differs from layout")
    values = struct.unpack(">16I", record_bytes[32:96])
    header = dict(zip(HEADER_FIELDS, values))
    for field in ("commandReturn", "postSystemIoErr", "errorIoErr"):
        if header[field] >= 0x80000000:
            header[field] -= 0x100000000
    header.update(recordAddress=record, portAddress=port)
    require(header["magic"] == MAGIC and header["version"] == 1, "Bad probe magic/version")
    require(header["recordBytes"] == 96 and header["reserved"] == 0, "Bad record size/reserved field")
    require(header["stage"] in STAGES, "Unknown probe stage")
    # Publication precedes ReadArgs. Stage 1 may still contain cleared token 0;
    # it is never accepted as completion, even when expected_token is also zero.
    before_parse = header["stage"] == 1 or header["stage"] == 200 and not header["flags"] & 1
    require(header["token"] == expected_token or before_parse and header["token"] == 0,
            "Probe token mismatch")
    header["tokenVerified"] = header["token"] == expected_token and not before_parse
    require(header["probeError"] in ERRORS, "Unknown probe error")
    require(header["flags"] & ~31 == 0, "Unknown probe flags")
    require(header["publishedPort"] == port, "Record does not identify its publishing port")
    process = header["currentProcess"]
    memory.pointer(process, 228)
    require(memory.n(process + 8, 1) == 13, "Probe owner is not NT_PROCESS")
    require(memory.n(port + 16) == process, "Port signal owner differs from recorded process")
    task_number = memory.n(process + 140)
    task_priority = memory.n(process + 9, 1)
    task_signals_received = memory.n(process + 26)
    if task_priority >= 0x80:
        task_priority -= 0x100
    header.update(ownerTaskNumber=task_number, ownerTaskPriority=task_priority,
                  ownerTaskSignalsReceived=task_signals_received)
    require(header["outputCapacity"] == 4096, "Unexpected output capacity")
    require(header["outputLength"] <= 4096, "Output length overflows capacity")
    if header["outputBuffer"]:
        allocation = memory.pointer(header["outputBuffer"], 4096)
        require(header["outputBuffer"] + 4096 <= record or record + 96 <= header["outputBuffer"],
                "Output allocation overlaps record")
        output = allocation[:header["outputLength"]]
    else:
        require(header["outputLength"] == 0 and header["stage"] != 100, "Missing output allocation")
        output = b""
    header["stageName"] = STAGES[header["stage"]]
    header["probeErrorName"] = ERRORS[header["probeError"]]
    header["outputSha256"] = hashlib.sha256(output).hexdigest()
    header["outputHex"] = output.hex()
    header["outputLatin1"] = output.decode("latin-1")
    if header["stage"] == 100:
        require(header["flags"] == 15 and header["probeError"] == 0 and header["errorIoErr"] == 0,
                "Complete record has failure/truncation/incomplete flags")
        require(header["commandReturn"] != -1, "Complete record has SystemTagList failure")
    return header


def observe(memory, state, expected_token):
    row = {"frame": state.get("frame"), "cycle": state.get("Cycle"), "pc": state.get("pc"),
           "status": "invalid", "candidateFound": False}
    try:
        require(state.get("UnsupportedActiveFeature") is None, "Unsupported active engine feature")
        if state.get("RomOverlayEnabled") is not False:
            row.update(status="bootstrap", reason="ROM overlay still enabled")
            return row
        base = memory.n(4)
        row["execBase"] = base
        matches = probe_ports(memory, base)
        if not matches:
            row.update(status="missing", reason="Named probe port is absent")
            return row
        row["candidateFound"] = True
        require(len(matches) == 1, "Duplicate probe ports")
        port, record = matches[0]
        header = decode_record(memory, port, record, expected_token)
        row["record"] = header
        if header["stage"] == 100:
            row["status"] = "complete"
        elif header["stage"] == 200:
            row.update(status="failed", reason="Probe reported terminal failure")
        else:
            row.update(status="in-progress", reason="Terminal completion not published")
    except InvalidSnapshot as error:
        row.update(status="invalid", reason=str(error))
    return row


def evaluate(observations):
    errors = []
    if not observations or observations[-1]["status"] != "complete":
        errors.append("Final observation has no valid complete record")
    terminal = None
    for row in observations:
        if row["status"] == "failed" or row["status"] == "invalid" and row.get("candidateFound"):
            errors.append(f"Rejected probe record at frame {row['frame']}: {row.get('reason')}")
        if terminal is not None:
            if row["status"] != "complete" or row.get("record") != terminal:
                errors.append(f"Published complete record changed/disappeared at frame {row['frame']}")
        elif row["status"] == "complete":
            terminal = row["record"]
    return errors


def analyze(capture_path, expected_token):
    capture = json.loads(capture_path.read_text(encoding="utf-8-sig"))
    errors, observations = [], []
    require(capture.get("schemaVersion") == 1 and capture.get("kind") == "workbench31-guest-probe-passive-capture",
            "Unknown capture receipt schema/kind")
    require(capture.get("status") == "capture-complete-not-probe-success" and not capture.get("errors"),
            "Capture did not complete cleanly")
    require(capture.get("process", {}).get("exitCode") == 0, "Capture process exit was not zero")
    require(capture.get("inputsUnchanged") is True and capture.get("inputsBefore") == capture.get("inputsAfter"),
            "Capture input identities changed")
    require(bool(capture.get("inputsBefore")) and bool(capture.get("qualifiedRuntimeBinding")),
            "Capture lacks bound inputs/qualified runtime provenance")
    token = capture.get("invocation", {}).get("token")
    require(type(token) is int and token & 0xffffffff == expected_token, "Invocation token differs from expected token")
    require(isinstance(capture.get("invocation", {}).get("command"), str), "Missing bound command invocation")
    replacement = capture.get("replacement")
    if replacement is not None:
        from capture_probe import bind_command_replacement
        recorded_inputs = {item["path"]: item for item in capture["inputsAfter"]}
        preparation_path = Path(capture["roles"]["derivativeReceipt"]).resolve(strict=True)
        require(recorded_inputs.get(str(preparation_path)) == identity(preparation_path),
                "Replacement preparation receipt is not unchanged and capture-bound")
        preparation = json.loads(preparation_path.read_text(encoding="utf-8-sig"))
        require(preparation.get("schema_version") == 1
                and preparation.get("kind") == "workbench31-diagnostic-probe-adf"
                and preparation.get("status") == "prepared" and preparation.get("replacement") == replacement,
                "Capture replacement differs from successful preparation")
        require(preparation.get("invocation") == capture["invocation"],
                "Candidate invocation differs from preparation")
        verified, paths = bind_command_replacement(
            preparation, Path(replacement["candidate"]["path"]), replacement.get("guest_path"))
        require(verified == replacement and all(recorded_inputs.get(str(path)) == identity(path) for path in paths),
                "Replacement source/snapshot identities were not bound by capture")
    expected = capture.get("expectedFrames")
    require(isinstance(expected, list) and expected and expected == sorted(set(expected)), "Invalid frame schedule")
    snapshot_dir = Path(capture["snapshotDirectory"]).resolve(strict=True)
    bound_files = capture.get("snapshotIdentities", [])
    require(bound_files and len({item["path"] for item in bound_files}) == len(bound_files), "Missing/duplicate snapshot identities")
    by_path = {}
    for item in bound_files:
        path = Path(item["path"]).resolve(strict=True)
        require(path.parent == snapshot_dir, "Snapshot identity escapes capture directory")
        require(identity(path) == item, f"Snapshot hash/length changed: {path.name}")
        by_path[path] = item
    paths = sorted(snapshot_dir.glob("frame-*.json"))
    require([int(path.stem.removeprefix("frame-")) for path in paths] == expected, "Actual frame schedule differs")
    for path in paths:
        state = json.loads(path.read_text(encoding="utf-8-sig"))
        frame = state.get("frame")
        require(type(frame) is int and path.stem == f"frame-{frame:06d}" and state.get("CompletedFrames") == frame,
                "Frame JSON identity mismatch")
        inputs = [path, path.with_suffix(".chipram"), path.with_suffix(".slowram")]
        require(all(item in by_path for item in inputs), "Snapshot bytes are not all bound by capture receipt")
        memory = Memory(inputs[1].read_bytes(), inputs[2].read_bytes())
        row = observe(memory, state, expected_token)
        row["provenance"] = [by_path[item] for item in inputs]
        observations.append(row)
    errors.extend(evaluate(observations))
    return {
        "schemaVersion": 1, "kind": "workbench31-guest-probe-passive-analysis",
        "status": "complete-probe-record-observed" if not errors else "probe-not-established",
        "capture": identity(capture_path), "analyzer": identity(Path(__file__)),
        "expectedToken": expected_token, "invocation": capture["invocation"],
        "replacement": replacement,
        "captureProvenance": {"roles": capture.get("roles"), "inputCount": len(capture["inputsBefore"]),
                              "qualifiedRuntimeBinding": capture["qualifiedRuntimeBinding"]},
        "errors": errors, "observations": observations,
        "result": observations[-1].get("record") if not errors else None,
        "scope": "Saved public chip/slow RAM only, discovered through Exec.PortList and the named guest-owned port. A complete probe record is not a command-parity qualification.",
        "postSystemIoErrMeaning": "Immediate caller DOS.IoErr after SystemTagList; not proof of child pr_Result2 or command secondary-error parity.",
        "commandParityClaim": False,
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--capture", type=Path, required=True)
    parser.add_argument("--expected-token", type=lambda value: int(value, 0), required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    if not 0 <= args.expected_token <= 0xffffffff:
        parser.error("Expected token must be unsigned 32-bit")
    if args.output.exists() or not args.output.parent.is_dir():
        parser.error("Analysis output must be a fresh file under an existing directory")
    try:
        report = analyze(args.capture.resolve(strict=True), args.expected_token)
    except (OSError, ValueError, KeyError, TypeError) as error:
        report = {"schemaVersion": 1, "kind": "workbench31-guest-probe-passive-analysis",
                  "status": "analysis-rejected", "errors": [str(error)], "result": None,
                  "expectedToken": args.expected_token, "commandParityClaim": False}
    with args.output.open("x", encoding="utf-8") as stream:
        json.dump(report, stream, indent=2)
        stream.write("\n")
    print(json.dumps({"status": report["status"], "output": str(args.output), "errors": report["errors"]}, indent=2))
    return 0 if report["status"] == "complete-probe-record-observed" else 1


if __name__ == "__main__":
    sys.exit(main())
