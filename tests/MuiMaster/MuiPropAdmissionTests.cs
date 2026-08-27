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
		Assert.False(MuiPropPolicyStateRecordMemoryCodec.TryGetAddress(ref platform,
			address, MuiPropPolicyStateRecord.Size, out _));
		Assert.False(MuiPropPolicyStateRecordMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, 0, out _));
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
		Assert.False(MuiPropRangeStateRecordMemoryCodec.TryGetAddress(ref platform,
			address, MuiPropRangeStateRecord.Size, out _));
		Assert.False(MuiPropRangeStateRecordMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, 0, out _));
		Assert.False(MuiPropRangeStateRecordCodec.TryReadStructural(ref platform,
			APTR.Null, out _));
	}

	private static MuiHeadlessTestPlatform CreatePlatform() =>
		new(0x1000, 0x20000, 0x4000, APTR.FromPointer(0x1000));
}
