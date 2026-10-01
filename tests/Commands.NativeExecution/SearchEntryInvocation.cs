namespace CopperOS.Commands.NativeExecution;

internal sealed partial class Invocation
{
    public SearchEntryNativeLayout? SearchEntryLayout { get; set; }
    public int SearchReadArgsCalls { get; set; }
    public int SearchFreeArgsCalls { get; set; }
    public int SearchAllocMemCalls { get; set; }
    public int SearchFreeMemCalls { get; set; }
    public int SearchSetSignalCalls { get; set; }
    public int SearchMatchFirstCalls { get; set; }
    public int SearchMatchNextCalls { get; set; }
    public int SearchMatchEndCalls { get; set; }
    public int SearchFileOpenCalls { get; set; }
    public int SearchFileCloseCalls { get; set; }
    public int SearchReadCalls { get; set; }
    public int SearchReadOffset { get; set; }
    public int SearchConvToUpperCalls { get; set; }
    public int SearchIsCntrlCalls { get; set; }
    public int SearchIsPrintCalls { get; set; }
    public int SearchParsePatternCalls { get; set; }
    public int SearchMatchPatternCalls { get; set; }
    public int SearchWriteCalls { get; set; }
    public int SearchPrintFaultCalls { get; set; }
}
