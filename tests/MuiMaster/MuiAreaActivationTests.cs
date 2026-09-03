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
		var activePacket = new MuiAreaActivationMessage
		{
			MethodId = MuiAreaActivationMessageCodec.GoActive,
			Flags = 0xA5A5,
		};
		Assert.True(MuiAreaActivationMessageCodec.Write(ref platform, packet,
			activePacket));
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
		Assert.True(MuiAreaActivationStateCodec.TryReadStructural(ref platform,
			address, out var malformed));
		Assert.Equal(0u, malformed.Signature);
		Assert.False(MuiAreaActivationStateCodec.TryRead(ref platform, address,
			out _));
	}

	[Fact]
	public void AreaActivationStateUsesDedicatedStructCodec()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1580);
		var value = default(MuiAreaActivationStateRecord);
		value.Signature = MuiAreaActivationStateRecord.Cookie;
		value.Active = 1;
		value.Flags = 0xA5A5;
		value.Generation = 9;

		Assert.True(MuiAreaActivationStateCodec.Write(ref platform, address,
			value));
		Assert.True(MuiAreaActivationStateCodec.TryReadStructural(ref platform,
			address, out var decoded));
		Assert.Equal(value.Signature, decoded.Signature);
		Assert.Equal(value.Active, decoded.Active);
		Assert.Equal(value.Flags, decoded.Flags);
		Assert.Equal(value.Generation, decoded.Generation);
		Assert.True(MuiAreaActivationStateCodec.TryRead(ref platform, address,
			out decoded));
		Assert.False(MuiAreaActivationStateCodec.TryReadStructural(ref platform,
			APTR.Null, out _));
	}

	[Fact]
	public void AreaActivationAdmissionValidatesBooleanAndLiveOwner()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		var value = default(MuiAreaActivationStateRecord);
		value.Signature = MuiAreaActivationStateRecord.Cookie;
		Assert.True(MuiAreaActivationStateAdmission.Validate(value));
		Assert.True(MuiAreaActivationStateAdmission.ValidateLive(ref platform,
			State, obj, value));
		value.Active = 1;
		Assert.True(MuiAreaActivationStateAdmission.Validate(value));
		value.Active = 2;
		Assert.False(MuiAreaActivationStateAdmission.Validate(value));
		value.Active = 1;
		Assert.False(MuiAreaActivationStateAdmission.ValidateLive(ref platform,
			State, APTR.FromPointer(0x32000), value));
	}

	[Fact]
	public void MalformedAreaActivationStateFailsClosedBeforeTransition()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		Assert.True(MuiAreaActivationCore.GoActive(ref platform, State, obj, 7));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, obj,
			MuiAreaActivationCore.StateKey);
		Assert.True(block.IsNotNull);
		platform.WriteUInt32(block, 4, 2);
		Assert.False(MuiAreaActivationCore.TryGetState(ref platform, State, obj,
			out _));
		Assert.False(MuiAreaActivationCore.IsActive(ref platform, State, obj));
		Assert.False(MuiAreaActivationCore.GoInactive(ref platform, State, obj,
			11));
		Assert.True(MuiAreaActivationStateCodec.TryReadStructural(ref platform,
			block, out var structural));
		Assert.Equal(2u, structural.Active);
		Assert.Equal(7u, structural.Flags);
		Assert.Equal(1u, structural.Generation);

		// Repair the caller-owned sidecar before retrying; transition admission
		// remains fail-closed while Active is non-canonical.
		platform.WriteUInt32(block, 4, 1);
		Assert.True(MuiAreaActivationCore.GoInactive(ref platform, State, obj,
			11));
		Assert.True(MuiAreaActivationCore.TryGetState(ref platform, State, obj,
			out var repaired));
		Assert.Equal(0u, repaired.Active);
		Assert.Equal(11u, repaired.Flags);
		Assert.Equal(2u, repaired.Generation);
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
		Assert.True(MuiAreaActivationMethodMessageCodec.TryReadValue(ref platform,
			packet, out var scalarMethodId));
		Assert.Equal(MuiAreaActivationMessageCodec.GoActive, scalarMethodId);
		Assert.True(MuiAreaActivationMethodMessageCodec.WriteValue(ref platform,
			packet, 0xF1234567u));
		Assert.True(MuiAreaActivationMethodMessageCodec.TryReadValue(ref platform,
			packet, out scalarMethodId));
		Assert.Equal(0xF1234567u, scalarMethodId);
		Assert.False(MuiAreaActivationMethodMessageCodec.WriteValue(ref platform,
			APTR.Null, 1));
		Assert.False(MuiAreaActivationMethodMessageCodec.TryReadValue(ref platform,
			APTR.FromPointer(0x20FFFu), out _));
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

	[Fact]
	public void AreaActivationRecordAdapterUsesStructMembers()
	{
		var platform = CreatePlatform(out _);
		var packet = APTR.FromPointer(0x1280);
		Assert.True(MuiAreaActivationRecordMemoryCodec.TryWriteUInt32(ref platform,
			packet, MuiAreaActivationPacketKind.Activation,
			MuiAreaActivationField.MethodId, MuiAreaActivationMessageCodec.GoActive));
		Assert.True(MuiAreaActivationRecordMemoryCodec.TryWriteUInt32(ref platform,
			packet, MuiAreaActivationPacketKind.Activation,
			MuiAreaActivationField.Flags, 0xA5A5u));
		Assert.True(MuiAreaActivationRecordMemoryCodec.TryGetAddress(ref platform,
			packet, MuiAreaActivationPacketKind.Activation,
			MuiAreaActivationField.Flags, out var flagsAddress));
		Assert.Equal(packet.Raw + MuiAreaActivationMessage.FlagsOffset,
			flagsAddress.Raw);
		Assert.True(MuiAreaActivationMessageCodec.TryRead(ref platform, packet,
			out var decoded));
		Assert.Equal(MuiAreaActivationMessageCodec.GoActive, decoded.MethodId);
		Assert.Equal(0xA5A5u, decoded.Flags);

		var method = APTR.FromPointer(0x12A0);
		Assert.True(MuiAreaActivationRecordMemoryCodec.TryWriteUInt32(ref platform,
			method, MuiAreaActivationPacketKind.Method,
			MuiAreaActivationField.MethodId, MuiAreaActivationMessageCodec.GoInactive));
		Assert.True(MuiAreaActivationRecordMemoryCodec.TryGetAddress(ref platform,
			method, MuiAreaActivationPacketKind.Method,
			MuiAreaActivationField.MethodId, out var methodAddress));
		Assert.Equal(method.Raw + MuiAreaActivationMethodMessage.MethodIdOffset,
			methodAddress.Raw);
		Assert.False(MuiAreaActivationRecordMemoryCodec.TryGetAddress(ref platform,
			packet, MuiAreaActivationPacketKind.Method,
			MuiAreaActivationField.Flags, out _));
		Assert.False(MuiAreaActivationRecordMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, MuiAreaActivationPacketKind.Activation,
			MuiAreaActivationField.Flags, out _));
	}

	[Fact]
	public void AreaActivationFieldAccessUsesCompleteNamedRecords()
	{
		var platform = CreatePlatform(out _);
		var activationAddress = APTR.FromPointer(0x1600);
		var activation = new MuiAreaActivationMessage
		{
			MethodId = MuiAreaActivationMessageCodec.GoActive,
			Flags = 0x11223344u,
		};
		Assert.True(MuiAreaActivationMessageCodec.WriteStructural(ref platform,
			activationAddress, activation));
		Assert.True(MuiAreaActivationRecordMemoryCodec.TryReadUInt32(ref platform,
			activationAddress, MuiAreaActivationPacketKind.Activation,
			MuiAreaActivationField.Flags, out var flags));
		Assert.Equal(activation.Flags, flags);
		Assert.True(MuiAreaActivationRecordMemoryCodec.TryWriteUInt32(ref platform,
			activationAddress, MuiAreaActivationPacketKind.Activation,
			MuiAreaActivationField.Flags, 0x55667788u));
		Assert.True(MuiAreaActivationFieldCursorCodec.TryWriteUInt32(ref platform,
			activationAddress, MuiAreaActivationPacketKind.Activation,
			MuiAreaActivationField.MethodId, MuiAreaActivationMessageCodec.GoInactive));
		Assert.True(MuiAreaActivationMessageCodec.TryReadStructural(ref platform,
			activationAddress, out var updatedActivation));
		Assert.Equal(MuiAreaActivationMessageCodec.GoInactive,
			updatedActivation.MethodId);
		Assert.Equal(0x55667788u, updatedActivation.Flags);

		var methodAddress = APTR.FromPointer(0x1620);
		var method = new MuiAreaActivationMethodMessage
		{
			MethodId = MuiAreaActivationMessageCodec.GoActive,
		};
		Assert.True(MuiAreaActivationMethodMessageCodec.WriteStructural(ref platform,
			methodAddress, method));
		Assert.True(MuiAreaActivationRecordMemoryCodec.TryWriteUInt32(ref platform,
			methodAddress, MuiAreaActivationPacketKind.Method,
			MuiAreaActivationField.MethodId, MuiAreaActivationMessageCodec.GoInactive));
		Assert.True(MuiAreaActivationMethodMessageCodec.TryReadStructural(ref platform,
			methodAddress, out var updatedMethod));
		Assert.Equal(MuiAreaActivationMessageCodec.GoInactive, updatedMethod.MethodId);

		Assert.False(MuiAreaActivationRecordMemoryCodec.TryReadUInt32(ref platform,
			activationAddress, MuiAreaActivationPacketKind.Method,
			MuiAreaActivationField.Flags, out _));
		Assert.False(MuiAreaActivationRecordMemoryCodec.TryWriteUInt32(ref platform,
			APTR.FromPointer(0x20FFCu), MuiAreaActivationPacketKind.Activation,
			MuiAreaActivationField.Flags, 1));
		Assert.False(MuiAreaActivationFieldCursorCodec.TryReadUInt32(ref platform,
			APTR.Null, MuiAreaActivationPacketKind.Activation,
			MuiAreaActivationField.Flags, out _));
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
