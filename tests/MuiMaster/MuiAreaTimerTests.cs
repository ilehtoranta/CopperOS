using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaTimerTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void StateRecordUsesNamedSignedValueAndGenerationFields()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1500);
		var expected = default(MuiAreaTimerStateRecord);
		expected.Magic = MuiAreaTimerStateRecord.Cookie;
		expected.Value = -9;
		expected.Generation = 7;

		Assert.True(MuiAreaTimerStateRecordCodec.Write(ref platform, address,
			expected));
		Assert.True(MuiAreaTimerStateRecordCodec.TryRead(ref platform, address,
			out var actual));
		Assert.Equal(expected.Magic, actual.Magic);
		Assert.Equal(expected.Value, actual.Value);
		Assert.Equal(expected.Generation, actual.Generation);

		var cursor = default(MuiAreaTimerStateFieldCursor);
		cursor.Record = address;
		cursor.Field = MuiAreaTimerStateField.Value;
		Assert.True(MuiAreaTimerStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var valueAddress));
		Assert.Equal(address.Raw + 4, valueAddress.Raw);
		Assert.False(MuiAreaTimerStateRecordCodec.TryRead(ref platform,
			APTR.FromPointer(0x21000u), out _));
	}

	[Fact]
	public void AreaTimerStateUsesDedicatedStructCodec()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1580);
		var value = default(MuiAreaTimerStateRecord);
		value.Magic = MuiAreaTimerStateRecord.Cookie;
		value.Value = int.MinValue;
		value.Generation = 7;

		Assert.True(MuiAreaTimerStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiAreaTimerStateRecordCodec.TryReadStructural(ref platform,
			address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Value, decoded.Value);
		Assert.Equal(value.Generation, decoded.Generation);
		Assert.True(MuiAreaTimerStateRecordCodec.TryRead(ref platform, address,
			out decoded));
		Assert.False(MuiAreaTimerStateRecordCodec.TryReadStructural(ref platform,
			APTR.Null, out _));
	}

	[Fact]
	public void TimerAdmissionRequiresGenerationAndLiveOwner()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		var valid = new MuiAreaTimerStateRecord
		{
			Magic = MuiAreaTimerStateRecord.Cookie,
			Value = -9,
			Generation = 1,
		};
		Assert.True(MuiAreaTimerStateAdmission.Validate(valid));
		Assert.True(MuiAreaTimerStateAdmission.ValidateLive(ref platform, State,
			obj, valid));
		var malformed = valid;
		malformed.Generation = 0;
		Assert.False(MuiAreaTimerStateAdmission.Validate(malformed));
		Assert.False(MuiAreaTimerStateAdmission.ValidateLive(ref platform, State,
			obj, malformed));
		malformed = valid;
		malformed.Magic = 0;
		Assert.False(MuiAreaTimerStateAdmission.ValidateLive(ref platform, State,
			APTR.FromPointer(0xDEAD), malformed));
	}

	[Fact]
	public void MalformedTimerFailsClosedBeforeRawRepairOrPublication()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		Assert.True(MuiAreaTimerPacketCore.TryGet(ref platform, State, obj,
			out _));
		Assert.True(MuiAreaTimerPacketCore.Publish(ref platform, State, obj,
			-9, false));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, obj,
			MuiAreaTimerCore.StateKey);
		Assert.True(block.IsNotNull);
		Assert.True(MuiAreaTimerStateFieldCursorCodec.TryWriteUInt32(ref platform,
			block, MuiAreaTimerStateField.Generation, 0));
		Assert.True(MuiAreaTimerStateRecordCodec.TryReadStructural(ref platform,
			block, out var structural));
		Assert.Equal(0u, structural.Generation);
		Assert.False(MuiAreaTimerStateAdmission.Validate(structural));
		Assert.False(MuiAreaTimerStateRecordCodec.TryRead(ref platform, block,
			out _));
		var allocationsBefore = platform.AllocationCount;
		Assert.False(MuiAreaTimerPacketCore.TryGet(ref platform, State, obj,
			out _));
		Assert.Equal(allocationsBefore, platform.AllocationCount);
		Assert.False(MuiAreaTimerPacketCore.Publish(ref platform, State, obj,
			12, false));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State, obj,
			MuiCommonControlCore.Timer, out var raw));
		Assert.Equal(unchecked((uint)-9), raw);
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State, obj,
			MuiAreaTimerCore.StateKey));
	}

	[Fact]
	public void TimerEventAdmissionRequiresCanonicalFlagsAndGeneration()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		var valid = new MuiAreaTimerEventStateRecord
		{
			Magic = MuiAreaTimerEventStateRecord.Cookie,
			Armed = 1,
			MouseOver = 1,
			DelayElapsed = 0,
			LastTick = 42,
			Generation = 1,
		};
		Assert.True(MuiAreaTimerEventStateAdmission.Validate(valid));
		var malformed = valid;
		malformed.MouseOver = 2;
		Assert.False(MuiAreaTimerEventStateAdmission.Validate(malformed));
		malformed = valid;
		malformed.Generation = 0;
		Assert.False(MuiAreaTimerEventStateAdmission.Validate(malformed));
	}

	[Fact]
	public void MalformedTimerEventFailsClosedWithoutReplacement()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		Assert.True(MuiAreaTimerPacketCore.ProcessEvent(ref platform, State, obj,
			new MuiAreaTimerEventInput
			{
				Kind = MuiAreaTimerEventKind.RelVerifyPress,
				Tick = 10,
				PointerOver = 1,
				DelayElapsed = 1,
			}, false));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, obj,
			MuiAreaTimerCore.EventStateKey);
		Assert.True(block.IsNotNull);
		Assert.True(MuiAreaTimerEventStateCodec.TryReadStructural(ref platform,
			block, out var structural));
		structural.Armed = 2;
		Assert.False(MuiAreaTimerEventStateCodec.Write(ref platform, block,
			structural));
		// The bounded codec rejects non-canonical flags, so inject the malformed
		// value through its named guest field for structural diagnostics.
		Assert.True(MuiAreaTimerEventStateCodec.TryWriteUInt32(ref platform,
			block, MuiAreaTimerEventStateField.Armed, 2));
		Assert.True(MuiAreaTimerEventStateCodec.TryReadStructural(ref platform,
			block, out structural));
		Assert.Equal(2u, structural.Armed);
		Assert.False(MuiAreaTimerEventStateAdmission.Validate(structural));
		Assert.False(MuiAreaTimerEventStateCodec.TryRead(ref platform, block,
			out _));
		var allocationsBefore = platform.AllocationCount;
		Assert.False(MuiAreaTimerPacketCore.ProcessEvent(ref platform, State, obj,
			new MuiAreaTimerEventInput
			{
				Kind = MuiAreaTimerEventKind.IntuiTick,
				Tick = 11,
				PointerOver = 1,
				DelayElapsed = 1,
			}, false));
		Assert.Equal(allocationsBefore, platform.AllocationCount);
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State, obj,
			MuiAreaTimerCore.EventStateKey));
	}

	[Fact]
	public void TypedTimerStatePublishesAndReadsSignedValues()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);

		Assert.True(MuiAreaTimerPacketCore.TryGet(ref platform, State, obj,
			out var initial));
		Assert.Equal(0, initial.Value);
		Assert.True(MuiAreaTimerPacketCore.Publish(ref platform, State, obj,
			-9, false));
		Assert.True(MuiAreaTimerPacketCore.TryGet(ref platform, State, obj,
			out var value));
		Assert.Equal(-9, value.Value);
		Assert.True(MuiAreaTimerPacketCore.Publish(ref platform, State, obj,
			12, false));
		Assert.True(MuiAreaTimerPacketCore.TryGet(ref platform, State, obj,
			out value));
		Assert.Equal(12, value.Value);
	}

	[Fact]
	public void GenericRawPublicationProjectsThroughCommonGetter()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);

		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, obj,
			MuiCommonControlCore.Timer, unchecked((uint)-11), false));
		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, obj,
			MuiCommonControlCore.Timer, out var raw, out var handled));
		Assert.True(handled);
		Assert.Equal(unchecked((uint)-11), raw);
		Assert.True(MuiAreaTimerPacketCore.TryGet(ref platform, State, obj,
			out var typed));
		Assert.Equal(-11, typed.Value);
	}

	[Fact]
	public void GetterOnlyDispatcherSetIsRejectedAndOmGetReturnsValue()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		Assert.True(MuiAreaTimerPacketCore.Publish(ref platform, State, obj,
			-13, false));

		var packet = APTR.FromPointer(0x1300);
		platform.WriteUInt32(packet, 0, MuiCommonControlPacketCore.Set);
		platform.WriteUInt32(packet, 4, MuiCommonControlCore.Timer);
		platform.WriteUInt32(packet, 8, 2);
		Assert.Equal(0u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			obj, packet));

		var storage = APTR.FromPointer(0x1400);
		platform.WriteUInt32(packet, 0, MuiCommonControlPacketCore.OmGet);
		platform.WriteUInt32(packet, 4, MuiCommonControlCore.Timer);
		platform.WriteUInt32(packet, 8, storage.Raw);
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			obj, packet));
		Assert.Equal(unchecked((uint)-13), platform.ReadUInt32(storage, 0));
	}

	[Fact]
	public void TimerEventStateAdvancesOnlyAfterDelayAndWhilePointerIsOver()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);

		Assert.True(MuiAreaTimerPacketCore.ProcessEvent(ref platform, State, obj,
			new MuiAreaTimerEventInput
			{
				Kind = MuiAreaTimerEventKind.RelVerifyPress,
				Tick = 10,
				PointerOver = 1,
				DelayElapsed = 0,
			}, false));
		Assert.True(MuiAreaTimerPacketCore.TryGet(ref platform, State, obj,
			out var state));
		Assert.Equal(0, state.Value);
		Assert.Equal(1u, state.Armed);
		Assert.Equal(1u, state.MouseOver);
		Assert.Equal(0u, state.DelayElapsed);

		Assert.True(MuiAreaTimerPacketCore.ProcessEvent(ref platform, State, obj,
			new MuiAreaTimerEventInput
			{
				Kind = MuiAreaTimerEventKind.IntuiTick,
				Tick = 11,
				PointerOver = 1,
				DelayElapsed = 0,
			}, false));
		Assert.True(MuiAreaTimerPacketCore.TryGet(ref platform, State, obj,
			out state));
		Assert.Equal(0, state.Value);

		Assert.True(MuiAreaTimerPacketCore.ProcessEvent(ref platform, State, obj,
			new MuiAreaTimerEventInput
			{
				Kind = MuiAreaTimerEventKind.IntuiTick,
				Tick = 12,
				PointerOver = 1,
				DelayElapsed = 1,
			}, false));
		Assert.True(MuiAreaTimerPacketCore.TryGet(ref platform, State, obj,
			out state));
		Assert.Equal(1, state.Value);
		Assert.Equal(12u, state.LastTick);

		// Re-delivery of one IntuiTick must not advance the counter twice.
		Assert.True(MuiAreaTimerPacketCore.ProcessEvent(ref platform, State, obj,
			new MuiAreaTimerEventInput
			{
				Kind = MuiAreaTimerEventKind.IntuiTick,
				Tick = 12,
				PointerOver = 1,
				DelayElapsed = 1,
			}, false));
		Assert.True(MuiAreaTimerPacketCore.TryGet(ref platform, State, obj,
			out state));
		Assert.Equal(1, state.Value);

		Assert.True(MuiAreaTimerPacketCore.ProcessEvent(ref platform, State, obj,
			new MuiAreaTimerEventInput
			{
				Kind = MuiAreaTimerEventKind.PointerLeave,
				Tick = 13,
			}, false));
		Assert.True(MuiAreaTimerPacketCore.ProcessEvent(ref platform, State, obj,
			new MuiAreaTimerEventInput
			{
				Kind = MuiAreaTimerEventKind.IntuiTick,
				Tick = 13,
				PointerOver = 0,
				DelayElapsed = 1,
			}, false));
		Assert.True(MuiAreaTimerPacketCore.TryGet(ref platform, State, obj,
			out state));
		Assert.Equal(1, state.Value);
		Assert.Equal(0u, state.MouseOver);

		Assert.True(MuiAreaTimerPacketCore.ProcessEvent(ref platform, State, obj,
			new MuiAreaTimerEventInput
			{
				Kind = MuiAreaTimerEventKind.RelVerifyRelease,
				Tick = 14,
			}, false));
		Assert.True(MuiAreaTimerPacketCore.TryGet(ref platform, State, obj,
			out state));
		Assert.Equal(0u, state.Armed);
		Assert.Equal(0u, state.DelayElapsed);
	}

	[Fact]
	public void TimerPointerReentryResumesAfterTheSameTypedBoundary()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		Assert.True(MuiAreaTimerPacketCore.ProcessEvent(ref platform, State, obj,
			new MuiAreaTimerEventInput
			{
				Kind = MuiAreaTimerEventKind.RelVerifyPress,
				Tick = 20,
				PointerOver = 1,
				DelayElapsed = 1,
			}, false));
		Assert.True(MuiAreaTimerPacketCore.ProcessEvent(ref platform, State, obj,
			new MuiAreaTimerEventInput
			{
				Kind = MuiAreaTimerEventKind.IntuiTick,
				Tick = 21,
				PointerOver = 1,
				DelayElapsed = 1,
			}, false));
		Assert.True(MuiAreaTimerPacketCore.ProcessEvent(ref platform, State, obj,
			new MuiAreaTimerEventInput
			{
				Kind = MuiAreaTimerEventKind.PointerLeave,
				Tick = 22,
			}, false));
		Assert.True(MuiAreaTimerPacketCore.ProcessEvent(ref platform, State, obj,
			new MuiAreaTimerEventInput
			{
				Kind = MuiAreaTimerEventKind.PointerEnter,
				Tick = 23,
			}, false));
		Assert.True(MuiAreaTimerPacketCore.ProcessEvent(ref platform, State, obj,
			new MuiAreaTimerEventInput
			{
				Kind = MuiAreaTimerEventKind.IntuiTick,
				Tick = 24,
				PointerOver = 1,
				DelayElapsed = 1,
			}, false));
		Assert.True(MuiAreaTimerPacketCore.TryGet(ref platform, State, obj,
			out var state));
		Assert.Equal(2, state.Value);
		Assert.Equal(1u, state.Armed);
		Assert.Equal(1u, state.MouseOver);
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
