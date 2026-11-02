using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiClassicFamilyMembershipTests
{
    [Fact]
    public void ClassicMembershipAdoptsDetachesAndReattachesDynamicGroup()
    {
        var state = APTR.FromPointer(0x1000);
        var p = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000, state);
        Assert.True(MuiHeadlessObjectCore.Initialize(ref p, state));
        var name = APTR.FromPointer(0x1100);
        p.WriteCString(name, "Group.mui");
        var cls = MuiHeadlessObjectCore.RegisterBuiltinClass(ref p, state, name, APTR.Null, 0, APTR.FromPointer(1));
        var parent = MuiHeadlessObjectCore.CreateObjectA(ref p, state, cls, APTR.Null);
        var child = MuiHeadlessObjectCore.CreateObjectA(ref p, state, cls, APTR.Null);
        var packet = APTR.FromPointer(0x1800);
        p.WriteUInt32(packet, 0, BOOPSI.OM_ADDMEMBER);
        p.WriteUInt32(packet, 4, child.Raw);
        Assert.Equal(1u, MuiHeadlessDispatcher.Dispatch(ref p, state, parent, packet));
        var childRecord = MuiHeadlessObjectCore.FindObject(ref p, state, child);
        Assert.True(MuiHeadlessObjectCodec.TryRead(ref p, childRecord, out var adopted));
        Assert.Equal(MuiHeadlessObjectCore.FindObject(ref p, state, parent), adopted.Parent);
        Assert.Equal(0u, MuiHeadlessDispatcher.Dispatch(ref p, state, parent, packet));
        p.WriteUInt32(packet, 0, BOOPSI.OM_REMMEMBER);
        Assert.Equal(1u, MuiHeadlessDispatcher.Dispatch(ref p, state, parent, packet));
        Assert.True(MuiHeadlessObjectCodec.TryRead(ref p, childRecord, out var detached));
        Assert.True(detached.Parent.IsNull);
        p.WriteUInt32(packet, 0, BOOPSI.OM_ADDMEMBER);
        Assert.Equal(1u, MuiHeadlessDispatcher.Dispatch(ref p, state, parent, packet));
    }
    [Theory]
    [InlineData(MuiNotifyCore.NotifyMethod)]
    [InlineData(MuiNotifyCore.KillNotifyMethod)]
    [InlineData(MuiNotifyCore.KillNotifyObjectMethod)]
    public void GroupNotificationMethodsReachBothMemberControls(uint method)
    {
        var state = APTR.FromPointer(0x1000);
        var p = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000, state);
        Assert.True(MuiHeadlessObjectCore.Initialize(ref p, state));
        var name = APTR.FromPointer(0x1100); p.WriteCString(name, "Group.mui");
        var cls = MuiHeadlessObjectCore.RegisterBuiltinClass(ref p, state, name, APTR.Null, 0, APTR.FromPointer(1));
        var parent = MuiHeadlessObjectCore.CreateObjectA(ref p, state, cls, APTR.Null);
        var first = MuiHeadlessObjectCore.CreateObjectA(ref p, state, cls, APTR.Null);
        var last = MuiHeadlessObjectCore.CreateObjectA(ref p, state, cls, APTR.Null);
        Assert.True(MuiFamilyCore.AddTail(ref p, state, parent, first));
        Assert.True(MuiFamilyCore.AddTail(ref p, state, parent, last));
        var packet = APTR.FromPointer(0x1800);
        p.WriteUInt32(packet, 0, method); p.WriteUInt32(packet, 4, MuiCommonControlCore.Pressed);
        var before = p.DispatchCount;
        Assert.True(MuiNativeBuiltinGroupNotifications.TryForward(ref p, state, parent, packet, method, out _));
        Assert.Equal(before + 2, p.DispatchCount);
        Assert.Equal(last, p.LastDispatchObject);
        Assert.Equal(method, p.LastDispatchMethod);
        Assert.Equal(MuiCommonControlCore.Pressed, p.LastDispatchArgument);
    }

}
