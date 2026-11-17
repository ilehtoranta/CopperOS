using System.Buffers.Binary;
using Copper68k;
using CopperSharp.Compiler;
using CopperSharp.Targets.Amiga;
using CopperOS.MathTrans.LibraryBuilder;

namespace CopperOS.MathTrans.Tests;

public sealed class OriginalMathTransFactAttribute : FactAttribute
{
    public static string Reference => Environment.GetEnvironmentVariable("COPPEROS_ORIGINAL_MATHTRANS") ??
        Path.GetFullPath("../../../../../artifacts/mathtrans-reference/mathtrans.library", AppContext.BaseDirectory);
    public static string Rom => Environment.GetEnvironmentVariable("COPPERSHARP_KICKSTART31_ROM") ?? "C:/Data/ROM/kickstart-3.1-a500.rom";
    public OriginalMathTransFactAttribute()
    {
        if (!File.Exists(Reference) || !File.Exists(Rom))
            Skip = "Set COPPEROS_ORIGINAL_MATHTRANS and COPPERSHARP_KICKSTART31_ROM to owned Workbench 3.1 reference files.";
    }
}

public class OriginalTests
{
    [OriginalMathTransFact]
    public void HyperbolicUnderflowMatchesOriginalAndResumesAfterTrap()
    {
        using var original=new OriginalGuest(File.ReadAllBytes(OriginalMathTransFactAttribute.Reference));
        var build=AmigaM68kCompiler.Compile(new M68kCompilationRequest
        {
            AssemblyPath=typeof(MathTransNativeLibrary).Assembly.Location, EntryPoint="CopperOS.MathTrans.MathTransNativeLibrary::LibraryImageEntry",
            IncludedExportNames=MathTransLibraryPackager.ExportNames, ManagedAssemblyPaths=Directory.GetFiles(AppContext.BaseDirectory,"CopperSharp.*.dll"),
            Cpu=M68kCpuTarget.M68000, FloatingPoint=M68kFloatingPointMode.Disabled, ClrPolicy=M68kClrPolicy.Always,
            ExceptionMode=M68kExceptionMode.Yolo, OutputFormat=M68kOutputFormat.Hunk,
            RuntimeProfile=M68kRuntimeProfile.Resident, MemoryManagement=M68kMemoryManagement.None,
            PeepholeOptimization=M68kPeepholeOptimizationMode.Disabled,
        });
        using var candidate=new OriginalGuest(MathTransLibraryPackager.Build(build.Code,build.Relocations,build.Symbols).Image);
        foreach(uint ffp in new uint[]{0xa00000c6,0xac0000c6,0xb00000c6,0xb0cccdc6,0xb17217c6,0xb17218c6,0xb17219c6,0xb1721ac6,0xb40000c6,0xc00000c6,0x800000c7,0xc80000c7})
        foreach(uint lvo in new uint[]{60,66})
        {
            var expected=original.CallWithTraps(lvo,ffp,0); var actual=candidate.CallWithTraps(lvo,ffp,0);
            Assert.True(expected.Traps==actual.Traps,$"LVO -{lvo} {ffp:x8}: traps {expected.Traps}/{actual.Traps}");
            Assert.True((expected.Ccr&14)==(actual.Ccr&14),$"LVO -{lvo} {ffp:x8}: flags {expected.Ccr:x}/{actual.Ccr:x}");
            CompareNumeric(expected.Value,actual.Value,0.0001,$"LVO -{lvo} {ffp:x8}");
            if (ffp >= 0xb17219c6 && (ffp & 0x7f) == 0x46 && lvo == 60) Assert.Equal(1u,actual.Traps);
            if (lvo == 66) Assert.Equal(0u,actual.Traps);
        }
    }
    [OriginalMathTransFact]
    public void MatchesOriginalVectorsAndDocumentedFlags()
    {
        var build=AmigaM68kCompiler.Compile(new M68kCompilationRequest
        {
            AssemblyPath=typeof(MathTransNativeLibrary).Assembly.Location,
            EntryPoint="CopperOS.MathTrans.MathTransNativeLibrary::LibraryImageEntry",
            IncludedExportNames=MathTransLibraryPackager.ExportNames,
            ManagedAssemblyPaths=Directory.GetFiles(AppContext.BaseDirectory,"CopperSharp.*.dll"),
            Cpu=M68kCpuTarget.M68000, FloatingPoint=M68kFloatingPointMode.Disabled,
            ClrPolicy=M68kClrPolicy.Always, ExceptionMode=M68kExceptionMode.Yolo,
            OutputFormat=M68kOutputFormat.Hunk, RuntimeProfile=M68kRuntimeProfile.Resident,
            MemoryManagement=M68kMemoryManagement.None, PeepholeOptimization=M68kPeepholeOptimizationMode.Disabled,
        });
        using var original=new OriginalGuest(File.ReadAllBytes(OriginalMathTransFactAttribute.Reference));
        using var candidate=new OriginalGuest(MathTransLibraryPackager.Build(build.Code,build.Relocations,build.Symbols).Image);
        uint[] ordinary=[0,0x80000040,0x800000c0,0x80000041,0x800000c1,0x80000042,0x80000045,0x800000c5,0x80000047,0x800000c7];
        foreach(uint lvo in new uint[]{30,36,42,48,60,66,72,78,84,96,114,120,126})
        foreach(uint x in ordinary)
        {
            (uint Value,ushort Ccr) expected,actual;
            try { expected=original.Call(lvo,x,0); actual=candidate.Call(lvo,x,0); }
            catch(Exception e) { throw new InvalidOperationException($"LVO -{lvo}, input {x:x8}",e); }
            Assert.True((expected.Ccr&14)==(actual.Ccr&14),$"LVO -{lvo}, {x:x8}: flags {expected.Ccr:x}/{actual.Ccr:x}");
            CompareNumeric(expected.Value,actual.Value,0.0001,$"LVO -{lvo}, {x:x8}");
        }
        foreach(uint lvo in new uint[]{30,36,42,48,60,66,72,78,84,96,114,120,126})
        for(int n=-20;n<=20;n++)
        {
            uint ieee=BitConverter.SingleToUInt32Bits(n/10f);
            Assert.True(FfpConversions.TryFromIeee(ieee,out uint input));
            var expected=original.Call(lvo,input,0); var actual=candidate.Call(lvo,input,0);
            int mask = 14;
            Assert.True((expected.Ccr&mask)==(actual.Ccr&mask),$"LVO -{lvo}, {input:x8}: flags {expected.Ccr:x}/{actual.Ccr:x}, values {expected.Value:x8}/{actual.Value:x8}");
            CompareNumeric(expected.Value,actual.Value,0.0001,$"LVO -{lvo}, {input:x8}");
        }
        // Original aborts at 2^20, including negative and maximum FFP inputs.
        foreach(uint lvo in new uint[]{36,42,48})
        foreach(uint x in new uint[]{0x80000055,0x800000d5,0xabcdef55,0xffffff7f})
        {
            var expected=original.Call(lvo,x,0); var actual=candidate.Call(lvo,x,0);
            Assert.Equal(expected.Value,actual.Value); Assert.Equal(expected.Ccr&14,actual.Ccr&14);
        }
        for(uint exponent=66; exponent<=84; exponent++)
        foreach(uint sign in new uint[]{0,0x80})
        {
            uint x=0x80000000|exponent|sign;
            Assert.True(FfpConversions.TryToIeee(x,out uint bits));
            double input=BitConverter.UInt32BitsToSingle(bits);
            foreach(uint lvo in new uint[]{36,42})
            {
                var actual=candidate.Call(lvo,x,0);
                Assert.True(FfpConversions.TryToIeee(actual.Value,out uint result));
                double wanted=lvo==36?Math.Sin(input):Math.Cos(input);
                double got=BitConverter.UInt32BitsToSingle(result);
                Assert.True(Math.Abs(wanted-got)<0.001,$"large LVO -{lvo}, {x:x8}: {wanted} vs {got}");
            }
        }
        var random=new Random(0x31);
        for(int i=0;i<128;i++)
        {
            uint ffp=((uint)random.Next(0x800000,0x1000000)<<8) | (uint)random.Next(1,128) | (i%2==0?0u:0x80u);
            foreach(uint lvo in new uint[]{84,96,126})
            {
                var expected=original.Call(lvo,ffp,0);var actual=candidate.Call(lvo,ffp,0);
                Assert.True((expected.Ccr&14)==(actual.Ccr&14),$"sample LVO -{lvo} {ffp:x8}: flags {expected.Ccr:x}/{actual.Ccr:x}");
                CompareNumeric(expected.Value,actual.Value,0.0001,$"sample LVO -{lvo} {ffp:x8}");
            }
        }
        for(int i=0;i<96;i++)
        {
            float input=(float)((random.NextDouble()*2-1)*1048575);
            Assert.True(FfpConversions.TryFromIeee(BitConverter.SingleToUInt32Bits(input),out uint ffp));
            foreach(uint lvo in new uint[]{36,42})
            {
                var actual=candidate.Call(lvo,ffp,0);
                Assert.True(FfpConversions.TryToIeee(actual.Value,out uint bits));
                double wanted=lvo==36?Math.Sin(input):Math.Cos(input);
                double got=BitConverter.UInt32BitsToSingle(bits);
                Assert.True(Math.Abs(wanted-got)<0.001,$"sample LVO -{lvo}, {input}: {wanted} vs {got}");
            }
        }
        foreach(uint x in new uint[]{0,0x80000040,0x800000c0,0x80000041,0x80000055,0x800000d5})
        {
            var expected=original.Sincos(x);var actual=candidate.Sincos(x);
            Assert.Equal(expected.Ccr&14,actual.Ccr&14);
            CompareNumeric(expected.Value,actual.Value,0.0001,$"SPSincos sine {x:x8}");
            if((x&0x7f)>=85) Assert.Equal(expected.Cos,actual.Cos);
            else CompareNumeric(expected.Cos,actual.Cos,0.0001,$"SPSincos cosine {x:x8}");
        }
        foreach(uint x in new uint[]{0x80000055,0x800000d5,0xabcdef55,0xffffff7f})
        foreach(uint d6 in new uint[]{0,1,0x12345678,0x80000000,0xffffffff})
        {
            var expected=original.Sincos(x,d6); var actual=candidate.Sincos(x,d6);
            Assert.Equal(expected,actual);
            Assert.Equal(d6,actual.Value);
        }
        foreach(uint bits in new uint[]{0,0x80000000,1,0x1f7fffff,0x1f800000,0x3f800000,0xbf800000,0x5effffff,0x5f000000,0xdf000000,0x7f800000,0xff800000,0x7fc00000,0xffffffff})
        {
            var expected=original.Call(108,bits,0);var actual=candidate.Call(108,bits,0);
            Assert.Equal(expected.Value,actual.Value);Assert.Equal(expected.Ccr&6,actual.Ccr&6);
        }
        foreach(uint ffp in new uint[]{0,0x80000001,0x80000041,0x800000c1,0xffffff7f})
        {
            var expected=original.Call(102,ffp,0);var actual=candidate.Call(102,ffp,0);
            Assert.Equal(expected.Value,actual.Value);Assert.Equal(expected.Ccr&12,actual.Ccr&12);
        }
        foreach(var (x,y) in new (uint,uint)[]{(0,0),(0,0x80000041),(0,0x800000c1),(0x800000c1,0x80000042),(0x800000c1,0x80000041),(0x80000041,0),(0x80000042,0x80000041)})
        {
            var expected=original.Call(90,x,y);var actual=candidate.Call(90,x,y);
            Assert.Equal(expected.Ccr&14,actual.Ccr&14); CompareNumeric(expected.Value,actual.Value,0.0001,$"SPPow {x:x8},{y:x8}");
        }
    }
    private static void CompareNumeric(uint expected,uint actual,double tolerance,string label)
    {
        if(expected==actual)return;
        bool validExpected=FfpConversions.TryToIeee(expected,out uint e);
        bool validActual=FfpConversions.TryToIeee(actual,out uint a);
        Assert.True(validExpected && validActual,$"{label}: {expected:x8} != {actual:x8}");
        double ev=BitConverter.UInt32BitsToSingle(e), av=BitConverter.UInt32BitsToSingle(a);
        Assert.True(Math.Abs(ev-av)<=tolerance*Math.Max(1,Math.Abs(ev)),$"{label}: {expected:x8} ({ev}) != {actual:x8} ({av})");
    }
}

internal sealed class OriginalGuest : IDisposable
{
    private readonly NativeTests.Bus bus = new();
    private readonly IM68kCore cpu;
    public OriginalGuest(byte[] file)
    {
        bus.UseKickstartMathFfp(File.ReadAllBytes(OriginalMathTransFactAttribute.Rom));
        // Flat-bus vector 5 handler counts the CPU exception and returns via RTE.
        bus.W32(20,0x2e000); bus.W16(0x2e000,0x52b9); bus.W32(0x2e002,0x2e100); bus.W16(0x2e006,0x4e73);
        int at=0; uint Long() { uint v=BinaryPrimitives.ReadUInt32BigEndian(file.AsSpan(at,4)); at+=4; return v; }
        Assert.Equal(1011u,Long()); Assert.Equal(0u,Long()); uint count=Long(), first=Long(), last=Long();
        var bases=new uint[count]; var sizes=new uint[count]; uint next=NativeTests.Bus.Code;
        for(uint i=first;i<=last;i++) { sizes[i]=(Long()&0x3fffffff)*4; bases[i]=next; next+=(sizes[i]+3)&~3u; }
        uint hunk=first;
        while(at<file.Length)
        {
            uint type=Long()&0x3fffffff;
            if(type is 1001 or 1002) { uint bytes=Long()*4; file.AsSpan(at,(int)bytes).CopyTo(bus.Memory.AsSpan((int)bases[hunk])); at+=(int)bytes; }
            else if(type==1003) Assert.Equal(sizes[hunk],Long()*4);
            else if(type==1004) { uint n; while((n=Long())!=0) { uint target=Long(); while(n--!=0) { uint site=bases[hunk]+Long(); bus.W32(site,bus.R32(site)+bases[target]); } } }
            else if(type==1010) hunk++;
            else throw new InvalidDataException($"Unsupported reference HUNK {type}");
        }
        uint resident=bases[0]; while (resident<bases[0]+128 && bus.R16(resident)!=0x4afc) resident+=2;
        Assert.Equal((ushort)0x4afc,bus.R16(resident));
        uint auto=bus.R32(resident+22), table=bus.R32(auto+4), initStruct=bus.R32(auto+8), init=bus.R32(auto+12);
        uint vector=NativeTests.Bus.Library-6, cursor=table;
        if(bus.R16(cursor)==0xffff)
        { cursor+=2; while(bus.R16(cursor)!=0xffff) { bus.W16(vector,0x4ef9); bus.W32(vector+2,table+(uint)(short)bus.R16(cursor)); vector-=6; cursor+=2; } }
        else while(bus.R32(cursor)!=uint.MaxValue) { bus.W16(vector,0x4ef9); bus.W32(vector+2,bus.R32(cursor)); vector-=6; cursor+=4; }
        bus.W16(NativeTests.Bus.Library+16,(ushort)(NativeTests.Bus.Library-vector-6)); bus.W16(NativeTests.Bus.Library+18,(ushort)bus.R32(auto));
        if(initStruct!=0) Initialize(initStruct);
        cpu=M68kCoreFactory.Default.Create(M68kCpuModel.M68000,bus);
        cpu.State.ResetStackPointers(0xe00000,0xf00000,false);
        Execute(init,NativeTests.Bus.Library,0,NativeTests.Bus.Exec,0x777);
        Assert.Equal(NativeTests.Bus.Library,cpu.State.D[0]);
        Execute(NativeTests.Bus.Library-6,0,0,NativeTests.Bus.Library);
        Assert.Equal(NativeTests.Bus.Library,cpu.State.D[0]);
    }
    private void Initialize(uint at)
    {
        uint offset=0;
        for(;;)
        {
            at=(at+1)&~1u; uint command=bus.Memory[at++]; if(command==0) return;
            uint mode=command>>6,kind=(command>>4)&3,n=(command&15)+1;
            if(mode==2) offset=bus.Memory[at++];
            if(mode==3) { offset=((uint)bus.Memory[at]<<16)|((uint)bus.Memory[at+1]<<8)|bus.Memory[at+2];at+=3; }
            uint width=kind==0?4u:kind==1?2u:1u,value=0;
            for(uint i=0;i<n;i++)
            {
                if(i==0||mode!=1) { if(width>1)at=(at+1)&~1u; value=width==4?bus.R32(at):width==2?bus.R16(at):bus.Memory[at];at+=width; }
                uint dest=NativeTests.Bus.Library+offset;
                if(width==4)bus.W32(dest,value);else if(width==2)bus.W16(dest,(ushort)value);else bus.Memory[dest]=(byte)value;
                offset+=width;
            }
        }
    }
    private void Execute(uint address,uint d0,uint d1,uint a6,uint a0=0,uint d2=0,uint d6=0)
    {
        cpu.State.StatusRegister=0;cpu.BeginSubroutine(address,0xf00000,0xff0000);
        for(int i=0;i<8;i++)cpu.State.D[i]=0;for(int i=0;i<7;i++)cpu.State.A[i]=0;
        cpu.State.D[0]=d0;cpu.State.D[1]=d1;cpu.State.D[2]=d2;cpu.State.D[6]=d6;cpu.State.A[0]=a0;cpu.State.A[6]=a6;
        int steps=0; var trace=new Queue<string>(); while(cpu.State.ProgramCounter!=0xff0000&&!cpu.State.Halted&&steps++<4000000) { trace.Enqueue($"{cpu.State.ProgramCounter:x} a6={cpu.State.A[6]:x} d0={cpu.State.D[0]:x}"); if(trace.Count>16)trace.Dequeue(); if(cpu.State.ProgramCounter<0x8000) throw new InvalidOperationException(string.Join(" ",trace)); cpu.ExecuteInstruction(); }
        Assert.True(cpu.State.ProgramCounter==0xff0000,$"PC {cpu.State.ProgramCounter:x8}, address {address:x8}, D0 {d0:x8}, D1 {d1:x8}, steps {steps}");
        Assert.Equal(0xf00000u,cpu.State.A[7]);
        Assert.Equal(d2,cpu.State.D[2]);
        for(int i=3;i<8;i++) Assert.Equal(i==6?d6:0u,cpu.State.D[i]);
        for(int i=2;i<6;i++) Assert.Equal(0u,cpu.State.A[i]);
        Assert.Equal(a6,cpu.State.A[6]);
    }
    public (uint Value,ushort Ccr) Call(uint lvo,uint d0,uint d1)
    { Execute(NativeTests.Bus.Library-lvo,d0,d1,NativeTests.Bus.Library);return(cpu.State.D[0],(ushort)(cpu.State.StatusRegister&15)); }
    public (uint Value,ushort Ccr,uint Traps) CallWithTraps(uint lvo,uint d0,uint d1)
    { bus.W32(0x2e100,0); var r=Call(lvo,d0,d1);return(r.Value,r.Ccr,bus.R32(0x2e100)); }
    public (uint Value,ushort Ccr,uint Cos) Sincos(uint x,uint d6=0)
    { bus.W32(0x30000,0x12345678); Execute(NativeTests.Bus.Library-54,x,0x30000,NativeTests.Bus.Library,0x30000,7,d6); return (cpu.State.D[0],(ushort)(cpu.State.StatusRegister&15),bus.R32(0x30000)); }
    public void Dispose()=>cpu.Dispose();
}

