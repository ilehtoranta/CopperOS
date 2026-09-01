using Amiga;
using CopperOS.Commands;
using CopperSharp.Compiler;

namespace CopperOS.Commands.QuoteNativeRoot;

/// <summary>
/// Private native reachability root for the bounded Quote rule/parser stages.
/// It is not a Quote command entry and performs no DOS I/O or argument parsing.
/// </summary>
public static class QuoteForwardProbe
{
    // Control LONGs: source/address+length, rule text/address+length, parsed
    // rule-list/address+capacity, first/address+capacity, second/address+
    // capacity, returned output/address+length, borrowed region/address+size,
    // then completion marker.
    public const int ControlBytes = 60;

    [M68kEntryPoint]
    public static int Main(int argumentLength, CONST_STRPTR argumentText)
    {
        if (argumentLength != ControlBytes || argumentText.IsNull)
            return 20;

        var control = argumentText.Address;
        var source = new APTR(APTR.ReadUInt32(control, 0));
        var sourceLength = APTR.ReadUInt32(control, 4);
        var ruleText = new APTR(APTR.ReadUInt32(control, 8));
        var ruleTextLength = APTR.ReadUInt32(control, 12);
        var ruleList = new APTR(APTR.ReadUInt32(control, 16));
        var ruleCapacity = APTR.ReadUInt32(control, 20);
        var first = new APTR(APTR.ReadUInt32(control, 24));
        var firstCapacity = APTR.ReadUInt32(control, 28);
        var second = new APTR(APTR.ReadUInt32(control, 32));
        var secondCapacity = APTR.ReadUInt32(control, 36);
        BorrowedMemory memory = new(control);

        if (!QuoteRuleParser.TryParse(ref memory, ruleText, ruleTextLength,
                ruleList, ruleCapacity, out var ruleCount) ||
            !QuoteForwardPipeline.TryApply(ref memory, source, sourceLength,
                ruleList, ruleCount, first, firstCapacity, second,
                secondCapacity, out var output, out var outputLength))
            return 10;

        APTR.WriteUInt32(control, 40, output.Raw);
        APTR.WriteUInt32(control, 44, outputLength);
        APTR.WriteUInt32(control, 56, 0x51544650); // QTFP, only after success.
        return 0;
    }

    // The native probe is deliberately a one-pointer generic memory adapter.
    // The caller describes the sole mapped guest region in its control block.
    private readonly struct BorrowedMemory(APTR control) : IAmigaGuestMemory
    {
        public bool IsMapped(APTR address, uint byteSize)
        {
            var start = APTR.ReadUInt32(control, 48);
            var length = APTR.ReadUInt32(control, 52);
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
