using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>Public DOS adapter for the bounded Exe2Arc output selector.</summary>
public readonly struct NativeExe2ArcOutputIo(APTR mappedBase) : IExe2ArcOutputIo
{
    // The owner supplies one contiguous 102400-byte scratch region followed by
    // two 512-byte path buffers. Keeping only the base pointer makes this
    // borrowed adapter a four-byte resident generic argument.
    private const uint MappedBytes = 102400u + 512u + 512u;

    public BPTR OpenNewFile(APTR path) => DOS.OpenRaw(
        CString.FromPointer(path.Raw), DOS.FileMode.NewFile);

    public int AddPart(APTR buffer, APTR part, uint capacity) => DOS.AddPart(
        CString.FromPointer(buffer.Raw), CString.FromPointer(part.Raw), capacity);

    public bool IsMapped(APTR address, uint byteSize) =>
        !mappedBase.IsNull && byteSize != 0 && byteSize <= MappedBytes &&
        address.Raw >= mappedBase.Raw &&
        address.Raw - mappedBase.Raw <= MappedBytes - byteSize;

    public byte ReadUInt8(APTR address, int offset = 0) =>
        APTR.ReadUInt8(address, offset);
    public ushort ReadUInt16(APTR address, int offset = 0) =>
        APTR.ReadUInt16(address, offset);
    public uint ReadUInt32(APTR address, int offset = 0) =>
        APTR.ReadUInt32(address, offset);
    public void WriteUInt8(APTR address, int offset, byte value) =>
        APTR.WriteUInt8(address, offset, value);
    public void WriteUInt16(APTR address, int offset, ushort value) =>
        APTR.WriteUInt16(address, offset, value);
    public void WriteUInt32(APTR address, int offset, uint value) =>
        APTR.WriteUInt32(address, offset, value);
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
