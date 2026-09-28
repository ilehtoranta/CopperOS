using System.Text;
using Amiga;
using CopperOS.Commands.Native;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record DiskChangeEntryCase(string Device,
    bool DeviceFound = true, int FirstPacketResult = -1,
    int SecondPacketResult = -1, int Error = 0, int ParserError = 0);

internal sealed record DiskChangeNativeLayout(uint RdArgs, uint Port)
{
    public uint Control => RdArgs;
}

internal sealed partial class ProbeFixture
{
    public const string DiskChangeEntrySuite =
        "morphos320-diskchange-native-entry-vector-fixture";
    public const string Workbench31DiskChangeEntrySuite =
        "workbench31-diskchange-native-entry-vector-fixture";

    private List<object> RunDiskChangeEntryCases()
    {
        ProbeCase[] cases =
        [
            DiskCase("disk-change", "DF0:"),
            DiskCase("disk-change-second-packet-ignored", "DF0:",
                secondPacketResult: 0),
            DiskCase("device-missing", "Missing:", result: DOS.RETURN_FAIL,
                error: 205, found: false),
            DiskCase("inhibit-failure", "DF0:", result: DOS.RETURN_FAIL,
                error: (int)DOS.Error.DiskWriteProtected,
                firstPacketResult: 0),
            DiskCase("parser-failure", "DF0:", result: DOS.RETURN_ERROR,
                error: 116, parserError: 116),
            new("workbench-startup", "", DOS.RETURN_ERROR,
                (int)DOS.Error.ObjectWrongType, "")
            { DiskChange = new("DF0:"), Workbench = true },
            new("negative-entry-length", "", DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, "")
            { DiskChange = new("DF0:"), EntryLength = -1 },
            new("null-entry-buffer", "", DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, "")
            { DiskChange = new("DF0:"), EntryLength = 3,
                NullArgumentPointer = true },
            new("missing-dos", "", DOS.RETURN_FAIL,
                Invocation.InitialIoError, "")
            { DiskChange = new("DF0:"), MissingDos = true },
        ];

        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            DiskCase("interleaved-left", "DF0:"),
            DiskCase("interleaved-right", "DH0:")
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private static ProbeCase DiskCase(string name, string device,
        int result = DOS.RETURN_OK, int error = 0, bool found = true,
        int firstPacketResult = -1, int secondPacketResult = -1,
        int parserError = 0, string expected = "") => new(name, device,
            result, error, expected)
        {
            DiskChange = new(device, found, firstPacketResult,
                secondPacketResult, error, parserError)
        };

    private void RegisterDiskChangeEntryDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs", (state, invocation) =>
        {
            var definition = invocation.Definition.DiskChange!;
            Require(Bus.CString(state.D[1]) ==
                NativeMorphOSDiskChangeCommand.Template && state.D[3] == 0,
                "DiskChange ReadArgs ABI differs.");
            var results = state.D[2];
            Require(Bus.OwnedAllocation(invocation, results, "Exec").Size == 4 &&
                Bus.Long(results) == 0,
                "DiskChange result storage differs.");
            invocation.Reads++;
            if (definition.ParserError != 0)
            {
                invocation.IoError = definition.ParserError;
                return 0;
            }
            var rdArgs = Bus.Allocate(invocation, 64, "RDArgs", true);
            var device = rdArgs + 32;
            PutDiskChangeString(device, definition.Device);
            Bus.Long(results, device);
            invocation.DiskChangeLayout =
                new(rdArgs, 0x44000u + (uint)(invocation.Slot * 4));
            return rdArgs;
        });
        Register(baseAddress, DosLvo.FreeArgs, "FreeArgs", (state, invocation) =>
        {
            Bus.Release(invocation, state.D[1], "RDArgs");
            invocation.FreeArgs++;
            return 0;
        });
        Register(baseAddress, DosLvo.DeviceProc, "DeviceProc",
            (state, invocation) =>
            {
                var definition = invocation.Definition.DiskChange!;
                Require(Bus.CString(state.D[1]) == definition.Device,
                    "DiskChange DeviceProc ABI differs.");
                invocation.DiskChangeDeviceProcCalls++;
                if (!definition.DeviceFound)
                {
                    invocation.IoError = definition.Error;
                    return 0;
                }
                return invocation.DiskChangeLayout!.Port;
            });
        Register(baseAddress, DosLvo.DoPkt, "DoPkt", (state, invocation) =>
        {
            var definition = invocation.Definition.DiskChange!;
            var layout = invocation.DiskChangeLayout!;
            Require(state.D[1] == layout.Port && state.D[2] == 31 &&
                state.D[4] == 0 && state.D[5] == 0 && state.D[6] == 0 &&
                state.D[7] == 0,
                "DiskChange DoPkt ABI differs.");
            var call = invocation.DiskChangePacketCalls++;
            Require(state.D[3] == (call == 0 ? uint.MaxValue : 0),
                "DiskChange inhibit state differs.");
            if (call == 0)
            {
                if (definition.FirstPacketResult == 0)
                    invocation.IoError = definition.Error;
                return unchecked((uint)definition.FirstPacketResult);
            }
            return unchecked((uint)definition.SecondPacketResult);
        });
        Register(baseAddress, DosLvo.PrintFault, "PrintFault",
            (state, invocation) =>
            {
                var definition = invocation.Definition.DiskChange!;
                var header = Bus.CString(state.D[2]);
                var expected = definition.ParserError != 0
                    ? "DiskChange"
                    : !definition.DeviceFound
                        ? "error while searching the device"
                        : "error while inhibiting the device";
                Require(header == expected,
                    "DiskChange fault header differs.");
                invocation.DiskChangePrintFaultCalls++;
                return 0;
            });
        Register(baseAddress, DosLvo.IoErr, "IoErr", (_, invocation) =>
            unchecked((uint)invocation.IoError));
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr", (state, invocation) =>
        {
            invocation.IoError = unchecked((int)state.D[1]);
            return 0;
        });
    }

    private void VerifyDiskChangeEntry(Invocation invocation)
    {
        var definition = invocation.Definition.DiskChange!;
        if (invocation.Definition.Workbench ||
            invocation.Definition.EntryLength is < 0 ||
            invocation.Definition.NullArgumentPointer ||
            invocation.Definition.MissingDos)
        {
            Require(invocation.Reads == 0 &&
                invocation.DiskChangeDeviceProcCalls == 0,
                "DiskChange parsed an invalid entry boundary.");
            return;
        }
        var parsed = definition.ParserError != 0;
        Require(invocation.Reads == 1 && invocation.FreeArgs == (parsed ? 0 : 1),
            "DiskChange parser lifetime differs.");
        var found = !parsed && definition.DeviceFound;
        Require(invocation.DiskChangeDeviceProcCalls == (parsed ? 0 : 1),
            "DiskChange device lookup count differs.");
        var packets = found ? definition.FirstPacketResult == 0 ? 1 : 2 : 0;
        Require(invocation.DiskChangePacketCalls == packets,
            "DiskChange packet count differs.");
        Require(invocation.DiskChangePrintFaultCalls ==
            (parsed || !definition.DeviceFound || definition.FirstPacketResult == 0
                ? 1 : 0),
            "DiskChange fault count differs.");
        invocation.DiskChangeLayout = null;
    }

    private void PutDiskChangeString(uint address, string value)
    {
        var bytes = Encoding.Latin1.GetBytes(value);
        bytes.CopyTo(Bus.Memory.AsSpan((int)address));
        Bus.Memory[address + (uint)bytes.Length] = 0;
    }
}
