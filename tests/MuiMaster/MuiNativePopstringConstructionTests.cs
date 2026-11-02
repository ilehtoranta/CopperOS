using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiNativePopstringConstructionTests
{
    [Theory]
    [InlineData("Popstring.mui")]
    [InlineData("Popasl.mui")]
    public void ConstructorAdoptsBothControlsAndParentDisposalReleasesTheirObjects(string className)
    {
        var state = APTR.FromPointer(0x1000);
        var p = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000, state);
        Assert.True(MuiHeadlessObjectCore.Initialize(ref p, state));
        var groupName = APTR.FromPointer(0x1100);
        var popName = APTR.FromPointer(0x1140);
        p.WriteCString(groupName, "Group.mui"); p.WriteCString(popName, className);
        var groupClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref p, state, groupName, APTR.Null, 0, APTR.FromPointer(1));
        var popClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref p, state, popName,
            MuiHeadlessObjectCore.ClassPointer(ref p, groupClass), 0, APTR.FromPointer(1));
        var text = MuiHeadlessObjectCore.CreateObjectA(ref p, state, groupClass, APTR.Null);
        var button = MuiHeadlessObjectCore.CreateObjectA(ref p, state, groupClass, APTR.Null);
        var pop = MuiHeadlessObjectCore.CreateObjectA(ref p, state, popClass, APTR.Null);
        Assert.True(MuiHeadlessObjectCore.SetAttribute(ref p, state, pop, MuiPopAttributes.Popstring_String, text.Raw, false));
        Assert.True(MuiHeadlessObjectCore.SetAttribute(ref p, state, pop, MuiPopAttributes.Popstring_Button, button.Raw, false));
        Assert.True(MuiNativeBuiltinPopstring.Construct(ref p, state, pop));
        var contents = APTR.FromPointer(0x1200); p.WriteCString(contents, "SYS:Utilities/Test");
        Assert.True(MuiHeadlessObjectCore.SetAttribute(ref p, state, text, MuiCommonControlCore.StringContents, contents.Raw, false));
        Assert.True(MuiNativeBuiltinPopstring.TryGetString(ref p, state, pop, MuiCommonControlCore.StringContents, out var forwarded));
        Assert.Equal(contents.Raw, forwarded);
        foreach (var child in new[] { text, button })
        {
            var rec = MuiHeadlessObjectCore.FindObject(ref p, state, child);
            Assert.True(MuiHeadlessObjectCodec.TryRead(ref p, rec, out var value));
            Assert.Equal(MuiHeadlessObjectCore.FindObject(ref p, state, pop), value.Parent);
        }
        Assert.True(MuiHeadlessObjectCore.DisposeObject(ref p, state, pop));
        Assert.True(MuiHeadlessObjectCore.FindObject(ref p, state, text).IsNull);
        Assert.True(MuiHeadlessObjectCore.FindObject(ref p, state, button).IsNull);
        Assert.True(MuiHeadlessObjectCore.FindObject(ref p, state, pop).IsNull);
    }
    [Fact]
    public void WindowConstructorRejectsMissingRootAndAcceptsItsAdoptedContents()
    {
        var state = APTR.FromPointer(0x1000);
        var p = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000, state);
        Assert.True(MuiHeadlessObjectCore.Initialize(ref p, state));
        var name = APTR.FromPointer(0x1100);
        p.WriteCString(name, "Window.mui");
        var cls = MuiHeadlessObjectCore.RegisterBuiltinClass(ref p, state, name, APTR.Null, 0, APTR.FromPointer(1));
        var window = MuiHeadlessObjectCore.CreateObjectA(ref p, state, cls, APTR.Null);
        Assert.False(MuiNativeBuiltinWindow.ValidateConstructorRoot(ref p, state, window));
        var groupName = APTR.FromPointer(0x1140); p.WriteCString(groupName, "Group.mui");
        var groupClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref p, state, groupName, APTR.Null, 0, APTR.FromPointer(1));
        var contents = MuiHeadlessObjectCore.CreateObjectA(ref p, state, groupClass, APTR.Null);
        Assert.True(MuiHeadlessObjectCore.SetAttribute(ref p, state, window, MuiWindowPublicCore.RootObject, contents.Raw, false));
        Assert.True(MuiNativeBuiltinWindow.ValidateConstructorRoot(ref p, state, window));
    }

}
