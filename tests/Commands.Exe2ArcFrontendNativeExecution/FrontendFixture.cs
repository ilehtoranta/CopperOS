using System.Buffers.Binary;
using System.Text;
using Amiga;
using Copper68k;
using CopperOS.Commands.NativeExecution;

namespace CopperOS.Commands.Exe2ArcFrontendNativeExecution;

internal sealed class FrontendFixture(HunkImage image, M68kCpuModel model, bool runtime, bool typeDiagnostic = false)
{
    public const uint LoadAddress = 0x100000;
    public const uint ReturnAddress = 0x2000;
    private readonly FrontendBus bus = new();
    public List<object> Results { get; } = [];
    public object? LastFailure { get; private set; }

    public void Run()
    {
        bus.LoadAndProtect(LoadAddress, image.Code);
        RegisterExec(); RegisterDos(0x8000); RegisterDos(0xc000);
        var cases = typeDiagnostic ? FrontendCases.All().Where(c => c.Id is "unknown-type" or "typed-no-match" or "type-bound").ToArray() : runtime ? FrontendCases.All() : FrontendCases.Smoke();
        foreach (var definition in cases) Execute([definition], false);
        if (runtime && !typeDiagnostic)
        {
            Execute([cases[2] with { Id = "repeat-zip" }, cases[6] with { Id = "repeat-rar" }], false);
            Execute([cases[2] with { Id = "interleaved-zip" }, cases[6] with { Id = "interleaved-rar", StackBytes = 8192 }], true);
            Execute([cases[0] with { Id = "interleaved-parser-failure" }, cases[6] with { Id = "interleaved-success" }], true);
        }
        bus.AssertImageUnchanged();
    }

    private void Execute(FrontendCase[] definitions, bool interleaved)
    {
        var invocations = definitions.Select((c,i) => new FrontendInvocation(c,i)).ToArray();
        var cpus = new List<IM68kCore>();
        try
        {
            foreach (var invocation in invocations)
            {
                bus.Current = invocation;
                bus.Long(invocation.Process+(uint)DosLayout.Process.CommandLineInterface, invocation.Definition.Workbench ? 0u : 1u);
                bus.Long(0x4000+(uint)ExecLayout.ExecBase.ThisTask, invocation.Process);
                bus.Memory.AsSpan((int)(invocation.StackTop-invocation.StackBytes)-16, (int)invocation.StackBytes+32).Fill(0xcd);
                WriteCString(invocation.Arguments, "ignored\n");
                var cpu = M68kCoreFactory.Default.Create(model, bus); cpus.Add(cpu);
                invocation.Cpu = cpu.State; cpu.State.StatusRegister = 0;
                cpu.BeginSubroutine(LoadAddress, invocation.StackTop, ReturnAddress);
                cpu.State.D[0] = unchecked((uint)invocation.Definition.EntryLength);
                cpu.State.A[0] = invocation.Definition.NullArgument ? 0 : invocation.Arguments;
                for (int i=0; i<6; i++) cpu.State.D[i+2] = invocation.Preserved[i] = 0x22220000u+(uint)i;
                for (int i=0; i<5; i++) cpu.State.A[i+2] = invocation.Preserved[i+6] = 0xaaaa0000u+(uint)i;
            }
            var done = new bool[cpus.Count];
            var observations = new object[cpus.Count];
            while (done.Any(d => !d))
            for (int index=0; index<cpus.Count; index++)
            {
                if (done[index]) continue;
                var invocation = invocations[index]; var cpu = cpus[index]; bus.Current = invocation;
                bus.Long(0x4000+(uint)ExecLayout.ExecBase.ThisTask, invocation.Process);
                do
                {
                    Require(!cpu.State.Halted && !cpu.State.Stopped, "CPU halted or stopped.");
                    bus.AssertProgramCounter(cpu.State.ProgramCounter);
                    invocation.MinStack = Math.Min(invocation.MinStack, cpu.State.A[7]);
                    Require(cpu.State.A[7] >= invocation.StackTop-invocation.StackBytes && cpu.State.A[7] <= invocation.StackTop, "Stack limit exceeded.");
                    if (++invocation.Instructions > 50_000_000) throw new InvalidOperationException("Instruction bound exceeded.");
                    cpu.ExecuteInstruction();
                    if (cpu.State.ProgramCounter == ReturnAddress) { done[index] = true; break; }
                } while (!interleaved);
                if (done[index]) observations[index] = Complete(invocation, interleaved);
            }
            // Faster code may change which interleaved invocation finishes first.
            // Publish in request order so comparisons keep each invocation's identity.
            Results.AddRange(observations);
        }
        catch (Exception error)
        {
            var invocation = bus.Current!;
            LastFailure = new { id = invocation.Definition.Id, invocation.Instructions, pc = invocation.Cpu!.ProgramCounter,
                sp = invocation.Cpu.A[7], ioError = invocation.IoErr, events = invocation.Events, error = error.Message };
            throw;
        }
        finally { foreach (var cpu in cpus) cpu.Dispose(); bus.Current = null; }
    }

    private object Complete(FrontendInvocation invocation, bool interleaved)
    {
        var d = invocation.Definition; var cpu = invocation.Cpu!;
        invocation.Result = unchecked((int)cpu.D[0]); invocation.FinalSp = cpu.A[7];
        Require(invocation.Result == d.ExpectedResult, $"{d.Id}: result {invocation.Result}, expected {d.ExpectedResult}.");
        Require(invocation.IoErr == d.ExpectedIoError, $"{d.Id}: IoErr {invocation.IoErr}, expected {d.ExpectedIoError}.");
        Require(invocation.FinalSp == invocation.StackTop, "Entry SP was not restored.");
        Require(invocation.Opens == 1 && invocation.Closes == (d.MissingDos ? 0 : 1), "DOS library ownership differs.");
        bool parse = !d.MissingDos && !d.Workbench && d.EntryLength >= 0 && !(d.EntryLength != 0 && d.NullArgument);
        bool parsed = parse && !d.AllocationFailure && d.ReadArgsError is null;
        Require(invocation.ReadArgsCalls == (parse && !d.AllocationFailure ? 1 : 0), "ReadArgs call count differs.");
        Require(invocation.FreeArgsCalls == (parsed ? 1 : 0), "FreeArgs ownership differs.");
        Require(invocation.Faults.SequenceEqual(d.ExpectedFaults), "PrintFault codes differ.");
        Require(invocation.Stdout.ToString() == d.Text, $"{d.Id}: stdout differs: {invocation.Stdout}.");
        Require(invocation.Output.SequenceEqual(d.Output), $"{d.Id}: extracted output bytes differ ({invocation.Output.Count}/{d.Output.Length}).");
        Require(invocation.OpenPaths.SequenceEqual(d.OpenPaths), "Output naming or open order differs.");
        Require(invocation.Handles.Count == 0, "File handle leak.");
        Require(invocation.DeletedPaths.SequenceEqual(d.DeleteOutput ? new[] { d.OpenPaths.Last() } : Array.Empty<string>()), "Incomplete-output deletion differs.");
        Require(invocation.Waits == (d.Workbench ? 1 : 0) && invocation.Gets == invocation.Waits && invocation.Forbids == invocation.Waits && invocation.Replies == invocation.Waits, "Workbench message lifecycle differs.");
        foreach(var fault in d.Faults) Require(invocation.Occurrences.GetValueOrDefault(fault.Operation) >= fault.Occurrence, "Fault injection was not reached.");
        var allocations = new List<string>();
        if (parse && !d.AllocationFailure) allocations.Add("Results:12");
        if (parsed)
        {
            allocations.Add("RDArgs:2048");
            if (!d.MissingFrom && d.FailAllocation != "Workspace") allocations.Add("Workspace:103424");
            if (d.OpenPaths.Length > 0 && !d.Faults.Any(f => f.Operation == "Open" && f.Occurrence == 1 && f.Result == 0) && d.FailAllocation != "FIB") allocations.Add("FIB:260");
        }
        Require(invocation.Allocations.SequenceEqual(allocations), "Allocation ownership/size differs.");
        Require(invocation.Allocations.Order().SequenceEqual(invocation.Releases.Order()), "Allocation release set differs.");
        bus.AssertReleased(invocation); bus.AssertImageUnchanged();
        Require(bus.Memory.AsSpan((int)(invocation.StackTop-invocation.StackBytes)-16,16).IndexOfAnyExcept((byte)0xcd)<0 && bus.Memory.AsSpan((int)invocation.StackTop,16).IndexOfAnyExcept((byte)0xcd)<0, "Stack guard changed.");
        return new { name = d.Id, instructionInterleaved = interleaved, result = invocation.Result, ioErr = invocation.IoErr,
            stdoutHex = Convert.ToHexStringLower(Encoding.Latin1.GetBytes(invocation.Stdout.ToString())),
            outputHex = Convert.ToHexStringLower(invocation.Output.ToArray()), instructions = invocation.Instructions,
            configuredStackBytes = invocation.StackBytes, stackBytesWritten = invocation.StackTop-invocation.MinStack,
            peakStackBytes = invocation.StackTop-invocation.MinStack, entryStackRestored = true,
            readArgsCalls = invocation.ReadArgsCalls, freeArgsCalls = invocation.FreeArgsCalls, faults = invocation.Faults,
            openPaths = invocation.OpenPaths, deletedPaths = invocation.DeletedPaths, allocations = invocation.Allocations,
            releases = invocation.Releases, transfers = invocation.Transfers, events = invocation.Events };
    }

    private void Register(uint address, string name, Action<M68kCpuState> handler)
    {
        bus.RegisterGateway(address, state =>
        {
            bus.Current!.Events.Add(name); handler(state);
            state.D[1] = 0xd1d1d1d1; state.A[0] = 0xa0a0a0a0; state.A[1] = 0xa1a1a1a1;
            state.StatusRegister = (ushort)((state.StatusRegister & 0xffe0) | 0x001f);
        });
    }
    private FrontendFault? Next(string operation)
    {
        var current = bus.Current!; int count = current.Occurrences.GetValueOrDefault(operation)+1;
        current.Occurrences[operation] = count;
        return current.Definition.Faults.SingleOrDefault(f => f.Operation == operation && f.Occurrence == count);
    }
    private void RegisterExec()
    {
        Register(Gateway(0x4000,ExecLvo.FindTask), "FindTask", s => { Require(s.A[1]==0,"FindTask ABI."); s.D[0]=bus.Current!.Process; });
        Register(Gateway(0x4000,ExecLvo.OpenLibrary), "OpenLibrary", s => { Require(bus.CString(s.A[1])=="dos.library" && s.D[0]==37,"OpenLibrary ABI."); bus.Current!.Opens++; s.D[0]=bus.Current.Definition.MissingDos ? 0u : bus.Current.DosBase; });
        Register(Gateway(0x4000,ExecLvo.CloseLibrary), "CloseLibrary", s => { var c=bus.Current!; Require(s.A[1]==c.DosBase && c.Handles.Count==0,"CloseLibrary ownership."); bus.AssertReleased(c); c.Closes++; s.D[0]=0; });
        Register(Gateway(0x4000,ExecLvo.AllocMem), "AllocMem", s =>
        {
            var c=bus.Current!; string kind = c.AllocMemCalls++ == 0 ? "Results" : "Workspace";
            Require(s.D[0]==(kind=="Results"?12u:103424u) && s.D[1]==0x10001,"AllocMem size/flags.");
            s.D[0] = (kind=="Results" && c.Definition.AllocationFailure) || c.Definition.FailAllocation==kind ? 0 : bus.Allocate(c,s.D[0],kind);
            if (kind=="Workspace") c.Workspace=s.D[0];
        });
        Register(Gateway(0x4000,ExecLvo.FreeMem), "FreeMem", s => { var c=bus.Current!; string kind=s.D[0]==12?"Results":"Workspace"; bus.Release(c,s.A[1],kind,s.D[0]); c.FreeMemCalls++; c.IoErr=901; s.D[0]=0; });
        Register(Gateway(0x4000,ExecLvo.SetSignal), "SetSignal", s => { Require(s.D[0]==0 && s.D[1]==0,"SetSignal ABI."); var c=bus.Current!; s.D[0]=++c.SignalCalls==c.Definition.BreakAt ? 1u<<12 : 0; });
        Register(Gateway(0x4000,ExecLvo.WaitPort), "WaitPort", s => { var c=bus.Current!; Require(c.Definition.Workbench && s.A[0]==c.Process+(uint)DosLayout.Process.MessagePort,"WaitPort ABI."); c.Waits++; s.D[0]=c.Process+0x400; });
        Register(Gateway(0x4000,ExecLvo.GetMsg), "GetMsg", s => { var c=bus.Current!; Require(s.A[0]==c.Process+(uint)DosLayout.Process.MessagePort && c.Waits==1,"GetMsg ABI."); c.Gets++; s.D[0]=c.Process+0x400; });
        Register(Gateway(0x4000,ExecLvo.Forbid), "Forbid", s => { var c=bus.Current!; Require(c.Closes==(c.Definition.MissingDos?0:1),"Forbid before DOS cleanup."); c.Forbids++; s.D[0]=0; });
        Register(Gateway(0x4000,ExecLvo.ReplyMsg), "ReplyMsg", s => { var c=bus.Current!; Require(s.A[1]==c.Process+0x400 && c.Forbids==1,"ReplyMsg lifecycle."); c.Replies++; s.D[0]=0; });
    }
    private void RegisterDos(uint dos)
    {
        void Dos(int lvo, string name, Action<M68kCpuState> handler) =>
            Register(Gateway(dos,lvo), name, state =>
            {
                var current = bus.Current!;
                Require(current.DosBase == dos && current.Opens == 1 && current.Closes == 0 && !current.Definition.MissingDos,
                    "DOS vector used outside this invocation's library lease.");
                handler(state);
            });
        Dos(DosLvo.ReadArgs, "ReadArgs", ReadArgs);
        Dos(DosLvo.FreeArgs, "FreeArgs", s => { var c=bus.Current!; Require(s.D[1]==c.RdArgs && c.RdArgs!=0,"FreeArgs ownership."); bus.Release(c,c.RdArgs,"RDArgs"); c.FreeArgsCalls++; c.IoErr=902; s.D[0]=0; });
        Dos(DosLvo.IoErr, "IoErr", s => s.D[0]=unchecked((uint)bus.Current!.IoErr));
        Dos(DosLvo.SetIoErr, "SetIoErr", s => { int old=bus.Current!.IoErr; bus.Current.IoErr=unchecked((int)s.D[1]); s.D[0]=unchecked((uint)old); });
        Dos(DosLvo.PrintFault, "PrintFault", s => { Require(s.D[2]==0,"PrintFault header."); bus.Current!.Faults.Add(unchecked((int)s.D[1])); bus.Current.PrintFaultCalls++; s.D[0]=1; });
        Dos(DosLvo.PutStr, "PutStr", s => { bus.ValidateRead(s.D[1],1); bus.Current!.Stdout.Append(bus.CString(s.D[1])); s.D[0]=0; });
        Dos(DosLvo.VPrintf, "VPrintf", s =>
        {
            string format=bus.CString(s.D[1]); uint args=s.D[2];
            if(format=="Saved %lu byte to %s\n") { bus.ValidateSpan(bus.Current!,args,8,"Workspace"); bus.Current!.Stdout.Append($"Saved {bus.Long(args)} byte to {bus.CString(bus.Long(args+4))}\n"); }
            else if(format=="%s") { bus.ValidateRead(args,4); var str=bus.Long(args); bus.ValidateRead(str,1); bus.Current!.Stdout.Append(bus.CString(str)); }
            else throw new InvalidOperationException("Unexpected VPrintf format: "+format);
            s.D[0]=0;
        });
        Dos(DosLvo.Open, "Open", s =>
        {
            var c=bus.Current!; var path=bus.CString(s.D[1]); var fault=Next("Open"); c.OpenCalls++; c.OpenPaths.Add(path);
            Require(c.OpenCalls<=c.Definition.OpenPaths.Length && path==c.Definition.OpenPaths[c.OpenCalls-1],"Open path/order.");
            Require(s.D[2]==(c.OpenCalls==1?1005u:1006u),"Open mode.");
            c.IoErr=fault?.Error ?? 0; if(fault?.Result==0) { s.D[0]=0; return; }
            uint handle=c.OpenCalls==1 ? c.InputHandle : c.OutputHandle; Require(c.Handles.Add(handle),"Duplicate handle open."); s.D[0]=handle;
        });
        Dos(DosLvo.Close, "Close", s => { var c=bus.Current!; Require(c.Handles.Remove(s.D[1]),"Wrong/double handle close."); c.Events.Add(s.D[1]==c.InputHandle?"CloseInput":"CloseOutput"); c.IoErr=903; s.D[0]=1; });
        Dos(DosLvo.DeleteFile, "DeleteFile", s => { var c=bus.Current!; Require(!c.Handles.Contains(c.OutputHandle),"Delete before output close."); c.DeletedPaths.Add(bus.CString(s.D[1])); c.IoErr=904; s.D[0]=1; });
        Dos(DosLvo.AllocDosObject, "AllocDosObject", s => { var c=bus.Current!; Require(s.D[1]==(uint)DosObjectType.FileInfoBlock && s.D[2]==0,"AllocDosObject ABI."); if(c.Definition.FailAllocation=="FIB") { c.IoErr=103; s.D[0]=0; } else s.D[0]=c.Fib=bus.Allocate(c,260,"FIB"); });
        Dos(DosLvo.FreeDosObject, "FreeDosObject", s => { var c=bus.Current!; Require(s.D[1]==(uint)DosObjectType.FileInfoBlock && s.D[2]==c.Fib,"FreeDosObject ABI."); bus.Release(c,s.D[2],"FIB",260); c.IoErr=905; s.D[0]=0; });
        Dos(DosLvo.ExamineFH, "ExamineFH", s => { var c=bus.Current!; Require(s.D[1]==c.InputHandle && c.Handles.Contains(c.InputHandle) && s.D[2]==c.Fib,"ExamineFH ABI."); var fault=Next("ExamineFH"); c.IoErr=fault?.Error??0; bus.Long(c.Fib+FileInfoBlock.SizeOffset,c.Definition.DeclaredLength??(uint)c.Definition.Input.Length); s.D[0]=unchecked((uint)(fault?.Result??1)); });
        Dos(DosLvo.Seek, "Seek", s =>
        {
            var c=bus.Current!; Require(s.D[1]==c.InputHandle && c.Handles.Contains(c.InputHandle),"Seek handle."); var fault=Next("Seek"); int mode=unchecked((int)s.D[3]); int offset=unchecked((int)s.D[2]); int previous=c.Cursor; int result=fault?.Result??previous;
            if(result>=0) { long next=mode==-1?offset:mode==0?(long)c.Cursor+offset:mode==1?(long)c.Definition.Input.Length+offset:-1; Require(next>=0 && next<=c.Definition.Input.Length,"Seek bounds/mode."); c.Cursor=(int)next; }
            c.IoErr=fault?.Error??0; c.Transfers.Add(new { operation="Seek", offset, mode, result }); s.D[0]=unchecked((uint)result);
        });
        Dos(DosLvo.Read, "Read", s => Transfer(s,false));
        Dos(DosLvo.Write, "Write", s => Transfer(s,true));
        Dos(DosLvo.FilePart, "FilePart", s => { var text=bus.CString(s.D[1]); int at=Math.Max(text.LastIndexOf(':'),text.LastIndexOf('/'))+1; s.D[0]=s.D[1]+(uint)at; });
        Dos(DosLvo.AddPart, "AddPart", s => { var c=bus.Current!; var fault=Next("AddPart"); Require(s.D[3]==512,"AddPart capacity."); bus.ValidateSpan(c,s.D[1],512,"Workspace"); string a=bus.CString(s.D[1]),b=bus.CString(s.D[2]); string joined=a.Length==0?b:a+(a.EndsWith(':')||a.EndsWith('/')?"":"/")+b; Require(joined.Length<512,"AddPart overflow."); c.IoErr=fault?.Error??0; if(fault?.Result==0) s.D[0]=0; else { WriteCString(s.D[1],joined); s.D[0]=1; } });
    }
    private void Transfer(M68kCpuState s, bool write)
    {
        var c=bus.Current!; string operation=write?"Write":"Read"; var fault=Next(operation);
        Require(s.D[1]==(write?c.OutputHandle:c.InputHandle) && c.Handles.Contains(s.D[1]),operation+" handle.");
        int bytes=checked((int)s.D[3]); Require(bytes>0 && bytes<=102400,operation+" count."); bus.ValidateSpan(c,s.D[2],bytes,"Workspace");
        Require(s.D[2] >= c.Workspace && (ulong)s.D[2]+(uint)bytes <= (ulong)c.Workspace+102400, operation+" scratch bounds.");
        int result=fault?.Result??(write?bytes:Math.Min(bytes,c.Definition.Input.Length-c.Cursor)); Require(result<=bytes,"Injected oversized transfer.");
        if(result>0) { if(write) c.Output.AddRange(bus.Memory.AsSpan((int)s.D[2],result).ToArray()); else { Require(result<=c.Definition.Input.Length-c.Cursor,"Read beyond EOF."); c.Definition.Input.AsSpan(c.Cursor,result).CopyTo(bus.Memory.AsSpan((int)s.D[2])); c.Cursor+=result; } }
        c.IoErr=fault?.Error??0; c.Transfers.Add(new { operation, requested=bytes, result }); s.D[0]=unchecked((uint)result);
    }
    private void ReadArgs(M68kCpuState s)
    {
        var c=bus.Current!; Require(bus.CString(s.D[1])=="FROM/A,TO,TYPE/K" && s.D[3]==0,"ReadArgs ABI."); bus.ValidateSpan(c,s.D[2],12,"Results"); c.ReadArgsCalls++;
        Require(bus.Memory.AsSpan((int)s.D[2],12).IndexOfAnyExcept((byte)0)<0,"Uncleared parser results.");
        if(c.Definition.ReadArgsError is {} error) { c.IoErr=error; s.D[0]=0; return; }
        uint storage=bus.Allocate(c,2048,"RDArgs"); c.RdArgs=storage;
        WriteCString(storage+32,c.Definition.From); if(c.Definition.Type is {} type) WriteCString(storage+1024,type); if(c.Definition.To is {} to) WriteCString(storage+1536,to);
        bus.Long(s.D[2],c.Definition.MissingFrom?0:storage+32); bus.Long(s.D[2]+4,c.Definition.To is null?0:storage+1536); bus.Long(s.D[2]+8,c.Definition.Type is null?0:storage+1024);
        c.IoErr=0; s.D[0]=storage;
    }
    private void WriteCString(uint address,string text) { var bytes=Encoding.Latin1.GetBytes(text); bytes.CopyTo(bus.Memory.AsSpan((int)address)); bus.Memory[(int)address+bytes.Length]=0; }
    private static uint Gateway(uint address,int lvo) => unchecked((uint)((int)address+lvo));
    private static void Require(bool condition,string message) { if(!condition) throw new InvalidOperationException(message); }
}
