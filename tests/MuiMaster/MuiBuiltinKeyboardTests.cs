using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiBuiltinKeyboardTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ClickingStringFocusesItAndTranslatedInputEditsTheOwnedBuffer(bool controlChar)
    {
        var p = Create(out var state, out var window, out _, out var text, out var message);
        Assert.True(MuiBuiltinPointerCore.Handle(ref p, state, window, 15, 15, true, out var focused, out var notify));
        Assert.Equal(text, focused);
        Assert.False(notify);
        Assert.True(MuiHeadlessObjectCore.GetAttribute(ref p, state, window, MuiWindowPublicCore.ActiveObject, out var active));
        Assert.Equal(text.Raw, active);
        if (controlChar) Assert.True(MuiHeadlessObjectCore.SetAttribute(ref p, state, text,
            MuiCommonControlCore.ControlChar, 'x', false));
        Assert.Equal(1u, MuiBuiltinKeyboardCore.Handle(ref p, state, window, message,
            controlChar ? (int)MuiNativeHandleEventKey.Press : -1, out var changed));
        Assert.Equal(text, changed);
        Assert.True(MuiHeadlessObjectCore.GetAttribute(ref p, state, text, MuiCommonControlCore.StringContents, out var value));
        Assert.Equal((byte)'x', p.ReadUInt8(APTR.FromPointer(value), 0));
        Assert.Equal((byte)0, p.ReadUInt8(APTR.FromPointer(value), 1));
    }

    [Theory]
    [InlineData(0)] // key release
    [InlineData(1)] // hidden ancestor after focus
    [InlineData(2)] // disabled ancestor after focus
    [InlineData(3)] // detached focused object
    public void IneligibleInputDoesNotReachThePreviouslyFocusedString(int scenario)
    {
        var p = Create(out var state, out var window, out var root, out var text, out var message);
        Assert.True(MuiBuiltinPointerCore.Handle(ref p, state, window, 15, 15, true, out _, out _));
        if (scenario == 0) p.WriteUInt16(message, 24, (ushort)('x' | 0x80));
        if (scenario == 1) Assert.True(MuiHeadlessObjectCore.SetAttribute(ref p, state, root, MuiCommonControlCore.ShowMe, 0, false));
        if (scenario == 2) Assert.True(MuiHeadlessObjectCore.SetAttribute(ref p, state, root, MuiCommonControlCore.Disabled, 1, false));
        if (scenario == 3) Assert.True(MuiFamilyCore.Remove(ref p, state, root, text));
        Assert.Equal(0u, MuiBuiltinKeyboardCore.Handle(ref p, state, window, message, -1, out var changed));
        Assert.True(changed.IsNull);
        Assert.True(MuiHeadlessObjectCore.GetAttribute(ref p, state, text, MuiCommonControlCore.StringContents, out var value));
        Assert.Equal(0u, value);
    }

    [Fact]
    public void EachAcceptedReturnPublishesAnEventEvenWhenTheTextIsUnchanged()
    {
        var p = Create(out var state, out var window, out _, out var text, out var message);
        Assert.True(MuiBuiltinPointerCore.Handle(ref p, state, window, 15, 15, true, out _, out _));
        Assert.Equal(1u, MuiBuiltinKeyboardCore.Handle(ref p, state, window, message, -1,
            out _, out var edited, out var acknowledged));
        Assert.True(edited); Assert.False(acknowledged);
        p.WriteUInt16(message, 24, 0x44);
        for (var count = 0; count < 2; count++)
        {
            Assert.Equal(1u, MuiBuiltinKeyboardCore.Handle(ref p, state, window, message, -1,
                out var changed, out edited, out acknowledged));
            Assert.Equal(text, changed);
            Assert.False(edited); Assert.True(acknowledged);
        }
    }

    private static MuiHeadlessTestPlatform Create(out APTR state, out APTR window,
        out APTR root, out APTR text, out APTR message)
    {
        state = APTR.FromPointer(0x1800);
        var p = new MuiHeadlessTestPlatform(0x1000, 0x100000, 0x3000, state);
        Assert.True(MuiHeadlessObjectCore.Initialize(ref p, state));
        var windowClass = Class(ref p, state, 0x1100, "Window.mui");
        var groupClass = Class(ref p, state, 0x1140, "Group.mui");
        var textClass = Class(ref p, state, 0x1180, "String.mui");
        window = MuiHeadlessObjectCore.CreateObjectA(ref p, state, windowClass, APTR.Null);
        root = MuiHeadlessObjectCore.CreateObjectA(ref p, state, groupClass, APTR.Null);
        text = MuiHeadlessObjectCore.CreateObjectA(ref p, state, textClass, APTR.Null);
        Assert.True(MuiCommonControlCore.Construct(ref p, state, textClass, text));
        Assert.True(MuiHeadlessObjectCore.SetAttribute(ref p, state, window, MuiWindowPublicCore.RootObject, root.Raw, false));
        Assert.True(MuiFamilyCore.AddTail(ref p, state, root, text));
        Assert.True(MuiAreaLayoutCore.Layout(ref p, state, root, 0, 0, 100, 50));
        Assert.True(MuiAreaLayoutCore.Layout(ref p, state, text, 10, 10, 80, 15));
        message = APTR.FromPointer(0x1600);
        p.Clear(message, 52);
        p.WriteUInt32(message, 20, MuiIntuiMessageCodec.RawKeyClass);
        // This platform translates the fixture's code to a character. Native
        // keymap.library translation is separately exercised by guest input.
        p.WriteUInt16(message, 24, 'x');
        return p;
    }
    private static APTR Class(ref MuiHeadlessTestPlatform p, APTR state, uint address, string name)
    {
        var text = APTR.FromPointer(address); p.WriteCString(text, name);
        return MuiHeadlessObjectCore.RegisterBuiltinClass(ref p, state, text, APTR.Null, 0, APTR.FromPointer(address));
    }
}
