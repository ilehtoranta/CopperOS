using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiPropAdmissionTests
{
	[Fact]
	public void PropPolicyAndRangeRecordsRoundTripThroughNamedStructs()
	{
		var platform = CreatePlatform();
		var policyAddress = APTR.FromPointer(0x1500);
		var rangeAddress = APTR.FromPointer(0x1530);
		var policy = new MuiPropPolicyStateRecord
		{
			Magic = MuiPropPolicyStateRecord.Cookie,
			Horizontal = 1,
			DeltaFactor = unchecked((uint)-2),
			Slider = 1,
			UseWinBorder = 2,
		};
		var range = new MuiPropRangeStateRecord
		{
			Magic = MuiPropRangeStateRecord.Cookie,
			Entries = 100,
			Visible = 10,
			First = 5,
		};
		Assert.True(MuiPropPolicyStateRecordCodec.Write(ref platform,
			policyAddress, policy));
		Assert.True(MuiPropRangeStateRecordCodec.Write(ref platform, rangeAddress,
			range));
		Assert.True(MuiPropPolicyStateRecordCodec.TryRead(ref platform,
			policyAddress, out var policyRead));
		Assert.True(MuiPropRangeStateRecordCodec.TryRead(ref platform,
			rangeAddress, out var rangeRead));
		Assert.Equal(policy.Horizontal, policyRead.Horizontal);
		Assert.Equal(policy.DeltaFactor, policyRead.DeltaFactor);
		Assert.Equal(policy.Slider, policyRead.Slider);
		Assert.Equal(policy.UseWinBorder, policyRead.UseWinBorder);
		Assert.Equal(range.Entries, rangeRead.Entries);
		Assert.Equal(range.Visible, rangeRead.Visible);
		Assert.Equal(range.First, rangeRead.First);
	}

	[Fact]
	public void PropPolicySequentialRecordPreservesSignedFactorAndPolicyBits()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x1500);
		var expected = new MuiPropPolicyStateRecord
		{
			Magic = MuiPropPolicyStateRecord.Cookie,
			Horizontal = 1,
			DeltaFactor = unchecked((uint)-2),
			Slider = 1,
			UseWinBorder = 2,
		};
		Assert.True(MuiPropPolicyStateRecordCodec.WriteRecord(ref platform,
			address, expected));
		Assert.True(MuiPropPolicyStateRecordCodec.TryReadRecord(ref platform,
			address, out var actual));
		Assert.Equal(expected.Magic, actual.Magic);
		Assert.Equal(expected.Horizontal, actual.Horizontal);
		Assert.Equal(expected.DeltaFactor, actual.DeltaFactor);
		Assert.Equal(expected.Slider, actual.Slider);
		Assert.Equal(expected.UseWinBorder, actual.UseWinBorder);
		Assert.False(MuiPropPolicyStateRecordCodec.TryReadRecord(ref platform,
			APTR.FromPointer(0x20FFC), out _));
	}

	[Fact]
	public void PropRangeSequentialRecordPreservesLongRangeAndBounds()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x1530);
		var expected = new MuiPropRangeStateRecord
		{
			Magic = MuiPropRangeStateRecord.Cookie,
			Entries = 100,
			Visible = 10,
			First = 5,
		};
		Assert.True(MuiPropRangeStateRecordCodec.WriteRecord(ref platform,
			address, expected));
		Assert.True(MuiPropRangeStateRecordCodec.TryReadRecord(ref platform,
			address, out var actual));
		Assert.Equal(expected.Magic, actual.Magic);
		Assert.Equal(expected.Entries, actual.Entries);
		Assert.Equal(expected.Visible, actual.Visible);
		Assert.Equal(expected.First, actual.First);
		Assert.False(MuiPropRangeStateRecordCodec.TryReadRecord(ref platform,
			APTR.FromPointer(0x20FFC), out _));
	}

	[Fact]
	public void MalformedPropMagicRemainsStructuralButFailsClosed()
	{
		var platform = CreatePlatform();
		var policyAddress = APTR.FromPointer(0x1600);
		var rangeAddress = APTR.FromPointer(0x1630);
		Assert.True(MuiPropPolicyStateRecordCodec.Write(ref platform,
			policyAddress, new MuiPropPolicyStateRecord
			{
				Magic = MuiPropPolicyStateRecord.Cookie,
				Horizontal = 0,
				DeltaFactor = 1,
				Slider = 0,
				UseWinBorder = 0,
			}));
		Assert.True(MuiPropRangeStateRecordCodec.Write(ref platform, rangeAddress,
			new MuiPropRangeStateRecord
			{
				Magic = MuiPropRangeStateRecord.Cookie,
				Entries = 10,
				Visible = 2,
				First = 1,
			}));
		Assert.True(MuiPropPolicyStateFieldCursorCodec.TryWriteUInt32(
			ref platform, policyAddress, MuiPropPolicyStateField.Magic, 0));
		Assert.True(MuiPropRangeStateFieldCursorCodec.TryWriteUInt32(ref platform,
			rangeAddress, MuiPropRangeStateField.Magic, 0));
		Assert.True(MuiPropPolicyStateRecordCodec.TryReadStructural(ref platform,
			policyAddress, out var policy));
		Assert.True(MuiPropRangeStateRecordCodec.TryReadStructural(ref platform,
			rangeAddress, out var range));
		Assert.Equal(0u, policy.Magic);
		Assert.Equal(0u, range.Magic);
		Assert.False(MuiPropPolicyStateRecordCodec.TryRead(ref platform,
			policyAddress, out _));
		Assert.False(MuiPropRangeStateRecordCodec.TryRead(ref platform,
			rangeAddress, out _));
		Assert.False(MuiPropPolicyStateAdmission.Validate(policy));
		Assert.False(MuiPropRangeStateAdmission.Validate(range));
	}

	[Fact]
	public void PropPolicyRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x1A60);
		var record = new MuiPropPolicyStateRecord
		{
			Magic = MuiPropPolicyStateRecord.Cookie,
			Horizontal = 1,
			DeltaFactor = unchecked((uint)-2),
			Slider = 1,
			UseWinBorder = 2,
		};
		Assert.True(MuiPropPolicyStateRecordCodec.Write(ref platform, address,
			record));
		Assert.True(MuiPropPolicyStateRecordMemoryCodec.TryGetAddress(ref platform,
			address, 16, out var borderAddress));
		Assert.Equal(0x1A70u, borderAddress.Raw);
		Assert.True(MuiPropPolicyStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 8, out var deltaFactor));
		Assert.Equal(unchecked((uint)-2), deltaFactor);
		Assert.True(MuiPropPolicyStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, 12, 0));
		Assert.True(MuiPropPolicyStateRecordCodec.TryReadStructural(ref platform,
			address, out var updated));
		Assert.Equal(0u, updated.Slider);
		Assert.True(MuiPropPolicyStateRecordMemoryCodec.TryGetAddress(ref platform,
			address, MuiPropPolicyStateField.DeltaFactor, out var typedDelta));
		Assert.Equal(address.Raw + MuiPropPolicyStateRecord.DeltaFactorOffset,
			typedDelta.Raw);
		var deltaCursor = new MuiPropPolicyStateFieldCursor
		{
			Record = address,
			Field = MuiPropPolicyStateField.DeltaFactor,
		};
		Assert.True(MuiPropPolicyStateFieldCursorCodec.TryGetAddress(ref platform,
			deltaCursor, out var cursorDelta, out var cursorFieldSize));
		Assert.Equal(typedDelta, cursorDelta);
		Assert.Equal(MuiPropPolicyStateRecord.FieldSize, cursorFieldSize);
		Assert.True(MuiPropPolicyStateRecordMemoryCodec.TryGetAddress(ref platform,
			deltaCursor, out var memoryDelta, out var memoryFieldSize));
		Assert.Equal(cursorDelta, memoryDelta);
		Assert.Equal(cursorFieldSize, memoryFieldSize);
		Assert.True(MuiPropPolicyStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiPropPolicyStateField.UseWinBorder, 1));
		Assert.True(MuiPropPolicyStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiPropPolicyStateField.Magic, out var typedMagic));
		Assert.Equal(record.Magic, typedMagic);
		Assert.True(MuiPropPolicyStateRecordCodec.TryReadStructural(ref platform,
			address, out var typedUpdated));
		Assert.Equal(1u, typedUpdated.UseWinBorder);
		Assert.Equal(updated.Horizontal, typedUpdated.Horizontal);
		Assert.False(MuiPropPolicyStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, (MuiPropPolicyStateField)0xFF, out _));
		Assert.False(MuiPropPolicyStateRecordMemoryCodec.TryGetAddress(ref platform,
			address, MuiPropPolicyStateRecord.Size, out _));
		Assert.False(MuiPropPolicyStateRecordMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, (uint)0, out _));
		deltaCursor.Field = (MuiPropPolicyStateField)255;
		Assert.False(MuiPropPolicyStateFieldCursorCodec.TryGetAddress(ref platform,
			deltaCursor, out _, out _));
		deltaCursor.Record = APTR.Null;
		deltaCursor.Field = MuiPropPolicyStateField.DeltaFactor;
		Assert.False(MuiPropPolicyStateFieldCursorCodec.TryGetAddress(ref platform,
			deltaCursor, out _, out _));
		Assert.False(MuiPropPolicyStateRecordCodec.TryReadStructural(ref platform,
			APTR.Null, out _));
	}

	[Fact]
	public void PropRangeRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x1A80);
		var record = new MuiPropRangeStateRecord
		{
			Magic = MuiPropRangeStateRecord.Cookie,
			Entries = 100,
			Visible = 10,
			First = 5,
		};
		Assert.True(MuiPropRangeStateRecordCodec.Write(ref platform, address, record));
		Assert.True(MuiPropRangeStateRecordMemoryCodec.TryGetAddress(ref platform,
			address, 12, out var firstAddress));
		Assert.Equal(0x1A8Cu, firstAddress.Raw);
		Assert.True(MuiPropRangeStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 4, out var entries));
		Assert.Equal(100u, entries);
		Assert.True(MuiPropRangeStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, 12, 7));
		Assert.True(MuiPropRangeStateRecordCodec.TryReadStructural(ref platform,
			address, out var updated));
		Assert.Equal(7u, updated.First);
		Assert.True(MuiPropRangeStateRecordMemoryCodec.TryGetAddress(ref platform,
			address, MuiPropRangeStateField.Visible, out var typedVisible));
		Assert.Equal(address.Raw + MuiPropRangeStateRecord.VisibleOffset,
			typedVisible.Raw);
		var visibleCursor = new MuiPropRangeStateFieldCursor
		{
			Record = address,
			Field = MuiPropRangeStateField.Visible,
		};
		Assert.True(MuiPropRangeStateFieldCursorCodec.TryGetAddress(ref platform,
			visibleCursor, out var cursorVisible, out var cursorFieldSize));
		Assert.Equal(typedVisible, cursorVisible);
		Assert.Equal(MuiPropRangeStateRecord.FieldSize, cursorFieldSize);
		Assert.True(MuiPropRangeStateRecordMemoryCodec.TryGetAddress(ref platform,
			visibleCursor, out var memoryVisible, out var memoryFieldSize));
		Assert.Equal(cursorVisible, memoryVisible);
		Assert.Equal(cursorFieldSize, memoryFieldSize);
		Assert.True(MuiPropRangeStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiPropRangeStateField.Entries, 120));
		Assert.True(MuiPropRangeStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiPropRangeStateField.Magic, out var typedMagic));
		Assert.Equal(record.Magic, typedMagic);
		Assert.True(MuiPropRangeStateRecordCodec.TryReadStructural(ref platform,
			address, out var typedUpdated));
		Assert.Equal(120u, typedUpdated.Entries);
		Assert.Equal(updated.First, typedUpdated.First);
		Assert.False(MuiPropRangeStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, (MuiPropRangeStateField)0xFF, out _));
		Assert.False(MuiPropRangeStateRecordMemoryCodec.TryGetAddress(ref platform,
			address, MuiPropRangeStateRecord.Size, out _));
		Assert.False(MuiPropRangeStateRecordMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, (uint)0, out _));
		visibleCursor.Field = (MuiPropRangeStateField)255;
		Assert.False(MuiPropRangeStateFieldCursorCodec.TryGetAddress(ref platform,
			visibleCursor, out _, out _));
		visibleCursor.Record = APTR.Null;
		visibleCursor.Field = MuiPropRangeStateField.Visible;
		Assert.False(MuiPropRangeStateFieldCursorCodec.TryGetAddress(ref platform,
			visibleCursor, out _, out _));
		Assert.False(MuiPropRangeStateRecordCodec.TryReadStructural(ref platform,
			APTR.Null, out _));
	}

	private static MuiHeadlessTestPlatform CreatePlatform() =>
		new(0x1000, 0x20000, 0x4000, APTR.FromPointer(0x1000));
}
