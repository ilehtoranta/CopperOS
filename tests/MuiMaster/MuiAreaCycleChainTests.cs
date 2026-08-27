using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaCycleChainTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void StateRecordUsesNamedSignedValueAndGenerationFields()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1500);
		var expected = default(MuiAreaCycleChainStateRecord);
		expected.Magic = MuiAreaCycleChainStateRecord.Cookie;
		expected.Value = -2;
		expected.Generation = 7;

		Assert.True(MuiAreaCycleChainStateRecordCodec.Write(ref platform,
			address, expected));
		Assert.True(MuiAreaCycleChainStateRecordCodec.TryRead(ref platform,
			address, out var actual));
		Assert.Equal(expected.Magic, actual.Magic);
		Assert.Equal(expected.Value, actual.Value);
		Assert.Equal(expected.Generation, actual.Generation);

		var cursor = default(MuiAreaCycleChainStateFieldCursor);
		cursor.Record = address;
		cursor.Field = MuiAreaCycleChainStateField.Value;
		Assert.True(MuiAreaCycleChainStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var valueAddress));
		Assert.Equal(address.Raw + 4, valueAddress.Raw);
		Assert.False(MuiAreaCycleChainStateRecordCodec.TryRead(ref platform,
			APTR.FromPointer(0x21000u), out _));
	}

	[Fact]
	public void StateRecordUsesDedicatedStructCodec()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1540);
		var value = default(MuiAreaCycleChainStateRecord);
		value.Magic = MuiAreaCycleChainStateRecord.Cookie;
		value.Value = int.MinValue;
		value.Generation = 7;

		Assert.True(MuiAreaCycleChainStateRecordCodec.Write(ref platform,
			address, value));
		Assert.True(MuiAreaCycleChainStateRecordCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Value, decoded.Value);
		Assert.Equal(value.Generation, decoded.Generation);
		Assert.True(MuiAreaCycleChainStateRecordCodec.TryRead(ref platform,
			address, out decoded));
		Assert.False(MuiAreaCycleChainStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
	}

	[Fact]
	public void CycleChainAdmissionRequiresGenerationAndLiveOwner()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		var valid = new MuiAreaCycleChainStateRecord
		{
			Magic = MuiAreaCycleChainStateRecord.Cookie,
			Value = -2,
			Generation = 1,
		};
		Assert.True(MuiAreaCycleChainStateAdmission.Validate(valid));
		Assert.True(MuiAreaCycleChainStateAdmission.ValidateLive(ref platform,
			State, obj, valid));
		var malformed = valid;
		malformed.Generation = 0;
		Assert.False(MuiAreaCycleChainStateAdmission.Validate(malformed));
		Assert.False(MuiAreaCycleChainStateAdmission.ValidateLive(ref platform,
			State, obj, malformed));
		Assert.False(MuiAreaCycleChainStateAdmission.ValidateLive(ref platform,
			State, APTR.FromPointer(0xDEAD), valid));
	}

	[Fact]
	public void MalformedCycleChainFailsClosedBeforeRawRepair()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		Assert.True(MuiAreaCycleChainPacketCore.TryGet(ref platform, State, obj,
			out _));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, obj,
			MuiAreaCycleChainCore.StateKey);
		Assert.True(block.IsNotNull);
		Assert.True(MuiAreaCycleChainStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiAreaCycleChainStateField.Generation, 0));
		Assert.True(MuiAreaCycleChainStateRecordCodec.TryReadStructural(
			ref platform, block, out var structural));
		Assert.Equal(0u, structural.Generation);
		Assert.False(MuiAreaCycleChainStateAdmission.Validate(structural));
		Assert.False(MuiAreaCycleChainStateRecordCodec.TryRead(ref platform,
			block, out _));
		var allocationsBefore = platform.AllocationCount;
		Assert.False(MuiAreaCycleChainPacketCore.TryGet(ref platform, State, obj,
			out _));
		Assert.Equal(allocationsBefore, platform.AllocationCount);
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State, obj,
			MuiAreaCycleChainCore.StateKey));
	}

	[Fact]
	public void TypedCycleChainStateRoundTripsSignedValues()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);

		Assert.True(MuiAreaCycleChainPacketCore.TryGet(ref platform, State, obj,
			out var initial));
		Assert.Equal(0, initial.Value);
		Assert.True(MuiAreaCycleChainPacketCore.Set(ref platform, State, obj, -2));
		Assert.True(MuiAreaCycleChainPacketCore.TryGet(ref platform, State, obj,
			out var value));
		Assert.Equal(-2, value.Value);
		Assert.True(MuiAreaCycleChainPacketCore.Set(ref platform, State, obj, 1));
		Assert.True(MuiAreaCycleChainPacketCore.TryGet(ref platform, State, obj,
			out value));
		Assert.Equal(1, value.Value);
	}

	[Fact]
	public void GenericRawSetAndCommonGetProjectCycleChainState()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);

		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, obj,
			MuiCommonControlCore.CycleChain, unchecked((uint)-3), false));
		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, obj,
			MuiCommonControlCore.CycleChain, out var raw, out var handled));
		Assert.True(handled);
		Assert.Equal(unchecked((uint)-3), raw);
		Assert.True(MuiAreaCycleChainPacketCore.TryGet(ref platform, State, obj,
			out var typed));
		Assert.Equal(-3, typed.Value);
	}

	[Fact]
	public void DispatcherSetAndOmGetProjectCycleChain()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		var packet = APTR.FromPointer(0x1300);
		platform.WriteUInt32(packet, 0, MuiCommonControlPacketCore.Set);
		platform.WriteUInt32(packet, 4, MuiCommonControlCore.CycleChain);
		platform.WriteUInt32(packet, 8, unchecked((uint)-4));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			obj, packet));

		var storage = APTR.FromPointer(0x1400);
		platform.WriteUInt32(packet, 0, MuiCommonControlPacketCore.OmGet);
		platform.WriteUInt32(packet, 4, MuiCommonControlCore.CycleChain);
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
