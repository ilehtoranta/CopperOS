using Amiga;
using CopperOS.MuiMaster;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiNativePopaslTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CompletionOwnershipDoesNotRequirePortableApplicationInitializer(bool detached)
    {
        var state = APTR.FromPointer(0x1400);
        var p = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000, state);
        var name = APTR.FromPointer(0x1100); p.WriteCString(name, "Group.mui");
        Assert.True(MuiHeadlessObjectCore.Initialize(ref p, state));
        var cls = MuiHeadlessObjectCore.RegisterClass(ref p, state, name, APTR.Null, 0, APTR.FromPointer(1), false);
        var app = MuiHeadlessObjectCore.CreateObjectA(ref p, state, cls, APTR.Null);
        var other = MuiHeadlessObjectCore.CreateObjectA(ref p, state, cls, APTR.Null);
        var window = MuiHeadlessObjectCore.CreateObjectA(ref p, state, cls, APTR.Null);
        var popup = MuiHeadlessObjectCore.CreateObjectA(ref p, state, cls, APTR.Null);
        Assert.True(MuiFamilyCore.AddTail(ref p, state, app, window));
        Assert.True(MuiFamilyCore.AddTail(ref p, state, window, popup));
        MuiHeadlessObjectCore.GetAttribute(ref p, state, popup, MuiApplicationMessageCore.ApplicationObject, out var portableApp);
        Assert.Equal(0u, portableApp);
        if (detached) Assert.True(MuiFamilyCore.Remove(ref p, state, window, popup));
        Assert.Equal(!detached, MuiNativeBuiltinPopasl.OwnsPopup(ref p, state, app, popup));
        Assert.False(MuiNativeBuiltinPopasl.OwnsPopup(ref p, state, other, popup));
    }
    [Theory]
    [InlineData("", "", "")]
    [InlineData("name", "", "name")]
    [InlineData("SYS:name", "SYS:", "name")]
    [InlineData("SYS:drawer/name", "SYS:drawer", "name")]
    [InlineData("drawer/name", "drawer", "name")]
    [InlineData("SYS:drawer/", "SYS:drawer", "")]
    public void FileDefaultsSplitAndRejoinClassicPaths(string original, string drawerText, string fileText)
    {
        var p = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000, APTR.FromPointer(0x1200));
        var source = APTR.FromPointer(0x1400); var path = APTR.FromPointer(0x1800);
        var drawer = APTR.FromPointer(0x1C00); var output = APTR.FromPointer(0x2000);
        p.WriteCString(source, original);
        Assert.True(MuiNativePopaslFiles.Split(ref p, source, path, drawer, 512, out var file));
        Assert.Equal(drawerText, Read(ref p, drawer));
        Assert.Equal(fileText, Read(ref p, file));
        Assert.True(MuiNativePopaslFiles.Join(ref p, drawer, file, output, 512));
        Assert.Equal(fileText.Length == 0 ? drawerText : original, Read(ref p, output));
        Assert.Equal(original, Read(ref p, source));
    }
    [Fact]
    public void FileDefaultsRejectTruncationAndAcceptNullInitialContents()
    {
        var p = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000, APTR.FromPointer(0x1200));
        var source = APTR.FromPointer(0x1400); var path = APTR.FromPointer(0x1800);
        var drawer = APTR.FromPointer(0x1C00); var output = APTR.FromPointer(0x2000);
        p.WriteCString(source, "name");
        Assert.False(MuiNativePopaslFiles.Split(ref p, source, path, drawer, 4, out _));
        Assert.False(MuiNativePopaslFiles.Join(ref p, APTR.Null, source, output, 4));
        Assert.True(MuiNativePopaslFiles.Split(ref p, APTR.Null, path, drawer, 4, out var file));
        Assert.Equal("", Read(ref p, file)); Assert.Equal("", Read(ref p, drawer));
    }
    [Fact]
    public void WorkerMessageRoundTripsWithoutTouchingFollowingAllocation()
    {
        Assert.Equal((int)MuiNativePopaslWork.Size, Marshal.SizeOf<MuiNativePopaslWork>());
        var p = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000, APTR.FromPointer(0x1200));
        var address = APTR.FromPointer(0x1800);
        var value = new MuiNativePopaslWork { Magic = MuiNativePopaslWork.Cookie,
            Message = new Message { ReplyPort = APTR.FromPointer(0x3400), Length = 1200 },
            Asl = APTR.FromPointer(0x3000), Requester = APTR.FromPointer(0x3200), ReplyPort = APTR.FromPointer(0x3400),
            Button = APTR.FromPointer(0x3600), String = APTR.FromPointer(0x3800), StopHook = APTR.FromPointer(0x3A00),
            Result = 1, OldDisabled = 1, RunTags = APTR.FromPointer(0x3C00), Bytes = 1200 };
        p.WriteUInt32(address, 64, 0xA5C36996);
        MuiNativePopaslWorkCodec.Write(ref p, address, value);
        Assert.True(MuiNativePopaslWorkCodec.Read(ref p, address, out var actual));
        Assert.Equal(value, actual);
        Assert.Equal(0xA5C36996u, p.ReadUInt32(address, 64));
        p.WriteUInt32(address, 20, 0);
        Assert.False(MuiNativePopaslWorkCodec.Read(ref p, address, out _));
    }
    private static string Read(ref MuiHeadlessTestPlatform p, APTR text)
    {
        var value = new System.Text.StringBuilder();
        for (var i = 0; i < 512 && p.ReadUInt8(text, i) != 0; i++) value.Append((char)p.ReadUInt8(text, i));
        return value.ToString();
    }
}
