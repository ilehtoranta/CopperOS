using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiConfigItemTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint PublicScreen = 0x24;

	[Fact]
	public void NotifyConfigStorageUsesNamedValue()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1520);
		var expected = new MuiNotifyConfigStorage { Value = 0x2A00 };

		Assert.True(MuiNotifyConfigStorageCodec.Write(ref platform, address,
			expected));
		Assert.True(MuiNotifyConfigStorageCodec.TryRead(ref platform, address,
			out var actual));
		Assert.Equal(expected.Value, actual.Value);
		Assert.False(MuiNotifyConfigStorageCodec.TryRead(ref platform,
			APTR.Null, out _));
	}

	[Fact]
	public void GetConfigItemWritesTheMorphosPublicScreenValue()
	{
		var platform = CreatePlatform(out var obj);
		var packet = APTR.FromPointer(0x1500);
		var storage = APTR.FromPointer(0x1510);
		platform.PublicScreenConfigValue = 0x2A00;
		Assert.True(MuiNotifyConfigMessageCore.WriteRecord(ref platform, packet,
			PublicScreen, storage));
		platform.WriteUInt32(storage, 0, 0xDEADBEEFu);

		Assert.Equal(1u, MuiHeadlessDispatcher.Dispatch(ref platform, State, obj,
			packet));
		Assert.Equal(0x2A00u, platform.ReadUInt32(storage, 0));
		Assert.Equal(1u, platform.ConfigItemRequestCount);
		Assert.Equal(obj, platform.LastConfigItemObject);
		Assert.Equal(PublicScreen, platform.LastConfigItemId);
	}

	[Fact]
	public void GetConfigItemRejectsUnknownIdsAndInvalidStorageBeforeCapability()
	{
		var platform = CreatePlatform(out var obj);
		var packet = APTR.FromPointer(0x1500);
		var storage = APTR.FromPointer(0x1510);
		platform.WriteUInt32(storage, 0, 0xA5A5A5A5u);
		Assert.True(MuiNotifyConfigMessageCore.WriteRecord(ref platform, packet,
			0x25, storage));

		Assert.Equal(0u, MuiHeadlessDispatcher.Dispatch(ref platform, State, obj,
			packet));
		Assert.Equal(0xA5A5A5A5u, platform.ReadUInt32(storage, 0));
		Assert.Equal(0u, platform.ConfigItemRequestCount);

		Assert.True(MuiNotifyConfigMessageCore.WriteRecord(ref platform, packet,
			PublicScreen, APTR.FromPointer(0x5FFF)));
		Assert.Equal(0u, MuiHeadlessDispatcher.Dispatch(ref platform, State, obj,
			packet));
		Assert.Equal(0u, platform.ConfigItemRequestCount);
	}

	[Fact]
	public void GetConfigItemMethodHeaderUsesNamedField()
	{
		var platform = CreatePlatform(out _);
		var packet = APTR.FromPointer(0x1500);
		Assert.True(MuiNotifyConfigMessageCore.WriteRecord(ref platform, packet,
			PublicScreen, APTR.FromPointer(0x1510)));
		Assert.True(MuiGetConfigItemMessageCodec.TryReadMethodId(ref platform,
			packet, out var header));
		Assert.Equal(MuiNotifyConfigMessageCore.GetConfigItemMethod,
			header.MethodId);
		Assert.False(MuiGetConfigItemMessageCodec.TryReadMethodId(ref platform,
			APTR.Null, out _));
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR obj)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x5000, 0x2000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Notify.mui");
		MuiHeadlessObjectCore.Initialize(ref platform, State);
		var cl = MuiHeadlessObjectCore.RegisterClass(ref platform, State, name,
			APTR.Null, 0, APTR.FromPointer(1), false);
		obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		return platform;
	}
}
