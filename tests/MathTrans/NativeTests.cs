using System.Buffers.Binary;
using System.Text;
using Copper68k;
using CopperOS.MathTrans.LibraryBuilder;
using CopperSharp.Compiler;
using CopperSharp.Targets.Amiga;

namespace CopperOS.MathTrans.Tests;

public class NativeTests
{
    [Fact]
    public void PackagedVectorsExecuteOn68000()
    {
        var result = AmigaM68kCompiler.Compile(new M68kCompilationRequest
        {
            AssemblyPath = typeof(MathTransNativeLibrary).Assembly.Location,
            EntryPoint = "CopperOS.MathTrans.MathTransNativeLibrary::LibraryImageEntry",
            IncludedExportNames = MathTransLibraryPackager.ExportNames,
            ManagedAssemblyPaths = Directory.GetFiles(AppContext.BaseDirectory, "CopperSharp.*.dll"),
            Cpu = M68kCpuTarget.M68000, FloatingPoint = M68kFloatingPointMode.Disabled,
            ClrPolicy = M68kClrPolicy.Always, ExceptionMode = M68kExceptionMode.Yolo,
            OutputFormat = M68kOutputFormat.Hunk, RuntimeProfile = M68kRuntimeProfile.Resident,
            MemoryManagement = M68kMemoryManagement.None, PeepholeOptimization = M68kPeepholeOptimizationMode.Disabled,
        });
        Assert.True(result.FrameworkAnalysis.IsCompatible);
        Assert.Empty(result.FrameworkAnalysis.ManagedAllocationSites);
        Assert.Empty(result.NativeCompatibility.RuntimeHelpers);
        Assert.Empty(result.NativeCompatibility.ExternalNativeTargets);
        var artifact = MathTransLibraryPackager.Build(result.Code, result.Relocations, result.Symbols);
        var hunk = artifact.Image;
        Assert.Contains(MathTransResidentMetadata.DevelopmentIdString,
            Encoding.ASCII.GetString(hunk));
        Assert.Contains(MathTransResidentMetadata.DevelopmentIdStringTail,
            Encoding.ASCII.GetString(artifact.Code));
        uint ReadHunk(int at) => BinaryPrimitives.ReadUInt32BigEndian(hunk.AsSpan(at,4));
        Assert.Equal(0x3f3u,ReadHunk(0)); Assert.Equal(0u,ReadHunk(4));
        Assert.Equal(1u,ReadHunk(8)); Assert.Equal(0u,ReadHunk(12)); Assert.Equal(0u,ReadHunk(16));
        Assert.Equal(0x3e9u,ReadHunk(24));
        int codeSize=checked((int)ReadHunk(28)*4);
        Assert.Equal(artifact.Code.Length,codeSize);
        Assert.Equal(artifact.Code,hunk.AsSpan(32,codeSize).ToArray());
        int cursor=32+codeSize;
        Assert.Equal(0x3ecu,ReadHunk(cursor)); cursor+=4;
        Assert.Equal((uint)artifact.Relocations.Count,ReadHunk(cursor)); cursor+=4;
        Assert.Equal(0u,ReadHunk(cursor)); cursor+=4;
        foreach (int relocation in artifact.Relocations) { Assert.Equal((uint)relocation,ReadHunk(cursor)); cursor+=4; }
        Assert.Equal(0u,ReadHunk(cursor)); Assert.Equal(0x3f2u,ReadHunk(cursor+4));
        foreach (uint loadBase in new uint[] { 0x40000, 0x70000 })
        {
            byte[] relocated=hunk.AsSpan(32,codeSize).ToArray();
            foreach (int relocation in artifact.Relocations)
            {
                uint value=BinaryPrimitives.ReadUInt32BigEndian(relocated.AsSpan(relocation,4));
                BinaryPrimitives.WriteUInt32BigEndian(relocated.AsSpan(relocation,4),value+loadBase);
            }
            int resident=artifact.Layout.Resident;
            Assert.Equal((ushort)0x4afc,BinaryPrimitives.ReadUInt16BigEndian(relocated.AsSpan(resident,2)));
            Assert.Equal(loadBase+(uint)resident,BinaryPrimitives.ReadUInt32BigEndian(relocated.AsSpan(resident+2,4)));
        }
        var bus = new Bus();
        foreach (int offset in artifact.Relocations)
        {
            uint address = BinaryPrimitives.ReadUInt32BigEndian(artifact.Code.AsSpan(offset,4));
            BinaryPrimitives.WriteUInt32BigEndian(artifact.Code.AsSpan(offset,4), address + Bus.Code);
        }
        artifact.Code.CopyTo(bus.Memory.AsSpan((int)Bus.Code));
        for (int i=0; i<MathTransLibraryPackager.FunctionTableExports.Count; i++)
        {
            uint v = Bus.Library - (uint)((i+1)*6);
            bus.W16(v,0x4ef9); bus.W32(v+2,Bus.Code + artifact.Exports[MathTransLibraryPackager.FunctionTableExports[i]]);
        }
        using var cpu = M68kCoreFactory.Default.Create(M68kCpuModel.M68000, bus);
        uint Call(uint address, uint x = 0, uint y = 0, bool init = false)
        {
            cpu.BeginSubroutine(address,0xf00000,0xff0000);
            for (int i=0;i<8;i++) cpu.State.D[i] = 0xd0000000u+(uint)i;
            for (int i=0;i<7;i++) cpu.State.A[i] = 0xa0000000u+(uint)i;
            cpu.State.A[6] = init ? Bus.Exec : Bus.Library;
            cpu.State.D[0] = x; cpu.State.D[1] = y;
            if (init) cpu.State.A[0] = 0x777;
            int steps=0; while (cpu.State.ProgramCounter != 0xff0000 && !cpu.State.Halted && steps++ < 4000000) cpu.ExecuteInstruction();
            Assert.Equal(0xff0000u,cpu.State.ProgramCounter);
            for (int i=2;i<8;i++) Assert.Equal(0xd0000000u+(uint)i,cpu.State.D[i]);
            for (int i=2;i<6;i++) Assert.Equal(0xa0000000u+(uint)i,cpu.State.A[i]);
            return cpu.State.D[0];
        }
        Assert.Equal(Bus.Library,Call(Bus.Code+artifact.Exports["copperos.mathtrans.Init"],Bus.Library,init:true));
        Assert.Equal(40, bus.R16(Bus.Library+20));
        Assert.Equal(Bus.Library,Call(Bus.Library-6));
        uint Vector(int index) => Bus.Library-(uint)((index+5)*6);
        uint one=0x80000041;
        // Exercise every public vector through its actual negative-table entry.
        for (int i=0;i<17;i++)
        {
            uint arg = i == 10 ? one : i == 13 ? 0x3f800000u : 0;
            uint second = i == 4 ? 0x30000u : i == 10 ? one : 0;
            uint value=Call(Vector(i),arg,second);
            switch(i)
            {
                case 1: case 3: case 4: case 5: case 7: case 11: case 12: case 14: Assert.Equal(0u,value); break;
                case 2: case 6: case 8: case 10: case 13: Assert.Equal(one,value); break;
            }
            if (i==4) Assert.Equal(one,bus.R32(second));
        }
        Assert.Equal(0u,Call(Vector(1),0));
        Assert.NotEqual(0,cpu.State.StatusRegister & 4); // Z
        Assert.Equal(0,cpu.State.StatusRegister & 8); // N
        Call(Vector(1),0x800000c0); // sin(-0.5)
        Assert.Equal(0,cpu.State.StatusRegister & 4);
        Assert.NotEqual(0,cpu.State.StatusRegister & 8);
        Call(Vector(9),0); Assert.NotEqual(0,cpu.State.StatusRegister & 2); // log(0) domain error
        Call(Vector(9),0x800000c1); Assert.NotEqual(0,cpu.State.StatusRegister & 2);
        Call(Vector(11),0x800000c1); Assert.NotEqual(0,cpu.State.StatusRegister & 2);
        Call(Vector(11),0); Assert.Equal(0,cpu.State.StatusRegister & 2);
        Call(Vector(8),0xc8000047); Assert.NotEqual(0,cpu.State.StatusRegister & 2);
        Call(Vector(13),0x7f800000); Assert.NotEqual(0,cpu.State.StatusRegister & 2);
        Call(Vector(10),0x800000c1,0x80000042); Assert.NotEqual(0,cpu.State.StatusRegister & 2);
        double[] expected = [Math.Atan(.5),Math.Sin(.5),Math.Cos(.5),Math.Tan(.5),Math.Sin(.5),
            Math.Sinh(.5),Math.Cosh(.5),Math.Tanh(.5),Math.Exp(.5),Math.Log(.5),Math.Pow(.5,2),
            Math.Sqrt(.5),.5,.5,Math.Asin(.5),Math.Acos(.5),Math.Log10(.5)];
        for (int i=0;i<expected.Length;i++)
        {
            uint input = i == 13 ? 0x3f000000u : 0x80000040u;
            uint second = i == 4 ? 0x30000u : i == 10 ? 0x80000042u : 0;
            uint value = Call(Vector(i),input,second);
            double actual;
            if (i == 12) actual = BitConverter.UInt32BitsToSingle(value);
            else { Assert.True(FfpConversions.TryToIeee(value,out var bits)); actual = BitConverter.UInt32BitsToSingle(bits); }
            Assert.InRange(Math.Abs(actual-expected[i]),0,0.00003);
            if (i==4) { Assert.True(FfpConversions.TryToIeee(bus.R32(second),out var bits)); Assert.InRange(Math.Abs(BitConverter.UInt32BitsToSingle(bits)-Math.Cos(.5)),0,0.00003); }
        }
        Assert.Equal(0u,Call(Bus.Library-18)); // An open library defers expunge.
        Assert.Equal(0x777u,Call(Bus.Library-12));
        Assert.True(bus.Removed && bus.Freed && bus.Closed);
    }

    internal sealed class Bus : IM68kBus
    {
        public const uint Code=0x40000, Exec=0x10000, Library=0x18000, Ffp=0x20000;
        public readonly byte[] Memory=new byte[0x1000000];
        private readonly Dictionary<uint,Action<M68kCpuState>> gates=new();
        private uint next=0x28000;
        public bool Removed, Freed, Closed;
        public Bus()
        {
            W32(4,Exec);
            Gate(Exec,-552,s=>s.D[0]=Ffp); Gate(Exec,-132,_=>{}); Gate(Exec,-138,_=>{});
            Gate(Exec,-252,_=>Removed=true); Gate(Exec,-210,_=>Freed=true); Gate(Exec,-414,_=>Closed=true);
            // GetCC is used by ROM mathffp to preserve the caller's CCR.
            W16(Exec-528,0x4ef9); W32(Exec-526,0x2f000); W16(0x2f000,0x40c0); W16(0x2f002,0x4e75);
            Gate(Ffp,-30,s=>s.D[0]=(uint)(int)Decode(s.D[0]));
            Gate(Ffp,-36,s=>s.D[0]=Encode((int)s.D[0]));
            Gate(Ffp,-66,s=>s.D[0]=Encode(Decode(s.D[1])+Decode(s.D[0])));
            Gate(Ffp,-72,s=>s.D[0]=Encode(Decode(s.D[0])-Decode(s.D[1])));
            Gate(Ffp,-78,s=>s.D[0]=Encode(Decode(s.D[1])*Decode(s.D[0])));
            Gate(Ffp,-84,s=>s.D[0]=Encode(Decode(s.D[0])/Decode(s.D[1])));
        }
        internal void UseKickstartMathFfp(byte[] rom)
        {
            if (rom.Length != 0x80000) throw new InvalidDataException("Expected a 512 KiB Kickstart ROM.");
            rom.CopyTo(Memory,0xf80000);
            uint name=0;
            for (int i=0;i<rom.Length-15;i++)
                if (rom.AsSpan(i,15).SequenceEqual("mathffp.library"u8)) { name=0xf80000u+(uint)i; break; }
            Assert.NotEqual(0u,name);
            uint resident=0;
            for (uint a=0xf80000;a<0xffffe0;a+=2)
                if(R16(a)==0x4afc && R32(a+14)==name) { resident=a; break; }
            Assert.NotEqual(0u,resident);
            uint autoinit=R32(resident+22), table=R32(autoinit+4), cursor=table, vector=Ffp-6;
            Assert.Equal((ushort)0xffff,R16(cursor)); cursor+=2;
            while(R16(cursor)!=0xffff)
            { W16(vector,0x4ef9); W32(vector+2,table+(uint)(short)R16(cursor)); cursor+=2; vector-=6; }
            W16(Ffp+16,(ushort)(Ffp-vector-6)); W16(Ffp+18,(ushort)R32(autoinit)); W16(Ffp+20,40);
        }
        private static double Decode(uint x) { Assert.True(FfpConversions.TryToIeee(x,out var b)); return BitConverter.UInt32BitsToSingle(b); }
        private static uint Encode(double x)
        {
            if (Math.Abs(x)<Math.ScaleB(1,-64)) return 0;
            if (Math.Abs(x)>=Math.ScaleB(1,63)) return 0xffffff7f | (x<0?0x80u:0);
            Assert.True(FfpConversions.TryFromIeee(BitConverter.SingleToUInt32Bits((float)x),out uint f)); return f;
        }
        private void Gate(uint b,int lvo,Action<M68kCpuState> fn)
        { uint a=next; next+=8; W16(b+(uint)lvo,0x4ef9); W32(b+(uint)lvo+2,a); W16(a,0xff00); W32(a+2,1); gates.Add(a,fn); }
        public bool HasHostGateway(uint a)=>gates.ContainsKey(a);
        public bool TryInvokeHostGateway(uint pc,uint token,M68kCpuState s) { if (!gates.TryGetValue(pc,out var f)) return false; f(s); return true; }
        public ushort R16(uint a)=>BinaryPrimitives.ReadUInt16BigEndian(Memory.AsSpan((int)a,2));
        public uint R32(uint a)=>BinaryPrimitives.ReadUInt32BigEndian(Memory.AsSpan((int)a,4));
        public void W16(uint a,ushort v)=>BinaryPrimitives.WriteUInt16BigEndian(Memory.AsSpan((int)a,2),v);
        public void W32(uint a,uint v)=>BinaryPrimitives.WriteUInt32BigEndian(Memory.AsSpan((int)a,4),v);
        public byte ReadByte(uint a,ref long c,M68kBusAccessKind k)=>Memory[a];
        public ushort ReadWord(uint a,ref long c,M68kBusAccessKind k)=>R16(a);
        public uint ReadLong(uint a,ref long c,M68kBusAccessKind k)=>R32(a);
        public void WriteByte(uint a,byte v,ref long c,M68kBusAccessKind k)=>Memory[a]=v;
        public void WriteWord(uint a,ushort v,ref long c,M68kBusAccessKind k)=>W16(a,v);
        public void WriteLong(uint a,uint v,ref long c,M68kBusAccessKind k)=>W32(a,v);
        public void ResetExternalDevices(long c)=>throw new InvalidOperationException();
    }
}
