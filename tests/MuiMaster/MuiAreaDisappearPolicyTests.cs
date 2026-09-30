using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaDisappearPolicyTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void CommonControlProjectsSignedDisappearPrioritiesThroughNamedState()
	{
		var platform = CreatePlatform(out var rectangleClass);
		var rectangle = MuiCommonControlCore.CreateControl(ref platform, State,
			rectangleClass, APTR.Null);

		Assert.True(MuiAreaDisappearPacketCore.TryGet(ref platform, State,
			rectangle, out var defaults));
		Assert.Equal(0, defaults.HorizDisappear);
		Assert.Equal(0, defaults.VertDisappear);

		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			rectangle, MuiCommonControlCore.HorizDisappear,
			unchecked((uint)-7), false));
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			rectangle, MuiCommonControlCore.VertDisappear,
			unchecked((uint)13), false));

		Assert.True(MuiAreaDisappearPacketCore.TryGet(ref platform, State,
			rectangle, out var value));
		Assert.Equal(-7, value.HorizDisappear);
		Assert.Equal(13, value.VertDisappear);
		Assert.True(MuiAreaDisappearCore.TryReadStateRecord(ref platform, State,
			rectangle, out var record));
		Assert.Equal(MuiAreaDisappearPolicyStateRecord.Cookie, record.Magic);
		Assert.Equal(-7, record.HorizDisappear);
		Assert.Equal(13, record.VertDisappear);
	}

	[Fact]
	public void TypedDisappearPacketUpdatesBothSignedFields()
	{
		var platform = CreatePlatform(out var rectangleClass);
		var rectangle = MuiCommonControlCore.CreateControl(ref platform, State,
			rectangleClass, APTR.Null);
		var input = new MuiAreaDisappearPolicyStateInput
		{
			HorizDisappear = 42,
			VertDisappear = -19,
		};

		Assert.True(MuiAreaDisappearPacketCore.Set(ref platform, State, rectangle,
			input));
		Assert.True(MuiAreaDisappearPacketCore.TryGet(ref platform, State,
			rectangle, out var value));
		Assert.Equal(input.HorizDisappear, value.HorizDisappear);
		Assert.Equal(input.VertDisappear, value.VertDisappear);
		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, rectangle,
			MuiCommonControlCore.HorizDisappear, out var horizontal, out var handled));
		Assert.True(handled);
		Assert.Equal(unchecked((uint)input.HorizDisappear), horizontal);
	}

	[Fact]
	public void DisappearPolicyAdmissionPreservesSignedFieldsAndLiveOwner()
	{
		var platform = CreatePlatform(out var rectangleClass);
		var rectangle = MuiCommonControlCore.CreateControl(ref platform, State,
			rectangleClass, APTR.Null);
		var valid = new MuiAreaDisappearPolicyStateRecord
		{
			Magic = MuiAreaDisappearPolicyStateRecord.Cookie,
			HorizDisappear = int.MinValue,
			VertDisappear = int.MaxValue,
		};
		Assert.True(MuiAreaDisappearPolicyStateAdmission.Validate(valid));
		Assert.True(MuiAreaDisappearPolicyStateAdmission.ValidateLive(ref platform,
			State, rectangle, valid));
		valid.Magic = 0;
		Assert.False(MuiAreaDisappearPolicyStateAdmission.Validate(valid));
		valid.Magic = MuiAreaDisappearPolicyStateRecord.Cookie;
		Assert.False(MuiAreaDisappearPolicyStateAdmission.ValidateLive(ref platform,
			State, APTR.FromPointer(0xDEAD), valid));
	}

	[Fact]
	public void DisappearPolicyUsesDedicatedStructCodec()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1680);
		var value = default(MuiAreaDisappearPolicyStateRecord);
		value.Magic = MuiAreaDisappearPolicyStateRecord.Cookie;
		value.HorizDisappear = int.MinValue;
		value.VertDisappear = int.MaxValue;

		Assert.True(MuiAreaDisappearPolicyStateRecordCodec.Write(ref platform,
			address, value));
		var fieldCursor = new MuiAreaDisappearPolicyStateFieldCursor
		{
			Record = address,
			Field = MuiAreaDisappearPolicyStateField.VertDisappear,
		};
		Assert.True(MuiAreaDisappearPolicyStateFieldCursorCodec.TryGetAddress(ref platform,
			fieldCursor, out var cursorVerticalAddress));
		Assert.Equal(address.Raw + 8, cursorVerticalAddress.Raw);
		Assert.True(MuiAreaDisappearPolicyStateFieldCursorCodec.TryGetAddress(ref platform,
			fieldCursor, out var typedCursorVerticalAddress, out var typedCursorFieldSize));
		Assert.Equal(cursorVerticalAddress, typedCursorVerticalAddress);
		Assert.Equal(MuiAreaDisappearPolicyStateRecord.FieldSize, typedCursorFieldSize);
		Assert.True(MuiAreaDisappearPolicyStateRecordMemoryCodec.TryGetAddress(ref platform,
			fieldCursor, out var memoryCursorVerticalAddress, out var memoryCursorFieldSize));
		Assert.Equal(typedCursorVerticalAddress, memoryCursorVerticalAddress);
		Assert.Equal(typedCursorFieldSize, memoryCursorFieldSize);
		Assert.True(MuiAreaDisappearPolicyStateRecordCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.HorizDisappear, decoded.HorizDisappear);
		Assert.Equal(value.VertDisappear, decoded.VertDisappear);
		Assert.True(MuiAreaDisappearPolicyStateRecordCodec.TryRead(ref platform,
			address, out decoded));
		Assert.False(MuiAreaDisappearPolicyStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
		fieldCursor.Field = (MuiAreaDisappearPolicyStateField)255;
		Assert.False(MuiAreaDisappearPolicyStateFieldCursorCodec.TryGetAddress(ref platform,
			fieldCursor, out _, out _));
		fieldCursor.Record = APTR.Null;
		fieldCursor.Field = MuiAreaDisappearPolicyStateField.VertDisappear;
		Assert.False(MuiAreaDisappearPolicyStateFieldCursorCodec.TryGetAddress(ref platform,
			fieldCursor, out _, out _));
	}

	[Fact]
	public void MalformedDisappearPolicyFailsClosedBeforeRawRepairOrSet()
	{
		var platform = CreatePlatform(out var rectangleClass);
		var rectangle = MuiCommonControlCore.CreateControl(ref platform, State,
			rectangleClass, APTR.Null);
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			rectangle, MuiCommonControlCore.HorizDisappear,
			unchecked((uint)-7), false));
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			rectangle, MuiCommonControlCore.VertDisappear, 13, false));
		Assert.True(MuiAreaDisappearCore.TryReadStateRecord(ref platform, State,
			rectangle, out _));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, rectangle,
			MuiAreaDisappearCore.StateKey);
		Assert.True(block.IsNotNull);
		Assert.True(MuiAreaDisappearPolicyStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiAreaDisappearPolicyStateField.Magic, 0));
		Assert.True(MuiAreaDisappearPolicyStateRecordCodec.TryReadStructural(
			ref platform, block, out var structural));
		Assert.Equal(0u, structural.Magic);
		Assert.Equal(-7, structural.HorizDisappear);
		Assert.False(MuiAreaDisappearPolicyStateAdmission.Validate(structural));
		Assert.False(MuiAreaDisappearPolicyStateRecordCodec.TryRead(ref platform,
			block, out _));
		var allocationsBefore = platform.AllocationCount;
		Assert.False(MuiAreaDisappearPacketCore.TryGet(ref platform, State,
			rectangle, out _));
		Assert.Equal(allocationsBefore, platform.AllocationCount);
		Assert.False(MuiAreaDisappearCore.TryReadStateRecord(ref platform, State,
			rectangle, out _));
		Assert.False(MuiCommonControlCore.TryGet(ref platform, State, rectangle,
			MuiCommonControlCore.HorizDisappear, out _, out _));
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			rectangle, MuiCommonControlCore.VertDisappear, 22, false));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			rectangle, MuiCommonControlCore.VertDisappear, out var raw));
		Assert.Equal(13u, raw);
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State,
			rectangle, MuiAreaDisappearCore.StateKey));
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR rectangleClass)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Rectangle.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		rectangleClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			name, APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}
}
