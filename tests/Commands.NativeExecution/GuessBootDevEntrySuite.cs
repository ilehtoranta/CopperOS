using System.Text;
using Amiga;
using CopperOS.Commands.Native;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record GuessBootDevNodeCase(
    string Name,
    int Priority,
    bool Disabled = false,
    bool Usable = true);

internal sealed record GuessBootDevEntryCase(
    string? RequestedName = "Install3.1",
    bool RequestedLockAvailable = true,
    bool RequestedMatches = true,
    bool SystemLockAvailable = true,
    bool UtilityAvailable = true,
    bool ExpansionAvailable = true,
    int ParserError = 0,
    string FallbackName = "SYS",
    GuessBootDevNodeCase[]? Nodes = null);

internal sealed record GuessBootDevNativeLayout(uint Control, uint Requested,
    uint Expansion, IReadOnlyList<(uint Address, string Kind)> FixtureAllocations);

internal sealed partial class Invocation
{
    public uint GuessBootDevExpansionBase { get; set; }
    public GuessBootDevNativeLayout? GuessBootDevLayout { get; set; }
    public int GuessBootDevUtilityOpens { get; set; }
    public int GuessBootDevUtilityCloses { get; set; }
    public int GuessBootDevExpansionOpens { get; set; }
    public int GuessBootDevExpansionCloses { get; set; }
    public int GuessBootDevReadArgsCalls { get; set; }
    public int GuessBootDevFreeArgsCalls { get; set; }
    public int GuessBootDevPrintFaultCalls { get; set; }
    public int GuessBootDevLockCalls { get; set; }
    public int GuessBootDevUnlockCalls { get; set; }
    public int GuessBootDevSameLockCalls { get; set; }
    public int GuessBootDevNameFromLockCalls { get; set; }
    public int GuessBootDevPutStrCalls { get; set; }
    public int GuessBootDevIoErrCalls { get; set; }
    public int GuessBootDevSetIoErrCalls { get; set; }
    public uint GuessBootDevSystemLock { get; } = 0x120;
    public uint GuessBootDevRequestedLock { get; } = 0x130;
}

internal sealed partial class ProbeFixture
{
    public const string GuessBootDevEntrySuite =
        "workbench31-guessbootdev-native-entry-vector-fixture";

    private List<object> RunGuessBootDevEntryCases()
    {
        ProbeCase[] cases =
        [
            GuessBootDevCase("selects-highest-priority", new(
                Nodes: [new("DH0", 5), new("DH1", 2)]), "DH0:\n"),
            GuessBootDevCase("selects-signed-priority", new(
                Nodes: [new("DH0", -1), new("DH1", 7)]), "DH1:\n"),
            GuessBootDevCase("fallback-without-request", new(
                RequestedName: null, RequestedLockAvailable: false), "SYS:\n"),
            GuessBootDevCase("fallback-different-lock", new(
                RequestedMatches: false, Nodes: [new("DH0", 9)]), "SYS:\n"),
            GuessBootDevCase("ignores-disabled-and-unusable", new(
                Nodes: [new("DH0", 9, Disabled: true), new("DH1", 8, Usable: false)]), "SYS:\n"),
            GuessBootDevCase("missing-system-lock", new(SystemLockAvailable: false), ""),
            GuessBootDevCase("missing-requested-lock", new(RequestedLockAvailable: false), "SYS:\n"),
            GuessBootDevCase("missing-utility", new(UtilityAvailable: false), ""),
            GuessBootDevCase("missing-expansion", new(ExpansionAvailable: false), ""),
            GuessBootDevCase("parser-failure", new(ParserError: 116), "",
                result: DOS.RETURN_FAIL, error: 116),
            new("workbench-startup", "", DOS.RETURN_ERROR,
                (int)DOS.Error.ObjectWrongType, "")
            { GuessBootDev = new(), Workbench = true },
            new("negative-entry-length", "", DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, "")
            { GuessBootDev = new(), EntryLength = -1 },
            new("null-entry-buffer", "", DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, "")
            { GuessBootDev = new(), EntryLength = 4, NullArgumentPointer = true },
            new("missing-dos", "", DOS.RETURN_FAIL,
                Invocation.InitialIoError, "")
            { GuessBootDev = new(), MissingDos = true },
        ];

        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            GuessBootDevCase("interleaved-dh0", new(
                Nodes: [new("DH0", 2), new("DH1", 1)]), "DH0:\n"),
            GuessBootDevCase("interleaved-fallback", new(
                RequestedMatches: false, Nodes: [new("DH2", 4)]), "SYS:\n")
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private static ProbeCase GuessBootDevCase(string name,
        GuessBootDevEntryCase definition, string output,
        int result = DOS.RETURN_FAIL, int error = 0) =>
        new(name, definition.RequestedName ?? "", result, error, output)
        { GuessBootDev = definition };

    private void PrepareGuessBootDevEntry(Invocation invocation)
    {
        var definition = invocation.Definition.GuessBootDev ?? new();
        var control = invocation.Arguments + 0x300;
        var requested = invocation.Arguments + 0x340;
        var allocations = new List<(uint Address, string Kind)>();
        var expansion = Bus.Allocate(invocation, 0x100, "GuessBootDevExpansion", true);
        allocations.Add((expansion, "GuessBootDevExpansion"));
        if (definition.RequestedName is not null)
        {
            WriteCString(requested, definition.RequestedName);
            Bus.Long(control, requested);
        }
        else Bus.Long(control, 0);

        uint previousBoot = 0;
        var nodes = definition.Nodes ?? [];
        foreach (var node in nodes)
        {
            var boot = Bus.Allocate(invocation, 0x20, "GuessBootDevBootNode", true);
            var device = Bus.Allocate(invocation, 0x40, "GuessBootDevDeviceNode", true);
            var startup = Bus.Allocate(invocation, 0x10, "GuessBootDevStartup", true);
            var environment = Bus.Allocate(invocation, 0x80, "GuessBootDevEnvironment", true);
            var bstr = Bus.Allocate(invocation,
                checked((uint)Encoding.Latin1.GetByteCount(node.Name) + 1),
                "GuessBootDevBString", true);
            allocations.Add((boot, "GuessBootDevBootNode"));
            allocations.Add((device, "GuessBootDevDeviceNode"));
            allocations.Add((startup, "GuessBootDevStartup"));
            allocations.Add((environment, "GuessBootDevEnvironment"));
            allocations.Add((bstr, "GuessBootDevBString"));
            if (previousBoot == 0)
                Bus.Long(expansion + NativeGuessBootDevExpansionBootNodeOffset, boot);
            else Bus.Long(previousBoot, boot);
            previousBoot = boot;
            Bus.Memory[boot + 8] = 0x10;
            Bus.Memory[boot + 9] = unchecked((byte)(sbyte)node.Priority);
            Bus.Long(boot + 0x10, device);
            Bus.Memory[device + 0x10] = node.Disabled ? (byte)0x80 : (byte)0;
            Bus.Long(device + 0x1c, startup >> 2);
            Bus.Long(device + 0x28, bstr >> 2);
            Bus.Long(startup + (uint)DosLayout.FileSysStartupMsg.Environment,
                environment >> 2);
            Bus.Long(environment + (uint)DosLayout.DosEnvec.TableSize,
                node.Usable ? 0x13u : 0x12u);
            Bus.Long(environment + NativeGuessBootDevEnvironmentRequiredOffset,
                node.Usable ? 1u : 0u);
            var bytes = Encoding.Latin1.GetBytes(node.Name);
            Bus.Memory[bstr] = checked((byte)bytes.Length);
            bytes.CopyTo(Bus.Memory.AsSpan((int)bstr + 1));
        }
        if (previousBoot != 0) Bus.Long(previousBoot, 0);
        Bus.Long(control, definition.RequestedName is null ? 0u : requested);
        invocation.GuessBootDevExpansionBase = expansion;
        invocation.GuessBootDevLayout = new(control, definition.RequestedName is null ? 0u : requested,
            expansion, allocations);
    }

    private void RegisterGuessBootDevEntryDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs", (state, invocation) =>
        {
            var definition = invocation.Definition.GuessBootDev!;
            Require(Bus.CString(state.D[1]) == NativeWorkbench31GuessBootDevCommand.Template &&
                state.D[3] == 0 && state.D[2] % 4 == 0 &&
                Bus.OwnedAllocation(invocation, state.D[2], "Exec").Size == 4 &&
                Bus.Long(state.D[2]) == 0,
                "GuessBootDev ReadArgs ABI differs.");
            invocation.GuessBootDevReadArgsCalls++;
            if (definition.ParserError != 0)
            {
                invocation.IoError = definition.ParserError;
                return 0;
            }
            Bus.Long(state.D[2], invocation.GuessBootDevLayout!.Requested);
            return Bus.Allocate(invocation, 40, "RDArgs", true);
        });
        Register(baseAddress, DosLvo.FreeArgs, "FreeArgs", (state, invocation) =>
        {
            Bus.Release(invocation, state.D[1], "RDArgs");
            invocation.GuessBootDevFreeArgsCalls++;
            invocation.IoError = 901;
            return 0xf4ee;
        });
        Register(baseAddress, DosLvo.Lock, "LockRaw", (state, invocation) =>
        {
            Require(state.D[2] == unchecked((uint)DOS.LockMode.Shared),
                "GuessBootDev Lock mode differs.");
            invocation.GuessBootDevLockCalls++;
            var definition = invocation.Definition.GuessBootDev!;
            var path = Bus.CString(state.D[1]);
            if (path == "SYS:")
                return definition.SystemLockAvailable ? invocation.GuessBootDevSystemLock : 0u;
            return definition.RequestedLockAvailable ? invocation.GuessBootDevRequestedLock : 0u;
        });
        Register(baseAddress, DosLvo.SameLock, "SameLock", (state, invocation) =>
        {
            Require(state.D[1] == invocation.GuessBootDevSystemLock &&
                state.D[2] == invocation.GuessBootDevRequestedLock,
                "GuessBootDev SameLock handles differ.");
            invocation.GuessBootDevSameLockCalls++;
            return invocation.Definition.GuessBootDev!.RequestedMatches ? 0u : 1u;
        });
        Register(baseAddress, DosLvo.UnLock, "UnLock", (state, invocation) =>
        {
            Require(state.D[1] == invocation.GuessBootDevSystemLock ||
                state.D[1] == invocation.GuessBootDevRequestedLock,
                "GuessBootDev unlocked an unknown lock.");
            invocation.GuessBootDevUnlockCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.NameFromLock, "NameFromLock", (state, invocation) =>
        {
            Require(state.D[1] == invocation.GuessBootDevSystemLock &&
                state.D[3] == 300 && state.D[2] != 0,
                "GuessBootDev NameFromLock ABI differs.");
            invocation.GuessBootDevNameFromLockCalls++;
            WriteCString(state.D[2], invocation.Definition.GuessBootDev!.FallbackName);
            return 1;
        });
        Register(baseAddress, DosLvo.PutStr, "PutStr", (state, invocation) =>
        {
            invocation.GuessBootDevPutStrCalls++;
            invocation.Output.Write(Encoding.Latin1.GetBytes(Bus.CString(state.D[1])));
            return 0;
        });
        Register(baseAddress, DosLvo.PrintFault, "PrintFault", (_, invocation) =>
        {
            invocation.GuessBootDevPrintFaultCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.IoErr, "IoErr", (_, invocation) =>
        {
            invocation.GuessBootDevIoErrCalls++;
            return unchecked((uint)invocation.IoError);
        });
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr", (state, invocation) =>
        {
            invocation.GuessBootDevSetIoErrCalls++;
            invocation.IoError = unchecked((int)state.D[1]);
            return 0;
        });
    }

    private void RegisterGuessBootDevEntryExec()
    {
        // OpenLibrary leases are registered by the shared Exec gateway;
        // GuessBootDev has no additional Exec vectors.
    }

    private void VerifyGuessBootDevEntry(Invocation invocation)
    {
        var definition = invocation.Definition.GuessBootDev!;
        var boundary = invocation.Definition.Workbench ||
            invocation.Definition.EntryLength is < 0 ||
            invocation.Definition.NullArgumentPointer ||
            invocation.Definition.MissingDos;
        if (!boundary)
        {
            if (!definition.UtilityAvailable || !definition.ExpansionAvailable)
            {
                Require(invocation.GuessBootDevReadArgsCalls == 0 &&
                    invocation.GuessBootDevPutStrCalls == 0 &&
                    invocation.GuessBootDevUtilityOpens == 1 &&
                    invocation.GuessBootDevUtilityCloses == (definition.UtilityAvailable ? 1 : 0) &&
                    invocation.GuessBootDevExpansionOpens == (definition.UtilityAvailable ? 1 : 0) &&
                    invocation.GuessBootDevExpansionCloses == 0 &&
                    invocation.Allocations == 0 && invocation.FreeMem == 0,
                    "GuessBootDev library-open failure crossed the parser path.");
                goto releaseFixture;
            }
            var parser = definition.ParserError != 0;
            Require(invocation.GuessBootDevReadArgsCalls == 1 &&
                invocation.GuessBootDevFreeArgsCalls == (parser ? 0 : 1) &&
                invocation.Allocations == (parser ? 1 : 2) &&
                invocation.FreeMem == (parser ? 1 : 2),
                "GuessBootDev parser/result ownership differs.");
            if (parser)
            {
                Require(invocation.GuessBootDevPrintFaultCalls == 1 &&
                    invocation.GuessBootDevLockCalls == 0 &&
                    invocation.GuessBootDevPutStrCalls == 0,
                    "GuessBootDev parser failure crossed the filesystem path.");
            }
            else
            {
                Require(invocation.GuessBootDevUtilityOpens == 1 &&
                    invocation.GuessBootDevUtilityCloses == invocation.GuessBootDevUtilityOpens &&
                    invocation.GuessBootDevExpansionOpens == (definition.UtilityAvailable && definition.ExpansionAvailable ? 1 : 0) &&
                    invocation.GuessBootDevExpansionCloses == invocation.GuessBootDevExpansionOpens,
                    "GuessBootDev library lifetime differs.");
                if (definition.UtilityAvailable && definition.ExpansionAvailable)
                {
                    var expectedUnlocks = definition.SystemLockAvailable
                        ? 1 + (definition.RequestedName is not null &&
                            definition.RequestedLockAvailable ? 1 : 0)
                        : 0;
                    Require(invocation.GuessBootDevLockCalls >= 1 &&
                        invocation.GuessBootDevUnlockCalls == expectedUnlocks &&
                        invocation.GuessBootDevPutStrCalls <= 1,
                        $"GuessBootDev lock/output lifecycle differs (name={invocation.Definition.Name}, locks={invocation.GuessBootDevLockCalls}, unlocks={invocation.GuessBootDevUnlockCalls}, same={invocation.GuessBootDevSameLockCalls}, nameFrom={invocation.GuessBootDevNameFromLockCalls}, put={invocation.GuessBootDevPutStrCalls}).");
                    Require(invocation.GuessBootDevPutStrCalls ==
                        (invocation.Definition.Output.Length == 0 ? 0 : 1),
                        "GuessBootDev output count differs.");
                }
            }
        }
        else Require(invocation.GuessBootDevReadArgsCalls == 0 &&
            invocation.GuessBootDevLockCalls == 0 &&
            invocation.GuessBootDevPutStrCalls == 0,
            "GuessBootDev crossed an invalid startup boundary.");

    releaseFixture:
        foreach (var (address, kind) in invocation.GuessBootDevLayout?.FixtureAllocations ?? [])
            Bus.Release(invocation, address, kind);
    }

    private const uint NativeGuessBootDevExpansionBootNodeOffset = 0x4a;
    private const uint NativeGuessBootDevEnvironmentRequiredOffset = 0x4c;
}
