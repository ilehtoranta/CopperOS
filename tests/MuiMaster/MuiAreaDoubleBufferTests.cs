using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaDoubleBufferTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void StateRecordUsesNamedFields()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1500);
		var expected = default(MuiAreaDoubleBufferStateRecord);
		expected.Magic = MuiAreaDoubleBufferStateRecord.Cookie;
		expected.Enabled = 1;
		expected.Generation = 7;

		Assert.True(MuiAreaDoubleBufferStateRecordCodec.Write(ref platform,
			address, expected));
		Assert.True(MuiAreaDoubleBufferStateRecordCodec.TryRead(ref platform,
			address, out var actual));
		Assert.Equal(expected.Magic, actual.Magic);
		Assert.Equal(expected.Enabled, actual.Enabled);
		Assert.Equal(expected.Generation, actual.Generation);

		var cursor = default(MuiAreaDoubleBufferStateFieldCursor);
		cursor.Record = address;
		cursor.Field = MuiAreaDoubleBufferStateField.Generation;
		Assert.True(MuiAreaDoubleBufferStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var generationAddress));
		Assert.Equal(address.Raw + 8, generationAddress.Raw);
		Assert.False(MuiAreaDoubleBufferStateRecordCodec.TryRead(ref platform,
			APTR.FromPointer(0x20FFFu), out _));
	}

	[Fact]
	public void TypedDoubleBufferStateNormalizesAndRoundTrips()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);

		Assert.True(MuiAreaDoubleBufferPacketCore.TryGet(ref platform, State, obj,
			out var initial));
		Assert.Equal(0u, initial.Enabled);
		Assert.True(MuiAreaDoubleBufferPacketCore.Set(ref platform, State, obj, 9));
		Assert.True(MuiAreaDoubleBufferPacketCore.TryGet(ref platform, State, obj,
			out var enabled));
		Assert.Equal(1u, enabled.Enabled);
		Assert.True(MuiAreaDoubleBufferStateRecordCodec.TryRead(ref platform,
			MuiStoreCore.DataspaceFind(ref platform, State, obj,
				MuiAreaDoubleBufferCore.StateKey), out var record));
		Assert.Equal(1u, record.Enabled);
		Assert.True(record.Generation >= 1u);
	}

	[Fact]
	public void GenericSetAndGetUseDoubleBufferState()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);

		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, obj,
			MuiCommonControlCore.DoubleBuffer, 2, false));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, obj,
			MuiCommonControlCore.DoubleBuffer, out var value));
		Assert.Equal(1u, value);
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			obj, MuiCommonControlCore.DoubleBuffer, 0, false));
		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, obj,
			MuiCommonControlCore.DoubleBuffer, out value, out var handled));
		Assert.True(handled);
		Assert.Equal(0u, value);
	}

	[Fact]
	public void DispatcherSetAndOmGetProjectDoubleBuffer()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		var packet = APTR.FromPointer(0x1300);
		platform.WriteUInt32(packet, 0, MuiCommonControlPacketCore.Set);
		platform.WriteUInt32(packet, 4, MuiCommonControlCore.DoubleBuffer);
		platform.WriteUInt32(packet, 8, 3);
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			obj, packet));

		var storage = APTR.FromPointer(0x1400);
		platform.WriteUInt32(packet, 0, MuiCommonControlPacketCore.OmGet);
		platform.WriteUInt32(packet, 4, MuiCommonControlCore.DoubleBuffer);
		platform.WriteUInt32(packet, 8, storage.Raw);
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			obj, packet));
		Assert.Equal(1u, platform.ReadUInt32(storage, 0));
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR areaClass)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Text.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		areaClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State, name,
			APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}
}
