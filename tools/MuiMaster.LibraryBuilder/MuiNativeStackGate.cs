using System.Buffers.Binary;
using Amiga;

namespace CopperOS.MuiMaster.LibraryBuilder;

// Native 68000 entry guard, before any compiled C# frame is created. The OS
// owns scheduling and stack bounds through StackSwap; each allocation belongs
// to this invocation, including reentrant callbacks on other tasks/stacks.
internal static class MuiNativeStackGate
{
    internal const uint StackBytes = 128 * 1024;
    internal const uint RequiredHeadroom = 32 * 1024;
    internal static (byte[] Code, int[] Relocations) Build(uint target)
    {
        var bytes = new List<byte>();
        var labels = new Dictionary<string, int>();
        var branches = new List<(int Offset, string Label)>();
        var relocations = new List<int>();
        void W(params ushort[] words) { foreach (var v in words) { bytes.Add((byte)(v >> 8)); bytes.Add((byte)v); } }
        void L(uint value) => W((ushort)(value >> 16), (ushort)value);
        void Branch(ushort op, string label) { W(op); branches.Add((bytes.Count, label)); W(0); }
        void Target(ushort op) { W(op); relocations.Add(bytes.Count); L(target); }
        W(0x48e7, 0xfffe, 0x4fef, 0xfff0); // Save all registers except SP; 16-byte swap record.
        W(0x2c78, 4); // ExecBase
        W(0x0c6e, 37, 20); Branch(0x6500, "fail"); // StackSwap needs Exec V37+.
        W(0x7000, 0x7200, 0x4eae, unchecked((ushort)ExecLvo.SetSR));
        W(0x0240, 0x2700); Branch(0x6600, "fail"); // Ordinary user/IPL0 calls only.
        W(0x206e, (ushort)ExecLayout.ExecBase.ThisTask, 0x2008); Branch(0x6700, "fail");
        W(0x200f, 0xb0a8, (ushort)ExecLayout.Task.StackLower); Branch(0x6500, "swap");
        // Manual application stacks need not update tc_SPLower/tc_SPUpper.
        // A large distance above the recorded lower bound proves no headroom
        // when SP is already outside that recorded stack (MUImem does this).
        W(0xb0a8, (ushort)ExecLayout.Task.StackUpper); Branch(0x6200, "swap");
        W(0x90a8, (ushort)ExecLayout.Task.StackLower, 0x0c80); L(RequiredHeadroom);
        Branch(0x6400, "fast");
        labels.Add("swap", bytes.Count);
        W(0x203c); L(StackBytes); W(0x7201, 0x4eae, unchecked((ushort)ExecLvo.AllocMem));
        W(0x2f40, 12, 0x4a80); Branch(0x6700, "fail");
        W(0x2a4f, 0x2a80, 0x0680); L(StackBytes); // A5=old frame; lower/upper/new SP.
        W(0x2b40, 4, 0x0480); L(16);
        W(0x2b40, 8, 0x2040, 0x208d, 0x204d, 0x4eae, unchecked((ushort)ExecLvo.StackSwap));
        W(0x4ced, 0x7fff, 16); // Restore original arguments from the old frame.
        Target(0x4eb9); // JSR raw exported ABI adapter.
        W(0x2a57, 0x2b40, 16, 0x204d, 0x2c78, 4, 0x4eae, unchecked((ushort)ExecLvo.StackSwap));
        W(0x226f, 12, 0x203c); L(StackBytes);
        W(0x4eae, unchecked((ushort)ExecLvo.FreeMem), 0x4fef, 16, 0x4cdf, 0x7fff, 0x4e75);
        labels.Add("fast", bytes.Count);
        W(0x4fef, 16, 0x4cdf, 0x7fff); Target(0x4ef9); // JMP, preserving the caller's return PC.
        labels.Add("fail", bytes.Count);
        W(0x4fef, 16, 0x4cdf, 0x7fff, 0x7000, 0x4e75);
        var result = bytes.ToArray();
        foreach (var (offset, label) in branches)
            BinaryPrimitives.WriteInt16BigEndian(result.AsSpan(offset, 2), checked((short)(labels[label] - offset)));
        return (result, relocations.ToArray());
    }
}
