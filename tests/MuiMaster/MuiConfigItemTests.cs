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
		Assert.False(MuiNotifyConfigStorageCodec.TryRead(ref platform,
			APTR.FromPointer(0x20FFFu), out _));
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
		Assert.True(MuiGetConfigItemMessageCodec.TryReadMethodHeaderValue(ref
			platform, packet, out var methodId));
		Assert.Equal(MuiNotifyConfigMessageCore.GetConfigItemMethod, methodId);
		Assert.True(MuiGetConfigItemMessageCodec.WriteMethodHeaderValue(ref
			platform, packet, 0xF1234567u));
		Assert.True(MuiGetConfigItemMessageCodec.TryReadMethodHeaderValue(ref
			platform, packet, out methodId));
		Assert.Equal(0xF1234567u, methodId);
		Assert.False(MuiGetConfigItemMessageCodec.TryReadMethodHeaderValue(ref
			platform, APTR.FromPointer(0x5FFEu), out _));
		Assert.False(MuiGetConfigItemMessageCodec.WriteMethodHeaderValue(ref
			platform, APTR.Null, 1));
		Assert.False(MuiGetConfigItemMessageCodec.TryReadMethodId(ref platform,
			APTR.Null, out _));
	}

	[Fact]
	public void GetConfigItemMessageAdapterOwnsStructBounds()
	{
		var platform = CreatePlatform(out _);
		var packet = APTR.FromPointer(0x1500);
		Assert.True(MuiGetConfigItemMessageMemoryCodec.TryWriteUInt32(ref platform,
			packet, MuiGetConfigItemPacketField.ConfigId, PublicScreen));
		Assert.True(MuiGetConfigItemMessageMemoryCodec.TryReadUInt32(ref platform,
			packet, MuiGetConfigItemPacketField.ConfigId, out var configId));
		Assert.Equal(PublicScreen, configId);
		Assert.True(MuiGetConfigItemMessageMemoryCodec.TryWriteUInt32(ref platform,
			packet, MuiGetConfigItemPacketField.Storage, 0x1510));
		Assert.True(MuiGetConfigItemMessageMemoryCodec.TryReadUInt32(ref platform,
			packet, MuiGetConfigItemPacketField.Storage, out var storage));
		Assert.Equal(0x1510u, storage);
		Assert.True(MuiGetConfigItemMessageMemoryCodec.TryWriteUInt32(ref platform,
			packet, MuiGetConfigItemPacketField.MethodId,
			MuiNotifyConfigMessageCore.GetConfigItemMethod));
		Assert.True(MuiGetConfigItemMessageMemoryCodec.TryReadUInt32(ref platform,
			packet, MuiGetConfigItemPacketField.MethodId, out var method));
		Assert.Equal(MuiNotifyConfigMessageCore.GetConfigItemMethod, method);
		Assert.False(MuiGetConfigItemMessageMemoryCodec.TryReadUInt32(ref platform,
			APTR.FromPointer(0x5FF5), MuiGetConfigItemPacketField.Storage,
			out _));
		Assert.False(MuiGetConfigItemMessageMemoryCodec.TryReadUInt32(ref platform,
			packet, (MuiGetConfigItemPacketField)255, out _));
		Assert.False(MuiGetConfigItemMessageMemoryCodec.TryReadUInt32(ref platform,
			APTR.Null, MuiGetConfigItemPacketField.ConfigId, out _));
	}

	[Fact]
	public void GetConfigItemFieldAccessUsesCompleteNamedPacketRecord()
	{
		var platform = CreatePlatform(out _);
		var packet = APTR.FromPointer(0x1800);
		var original = new MuiGetConfigItemMessage
		{
			MethodId = MuiGetConfigItemMessageCodec.Method,
			ConfigId = PublicScreen,
			Storage = APTR.FromPointer(0x1910),
		};
		Assert.True(MuiGetConfigItemMessageCodec.WriteStructural(ref platform,
			packet, original));
		Assert.True(MuiGetConfigItemMessageMemoryCodec.TryWriteUInt32(ref platform,
			packet, MuiGetConfigItemPacketField.ConfigId, 0x2A));
		Assert.True(MuiGetConfigItemMessageMemoryCodec.TryWriteUInt32(ref platform,
			packet, MuiGetConfigItemPacketField.Storage, 0x1A10));
		Assert.True(MuiGetConfigItemMessageCodec.TryReadStructural(ref platform,
			packet, out var afterFields));
		Assert.Equal(original.MethodId, afterFields.MethodId);
		Assert.Equal(0x2Au, afterFields.ConfigId);
		Assert.Equal(0x1A10u, afterFields.Storage.Raw);

		Assert.True(MuiGetConfigItemMessageMemoryCodec.TryWriteUInt32(ref platform,
			packet, MuiGetConfigItemPacketField.MethodId, 0xF1234567u));
		Assert.True(MuiGetConfigItemMessageCodec.TryReadStructural(ref platform,
			packet, out var afterMethod));
		Assert.Equal(0xF1234567u, afterMethod.MethodId);
		Assert.Equal(afterFields.ConfigId, afterMethod.ConfigId);
		Assert.Equal(afterFields.Storage.Raw, afterMethod.Storage.Raw);

		Assert.False(MuiGetConfigItemMessageMemoryCodec.TryReadUInt32(ref platform,
			APTR.FromPointer(0x5FF5), MuiGetConfigItemPacketField.ConfigId, out _));
		Assert.False(MuiGetConfigItemMessageMemoryCodec.TryWriteUInt32(ref platform,
			APTR.FromPointer(0x5FF5), MuiGetConfigItemPacketField.Storage, 1));
		Assert.False(MuiGetConfigItemMessageMemoryCodec.TryReadUInt32(ref platform,
			packet, (MuiGetConfigItemPacketField)255, out _));
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
