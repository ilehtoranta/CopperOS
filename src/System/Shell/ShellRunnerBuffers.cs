using Amiga;

namespace CopperOS.Shell;

/// <summary>Exec-style memory operations used by a Shell-owned runner.</summary>
public interface IShellRunnerMemory : IAmigaGuestMemory
{
    APTR AllocateRunnerMemory(uint size);
    void FreeRunnerMemory(APTR address, uint size);
}

/// <summary>Owns the optional buffers retained across script steps.</summary>
public static class ShellRunnerBuffers
{
    public const uint RecordSize = 140;
    public const uint PromptBytes = 4100;
    public const uint KeyBytes = 4096;
    public const uint KeyResultsBytes = 1024;
    private const int ErrorCodesOffset = 48;
    private const int KeyOffset = 100;
    private const int PromptOffset = 124;
    private const int CapturePathLengthOffset = 132;
    private const int ErrorCodeBytesOffset = 136;
    private const int FrameKeyOffset = 104;

    public static bool EnsurePrompt<TMemory>(ref TMemory memory, APTR runner,
        ref APTR buffer) where TMemory : struct, IShellRunnerMemory =>
        Ensure(ref memory, runner, PromptOffset, PromptBytes, ref buffer);

    public static bool EnsureKeyResults<TMemory>(ref TMemory memory, APTR runner,
        ref APTR buffer, ref uint byteCount) where TMemory : struct, IShellRunnerMemory
    {
        if (!Mapped(ref memory, runner, RecordSize) ||
            buffer.IsNull != (byteCount == 0) ||
            memory.ReadUInt32(runner, ErrorCodeBytesOffset) != byteCount) return false;
        if (!Ensure(ref memory, runner, ErrorCodesOffset, KeyResultsBytes, ref buffer)) return false;
        if (byteCount == 0)
        {
            byteCount = KeyResultsBytes;
            memory.WriteUInt32(runner, ErrorCodeBytesOffset, byteCount);
        }
        return true;
    }

    public static bool EnsureKey<TMemory>(ref TMemory memory, APTR runner,
        APTR frame, ref APTR buffer) where TMemory : struct, IShellRunnerMemory
    {
        if (!Mapped(ref memory, frame, ShellScriptFrameCodec.Size) ||
            memory.ReadUInt32(frame, FrameKeyOffset) != buffer.Raw ||
            !Ensure(ref memory, runner, KeyOffset, KeyBytes, ref buffer)) return false;
        memory.WriteUInt32(frame, FrameKeyOffset, buffer.Raw);
        return true;
    }

    public static bool ReleaseInactivePrompt<TMemory>(ref TMemory memory, APTR runner,
        ref APTR buffer, ref uint capturePathLength, BPTR captureInput,
        ShellScriptFrameFlags flags, ShellScriptPromptExpansionPhase phase,
        ShellScriptDeferredCommandKind deferred) where TMemory : struct, IShellRunnerMemory
    {
        if (buffer.IsNull || (flags & ShellScriptFrameFlags.Interactive) != 0 ||
            phase != ShellScriptPromptExpansionPhase.None ||
            deferred != ShellScriptDeferredCommandKind.None || captureInput.IsNotNull)
            return true;
        if (!Mapped(ref memory, runner, RecordSize) ||
            memory.ReadUInt32(runner, PromptOffset) != buffer.Raw) return false;
        var released = buffer;
        memory.WriteUInt32(runner, PromptOffset, 0);
        memory.WriteUInt32(runner, CapturePathLengthOffset, 0);
        buffer = APTR.Null;
        capturePathLength = 0;
        memory.FreeRunnerMemory(released, PromptBytes);
        return true;
    }

    public static bool ReleaseInactiveKey<TMemory>(ref TMemory memory, APTR runner,
        APTR frame, ref APTR buffer, uint storedLength, uint currentLine,
        APTR frameBuffer, uint frameLength) where TMemory : struct, IShellRunnerMemory
    {
        if (buffer.IsNull || storedLength != 0 || currentLine <= 1 ||
            frameLength != 0 || frameBuffer != buffer) return true;
        if (!Mapped(ref memory, runner, RecordSize) ||
            !Mapped(ref memory, frame, ShellScriptFrameCodec.Size) ||
            memory.ReadUInt32(runner, KeyOffset) != buffer.Raw ||
            memory.ReadUInt32(frame, FrameKeyOffset) != buffer.Raw) return false;
        var released = buffer;
        memory.WriteUInt32(frame, FrameKeyOffset, 0);
        memory.WriteUInt32(runner, KeyOffset, 0);
        buffer = APTR.Null;
        memory.FreeRunnerMemory(released, KeyBytes);
        return true;
    }

    private static bool Ensure<TMemory>(ref TMemory memory, APTR runner, int offset,
        uint size, ref APTR buffer) where TMemory : struct, IShellRunnerMemory
    {
        if (!Mapped(ref memory, runner, RecordSize) ||
            memory.ReadUInt32(runner, offset) != buffer.Raw) return false;
        if (buffer.IsNotNull) return Mapped(ref memory, buffer, size);
        var allocated = memory.AllocateRunnerMemory(size);
        if (!Mapped(ref memory, allocated, size))
        {
            if (allocated.IsNotNull) memory.FreeRunnerMemory(allocated, size);
            return false;
        }
        memory.WriteUInt32(runner, offset, allocated.Raw);
        buffer = allocated;
        return true;
    }

    private static bool Mapped<TMemory>(ref TMemory memory, APTR address, uint size)
        where TMemory : struct, IAmigaGuestMemory =>
        address.IsNotNull && address.Raw <= uint.MaxValue - size && memory.IsMapped(address, size);
}
