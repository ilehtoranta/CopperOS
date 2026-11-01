using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiNativeBuiltinWindowTests
{
    [Fact]
    public void NativeScreenDimensionsMatchTheSdkScreenLayout()
    {
        var p = Platform(out _, out _);
        var address = APTR.FromPointer(0x2800);
        IntuitionScreenWindowGuestCodec.WriteScreen(ref p, address,
            new Screen { Width = 640, Height = 256 });
        Assert.True(MuiNativeScreenStructCodec.TryRead(ref p, address, out var screen));
        Assert.Equal(640, screen.Width);
        Assert.Equal(256, screen.Height);
    }
    [Fact]
    public void NativeGenericPlatformRemainsAFourByteCarrier()
    {
        Assert.Equal(4, System.Runtime.InteropServices.Marshal.SizeOf<MuiNativeBuiltinPlatform>());
    }
    [Fact]
    public void MeasurementCallbackInvalidatesParentBoundsAndPassStorageIsReleased()
    {
        var p = Platform(out var state, out _);
        var groupName = APTR.FromPointer(0x1200); p.WriteCString(groupName, "Group.mui");
        var textName = APTR.FromPointer(0x1240); p.WriteCString(textName, "Text.mui");
        var label = APTR.FromPointer(0x1280); p.WriteCString(label, "Test");
        var groupClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref p, state, groupName, APTR.Null, 0, APTR.FromPointer(42));
        var textClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref p, state, textName, APTR.Null, 0, APTR.FromPointer(43));
        var root = MuiHeadlessObjectCore.CreateObjectA(ref p, state, groupClass, APTR.Null);
        var text = MuiHeadlessObjectCore.CreateObjectA(ref p, state, textClass, APTR.Null);
        Assert.True(MuiCommonControlCore.Construct(ref p, state, textClass, text));
        Assert.True(MuiHeadlessObjectCore.SetAttribute(ref p, state, text, MuiCommonControlCore.TextContents, label.Raw, false));
        Assert.True(MuiFamilyCore.AddTail(ref p, state, root, text));
        p.MinMaxPass = p.Allocate(MuiNativeBuiltinMinMax.PassSize, 0x10001);
        Assert.True(MuiNativeBuiltinMinMax.Begin(ref p, p.MinMaxPass, state, root));
        Assert.True(MuiAreaLayoutCore.ComputeMinMax(ref p, state, root).DefWidth > 0);
        var message = APTR.FromPointer(0x1600); p.WriteUInt32(message, 0, 0);
        p.InvokeHook(APTR.FromPointer(0x1500), root, message);
        Assert.True(MuiHeadlessObjectCore.DisposeObject(ref p, state, text));
        Assert.Equal(0, MuiAreaLayoutCore.ComputeMinMax(ref p, state, root).DefWidth);
        var before = p.FreeCount;
        MuiNativeBuiltinMinMax.End(ref p, p.MinMaxPass);
        Assert.Equal(before + 2, p.FreeCount);
        Assert.Equal(0u, p.ReadUInt32(p.MinMaxPass, 0));
        p.Free(p.MinMaxPass, MuiNativeBuiltinMinMax.PassSize); p.MinMaxPass = APTR.Null;
    }
    [Theory]
    [InlineData(0, 180, 100, 220, 13, 240, 233)]
    [InlineData(0, 180, 0, 10, 13, 240, 180)]
    [InlineData(180, 180, 100, 220, 13, 240, 180)]
    [InlineData(-1, 180, 100, 300, 13, 240, 240)]
    public void AutomaticWindowExtentUsesPreferredContentWithinScreenBounds(
        int requested, int current, int minimum, int preferred, int borders, int available, int expected)
    {
        Assert.Equal(expected, MuiNativeBuiltinWindowSizing.PreferredExtent(
            requested, current, minimum, preferred, borders, available));
    }
    [Fact]
    public void UnpreparedPresentationTreeCleanupDoesNotCreateRuntimeStores()
    {
        var p = Platform(out var state, out _);
        var name = APTR.FromPointer(0x1200); p.WriteCString(name, "Group.mui");
        var cls = MuiHeadlessObjectCore.RegisterBuiltinClass(ref p, state, name, APTR.Null, 0, APTR.FromPointer(42));
        var group = MuiHeadlessObjectCore.CreateObjectA(ref p, state, cls, APTR.Null);
        var before = p.AllocationCount;
        Assert.True(MuiNativeBuiltinPresentation.CleanupTree(ref p, state, group, 0));
        Assert.Equal(before, p.AllocationCount);
    }
    [Fact]
    public void IndexedNativeScreenResolvesNoRgbOverrideWithoutFailingAreaSetup()
    {
        var native = default(MuiNativeBuiltinPlatform);
        var request = new MuiTextColorResolutionRequest
        { Object = APTR.FromPointer(0x1000), RenderInfo = APTR.FromPointer(0x2000), Available = 1 };
        Assert.True(native.ResolveMuiTextColor(ref request));
        Assert.Equal(0u, request.Available);
        request.RenderInfo = APTR.Null;
        Assert.False(native.ResolveMuiTextColor(ref request));
    }
    [Fact]
    public void DisposalReleasesCycleChainNodesAfterTheirChildTargetsHaveBeenDisposed()
    {
        var p = Platform(out var state, out var window);
        var cls = MuiHeadlessObjectCore.ObjectClassRecord(ref p, state, window);
        var child = MuiHeadlessObjectCore.CreateObjectA(ref p, state, cls, APTR.Null);
        Assert.True(MuiFamilyCore.AddTail(ref p, state, window, child));
        var vector = APTR.FromPointer(0x1300);
        p.WriteUInt32(vector, 0, child.Raw); p.WriteUInt32(vector, 4, 0);
        Assert.True(MuiApplicationWindowCore.SetCycleChain(ref p, state, window, vector));
        Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref p, state, window, 0x7FFE0039, out var ownedNode));
        Assert.NotEqual(0u, ownedNode);
        var freed = new HashSet<APTR>(); p.AllocationFreed = (address, _) => freed.Add(address);
        Assert.True(MuiHeadlessObjectCore.DisposeObject(ref p, state, window));
        Assert.True(MuiHeadlessObjectCore.FindObject(ref p, state, child).IsNull);
        Assert.Contains(APTR.FromPointer(ownedNode), freed);
    }
    [Fact]
    public void CleanupDoesNotConstructApplicationSchedulerOrWindowInteractionStores()
    {
        var p = Platform(out var state, out var window);
        var before = p.AllocationCount;
        MuiApplicationWindowCore.CleanupRecords(ref p, state, window);
        Assert.Equal(before, p.AllocationCount);
    }
    [Fact]
    public void NativePresentationPreparesNestedGroupsAndDrawsTheirTextThroughTheLayoutCore()
    {
        var p = Platform(out var state, out var window);
        var groupName = APTR.FromPointer(0x1200); p.WriteCString(groupName, "Group.mui");
        var textName = APTR.FromPointer(0x1240); p.WriteCString(textName, "Text.mui");
        var label = APTR.FromPointer(0x1280); p.WriteCString(label, "Calculator");
        var groupClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref p, state, groupName, APTR.Null, 0, APTR.FromPointer(42));
        var textClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref p, state, textName, APTR.Null, 0, APTR.FromPointer(43));
        var root = MuiHeadlessObjectCore.CreateObjectA(ref p, state, groupClass, APTR.Null);
        var nested = MuiHeadlessObjectCore.CreateObjectA(ref p, state, groupClass, APTR.Null);
        var text = MuiHeadlessObjectCore.CreateObjectA(ref p, state, textClass, APTR.Null);
        Assert.True(MuiCommonControlCore.Construct(ref p, state, textClass, text));
        Assert.True(MuiHeadlessObjectCore.SetAttribute(ref p, state, text, MuiCommonControlCore.TextContents, label.Raw, false));
        Assert.True(MuiFamilyCore.AddTail(ref p, state, root, nested));
        Assert.True(MuiFamilyCore.AddTail(ref p, state, nested, text));
        Assert.True(MuiFamilyCore.AddTail(ref p, state, window, root));
        var info = p.Allocate(MuiDrawingRenderInfoRecord.Size, 0x10001);
        Assert.True(MuiDrawingRenderInfoCodec.Write(ref p, info, new MuiDrawingRenderInfoRecord { RastPort = APTR.FromPointer(0x1400) }));
        Assert.True(MuiNativeBuiltinPresentation.PrepareTree(ref p, state, root, info, 0));
        p.MinMaxPass = p.Allocate(MuiNativeBuiltinMinMax.PassSize, 0x10001);
        Assert.True(MuiNativeBuiltinMinMax.Begin(ref p, p.MinMaxPass, state, root));
        var preferred = MuiAreaLayoutCore.ComputeMinMax(ref p, state, root);
        Assert.True(preferred.DefHeight >= 8);
        Assert.True(MuiNativeBuiltinPresentation.LayoutTree(ref p, state, root, 4, 12, 300, 160, 0));
        MuiNativeBuiltinMinMax.End(ref p, p.MinMaxPass);
        Assert.Equal(0u, p.ReadUInt32(p.MinMaxPass, 0));
        Assert.Equal(0u, p.ReadUInt32(p.MinMaxPass, 8));
        p.Free(p.MinMaxPass, MuiNativeBuiltinMinMax.PassSize); p.MinMaxPass = APTR.Null;
        Assert.True(MuiAreaLayoutCore.TryReadGeometryState(ref p, state, text, out var geometry));
        Assert.InRange(geometry.Left, 4, 303);
        Assert.InRange(geometry.Top, 12, 171);
        Assert.True(geometry.Width > 0 && geometry.Height > 0);
        Assert.True(MuiNativeBuiltinPresentation.DrawTree(ref p, state, root, 0));
        Assert.Equal(1u, p.TextCount);
        Assert.Equal(1u, p.LastPen);
        for (var i = 0; i <= 10; i++) Assert.Equal(p.ReadUInt8(label, i), p.ReadUInt8(p.LastText, i));
        Assert.Equal(10, p.LastTextLength);
        Assert.True(MuiNativeBuiltinPresentation.IsVisibleInTree(ref p, state, text));
        p.WriteCString(label, "11");
        Assert.True(MuiHeadlessObjectCore.SetAttribute(ref p, state, text, MuiCommonControlCore.TextContents, label.Raw, false));
        p.TextCount = 0;
        Assert.True(MuiNativeBuiltinPresentation.RedrawAfterSet(ref p, state, text));
        Assert.Equal(1u, p.TextCount);
        Assert.Equal(2, p.LastTextLength);
        Assert.Equal((byte)'1', p.ReadUInt8(p.LastText, 0));
        Assert.Equal((byte)'1', p.ReadUInt8(p.LastText, 1));
        // Calc requests background 4 for its number display. On one/two-plane
        // native screens an unavailable background must not become text pen 1.
        Assert.True(MuiHeadlessObjectCore.SetAttribute(ref p, state, text, MuiCommonControlCore.Background, 4, false));
        for (var depth = 1; depth <= 2; depth++)
        {
            p.NativeBackgroundDepth = depth;
            Assert.True(MuiNativeBuiltinPresentation.RedrawAfterSet(ref p, state, text));
            Assert.Equal(0u, p.LastFillPen);
            Assert.Equal(1u, p.LastPen);
            Assert.NotEqual(p.LastFillPen, p.LastPen);
        }
        // An inactive page has an empty parent box even if a descendant keeps
        // its previous nonempty rectangle. Neither tree nor direct redraw may
        // draw that descendant into the active page.
        Assert.True(MuiAreaLayoutCore.Layout(ref p, state, nested, 4, 12, 0, 0));
        p.TextCount = 0;
        Assert.True(MuiNativeBuiltinPresentation.DrawTree(ref p, state, root, 0));
        Assert.True(MuiNativeBuiltinPresentation.RedrawAfterSet(ref p, state, text));
        Assert.Equal(0u, p.TextCount);
        Assert.True(MuiHeadlessObjectCore.SetAttribute(ref p, state, root, MuiCommonControlCore.ShowMe, 0, false));
        Assert.False(MuiNativeBuiltinPresentation.IsVisibleInTree(ref p, state, text));
        p.TextCount = 0;
        Assert.True(MuiNativeBuiltinPresentation.DrawTree(ref p, state, root, 0));
        Assert.Equal(0u, p.TextCount);
        Assert.True(MuiNativeBuiltinPresentation.CleanupTree(ref p, state, root, 0));
        Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref p, state, text, 0x7FFF0001, out var renderAfterCleanup));
        Assert.Equal(0u, renderAfterCleanup);
        Assert.True(MuiHeadlessObjectCore.DisposeObject(ref p, state, root));
        Assert.True(MuiHeadlessObjectCore.FindObject(ref p, state, text).IsNull);
    }
    private struct Native : IMuiBuiltinWindowCapability
    {
        public APTR Result;
        public bool RejectPublish;
        public uint Opens, Closes, Publishes;
        public APTR Published;
        public APTR OpenBuiltinWindow(APTR obj) { Opens++; return Result; }
        public void CloseBuiltinWindow(APTR window) { Closes++; }
        public bool PublishBuiltinWindow(APTR obj, APTR window)
        { Publishes++; if (RejectPublish) return false; Published = window; return true; }
    }

    [Fact]
    public void NativeHandleIsPublishedOnlyAfterOpenAndRepeatedRequestsAreIdempotent()
    {
        var p = Platform(out var state, out var window);
        var native = new Native { Result = APTR.FromPointer(0x123456) };
        Set(ref p, state, window, 1);
        Assert.True(MuiNativeBuiltinWindow.Synchronize(ref p, ref native, state, window));
        Assert.Equal(native.Result, native.Published);
        Assert.True(MuiApplicationWindowCore.TryGetWindowLifecycleState(ref p, state, window, out var life));
        Assert.Equal(native.Result, life.NativeWindow);
        Assert.Equal(1u, life.Open);
        Assert.True(MuiNativeBuiltinWindow.Synchronize(ref p, ref native, state, window));
        Assert.Equal(1u, native.Opens);
        Set(ref p, state, window, 0);
        Assert.True(MuiNativeBuiltinWindow.Synchronize(ref p, ref native, state, window));
        Assert.Equal(APTR.Null, native.Published);
        Assert.Equal(1u, native.Closes);
        Assert.True(MuiApplicationWindowCore.TryGetWindowLifecycleState(ref p, state, window, out life));
        Assert.Equal(0u, life.Open);
        Assert.Equal(APTR.Null, life.NativeWindow);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FailedOpenOrPublicationLeavesNoOwnedNativeWindow(bool publishFailure)
    {
        var p = Platform(out var state, out var window);
        var native = new Native { Result = publishFailure ? APTR.FromPointer(0x123456) : APTR.Null, RejectPublish = publishFailure };
        Set(ref p, state, window, 1);
        Assert.False(MuiNativeBuiltinWindow.Synchronize(ref p, ref native, state, window));
        Assert.Equal(publishFailure ? 1u : 0u, native.Closes);
        Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref p, state, window, MuiWindowPublicCore.Open, out var open));
        Assert.Equal(0u, open);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RelationshipDisconnectionDoesNotSearchTheObjectRegistryForEveryCandidate(bool strings)
    {
        var small = CountReads(12, strings);
        var large = CountReads(24, strings);
        Assert.True(large < small * 3, $"Mapping checks grew from {small} to {large} when doubling the object count.");
    }
    private static uint CountReads(int count, bool strings)
    {
        var p = Platform(out var state, out var target);
        var name = APTR.FromPointer(0x1200); p.WriteCString(name, "Text.mui");
        var cls = MuiHeadlessObjectCore.RegisterBuiltinClass(ref p, state, name, APTR.Null, 0, APTR.FromPointer(42));
        for (var i = 0; i < count; i++) Assert.True(MuiHeadlessObjectCore.CreateObjectA(ref p, state, cls, APTR.Null).IsNotNull);
        uint reads = 0; p.MappingAdmission = (_, _) => { reads++; return true; };
        if (strings) MuiCommonControlCore.DisconnectStringAttachedListConnectionsToObject(ref p, state, target);
        else MuiListviewCore.DisconnectExternalScrollerConnectionsToObject(ref p, state, target);
        return reads;
    }
    private static void Set(ref MuiHeadlessTestPlatform p, APTR state, APTR obj, uint value) =>
        Assert.True(MuiHeadlessObjectCore.SetAttribute(ref p, state, obj, MuiWindowPublicCore.Open, value, false));
    private static MuiHeadlessTestPlatform Platform(out APTR state, out APTR window)
    {
        state = APTR.FromPointer(0x1800);
        var p = new MuiHeadlessTestPlatform(0x1000, 0x100000, 0x3000, state);
        Assert.True(MuiHeadlessObjectCore.Initialize(ref p, state));
        var name = APTR.FromPointer(0x1100); p.WriteCString(name, "Window.mui");
        var cls = MuiHeadlessObjectCore.RegisterBuiltinClass(ref p, state, name, APTR.Null, 0, APTR.FromPointer(41));
        window = MuiHeadlessObjectCore.CreateObjectA(ref p, state, cls, APTR.Null);
        Assert.True(window.IsNotNull);
        return p;
    }
}
