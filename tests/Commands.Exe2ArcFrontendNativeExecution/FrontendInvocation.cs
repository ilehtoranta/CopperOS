using System.Text;
using Copper68k;

namespace CopperOS.Commands.Exe2ArcFrontendNativeExecution;

internal sealed class FrontendInvocation(FrontendCase definition, int slot)
{
    public FrontendCase Definition { get; } = definition;
    public uint Process { get; } = (uint)(0x30000 + slot * 0x1000);
    public uint StackTop { get; } = (uint)(0x50000 + slot * 0x10000);
    public uint StackBytes { get; } = definition.StackBytes;
    public uint Arguments { get; } = (uint)(0x35000 + slot * 0x2000);
    public uint MinStack { get; set; } = (uint)(0x50000 + slot * 0x10000);
    public uint InputHandle { get; } = (uint)(0x500 + slot * 0x10);
    public uint OutputHandle { get; } = (uint)(0x600 + slot * 0x10);
    public uint DosBase { get; } = (uint)(0x8000 + slot * 0x4000);
    public uint Workspace { get; set; }
    public int Cursor { get; set; }
    public int OpenCalls { get; set; }
    public int SignalCalls { get; set; }
    public int Waits { get; set; }
    public int Gets { get; set; }
    public int Forbids { get; set; }
    public int Replies { get; set; }
    public uint Fib { get; set; }
    public HashSet<uint> Handles { get; } = [];
    public List<string> OpenPaths { get; } = [];
    public List<byte> Output { get; } = [];
    public List<string> DeletedPaths { get; } = [];
    public List<string> Allocations { get; } = [];
    public List<string> Releases { get; } = [];
    public List<object> Transfers { get; } = [];
    public Dictionary<string, int> Occurrences { get; } = [];
    public StringBuilder Stdout { get; } = new();
    public uint[] Preserved { get; } = new uint[11];
    public int Instructions { get; set; }
    public int Opens { get; set; }
    public int Closes { get; set; }
    public int AllocMemCalls { get; set; }
    public int FreeMemCalls { get; set; }
    public int ReadArgsCalls { get; set; }
    public int FreeArgsCalls { get; set; }
    public int PrintFaultCalls { get; set; }
    public int IoErr { get; set; } = 0x13572468;
    public uint ResultArray { get; set; }
    public uint RdArgs { get; set; }
    public uint ParserStorage { get; set; }
    public List<int> Faults { get; } = [];
    public List<string> Events { get; } = [];
    public int Result { get; set; }
    public uint FinalSp { get; set; }
    public M68kCpuState? Cpu { get; set; }
}
