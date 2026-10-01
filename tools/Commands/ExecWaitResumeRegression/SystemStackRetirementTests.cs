using System.Reflection;
using Amiga;
using Copper68k;
using CopperMod.Amiga;
using CopperMod.Amiga.Core;
using CopperMod.Amiga.Runtime;
using CopperMod.Amiga.Bus;
using CopperMod.Amiga.CopperStart.Exec;

namespace CopperMod.Amiga.Tests;

public sealed class SystemStackRetirementTests
{
    [Theory]
    [InlineData(false, 0x400u, 0u, false)]
    [InlineData(true, 0x400u, 0u, true)]
    [InlineData(true, 0x3F8u, 0x3F8u, false)]
    [InlineData(true, 0x400u, 0x6FF0u, false)]
    [InlineData(true, 0x7800u, 0u, false)]
    [InlineData(true, 0x8800u, 0u, false)]
    public void SupervisorBoundsDoNotHideLiveReferences(bool known, uint supervisorTop,
        uint liveReference, bool expected)
    {
        var machine = new Machine(MachineOptions.ForProfile(MachineProfile.A500Pal512KBoot).WithLiveAgnusDma(false));
        var bus = machine.Bus;
        var memory = new HostGuestMemory(bus);
        var lists = new ExecListServices(memory, (_, _) => string.Empty);
        const uint removed = 0x2000, current = 0x3000;
        lists.Initialize(removed + ExecLayout.Task.MemoryEntries);
        bus.WriteLong(removed + ExecLayout.Task.StackLower, 0x8000);
        bus.WriteLong(removed + ExecLayout.Task.StackUpper, 0x9000);
        bus.WriteLong(current + ExecLayout.Task.StackLower, 0x400);
        bus.WriteLong(current + ExecLayout.Task.StackUpper, 0x7000);
        // A stale value below USP is not part of the live user stack, and is
        // well beyond the separately owned supervisor stack's upper bound.
        bus.WriteLong(0x6000, 0x8800);
        if (liveReference != 0) bus.WriteLong(liveReference, 0x8800);
        var context = new CopperStartExecContext(memory, () => 0x1000, () => current,
            (_, _) => string.Empty, (_, _, _) => { }, () => { }, _ => false, 0,
            _ => true, (_, _) => { }, _ => { }, (_, _, _) => { }, () => false,
            () => false, (_, _) => 0, new ExecMemoryOperations((_, _) => 0,
                (_, _) => throw new InvalidOperationException("Read-only test must not free memory."), (_, _) => { }), () => 0);
        if (known) context.SystemStackBounds = () => (0u, 0x400u);
        var frame = new M68kCpuState { ProgramCounter = 0x5000 };
        frame.ResetStackPointers(supervisorTop, 0x6FF0, supervisorMode: false);
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
        var type = typeof(ExecTaskServices).GetNestedType("RetirementStorage", flags)!;
        var storage = type.GetMethod("Capture", flags)!.Invoke(null, new object?[] { context, removed, false, null })!;
        var actual = type.GetMethod("FrameIsClear", flags)!.Invoke(storage, new object[] { context, current, frame, true });
        Assert.Equal(expected, actual);
    }
}
