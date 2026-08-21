using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiNotificationTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint Attribute = 0x80420020;
	private const uint EveryTime = 1233727793;

	[Fact]
	public void MutationDuringNotificationDoesNotUseFreedOrDuplicateRecords()
	{
		var platform = CreatePlatform(out var cl);
		var source = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var firstDestination = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			State, cl, APTR.Null);
		var secondDestination = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			State, cl, APTR.Null);
		var follow = APTR.FromPointer(0x1300);
		platform.WriteUInt32(follow, 0, 0x90000001);
		platform.WriteUInt32(follow, 4, EveryTime);
		Assert.True(MuiNotifyCore.Add(ref platform, State, source, Attribute,
			EveryTime, firstDestination, 2, follow));
		Assert.True(MuiNotifyCore.Add(ref platform, State, source, Attribute,
			EveryTime, secondDestination, 2, follow));
		platform.MutationMode = 1;
		platform.MutationSource = source;
		platform.MutationDestination = firstDestination;
		platform.MutationAttribute = Attribute;

		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, source,
			Attribute, 77, true));

		Assert.Equal(1u, platform.DispatchCount);
		Assert.Equal(firstDestination, platform.LastDispatchObject);
		Assert.Equal(0x90000001u, platform.LastDispatchMethod);
		Assert.Equal(77u, platform.LastDispatchArgument);
		Assert.Equal(0u, MuiNotifyCore.Remove(ref platform, State, source,
			Attribute, APTR.Null, false));
	}

	[Fact]
	public void RecursiveNotificationsStopAtTheConfiguredDepth()
	{
		var platform = CreatePlatform(out var cl);
		var source = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var destination = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			cl, APTR.Null);
		var follow = APTR.FromPointer(0x1300);
		platform.WriteUInt32(follow, 0, 0x90000002);
		Assert.True(MuiNotifyCore.Add(ref platform, State, source, Attribute,
			EveryTime, destination, 1, follow));
		platform.MutationMode = 2;
		platform.MutationSource = source;
		platform.MutationDestination = destination;
		platform.MutationAttribute = Attribute;

		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, source,
			Attribute, 1, true));

		Assert.Equal(32u, platform.DispatchCount);
		Assert.Equal(32u, platform.RecursiveDispatches);
		Assert.Equal(0u, platform.ReadUInt32(State, 20));
		Assert.Equal(1u, MuiNotifyCore.Remove(ref platform, State, source,
			Attribute, destination, true));
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR cl)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x5000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Notify.mui");
		MuiHeadlessObjectCore.Initialize(ref platform, State);
		cl = MuiHeadlessObjectCore.RegisterClass(ref platform, State, name,
			APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}
}
