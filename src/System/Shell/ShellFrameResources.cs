using Amiga;

namespace CopperOS.Shell;

/// <summary>Owns the control blocks and label records of one Shell frame.</summary>
public static class ShellFrameResources
{
    private const uint HeaderBytes = 16;
    private const uint HeaderMagic = 0x53464f57; // SFOW
    private const uint ControlKind = 1;
    private const uint LabelKind = 2;
    private const uint ContinuationKind = 3;

    public static APTR AllocateControl<TMemory>(ref TMemory memory, APTR frame)
        where TMemory : struct, IShellRunnerMemory =>
        Allocate(ref memory, frame, ControlKind, ShellScriptControlCodec.Size, 0, out _);

    public static APTR AllocateLabel<TMemory>(ref TMemory memory, APTR frame,
        uint nameLength, out APTR name) where TMemory : struct, IShellRunnerMemory
    {
        name = APTR.Null;
        if (nameLength == 0 || nameLength > 65_535) return APTR.Null;
        return Allocate(ref memory, frame, LabelKind, ShellScriptLabelCodec.Size,
            nameLength, out name);
    }

    /// <summary>Allocates a continuation and its command text under one frame.</summary>
    public static APTR AllocateContinuation<TMemory>(ref TMemory memory, APTR frame,
        uint commandBytes, out APTR command) where TMemory : struct, IShellRunnerMemory
    {
        command = APTR.Null;
        if (commandBytes > 65_535) return APTR.Null;
        return Allocate(ref memory, frame, ContinuationKind,
            ShellProcessContinuationCodec.Size, commandBytes, out command);
    }

    public static bool IsContinuationOwned<TMemory>(ref TMemory memory, APTR frame, APTR record)
        where TMemory : struct, IShellRunnerMemory =>
        ValidFrame(ref memory, frame) && ValidRecord(ref memory, frame, record, ContinuationKind, out _);

    /// <summary>Frees a detached continuation after its observer has relinquished it.</summary>
    public static bool FreeContinuation<TMemory>(ref TMemory memory, APTR frame, APTR record)
        where TMemory : struct, IShellRunnerMemory =>
        ValidFrame(ref memory, frame) && Free(ref memory, frame, record, ContinuationKind);

    public static bool FreeControl<TMemory>(ref TMemory memory, APTR frame, APTR record)
        where TMemory : struct, IShellRunnerMemory =>
        Free(ref memory, frame, record, ControlKind);

    public static bool FreeLabel<TMemory>(ref TMemory memory, APTR frame, APTR record)
        where TMemory : struct, IShellRunnerMemory =>
        Free(ref memory, frame, record, LabelKind);

    /// <summary>Checks both lists before the caller detaches the frame.</summary>
    public static bool CanRelease<TMemory>(ref TMemory memory, APTR frame)
        where TMemory : struct, IShellRunnerMemory
    {
        if (!ValidFrame(ref memory, frame)) return false;
        var control = memory.ReadUInt32(frame, 68);
        var fast = control;
        var labels = memory.ReadUInt32(frame, 76);
        var expectedLabels = memory.ReadUInt32(frame, 64);
        while (control != 0)
        {
            if (!TryNextControl(ref memory, frame, control, out control) ||
                !TryNextControl(ref memory, frame, fast, out fast) ||
                !TryNextControl(ref memory, frame, fast, out fast) ||
                (control != 0 && control == fast)) return false;
        }
        var count = 0u;
        fast = labels;
        while (labels != 0)
        {
            if (count == expectedLabels ||
                !TryNextLabel(ref memory, frame, labels, out labels) ||
                !TryNextLabel(ref memory, frame, fast, out fast) ||
                !TryNextLabel(ref memory, frame, fast, out fast) ||
                (labels != 0 && labels == fast)) return false;
            count++;
        }
        return count == expectedLabels;
    }

    private static bool TryNextControl<TMemory>(ref TMemory memory, APTR frame,
        uint raw, out uint next) where TMemory : struct, IShellRunnerMemory
    {
        next = 0;
        if (raw == 0) return true;
        var record = APTR.FromPointer(raw);
        if (!ValidRecord(ref memory, frame, record, ControlKind, out _) ||
            memory.ReadUInt32(record, 0) != ShellScriptControlCodec.Magic ||
            memory.ReadUInt32(record, 4) != ShellScriptControlCodec.Version) return false;
        var kind = memory.ReadUInt32(record, 12);
        if (kind != (uint)ShellScriptBlockKind.If && kind != (uint)ShellScriptBlockKind.Skip) return false;
        next = memory.ReadUInt32(record, 8);
        return true;
    }

    private static bool TryNextLabel<TMemory>(ref TMemory memory, APTR frame,
        uint raw, out uint next) where TMemory : struct, IShellRunnerMemory
    {
        next = 0;
        if (raw == 0) return true;
        var record = APTR.FromPointer(raw);
        if (!ValidRecord(ref memory, frame, record, LabelKind, out var total) ||
            memory.ReadUInt32(record, 0) != ShellScriptLabelCodec.Magic ||
            memory.ReadUInt32(record, 4) != ShellScriptLabelCodec.Version) return false;
        var nameLength = memory.ReadUInt32(record, 16);
        if (nameLength == 0 || nameLength != total - HeaderBytes - ShellScriptLabelCodec.Size ||
            memory.ReadUInt32(record, 12) != record.Raw + ShellScriptLabelCodec.Size) return false;
        next = memory.ReadUInt32(record, 8);
        return true;
    }

    /// <summary>Detaches and frees all remaining frame-owned blocks and labels.</summary>
    public static bool Release<TMemory>(ref TMemory memory, APTR frame)
        where TMemory : struct, IShellRunnerMemory
    {
        if (!CanRelease(ref memory, frame)) return false;
        var control = memory.ReadUInt32(frame, 68);
        var labels = memory.ReadUInt32(frame, 76);
        memory.WriteUInt32(frame, 68, 0);
        memory.WriteUInt32(frame, 76, 0);
        memory.WriteUInt32(frame, 64, 0);
        while (control != 0)
        {
            var record = APTR.FromPointer(control);
            control = memory.ReadUInt32(record, 8);
            if (!FreeControl(ref memory, frame, record)) return false;
        }
        while (labels != 0)
        {
            var record = APTR.FromPointer(labels);
            labels = memory.ReadUInt32(record, 8);
            if (!FreeLabel(ref memory, frame, record)) return false;
        }
        return true;
    }

    private static APTR Allocate<TMemory>(ref TMemory memory, APTR frame, uint kind,
        uint recordBytes, uint extraBytes, out APTR extra)
        where TMemory : struct, IShellRunnerMemory
    {
        extra = APTR.Null;
        if (!ValidFrame(ref memory, frame) || extraBytes > uint.MaxValue - HeaderBytes - recordBytes)
            return APTR.Null;
        var total = HeaderBytes + recordBytes + extraBytes;
        var allocation = memory.AllocateRunnerMemory(total);
        if (allocation.IsNull || (allocation.Raw & 3) != 0 || allocation.Raw > uint.MaxValue - total ||
            !memory.IsMapped(allocation, total))
        {
            if (allocation.IsNotNull) memory.FreeRunnerMemory(allocation, total);
            return APTR.Null;
        }
        memory.Clear(allocation, total);
        memory.WriteUInt32(allocation, 0, HeaderMagic);
        memory.WriteUInt32(allocation, 4, frame.Raw);
        memory.WriteUInt32(allocation, 8, kind);
        memory.WriteUInt32(allocation, 12, total);
        var record = APTR.FromPointer(allocation.Raw + HeaderBytes);
        if (extraBytes != 0) extra = APTR.FromPointer(record.Raw + recordBytes);
        return record;
    }

    private static bool Free<TMemory>(ref TMemory memory, APTR frame, APTR record, uint kind)
        where TMemory : struct, IShellRunnerMemory
    {
        if (!ValidRecord(ref memory, frame, record, kind, out var total)) return false;
        var allocation = APTR.FromPointer(record.Raw - HeaderBytes);
        memory.WriteUInt32(allocation, 0, 0);
        memory.FreeRunnerMemory(allocation, total);
        return true;
    }

    private static bool ValidFrame<TMemory>(ref TMemory memory, APTR frame)
        where TMemory : struct, IShellRunnerMemory =>
        frame.IsNotNull && frame.Raw <= uint.MaxValue - ShellScriptFrameCodec.Size &&
        memory.IsMapped(frame, ShellScriptFrameCodec.Size) &&
        memory.ReadUInt32(frame, 0) == ShellScriptFrameCodec.Magic &&
        memory.ReadUInt32(frame, 4) == ShellScriptFrameCodec.Version;

    private static bool ValidRecord<TMemory>(ref TMemory memory, APTR frame, APTR record,
        uint kind, out uint total) where TMemory : struct, IShellRunnerMemory
    {
        total = 0;
        if (record.IsNull || record.Raw < HeaderBytes || (record.Raw & 3) != 0) return false;
        var allocation = APTR.FromPointer(record.Raw - HeaderBytes);
        if (!memory.IsMapped(allocation, HeaderBytes) || memory.ReadUInt32(allocation, 0) != HeaderMagic ||
            memory.ReadUInt32(allocation, 4) != frame.Raw || memory.ReadUInt32(allocation, 8) != kind) return false;
        total = memory.ReadUInt32(allocation, 12);
        var minimum = HeaderBytes + (kind == ControlKind ? ShellScriptControlCodec.Size :
            kind == LabelKind ? ShellScriptLabelCodec.Size : ShellProcessContinuationCodec.Size);
        return total >= minimum && (kind != ControlKind || total == minimum) &&
            (kind == ControlKind || total <= minimum + 65_535) && allocation.Raw <= uint.MaxValue - total &&
            memory.IsMapped(allocation, total);
    }
}
