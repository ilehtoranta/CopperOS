using Copper68k;

namespace CopperOS.Commands.Exe2ArcFrontendNativeExecution;

internal sealed class FrontendInvocation(FrontendCase definition, int slot)
{
    public FrontendCase Definition { get; } = definition;
    public uint Process { get; } = 0x30000;
    public uint StackTop { get; } = (uint)(0x50000 + slot * 0x10000);
    public uint StackBytes { get; } = 16384;
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
