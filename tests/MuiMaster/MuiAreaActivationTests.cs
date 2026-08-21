using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaActivationTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void GoActivePacketsRoundTripAsNamedRecords()
	{
		var platform = CreatePlatform(out _);
		var packet = APTR.FromPointer(0x1200);
		Assert.True(MuiAreaActivationMessageCodec.Write(ref platform, packet,
			MuiAreaActivationMessageCodec.GoActive, 0xA5A5));
		Assert.True(MuiAreaActivationMessageCodec.TryRead(ref platform, packet,
			out var active));
		Assert.Equal(MuiAreaActivationMessageCodec.GoActive, active.MethodId);
		Assert.Equal(0xA5A5u, active.Flags);

		Assert.True(MuiAreaActivationMessageCodec.Write(ref platform, packet,
			MuiAreaActivationMessageCodec.GoInactive, 0x55AA));
		Assert.True(MuiAreaActivationMessageCodec.TryRead(ref platform, packet,
			out var inactive));
		Assert.Equal(MuiAreaActivationMessageCodec.GoInactive, inactive.MethodId);
		Assert.Equal(0x55AAu, inactive.Flags);
		Assert.False(MuiAreaActivationMessageCodec.TryRead(ref platform,
			APTR.FromPointer(0x20FFCu), out _));
	}

	[Fact]
	public void GoActiveAndInactiveTrackFlagsAndActiveState()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		var packet = APTR.FromPointer(0x1300);

		Assert.True(MuiAreaActivationMessageCodec.Write(ref platform, packet,
			MuiAreaActivationMessageCodec.GoActive, 7));
		Assert.Equal(1u, MuiLayoutDispatcher.Dispatch(ref platform, State, obj,
			packet));
		Assert.True(MuiAreaActivationCore.IsActive(ref platform, State, obj));
		Assert.Equal(7u, MuiAreaActivationCore.Flags(ref platform, State, obj));
		Assert.True(MuiAreaActivationPacketCore.TryGet(ref platform, State, obj,
			out var activeState));
		Assert.Equal(1u, activeState.Active);
		Assert.Equal(7u, activeState.Flags);

		Assert.True(MuiAreaActivationMessageCodec.Write(ref platform, packet,
			MuiAreaActivationMessageCodec.GoInactive, 11));
		Assert.Equal(0u, MuiLayoutDispatcher.Dispatch(ref platform, State, obj,
			packet));
		Assert.False(MuiAreaActivationCore.IsActive(ref platform, State, obj));
		Assert.Equal(11u, MuiAreaActivationCore.Flags(ref platform, State, obj));
		Assert.True(MuiAreaActivationPacketCore.TryGet(ref platform, State, obj,
			out var inactiveState));
		Assert.Equal(0u, inactiveState.Active);
		Assert.Equal(11u, inactiveState.Flags);
	}

	[Fact]
	public void AreaActivationStateRecordUsesNamedFields()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1500);
		var expected = default(MuiAreaActivationStateRecord);
		expected.Signature = MuiAreaActivationStateRecord.Cookie;
		expected.Active = 1;
		expected.Flags = 0xA5A5;
		expected.Generation = 9;

		Assert.True(MuiAreaActivationStateCodec.Write(ref platform, address,
			expected));
		Assert.True(MuiAreaActivationStateCodec.TryRead(ref platform, address,
			out var decoded));
		Assert.Equal(expected.Signature, decoded.Signature);
		Assert.Equal(expected.Active, decoded.Active);
		Assert.Equal(expected.Flags, decoded.Flags);
		Assert.Equal(expected.Generation, decoded.Generation);

		var cursor = default(MuiAreaActivationStateFieldCursor);
		cursor.Address = address;
		cursor.Field = MuiAreaActivationStateField.Flags;
		Assert.True(MuiAreaActivationStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var fieldAddress));
		Assert.Equal(address.Raw + 8, fieldAddress.Raw);
		Assert.True(MuiAreaActivationStateFieldCursorCodec.TryWrite(ref platform,
			address, MuiAreaActivationStateField.Generation, 10));
		Assert.True(MuiAreaActivationStateFieldCursorCodec.TryRead(ref platform,
			address, MuiAreaActivationStateField.Generation, out var generation));
		Assert.Equal(10u, generation);

		Assert.False(MuiAreaActivationStateCodec.TryRead(ref platform,
			APTR.FromPointer(0x20FFFu), out _));
		Assert.True(MuiAreaActivationStateFieldCursorCodec.TryWrite(ref platform,
			address, MuiAreaActivationStateField.Signature, 0));
		Assert.False(MuiAreaActivationStateCodec.TryRead(ref platform, address,
			out _));
	}

	[Fact]
	public void AreaActivationPacketCoreUsesTypedTransitions()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);

		Assert.True(MuiAreaActivationPacketCore.GoActive(ref platform, State,
			obj, 3));
		Assert.True(MuiAreaActivationPacketCore.TryGet(ref platform, State, obj,
			out var active));
		Assert.Equal(1u, active.Active);
		Assert.Equal(3u, active.Flags);

		Assert.True(MuiAreaActivationPacketCore.GoInactive(ref platform, State,
			obj, 4));
		Assert.True(MuiAreaActivationPacketCore.TryGet(ref platform, State, obj,
			out var inactive));
		Assert.Equal(0u, inactive.Active);
		Assert.Equal(4u, inactive.Flags);
	}

	[Fact]
	public void UnsupportedActivationPacketsRemainUnclaimed()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		var packet = APTR.FromPointer(0x1400);
		platform.WriteUInt32(packet, 0, 0xDEADBEEFu);
		platform.WriteUInt32(packet, 4, 1);
		Assert.Equal(0u, MuiLayoutDispatcher.Dispatch(ref platform, State, obj,
			packet));
		Assert.False(MuiAreaActivationCore.IsActive(ref platform, State, obj));
	}

	[Fact]
	public void AreaActivationMethodHeaderUsesNamedField()
	{
		var platform = CreatePlatform(out _);
		var packet = APTR.FromPointer(0x1200);
		Assert.True(MuiAreaActivationMessageCodec.Write(ref platform, packet,
			MuiAreaActivationMessageCodec.GoActive, 7));
		Assert.True(MuiAreaActivationMessageCodec.TryReadMethodId(ref platform,
			packet, out var header));
		Assert.Equal(MuiAreaActivationMessageCodec.GoActive, header.MethodId);
		Assert.False(MuiAreaActivationMessageCodec.TryReadMethodId(ref platform,
			APTR.Null, out _));
	}

	[Fact]
	public void AreaActivationFieldCursorUsesNamedBoundaries()
	{
		var platform = CreatePlatform(out _);
		var packet = APTR.FromPointer(0x1200);
		var cursor = default(MuiAreaActivationFieldCursor);
		cursor.Message = packet;
		cursor.Packet = MuiAreaActivationPacketKind.Activation;
		cursor.Field = MuiAreaActivationField.MethodId;
		Assert.True(MuiAreaActivationFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var address));
		Assert.Equal(packet.Raw, address.Raw);
		cursor.Field = MuiAreaActivationField.Flags;
		Assert.True(MuiAreaActivationFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out address));
		Assert.Equal(packet.Raw + 4, address.Raw);

		Assert.True(MuiAreaActivationFieldCursorCodec.TryWriteUInt32(ref platform,
			packet, MuiAreaActivationPacketKind.Activation,
			MuiAreaActivationField.Flags, 0xAABBCCDD));
		Assert.True(MuiAreaActivationFieldCursorCodec.TryReadUInt32(ref platform,
			packet, MuiAreaActivationPacketKind.Activation,
			MuiAreaActivationField.Flags, out var flags));
		Assert.Equal(0xAABBCCDDu, flags);

		cursor.Packet = MuiAreaActivationPacketKind.Method;
		cursor.Field = MuiAreaActivationField.Flags;
		Assert.False(MuiAreaActivationFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _));
		cursor.Message = APTR.FromPointer(0xFFFFFFF0u);
		cursor.Packet = MuiAreaActivationPacketKind.Activation;
		cursor.Field = MuiAreaActivationField.Flags;
		Assert.False(MuiAreaActivationFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _));
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR areaClass)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Area.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		areaClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State, name,
			APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}
}
