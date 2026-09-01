using Amiga;
using CopperOS.Commands;
using CopperSharp.Compiler;

namespace CopperOS.Commands.EvalExpressionNativeRoot;

/// <summary>Private native reachability root for the bounded Eval parser.</summary>
public static class EvalExpressionProbe
{
    // Source address/length, mapped region address/length, result high/low,
    // status byte as LONG, then completion marker.
    public const int ControlBytes = 32;

    [M68kEntryPoint]
    public static int Main(int argumentLength, CONST_STRPTR argumentText)
    {
        if (argumentLength != ControlBytes || argumentText.IsNull)
            return 20;
        var control = argumentText.Address;
        var source = new APTR(APTR.ReadUInt32(control, 0));
        var sourceLength = APTR.ReadUInt32(control, 4);
        BorrowedMemory memory = new(control);
        if (!EvalExpressionEvaluator.TryEvaluate(ref memory, source, sourceLength,
                out var value, out var status))
        {
            APTR.WriteUInt32(control, 24, (uint)status);
            return 10;
        }
        var low = M68kRuntime.SplitInt64(value, out var high);
        APTR.WriteUInt32(control, 16, high);
        APTR.WriteUInt32(control, 20, low);
        APTR.WriteUInt32(control, 24, (uint)status);
        APTR.WriteUInt32(control, 28, 0x45565052); // EVPR
        return 0;
    }

    private readonly struct BorrowedMemory(APTR control) : IAmigaGuestMemory
    {
        public bool IsMapped(APTR address, uint byteSize)
        {
            var start = APTR.ReadUInt32(control, 8);
            var length = APTR.ReadUInt32(control, 12);
            return address.Raw >= start && byteSize <= length &&
                address.Raw - start <= length - byteSize;
        }

        public byte ReadUInt8(APTR address, int offset = 0) => APTR.ReadUInt8(address, offset);
        public ushort ReadUInt16(APTR address, int offset = 0) => APTR.ReadUInt16(address, offset);
        public uint ReadUInt32(APTR address, int offset = 0) => APTR.ReadUInt32(address, offset);
        public void WriteUInt8(APTR address, int offset, byte value) => APTR.WriteUInt8(address, offset, value);
        public void WriteUInt16(APTR address, int offset, ushort value) => APTR.WriteUInt16(address, offset, value);
        public void WriteUInt32(APTR address, int offset, uint value) => APTR.WriteUInt32(address, offset, value);
        public void Clear(APTR address, uint byteCount)
        {
            for (var index = 0u; index < byteCount; index++)
                APTR.WriteUInt8(address, (int)index, 0);
        }
        public void Copy(APTR source, APTR destination, uint byteCount)
        {
            for (var index = 0u; index < byteCount; index++)
                APTR.WriteUInt8(destination, (int)index,
                    APTR.ReadUInt8(source, (int)index));
        }
    }
}
