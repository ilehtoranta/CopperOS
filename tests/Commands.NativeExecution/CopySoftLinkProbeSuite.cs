using System.Text;
using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record CopySoftLinkProbeCase(bool Locked, int Error, bool Device,
    int LinkResult, bool Quiet, bool Enter);
internal sealed class CopySoftLinkNativeLayout(uint control)
{
    public uint Control { get; } = control;
    public int Directories, Devices, Links, Warnings, Released;
    public bool Contains(uint address, int size) => address >= Control &&
        (ulong)address + (uint)size <= (ulong)Control + 512;
}
internal sealed partial class ProbeFixture
{
    public const string CopySoftLinkProbeSuite = "copy-softlink-native-entry-vector-fixture";
    public const string CopyMatchedDirectoryProbeSuite = "copy-matched-directory-native-entry-vector-fixture";
    private List<object> RunCopySoftLinkProbeCases()
    {
        ProbeCase[] cases = [
            SoftLinkCase("locked", true, 0, false, 0, false, true),
            SoftLinkCase("other-error", false, 2050, false, 0, false, true),
            SoftLinkCase("no-device", false, 205, false, 0, false, true),
            SoftLinkCase("not-link", false, 205, true, -1, false, true),
            SoftLinkCase("dangling", false, 205, true, 7, false, false),
            SoftLinkCase("quiet", false, 205, true, 7, true, false),
            SoftLinkCase("allocation-failure", false, 205, true, 7, false, true)
                with { AllocationFailure = true },
        ];
        var results = new List<object>();
        foreach (var item in cases) results.AddRange(Execute([item], false));
        results.AddRange(Execute([cases[4] with { Name = "interleaved-warning" },
            cases[5] with { Name = "interleaved-quiet" }], true));
        Bus.AssertImageUnchanged();
        return results;
    }
    private static ProbeCase SoftLinkCase(string name, bool locked, int error,
        bool device, int link, bool quiet, bool enter) =>
        new(name, "", DOS.RETURN_OK, 0, "") { EntryLength = 24,
            CopySoftLink = new(locked, error, device, link, quiet, enter) };
    private void PrepareCopySoftLinkProbe(Invocation i)
    {
        var c = i.Arguments;
        Bus.Memory.AsSpan((int)c, 512).Clear();
        i.CopySoftLinkLayout = new(c);
        Bus.Long(c, 0x120); Bus.Long(c + 4, c + 64);
        Bus.Long(c + 8, i.Definition.CopySoftLink!.Quiet ? 1u : 0);
        Encoding.Latin1.GetBytes("link\0").CopyTo(Bus.Memory.AsSpan((int)c + 64));
        Bus.Long(c + 128, 0x6780); Bus.Long(c + 132, 0x234);
        if (suite == CopyMatchedDirectoryProbeSuite)
        {
            Bus.Long(c + 160 + (uint)DosLayout.AnchorPath.Current, c + 448);
            Bus.Long(c + 448 + (uint)DosLayout.AChain.Lock, 0x120);
            Encoding.Latin1.GetBytes("link\0").CopyTo(Bus.Memory.AsSpan(
                (int)(c + 160 + (uint)DosLayout.AnchorPath.Info + FileInfoBlock.FileNameOffset)));
        }
    }
    private void VerifyCopySoftLinkProbe(Invocation i)
    {
        var p = i.Definition.CopySoftLink!; var l = i.CopySoftLinkLayout!;
        var device = !p.Locked && p.Error == 205;
        var allocated = device && p.Device;
        var read = allocated && !i.Definition.AllocationFailure;
        if (suite == CopyMatchedDirectoryProbeSuite)
            Require(Bus.Long(l.Control + 24) == 1 &&
                Bus.Memory[l.Control + 160 + (uint)DosLayout.AnchorPath.Flags] ==
                    (p.Enter ? (byte)AnchorPathFlags.DoDirectory : 0),
                "Matched directory work and recursion flags differ.");
        Require(Bus.Long(l.Control + 12) == (p.Enter ? 1u : 0) &&
            l.Directories == 2 && l.Devices == (device ? 1 : 0) &&
            l.Released == (allocated ? 1 : 0) && l.Links == (read ? 1 : 0) &&
            l.Warnings == (read && p.LinkResult > 0 && !p.Quiet ? 1 : 0) &&
            i.Allocations == (allocated ? 1 : 0) && i.FreeMem == (read ? 1 : 0),
            $"{i.Definition.Name}: soft-link result or lifecycle differs.");
        i.CopySoftLinkLayout = null;
    }
    private void RegisterCopySoftLinkProbeDos(uint b)
    {
        Register(b, DosLvo.CurrentDir, "CurrentDir", (s,i) => {
            var l=i.CopySoftLinkLayout!;
            Require(s.D[1] == (l.Directories == 0 ? 0x120u : 0x321u), "CurrentDir restore differs.");
            if(l.Directories != 0 && !i.Definition.CopySoftLink!.Locked)
                Require(i.IoError == i.Definition.CopySoftLink.Error, "Saved IoErr lost before directory restore.");
            l.Directories++; return 0x321;
        });
        Register(b, DosLvo.Lock, "Lock", (s,i) => {
            Require(Bus.CString(s.D[1]) == "link" && s.D[2] == unchecked((uint)DOS.LockMode.Shared), "Lock ABI differs.");
            i.IoError=i.Definition.CopySoftLink!.Error;
            return i.Definition.CopySoftLink.Locked ? 0x456u : 0;
        });
        Register(b, DosLvo.UnLock, "UnLock", (s,i) => { Require(s.D[1]==0x456,"Unlock differs."); return 0; });
        Register(b, DosLvo.IoErr, "IoErr", (_,i) => unchecked((uint)i.IoError));
        Register(b, DosLvo.SetIoErr, "SetIoErr", (s,i) => { i.IoError=unchecked((int)s.D[1]); return 0; });
        Register(b, DosLvo.GetDeviceProc, "GetDeviceProc", (s,i) => {
            Require(s.D[1]!=0 && Bus.CString(s.D[1])=="" && s.D[2]==0,"Device lookup ABI differs.");
            i.CopySoftLinkLayout!.Devices++; i.IoError=901;
            return i.Definition.CopySoftLink!.Device ? i.Arguments+128 : 0;
        });
        Register(b, DosLvo.ReadLink, "ReadLink", (s,i) => {
            Require(s.D[1]==0x6780 && s.D[2]==0x234 && Bus.CString(s.D[3])=="link" && s.D[5]==511,"ReadLink ABI differs.");
            Bus.OwnedAllocation(i,s.D[4],"Exec");
            Encoding.Latin1.GetBytes("missing\0").CopyTo(Bus.Memory.AsSpan((int)s.D[4]));
            i.CopySoftLinkLayout!.Links++; i.IoError=903;
            return unchecked((uint)i.Definition.CopySoftLink!.LinkResult);
        });
        Register(b, DosLvo.VPrintf, "VPrintf", (s,i) => {
            Require(Bus.CString(s.D[1])=="Warning: Skipping dangling softlink %s -> %s\n" &&
                Bus.CString(Bus.Long(s.D[2]))=="link" && Bus.CString(Bus.Long(s.D[2]+4))=="missing", "Warning arguments differ.");
            i.CopySoftLinkLayout!.Warnings++; return 0;
        });
        Register(b, DosLvo.FreeDeviceProc, "FreeDeviceProc", (s,i) => {
            Require(s.D[1]==i.Arguments+128,"Device release differs.");
            i.CopySoftLinkLayout!.Released++; i.IoError=904; return 0;
        });
    }
}
