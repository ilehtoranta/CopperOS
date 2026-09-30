using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Real public-DOS adapter for bounded Exe2Arc components. The caller already
/// owns an open DOS base, valid handles and at least 102400 writable bytes at
/// scratch. This one-APTR value borrows those resources and never frees them.
/// Mapping here records the caller's allocation claim; it is not an OS probe.
/// </summary>
public readonly struct NativeExe2ArcIo(APTR scratch) : IExe2ArcIo,
    IExe2ArcBreakSource
{
    public int Read(BPTR file, APTR buffer, int length) => DOS.Read(file, buffer, length);
    public int Write(BPTR file, APTR buffer, int length) => DOS.Write(file, buffer, length);
    public int Seek(BPTR file, int position, int mode) => DOS.Seek(file, position, mode);
    public int IoErr() => (int)DOS.IoErr();
    public bool IsBreakPending() => NativeCommandIo.IsCtrlCPending();
    public void SetIoErr(int ioError) => DOS.SetIoErr((DOS.Error)ioError);

    public bool IsMapped(APTR address, uint byteSize) =>
        !scratch.IsNull &&
        scratch.Raw <= uint.MaxValue - (Exe2ArcIoBounds.BufferBytes - 1) &&
        byteSize <= Exe2ArcIoBounds.BufferBytes && address.Raw >= scratch.Raw &&
        address.Raw - scratch.Raw <= Exe2ArcIoBounds.BufferBytes - byteSize;

    public byte ReadUInt8(APTR address, int offset = 0) => APTR.ReadUInt8(address, offset);
    public ushort ReadUInt16(APTR address, int offset = 0) => APTR.ReadUInt16(address, offset);
    public uint ReadUInt32(APTR address, int offset = 0) => APTR.ReadUInt32(address, offset);
    public void WriteUInt8(APTR address, int offset, byte value) => APTR.WriteUInt8(address, offset, value);
    public void WriteUInt16(APTR address, int offset, ushort value) => APTR.WriteUInt16(address, offset, value);
    public void WriteUInt32(APTR address, int offset, uint value) => APTR.WriteUInt32(address, offset, value);

    // The scanner only reads individual bytes; copying uses DOS Read/Write.
    // These interface members are not used by either component's closure.
    public void Clear(APTR address, uint byteCount)
    {
        for (uint index = 0; index < byteCount; index++)
            APTR.WriteUInt8(new APTR(address.Raw + index), 0, 0);
    }

    public void Copy(APTR source, APTR destination, uint byteCount)
    {
        for (uint index = 0; index < byteCount; index++)
            APTR.WriteUInt8(new APTR(destination.Raw + index), 0,
                APTR.ReadUInt8(new APTR(source.Raw + index), 0));
    }
}
