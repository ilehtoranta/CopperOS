using System.Buffers.Binary;
using Amiga;
using Copper68k;

namespace CopperOS.MuiMaster.LibraryBuilder.Tests;

// Executes the emitted MC68000 guard. Exec services are explicit fixture
// boundaries; a full ROM replay separately qualifies the real StackSwap path.
public sealed class MuiNativeStackGateTests
{
    [Theory]
    [InlineData(0x10000u, 0x20000u, 0x1FFF0u, false)]
    [InlineData(0x10000u, 0x11000u, 0x10FF0u, true)]
    [InlineData(0x10000u, 0x11000u, 0x30FF0u, true)]
    [InlineData(0x10000u, 0x11000u, 0x8FF0u, true)]
    public void UsesOnlyProvenHeadroomAndRestoresArgumentsAndCallerStack(
        uint lower, uint upper, uint callerSp, bool swapped)
    {
        var bus = new Bus();
        bus.Long(4, Bus.ExecBase);
        bus.Word(Bus.ExecBase + 20, 40);
        bus.Long(Bus.ExecBase + (uint)ExecLayout.ExecBase.ThisTask, Bus.Task);
        bus.Long(Bus.Task + (uint)ExecLayout.Task.StackLower, lower);
        bus.Long(Bus.Task + (uint)ExecLayout.Task.StackUpper, upper);
        var gate = MuiNativeStackGate.Build(Bus.Target);
        gate.Code.CopyTo(bus.Bytes, (int)Bus.Entry);
        bus.Long(callerSp, Bus.Return);
        using var core = M68kCoreFactory.Default.Create(M68kCpuModel.M68000, bus);
        core.Reset(Bus.Entry, callerSp);
        core.State.ResetStackPointers(0xF0000, callerSp, false);
        core.State.StatusRegister = 0;
        for (var i = 0; i < 8; i++) core.State.D[i] = 0xD0000000u + (uint)i;
        for (var i = 0; i < 7; i++) core.State.A[i] = 0xA0000000u + (uint)i;
        var data = core.State.D.ToArray(); var address = core.State.A.ToArray();
        bus.Gateway(Bus.Target, state =>
        {
            bus.BodyCalls++;
            Assert.Equal(data, state.D.ToArray());
            Assert.Equal(address[..7], state.A.ToArray()[..7]);
            Assert.Equal(swapped, state.A[7] >= Bus.Allocation &&
                state.A[7] < Bus.Allocation + MuiNativeStackGate.StackBytes);
            state.D[0] = 0x12345678;
        });
        for (var instruction = 0; instruction < 1000 && core.State.ProgramCounter != Bus.Return; instruction++)
        {
            core.ExecuteInstruction();
            Assert.False(core.State.Halted);
        }
        Assert.Equal(Bus.Return, core.State.ProgramCounter);
        Assert.Equal(callerSp + 4, core.State.A[7]);
        Assert.Equal(0x12345678u, core.State.D[0]);
        Assert.Equal(data[1..], core.State.D.ToArray()[1..]);
        Assert.Equal(address[..7], core.State.A.ToArray()[..7]);
        Assert.Equal(lower, bus.Long(Bus.Task + (uint)ExecLayout.Task.StackLower));
        Assert.Equal(upper, bus.Long(Bus.Task + (uint)ExecLayout.Task.StackUpper));
        Assert.Equal(1, bus.BodyCalls);
        Assert.Equal(swapped ? 1 : 0, bus.Allocations);
        Assert.Equal(swapped ? 2 : 0, bus.Swaps);
        Assert.Equal(bus.Allocations, bus.Frees);
    }

    private sealed class Bus : IM68kBus
    {
        internal const uint ExecBase = 0x1000, Task = 0x2000, Entry = 0x3000,
            Target = 0x4000, Return = 0x5000, Allocation = 0x80000;
        internal readonly byte[] Bytes = new byte[0x100000];
        private readonly Dictionary<uint, Action<M68kCpuState>> _gateways = [];
        internal int Allocations, Frees, Swaps, BodyCalls;
        internal Bus()
        {
            Gateway(Vector(ExecLvo.SetSR), s => s.D[0] = s.StatusRegister);
            Gateway(Vector(ExecLvo.AllocMem), s =>
            {
                Assert.Equal(MuiNativeStackGate.StackBytes, s.D[0]);
                Allocations++; s.D[0] = Allocation;
            });
            Gateway(Vector(ExecLvo.FreeMem), s =>
            {
                Assert.Equal(Allocation, s.A[1]);
                Assert.Equal(MuiNativeStackGate.StackBytes, s.D[0]);
                Assert.False(s.A[7] >= Allocation && s.A[7] < Allocation + MuiNativeStackGate.StackBytes);
                Frees++;
            });
            Gateway(Vector(ExecLvo.StackSwap), s =>
            {
                var swap = s.A[0]; var oldSp = s.A[7];
                var lower = Long(swap); var upper = Long(swap + 4); var nextSp = Long(swap + 8);
                var oldLower = Long(Task + (uint)ExecLayout.Task.StackLower);
                var oldUpper = Long(Task + (uint)ExecLayout.Task.StackUpper);
                Long(swap, oldLower); Long(swap + 4, oldUpper); Long(swap + 8, oldSp + 4);
                Long(Task + (uint)ExecLayout.Task.StackLower, lower);
                Long(Task + (uint)ExecLayout.Task.StackUpper, upper);
                Long(nextSp - 4, Long(oldSp));
                s.SetActiveStackPointer(nextSp - 4);
                Swaps++;
            });
        }
        private static uint Vector(short lvo) => unchecked(ExecBase + (uint)lvo);
        internal void Gateway(uint pc, Action<M68kCpuState> callback)
        { _gateways.Add(pc, callback); Word(pc, 0xFF00); Long(pc + 2, 1); }
        internal uint Long(uint a) => BinaryPrimitives.ReadUInt32BigEndian(Bytes.AsSpan((int)a, 4));
        internal void Long(uint a, uint v) => BinaryPrimitives.WriteUInt32BigEndian(Bytes.AsSpan((int)a, 4), v);
        internal void Word(uint a, ushort v) => BinaryPrimitives.WriteUInt16BigEndian(Bytes.AsSpan((int)a, 2), v);
        public byte ReadByte(uint a, ref long cycle, M68kBusAccessKind kind) => Bytes[a];
        public ushort ReadWord(uint a, ref long cycle, M68kBusAccessKind kind) => BinaryPrimitives.ReadUInt16BigEndian(Bytes.AsSpan((int)a, 2));
        public uint ReadLong(uint a, ref long cycle, M68kBusAccessKind kind) => Long(a);
        public void WriteByte(uint a, byte v, ref long cycle, M68kBusAccessKind kind) => Bytes[a] = v;
        public void WriteWord(uint a, ushort v, ref long cycle, M68kBusAccessKind kind) => Word(a, v);
        public void WriteLong(uint a, uint v, ref long cycle, M68kBusAccessKind kind) => Long(a, v);
        public bool HasHostGateway(uint pc) => _gateways.ContainsKey(pc);
        public bool TryInvokeHostGateway(uint pc, uint token, M68kCpuState state)
        { if (!_gateways.TryGetValue(pc, out var callback)) return false; callback(state); return true; }
        public void ResetExternalDevices(long cycle) { }
    }
}
