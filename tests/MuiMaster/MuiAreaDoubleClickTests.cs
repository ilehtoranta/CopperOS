using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaDoubleClickTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void StateRecordUsesNamedSignedValueAndGenerationFields()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1500);
		var expected = default(MuiAreaDoubleClickStateRecord);
		expected.Magic = MuiAreaDoubleClickStateRecord.Cookie;
		expected.Value = -2;
		expected.Generation = 7;

		Assert.True(MuiAreaDoubleClickStateRecordCodec.Write(ref platform,
			address, expected));
		Assert.True(MuiAreaDoubleClickStateRecordCodec.TryRead(ref platform,
			address, out var actual));
		Assert.Equal(expected.Magic, actual.Magic);
		Assert.Equal(expected.Value, actual.Value);
		Assert.Equal(expected.Generation, actual.Generation);

		var cursor = default(MuiAreaDoubleClickStateFieldCursor);
		cursor.Record = address;
		cursor.Field = MuiAreaDoubleClickStateField.Value;
		Assert.True(MuiAreaDoubleClickStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var valueAddress));
		Assert.Equal(address.Raw + 4, valueAddress.Raw);
		Assert.False(MuiAreaDoubleClickStateRecordCodec.TryRead(ref platform,
			APTR.FromPointer(0x21000u), out _));
	}

	[Fact]
	public void TypedDoubleClickStatePublishesAndReadsSignedValues()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);

		Assert.True(MuiAreaDoubleClickPacketCore.TryGet(ref platform, State, obj,
			out var initial));
		Assert.Equal(0, initial.Value);
		Assert.True(MuiAreaDoubleClickPacketCore.Publish(ref platform, State,
			obj, -2, false));
		Assert.True(MuiAreaDoubleClickPacketCore.TryGet(ref platform, State, obj,
			out var value));
		Assert.Equal(-2, value.Value);
		Assert.True(MuiAreaDoubleClickPacketCore.Publish(ref platform, State,
			obj, 1, false));
		Assert.True(MuiAreaDoubleClickPacketCore.TryGet(ref platform, State, obj,
			out value));
		Assert.Equal(1, value.Value);
	}

	[Fact]
	public void GenericRawPublicationProjectsThroughCommonGetter()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);

		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, obj,
			MuiCommonControlCore.DoubleClick, unchecked((uint)-3), false));
		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, obj,
			MuiCommonControlCore.DoubleClick, out var raw, out var handled));
		Assert.True(handled);
		Assert.Equal(unchecked((uint)-3), raw);
		Assert.True(MuiAreaDoubleClickPacketCore.TryGet(ref platform, State, obj,
			out var typed));
		Assert.Equal(-3, typed.Value);
	}

	[Fact]
	public void GetterOnlyDispatcherSetIsRejectedAndOmGetReturnsValue()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		Assert.True(MuiAreaDoubleClickPacketCore.Publish(ref platform, State,
			obj, -4, false));

		var packet = APTR.FromPointer(0x1300);
		platform.WriteUInt32(packet, 0, MuiCommonControlPacketCore.Set);
		platform.WriteUInt32(packet, 4, MuiCommonControlCore.DoubleClick);
		platform.WriteUInt32(packet, 8, 2);
		Assert.Equal(0u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			obj, packet));

		var storage = APTR.FromPointer(0x1400);
		platform.WriteUInt32(packet, 0, MuiCommonControlPacketCore.OmGet);
		platform.WriteUInt32(packet, 4, MuiCommonControlCore.DoubleClick);
		platform.WriteUInt32(packet, 8, storage.Raw);
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			obj, packet));
		Assert.Equal(unchecked((uint)-4), platform.ReadUInt32(storage, 0));
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
