using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiBuiltinPointerTests
{
    [Fact]
    public void DuplicateDownDoesNotReplaceTheExistingCapture()
    {
        var p = Create(out var state, out var window, out _, out var button);
        Assert.True(MuiBuiltinPointerCore.Handle(ref p, state, window, 15, 15, true, out _, out _));
        Assert.True(MuiBuiltinPointerCore.Handle(ref p, state, window, 90, 90, true, out var changed, out _));
        Assert.Equal(APTR.Null, changed);
        Assert.True(MuiBuiltinPointerCore.Handle(ref p, state, window, 15, 15, false, out changed, out var notify));
        Assert.Equal(button, changed); Assert.True(notify); Assert.Equal(1u, p.DispatchCount);
    }
    [Fact]
    public void DetachedCapturedButtonIsResetWithoutActivation()
    {
        var p = Create(out var state, out var window, out var root, out var button);
        Assert.True(MuiBuiltinPointerCore.Handle(ref p, state, window, 15, 15, true, out _, out _));
        Assert.True(MuiFamilyCore.Remove(ref p, state, root, button));
        Assert.True(MuiBuiltinPointerCore.Handle(ref p, state, window, 15, 15, false, out var changed, out var notify));
        Assert.Equal(button, changed); Assert.False(notify); Assert.Equal(0u, p.DispatchCount);
        Assert.True(MuiHeadlessObjectCore.GetAttribute(ref p, state, button, MuiCommonControlCore.Pressed, out var pressed));
        Assert.Equal(0u, pressed);
    }
    [Fact]
    public void RelverifyPressAndAcceptedReleaseDeliverOneReleaseNotification()
    {
        var p = Create(out var state, out var window, out _, out var button);
        Assert.True(MuiBuiltinPointerCore.Handle(ref p, state, window, 15, 15, true, out var changed, out var notify));
        Assert.Equal(button, changed); Assert.True(notify); Assert.Equal(0u, p.DispatchCount);
        Assert.True(MuiHeadlessObjectCore.GetAttribute(ref p, state, button, MuiCommonControlCore.Pressed, out var pressed));
        Assert.Equal(1u, pressed);
        Assert.True(MuiBuiltinPointerCore.Handle(ref p, state, window, 15, 15, false, out changed, out notify));
        Assert.Equal(button, changed); Assert.True(notify); Assert.Equal(1u, p.DispatchCount);
        Assert.Equal(0x00C0FFEEu, p.LastDispatchMethod);
        Assert.True(MuiHeadlessObjectCore.GetAttribute(ref p, state, button, MuiCommonControlCore.Pressed, out pressed));
        Assert.Equal(0u, pressed);
    }
    [Fact]
    public void ReleaseOutsideCancelsWithoutInvokingTheButtonAction()
    {
        var p = Create(out var state, out var window, out _, out var button);
        Assert.True(MuiBuiltinPointerCore.Handle(ref p, state, window, 15, 15, true, out _, out _));
        Assert.True(MuiBuiltinPointerCore.Handle(ref p, state, window, 90, 90, false, out var changed, out var notify));
        Assert.Equal(button, changed); Assert.False(notify); Assert.Equal(0u, p.DispatchCount);
        Assert.True(MuiHeadlessObjectCore.GetAttribute(ref p, state, button, MuiCommonControlCore.Pressed, out var pressed));
        Assert.Equal(0u, pressed);
    }
    [Fact]
    public void DisposedCaptureIsIgnoredAndCannotBeInvokedOnRelease()
    {
        var p = Create(out var state, out var window, out _, out var button);
        Assert.True(MuiBuiltinPointerCore.Handle(ref p, state, window, 15, 15, true, out _, out _));
        Assert.True(MuiHeadlessObjectCore.DisposeObject(ref p, state, button));
        Assert.True(MuiBuiltinPointerCore.Handle(ref p, state, window, 15, 15, false, out var changed, out var notify));
        Assert.Equal(APTR.Null, changed); Assert.False(notify); Assert.Equal(0u, p.DispatchCount);
        Assert.False(MuiBuiltinPointerCore.Handle(ref p, state, window, 15, 15, false, out _, out _));
    }
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DisabledOrHiddenAncestorSuppressesItsButtons(bool disabled)
    {
        var p = Create(out var state, out var window, out var root, out _);
        Assert.True(MuiHeadlessObjectCore.SetAttribute(ref p, state, root,
            disabled ? MuiCommonControlCore.Disabled : MuiCommonControlCore.ShowMe, disabled ? 1u : 0u, false));
        Assert.False(MuiBuiltinPointerCore.Handle(ref p, state, window, 15, 15, true, out _, out _));
        Assert.Equal(0u, p.DispatchCount);
    }
    [Fact]
    public void WindowCancellationClearsPressedStateWithoutActivation()
    {
        var p = Create(out var state, out var window, out _, out var button);
        Assert.True(MuiBuiltinPointerCore.Handle(ref p, state, window, 15, 15, true, out _, out _));
        Assert.True(MuiBuiltinPointerCore.Cancel(ref p, state, window, out var changed));
        Assert.Equal(button, changed); Assert.Equal(0u, p.DispatchCount);
        Assert.True(MuiHeadlessObjectCore.GetAttribute(ref p, state, button, MuiCommonControlCore.Pressed, out var pressed));
        Assert.Equal(0u, pressed);
    }
    private static MuiHeadlessTestPlatform Create(out APTR state, out APTR window, out APTR root, out APTR button)
    {
        state = APTR.FromPointer(0x1800);
        var p = new MuiHeadlessTestPlatform(0x1000, 0x100000, 0x3000, state);
        Assert.True(MuiHeadlessObjectCore.Initialize(ref p, state));
        var windowClass = Class(ref p, state, 0x1100, "Window.mui");
        var groupClass = Class(ref p, state, 0x1140, "Group.mui");
        var textClass = Class(ref p, state, 0x1180, "Text.mui");
        window = MuiHeadlessObjectCore.CreateObjectA(ref p, state, windowClass, APTR.Null);
        root = MuiHeadlessObjectCore.CreateObjectA(ref p, state, groupClass, APTR.Null);
        button = MuiHeadlessObjectCore.CreateObjectA(ref p, state, textClass, APTR.Null);
        Assert.True(MuiCommonControlCore.Construct(ref p, state, textClass, button));
        Assert.True(MuiHeadlessObjectCore.SetAttribute(ref p, state, button, MuiCommonControlCore.InputMode, 1, false));
        Assert.True(MuiFamilyCore.AddTail(ref p, state, root, button));
        Assert.True(MuiHeadlessObjectCore.SetAttribute(ref p, state, window, MuiWindowPublicCore.RootObject, root.Raw, false));
        Assert.True(MuiAreaLayoutCore.Layout(ref p, state, root, 0, 0, 100, 100));
        Assert.True(MuiAreaLayoutCore.Layout(ref p, state, button, 10, 10, 40, 20));
        var method = APTR.FromPointer(0x1300); p.WriteUInt32(method, 0, 0x00C0FFEE);
        Assert.True(MuiNotifyCore.Add(ref p, state, button, MuiCommonControlCore.Pressed, 0, window, 1, method));
        p.DispatchCount = 0;
        return p;
    }
    private static APTR Class(ref MuiHeadlessTestPlatform p, APTR state, uint address, string name)
    {
        var text = APTR.FromPointer(address); p.WriteCString(text, name);
        return MuiHeadlessObjectCore.RegisterBuiltinClass(ref p, state, text, APTR.Null, 0, APTR.FromPointer(address));
    }
}
