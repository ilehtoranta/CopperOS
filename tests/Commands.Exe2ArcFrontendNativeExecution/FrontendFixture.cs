using System.Buffers.Binary;
using System.Text;
using Amiga;
using Copper68k;
using CopperOS.Commands.NativeExecution;

namespace CopperOS.Commands.Exe2ArcFrontendNativeExecution;

internal sealed class FrontendFixture(HunkImage image, M68kCpuModel model)
{
    public const uint LoadAddress = 0x100000;
    public const uint ReturnAddress = 0x2000;
    private readonly FrontendBus bus = new();
    public List<object> Results { get; } = [];
    public object? LastFailure { get; private set; }

    public void Run()
    {
        bus.LoadAndProtect(LoadAddress, image.Code);
        RegisterExec();
        RegisterDos();
        foreach (var (definition, index) in FrontendCases.All().Select((c, i) => (c, i)))
            Execute(definition, index);
        bus.AssertImageUnchanged();
    }

    private void Execute(FrontendCase definition, int slot)
    {
        var invocation = new FrontendInvocation(definition, slot);
        bus.Current = invocation;
        var cpu = M68kCoreFactory.Default.Create(model, bus);
        invocation.Cpu = cpu.State;
        cpu.State.StatusRegister = 0;
        cpu.BeginSubroutine(LoadAddress, invocation.StackTop, ReturnAddress);
        cpu.State.D[0] = 0;
        cpu.State.A[0] = 0;
        try
        {
            while (cpu.State.ProgramCounter != ReturnAddress)
            {
                if (cpu.State.Halted || cpu.State.Stopped)
                    throw new InvalidOperationException("CPU halted or stopped.");
                bus.AssertProgramCounter(cpu.State.ProgramCounter);
                if (++invocation.Instructions > 2_000_000)
                    throw new InvalidOperationException("Instruction bound exceeded.");
                cpu.ExecuteInstruction();
            }

            invocation.Result = unchecked((int)cpu.State.D[0]);
            invocation.FinalSp = cpu.State.A[7];
            Require(invocation.Result == definition.ExpectedResult,
                $"{definition.Id}: result {invocation.Result}, expected {definition.ExpectedResult}.");
            Require(invocation.IoErr == definition.ExpectedIoError,
                $"{definition.Id}: IoErr {invocation.IoErr}, expected {definition.ExpectedIoError}.");
            Require(invocation.FinalSp == invocation.StackTop,
                $"{definition.Id}: entry SP was not restored.");
            Require(invocation.Opens == 1 && invocation.Closes == 1,
                $"{definition.Id}: DOS open/close mismatch.");
            var expectedReadArgs = definition.AllocationFailure ? 0 : 1;
            Require(invocation.ReadArgsCalls == expectedReadArgs,
                $"{definition.Id}: ReadArgs call count mismatch.");
            var expectedFreeArgs = 0;
            Require(invocation.FreeArgsCalls == expectedFreeArgs,
                $"{definition.Id}: FreeArgs call count mismatch.");
            var expectedFaults = 1;
            Require(invocation.PrintFaultCalls == expectedFaults,
                $"{definition.Id}: PrintFault call count mismatch.");
            bus.AssertReleased(invocation);
            Results.Add(new
            {
                id = definition.Id, result = invocation.Result, ioError = invocation.IoErr,
                instructions = invocation.Instructions, stackBytes = invocation.StackTop -
                    invocation.StackBytes, entryStackRestored = true,
                dosOpenCloseBalanced = true, readArgsCalls = invocation.ReadArgsCalls,
                freeArgsCalls = invocation.FreeArgsCalls, printFaultCalls = invocation.PrintFaultCalls,
                events = invocation.Events,
                faults = invocation.Faults,
            });
        }
        catch (Exception error)
        {
            LastFailure = new
            {
                id = definition.Id, instructions = invocation.Instructions,
                pc = cpu.State.ProgramCounter, d0 = cpu.State.D[0],
                sp = cpu.State.A[7], ioError = invocation.IoErr,
                execBase = bus.Long(4), a6 = cpu.State.A[6],
                events = invocation.Events, error = error.Message,
            };
            throw;
        }
        finally
        {
            cpu.Dispose();
            bus.Current = null;
        }
    }

    private void Register(uint address, string name,
        Action<M68kCpuState> handler)
    {
        bus.RegisterGateway(address, state =>
        {
            var current = bus.Current ?? throw new InvalidOperationException(
                $"{name} without an active invocation.");
            current.Events.Add(name);
            handler(state);
            state.D[1] = 0xd1d1d1d1;
            state.A[0] = 0xa0a0a0a0;
            state.A[1] = 0xa1a1a1a1;
            state.StatusRegister = (ushort)((state.StatusRegister & 0xffe0) | 0x001f);
        });
    }

    private void RegisterExec()
    {
        Register(Gateway(0x4000, ExecLvo.FindTask), "FindTask", state =>
        {
            Require(state.A[1] == 0, "FindTask must use the current task.");
            state.D[0] = 0x30000;
        });
        Register(Gateway(0x4000, ExecLvo.OpenLibrary), "OpenLibrary", state =>
        {
            Require(bus.CString(state.A[1]) == "dos.library" && state.D[0] == 37,
                "Unexpected DOS library open request.");
            var invocation = bus.Current!;
            invocation.Opens++;
            state.D[0] = 0x8000;
        });
        Register(Gateway(0x4000, ExecLvo.CloseLibrary), "CloseLibrary", state =>
        {
            var invocation = bus.Current!;
            Require(state.A[1] == 0x8000 && invocation.AllocMemCalls >= invocation.FreeMemCalls,
                "Unexpected DOS close request.");
            invocation.Closes++;
            state.D[0] = 0;
        });
        Register(Gateway(0x4000, ExecLvo.AllocMem), "AllocMem", state =>
        {
            var invocation = bus.Current!;
            var bytes = state.D[0];
            invocation.AllocMemCalls++;
            if (invocation.Definition.AllocationFailure)
            {
                state.D[0] = 0;
                return;
            }
            state.D[0] = bus.Allocate(invocation, bytes, "Exec");
        });
        Register(Gateway(0x4000, ExecLvo.FreeMem), "FreeMem", state =>
        {
            var invocation = bus.Current!;
            bus.Release(invocation, state.A[1], "Exec", state.D[0]);
            invocation.FreeMemCalls++;
            state.D[0] = 0;
        });
    }

    private void RegisterDos()
    {
        const uint dos = 0x8000;
        Register(Gateway(dos, DosLvo.ReadArgs), "ReadArgs", state => ReadArgs(state));
        Register(Gateway(dos, DosLvo.FreeArgs), "FreeArgs", state =>
        {
            var invocation = bus.Current!;
            Require(state.D[1] == invocation.RdArgs && invocation.RdArgs != 0,
                "FreeArgs received the wrong RDArgs pointer.");
            bus.Release(invocation, invocation.RdArgs, "RDArgs");
            invocation.FreeArgsCalls++;
            state.D[0] = 1;
        });
        Register(Gateway(dos, DosLvo.IoErr), "IoErr", state =>
        {
            state.D[0] = unchecked((uint)bus.Current!.IoErr);
        });
        Register(Gateway(dos, DosLvo.SetIoErr), "SetIoErr", state =>
        {
            var invocation = bus.Current!;
            invocation.IoErr = unchecked((int)state.D[1]);
            state.D[0] = 0;
        });
        Register(Gateway(dos, DosLvo.PrintFault), "PrintFault", state =>
        {
            var invocation = bus.Current!;
            Require(state.D[2] == 0, "PrintFault header must be null.");
            invocation.PrintFaultCalls++;
            invocation.Faults.Add(unchecked((int)state.D[1]));
            state.D[0] = 1;
        });
    }

    private void ReadArgs(M68kCpuState state)
    {
        var invocation = bus.Current!;
        Require(bus.CString(state.D[1]) == "FROM/A,TO,TYPE/K" && state.D[3] == 0,
            "ReadArgs template or RDArgs ABI mismatch.");
        invocation.ReadArgsCalls++;
        if (invocation.Definition.ReadArgsError is { } error)
        {
            invocation.IoErr = error;
            state.D[0] = 0;
            return;
        }
        var storage = bus.Allocate(invocation, 64, "RDArgs");
        invocation.RdArgs = storage;
        invocation.ParserStorage = storage;
        var from = storage + 32;
        WriteCString(from, "input.exe");
        var type = invocation.Definition.Type is null ? 0u : storage + 48;
        if (type != 0) WriteCString(type, invocation.Definition.Type!);
        BinaryPrimitives.WriteUInt32BigEndian(bus.Memory.AsSpan((int)state.D[2], 12), from);
        BinaryPrimitives.WriteUInt32BigEndian(bus.Memory.AsSpan((int)state.D[2] + 4, 4), 0);
        BinaryPrimitives.WriteUInt32BigEndian(bus.Memory.AsSpan((int)state.D[2] + 8, 4), type);
        state.D[0] = storage;
    }

    private void WriteCString(uint address, string text)
    {
        var bytes = Encoding.Latin1.GetBytes(text);
        bytes.CopyTo(bus.Memory.AsSpan((int)address));
        bus.Memory[(int)address + bytes.Length] = 0;
    }

    private static uint Gateway(uint libraryBase, int lvo) =>
        unchecked((uint)((int)libraryBase + lvo));

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
