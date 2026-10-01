using System.Text;
using Amiga;
using CopperOS.Commands.Native;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record LockEntryCase(string Drive, bool On, bool Off,
    string Passkey, bool DeviceFound = true, uint DeviceType = 2,
    int PacketResult = -1, int Error = 0, int ParserError = 0);

internal sealed record LockNativeLayout(uint DeviceProc, uint DeviceNode,
    uint Port)
{
    public uint Control => DeviceProc;
}

internal sealed partial class ProbeFixture
{
    public const string LockEntrySuite =
        "morphos320-lock-native-entry-vector-fixture";
    public const string Workbench31LockEntrySuite =
        "workbench31-lock-native-entry-vector-fixture";

    private List<object> RunLockEntryCases()
    {
        ProbeCase[] cases =
        [
            Case("lock-on", "Work:", on: true, off: false,
                expected: "Work: locked\n"),
            Case("unlock-off", "Work:", on: false, off: true,
                expected: "Work: unlocked\n"),
            Case("passkey", "Work:", on: true, off: false,
                passkey: "123", expected: "Work: locked\n"),
            Case("no-action", "Work:", on: false, off: false),
            Case("wrong-device-type", "Work:", on: true, off: false,
                result: DOS.RETURN_FAIL, error: (int)DOS.Error.ObjectWrongType,
                deviceType: 3),
            Case("missing-device", "Missing:", on: true, off: false,
                result: DOS.RETURN_FAIL, error: 205, found: false),
            Case("packet-failure", "Work:", on: true, off: false,
                result: DOS.RETURN_FAIL, error: (int)DOS.Error.DiskWriteProtected,
                packetResult: 0,
                expected: "Attempt to lock drive Work: failed\n"),
            Case("parser-failure", "Work:", on: true, off: false,
                result: DOS.RETURN_ERROR, error: 116, parserError: 116),
            new("workbench-startup", "", DOS.RETURN_ERROR,
                (int)DOS.Error.ObjectWrongType, "")
            { Lock = new("Work:", true, false, "") , Workbench = true },
        ];

        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            Case("repeat-lock", "Work:", on: true, off: false,
                passkey: "123", expected: "Work: locked\n"),
            Case("repeat-unlock", "Work:", on: false, off: true,
                passkey: "123", expected: "Work: unlocked\n")
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private static ProbeCase Case(string name, string drive, bool on, bool off,
        string passkey = "", int result = DOS.RETURN_OK, int error = 0,
        bool found = true, uint deviceType = 2, int packetResult = -1,
        int parserError = 0, string expected = "") => new(name, drive,
            result, error, expected)
        {
            Lock = new(drive, on, off, passkey, found, deviceType,
                packetResult, error, parserError)
        };

    private void RegisterLockEntryDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs", (state, invocation) =>
        {
            var definition = invocation.Definition.Lock!;
            Require(Bus.CString(state.D[1]) ==
                NativeMorphOSLockCommand.Template && state.D[3] == 0,
                "Lock ReadArgs ABI differs.");
            var results = state.D[2];
            Require(Bus.OwnedAllocation(invocation, results, "Exec").Size ==
                NativeMorphOSLockCommand.ResultCount * 4,
                "Lock result storage differs.");
            for (var offset = 0u; offset < 16; offset += 4)
                Require(Bus.Long(results + offset) == 0,
                    "Lock ReadArgs defaults must be zeroed.");
            invocation.Reads++;
            if (definition.ParserError != 0)
            {
                invocation.IoError = definition.ParserError;
                return 0;
            }

            var rdArgs = Bus.Allocate(invocation, 96, "RDArgs", true);
            var drive = rdArgs + 32;
            var passkey = rdArgs + 64;
            PutString(drive, definition.Drive);
            if (definition.Passkey.Length != 0)
                PutString(passkey, definition.Passkey);
            Bus.Long(results, drive);
            Bus.Long(results + 4, definition.On ? 1u : 0u);
            Bus.Long(results + 8, definition.Off ? 1u : 0u);
            Bus.Long(results + 12, definition.Passkey.Length == 0 ? 0 : passkey);
            return rdArgs;
        });
        Register(baseAddress, DosLvo.FreeArgs, "FreeArgs", (state, invocation) =>
        {
            Bus.Release(invocation, state.D[1], "RDArgs");
            invocation.FreeArgs++;
            return 0;
        });
        Register(baseAddress, DosLvo.GetDeviceProc, "GetDeviceProc",
            (state, invocation) =>
            {
                var definition = invocation.Definition.Lock!;
                Require(state.D[2] == 0 &&
                    Bus.CString(state.D[1]) == definition.Drive,
                    "Lock GetDeviceProc ABI differs.");
                invocation.LockDeviceProcCalls++;
                if (!definition.DeviceFound)
                {
                    invocation.IoError = definition.Error;
                    return 0;
                }
                var deviceProc = Bus.Allocate(invocation, 64,
                    "LockDeviceProc", true);
                var node = deviceProc + 32;
                var port = 0x44000u + (uint)(invocation.LockDeviceProcCalls * 4);
                Bus.Long(deviceProc, port);
                Bus.Long(deviceProc + 12, node);
                Bus.Long(node + 4, definition.DeviceType);
                invocation.LockLayout = new(deviceProc, node, port);
                return deviceProc;
            });
        Register(baseAddress, DosLvo.FreeDeviceProc, "FreeDeviceProc",
            (state, invocation) =>
            {
                Require(invocation.LockLayout is not null &&
                    state.D[1] == invocation.LockLayout.DeviceProc &&
                    invocation.LockFreeDeviceProcCalls == 0,
                    "Lock FreeDeviceProc ownership differs.");
                Bus.Release(invocation, state.D[1], "LockDeviceProc");
                invocation.LockFreeDeviceProcCalls++;
                return 0;
            });
        Register(baseAddress, DosLvo.DoPkt, "DoPkt", (state, invocation) =>
        {
            var definition = invocation.Definition.Lock!;
            var layout = invocation.LockLayout!;
            var expectedKey = 0u;
            foreach (var character in Encoding.Latin1.GetBytes(definition.Passkey))
                expectedKey = expectedKey * 10u + character;
            Require(state.D[1] == layout.Port && state.D[2] == 1023 &&
                (state.D[3] == (definition.On ? uint.MaxValue : 0)) &&
                state.D[4] == expectedKey,
                "Lock DoPkt2 ABI differs.");
            invocation.LockPacketCalls++;
            invocation.LockPacketArgument = unchecked((int)state.D[3]);
            invocation.LockPacketKey = state.D[4];
            if (definition.PacketResult == 0)
                invocation.IoError = definition.Error;
            return unchecked((uint)definition.PacketResult);
        });
        Register(baseAddress, DosLvo.VPrintf, "VPrintf", (state, invocation) =>
        {
            var definition = invocation.Definition.Lock!;
            var format = Bus.CString(state.D[1]);
            Require(format is "%s locked\n" or "%s unlocked\n" or
                "Attempt to lock drive %s failed\n",
                "Lock display format differs.");
            var drive = Bus.CString(state.D[2]);
            Require(drive == definition.Drive, "Lock display argument differs.");
            invocation.Output.Write(Encoding.Latin1.GetBytes(
                format.StartsWith("Attempt", StringComparison.Ordinal)
                    ? $"Attempt to lock drive {drive} failed\n"
                    : $"{drive} {(format.Contains("unlocked") ? "unlocked" : "locked")}\n"));
            return 0;
        });
        Register(baseAddress, DosLvo.IoErr, "IoErr", (_, invocation) =>
            unchecked((uint)invocation.IoError));
        Register(baseAddress, DosLvo.PrintFault, "PrintFault",
            (state, invocation) =>
            {
                Require(Bus.CString(state.D[2]) == "Lock",
                    "Lock fault name differs.");
                invocation.LockPrintFaultCalls++;
                return 0;
            });
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr", (state, invocation) =>
        {
            invocation.IoError = unchecked((int)state.D[1]);
            return 0;
        });
    }

    private void VerifyLockEntry(Invocation invocation)
    {
        var definition = invocation.Definition.Lock!;
        if (invocation.Definition.Workbench)
        {
            Require(invocation.Reads == 0 && invocation.LockDeviceProcCalls == 0,
                "Lock parsed a Workbench startup message.");
            return;
        }
        var parsed = definition.ParserError != 0;
        Require(invocation.Reads == 1 && invocation.FreeArgs == (parsed ? 0 : 1),
            "Lock parser lifetime differs.");
        Require(invocation.LockDeviceProcCalls == (parsed ? 0 : 1),
            "Lock device lookup count differs.");
        var packet = !parsed && definition.DeviceFound &&
            definition.DeviceType <= (uint)DosListType.Volume &&
            (definition.On || definition.Off);
        Require(invocation.LockPacketCalls == (packet ? 1 : 0),
            "Lock packet count differs.");
        Require(invocation.LockFreeDeviceProcCalls == (parsed ||
            !definition.DeviceFound ? 0 : 1),
            "Lock device-process lifetime differs.");
        Require(invocation.LockPrintFaultCalls == (invocation.IoError != 0 ? 1 : 0),
            "Lock fault count differs.");
        if (packet)
            Require(invocation.LockPacketKey == ExpectedLockKey(definition.Passkey),
                "Lock passkey conversion differs.");
        invocation.LockLayout = null;
    }

    private static uint ExpectedLockKey(string passkey)
    {
        var key = 0u;
        foreach (var character in Encoding.Latin1.GetBytes(passkey))
            key = key * 10u + character;
        return key;
    }

    private void PutString(uint address, string value)
    {
        var bytes = Encoding.Latin1.GetBytes(value);
        bytes.CopyTo(Bus.Memory.AsSpan((int)address));
        Bus.Memory[address + (uint)bytes.Length] = 0;
    }
}
