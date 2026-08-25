using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaControlCharTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void StateRecordUsesNamedCharacterAndGenerationFields()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1500);
		var expected = default(MuiAreaControlCharStateRecord);
		expected.Magic = MuiAreaControlCharStateRecord.Cookie;
		expected.Character = 0x141;
		expected.Generation = 7;

		Assert.True(MuiAreaControlCharStateRecordCodec.Write(ref platform,
			address, expected));
		Assert.True(MuiAreaControlCharStateRecordCodec.TryRead(ref platform,
			address, out var actual));
		Assert.Equal(expected.Magic, actual.Magic);
		Assert.Equal(0x41u, actual.Character);
		Assert.Equal(expected.Generation, actual.Generation);

		var cursor = default(MuiAreaControlCharStateFieldCursor);
		cursor.Record = address;
		cursor.Field = MuiAreaControlCharStateField.Character;
		Assert.True(MuiAreaControlCharStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var characterAddress));
		Assert.Equal(address.Raw + 4, characterAddress.Raw);
		Assert.False(MuiAreaControlCharStateRecordCodec.TryRead(ref platform,
			APTR.FromPointer(0x21000u), out _));
	}

	[Fact]
	public void TypedControlCharStateNormalizesAndClears()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);

		Assert.True(MuiAreaControlCharPacketCore.TryGet(ref platform, State, obj,
			out var initial));
		Assert.Equal(0u, initial.Character);
		Assert.True(MuiAreaControlCharPacketCore.Set(ref platform, State, obj,
			0x141));
		Assert.True(MuiAreaControlCharPacketCore.TryGet(ref platform, State, obj,
			out var value));
		Assert.Equal(0x41u, value.Character);
		Assert.True(MuiAreaControlCharPacketCore.Set(ref platform, State, obj, 0));
		Assert.True(MuiAreaControlCharPacketCore.TryGet(ref platform, State, obj,
			out value));
		Assert.Equal(0u, value.Character);
	}

	[Fact]
	public void GenericRawSetAndCommonGetProjectControlCharState()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);

		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, obj,
			MuiCommonControlCore.ControlChar, 0x242, false));
		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, obj,
			MuiCommonControlCore.ControlChar, out var value, out var handled));
		Assert.True(handled);
		Assert.Equal(0x42u, value);
		Assert.True(MuiAreaControlCharPacketCore.TryGet(ref platform, State, obj,
			out var typed));
		Assert.Equal(0x42u, typed.Character);
	}

	[Fact]
	public void DispatcherSetAndOmGetProjectControlChar()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		var packet = APTR.FromPointer(0x1300);
		platform.WriteUInt32(packet, 0, MuiCommonControlPacketCore.Set);
		platform.WriteUInt32(packet, 4, MuiCommonControlCore.ControlChar);
		platform.WriteUInt32(packet, 8, 0x143);
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			obj, packet));

		var storage = APTR.FromPointer(0x1400);
		platform.WriteUInt32(packet, 0, MuiCommonControlPacketCore.OmGet);
		platform.WriteUInt32(packet, 4, MuiCommonControlCore.ControlChar);
		platform.WriteUInt32(packet, 8, storage.Raw);
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			obj, packet));
		Assert.Equal(0x43u, platform.ReadUInt32(storage, 0));
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
