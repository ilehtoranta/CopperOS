using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiFindObjectTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void FindObjectRecognizesTheCallingObjectAndDescendants()
	{
		var platform = CreatePlatform(out var cl);
		var root = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var child = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var grandchild = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			cl, APTR.Null);
		var unrelated = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			cl, APTR.Null);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, root, child));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, child, grandchild));

		var packet = APTR.FromPointer(0x1200);
		platform.WriteUInt32(packet, 0, MuiNotifyCore.FindObjectMethod);
		platform.WriteUInt32(packet, 4, root.Raw);
		Assert.Equal(1u, MuiHeadlessDispatcher.DispatchNotify(ref platform, State,
			root, packet));
		platform.WriteUInt32(packet, 4, child.Raw);
		Assert.Equal(1u, MuiHeadlessDispatcher.DispatchNotify(ref platform, State,
			root, packet));
		platform.WriteUInt32(packet, 4, grandchild.Raw);
		Assert.Equal(1u, MuiHeadlessDispatcher.DispatchNotify(ref platform, State,
			root, packet));

		platform.WriteUInt32(packet, 4, unrelated.Raw);
		Assert.Equal(0u, MuiHeadlessDispatcher.DispatchNotify(ref platform, State,
			root, packet));
	}

	[Fact]
	public void FindObjectRejectsDeadObjectsAndObjectsOutsideTheTree()
	{
		var platform = CreatePlatform(out var cl);
		var root = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var child = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var unrelated = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			cl, APTR.Null);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, root, child));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			unrelated));

		var packet = APTR.FromPointer(0x1200);
		platform.WriteUInt32(packet, 0, MuiNotifyCore.FindObjectMethod);
		platform.WriteUInt32(packet, 4, unrelated.Raw);
		Assert.Equal(0u, MuiHeadlessDispatcher.DispatchNotify(ref platform, State,
			root, packet));

		platform.WriteUInt32(packet, 0, 0xDEADBEEFu);
		Assert.Equal(0u, MuiHeadlessDispatcher.DispatchNotify(ref platform, State,
			root, packet));
	}

	[Fact]
	public void FindObjectRejectsTruncatedPackets()
	{
		var platform = CreatePlatform(out var cl);
		var root = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var packet = APTR.FromPointer(0x20FFC);
		platform.WriteUInt32(packet, 0, MuiNotifyCore.FindObjectMethod);
		Assert.Equal(0u, MuiHeadlessDispatcher.DispatchNotify(ref platform, State,
			root, packet));
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR cl)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Notify.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		cl = MuiHeadlessObjectCore.RegisterClass(ref platform, State, name,
			APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}
}
