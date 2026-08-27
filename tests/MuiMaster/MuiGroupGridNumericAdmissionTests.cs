using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiGroupGridNumericAdmissionTests
{
	[Fact]
	public void GroupGridAndNumericRecordsRoundTripThroughNamedStructs()
	{
		var platform = CreatePlatform();
		var gridAddress = APTR.FromPointer(0x1500);
		var numericAddress = APTR.FromPointer(0x1550);
		var grid = new MuiGroupGridStateRecord
		{
			Magic = MuiGroupGridStateRecord.Cookie,
			Columns = 3,
			Rows = 2,
			HorizontalSpacing = unchecked((uint)-4),
			VerticalSpacing = 6,
			SameWidth = 1,
			SameHeight = 0,
			HorizontalCenter = 1,
			VerticalCenter = 2,
		};
		var numeric = new MuiNumericStateRecord
		{
			Magic = MuiNumericStateRecord.Cookie,
			Minimum = unchecked((uint)-10),
			Maximum = 100,
			Value = 35,
			Default = 50,
			Reverse = 1,
		};
		Assert.True(MuiGroupGridStateRecordCodec.Write(ref platform, gridAddress,
			grid));
		Assert.True(MuiNumericStateRecordCodec.Write(ref platform, numericAddress,
			numeric));
		Assert.True(MuiGroupGridStateRecordCodec.TryRead(ref platform, gridAddress,
			out var gridRead));
		Assert.True(MuiNumericStateRecordCodec.TryRead(ref platform,
			numericAddress, out var numericRead));
		Assert.Equal(grid.Columns, gridRead.Columns);
		Assert.Equal(grid.Rows, gridRead.Rows);
		Assert.Equal(grid.HorizontalSpacing, gridRead.HorizontalSpacing);
		Assert.Equal(grid.VerticalSpacing, gridRead.VerticalSpacing);
		Assert.Equal(grid.SameWidth, gridRead.SameWidth);
		Assert.Equal(grid.SameHeight, gridRead.SameHeight);
		Assert.Equal(grid.HorizontalCenter, gridRead.HorizontalCenter);
		Assert.Equal(grid.VerticalCenter, gridRead.VerticalCenter);
		Assert.Equal(numeric.Minimum, numericRead.Minimum);
		Assert.Equal(numeric.Maximum, numericRead.Maximum);
		Assert.Equal(numeric.Value, numericRead.Value);
		Assert.Equal(numeric.Default, numericRead.Default);
		Assert.Equal(numeric.Reverse, numericRead.Reverse);
	}

	[Fact]
	public void MalformedGroupGridAndNumericMagicRemainStructuralButFailClosed()
	{
		var platform = CreatePlatform();
		var gridAddress = APTR.FromPointer(0x1600);
		var numericAddress = APTR.FromPointer(0x1650);
		Assert.True(MuiGroupGridStateRecordCodec.Write(ref platform, gridAddress,
			new MuiGroupGridStateRecord
			{
				Magic = MuiGroupGridStateRecord.Cookie,
				Columns = 1,
				Rows = 1,
				HorizontalSpacing = 0,
				VerticalSpacing = 0,
				SameWidth = 0,
				SameHeight = 0,
				HorizontalCenter = 0,
				VerticalCenter = 0,
			}));
		Assert.True(MuiNumericStateRecordCodec.Write(ref platform, numericAddress,
			new MuiNumericStateRecord
			{
				Magic = MuiNumericStateRecord.Cookie,
				Minimum = 0,
				Maximum = 10,
				Value = 5,
				Default = 5,
				Reverse = 0,
			}));
		Assert.True(MuiGroupGridStateFieldCursorCodec.TryWriteUInt32(ref platform,
			gridAddress, MuiGroupGridStateField.Magic, 0));
		Assert.True(MuiNumericStateFieldCursorCodec.TryWriteUInt32(ref platform,
			numericAddress, MuiNumericStateField.Magic, 0));
		Assert.True(MuiGroupGridStateRecordCodec.TryReadStructural(ref platform,
			gridAddress, out var grid));
		Assert.True(MuiNumericStateRecordCodec.TryReadStructural(ref platform,
			numericAddress, out var numeric));
		Assert.Equal(0u, grid.Magic);
		Assert.Equal(0u, numeric.Magic);
		Assert.False(MuiGroupGridStateRecordCodec.TryRead(ref platform, gridAddress,
			out _));
		Assert.False(MuiNumericStateRecordCodec.TryRead(ref platform,
			numericAddress, out _));
		Assert.False(MuiGroupGridStateAdmission.Validate(grid));
		Assert.False(MuiNumericStateAdmission.Validate(numeric));
	}

	[Fact]
	public void NumericRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x1700);
		var record = new MuiNumericStateRecord
		{
			Magic = MuiNumericStateRecord.Cookie,
			Minimum = unchecked((uint)-10),
			Maximum = 100,
			Value = 35,
			Default = 50,
			Reverse = 1,
		};
		Assert.True(MuiNumericStateRecordCodec.Write(ref platform, address, record));
		Assert.True(MuiNumericStateRecordMemoryCodec.TryGetAddress(ref platform,
			address, MuiNumericStateField.Value, out var valueAddress));
		Assert.Equal(0x170Cu, valueAddress.Raw);
		Assert.True(MuiNumericStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiNumericStateField.Minimum, out var minimum));
		Assert.Equal(unchecked((uint)-10), minimum);
		Assert.True(MuiNumericStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiNumericStateField.Value, 42));
		Assert.True(MuiNumericStateRecordCodec.TryReadStructural(ref platform,
			address, out var updated));
		Assert.Equal(42u, updated.Value);
		Assert.False(MuiNumericStateRecordMemoryCodec.TryGetAddress(ref platform,
			address, (MuiNumericStateField)255, out _));
		Assert.False(MuiNumericStateRecordMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, MuiNumericStateField.Magic, out _));
		Assert.False(MuiNumericStateRecordCodec.TryReadStructural(ref platform,
			APTR.Null, out _));
	}

	[Fact]
	public void GroupGridRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x1800);
		var record = new MuiGroupGridStateRecord
		{
			Magic = MuiGroupGridStateRecord.Cookie,
			Columns = 3,
			Rows = 2,
			HorizontalSpacing = unchecked((uint)-4),
			VerticalSpacing = 6,
			SameWidth = 1,
			SameHeight = 0,
			HorizontalCenter = 1,
			VerticalCenter = 2,
		};
		Assert.True(MuiGroupGridStateRecordCodec.Write(ref platform, address, record));
		Assert.True(MuiGroupGridStateRecordMemoryCodec.TryGetAddress(ref platform,
			address, MuiGroupGridStateField.VerticalCenter,
			out var verticalCenterAddress));
		Assert.Equal(0x1820u, verticalCenterAddress.Raw);
		Assert.True(MuiGroupGridStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiGroupGridStateField.HorizontalSpacing,
			out var horizontalSpacing));
		Assert.Equal(unchecked((uint)-4), horizontalSpacing);
		Assert.True(MuiGroupGridStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiGroupGridStateField.VerticalCenter, 0));
		Assert.True(MuiGroupGridStateRecordCodec.TryReadStructural(ref platform,
			address, out var decoded));
		Assert.Equal(0u, decoded.VerticalCenter);
		Assert.False(MuiGroupGridStateRecordMemoryCodec.TryGetAddress(ref platform,
			address, (MuiGroupGridStateField)255, out _));
		Assert.False(MuiGroupGridStateRecordMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, MuiGroupGridStateField.Magic, out _));
		Assert.False(MuiGroupGridStateRecordCodec.TryReadStructural(ref platform,
			APTR.Null, out _));
	}

	private static MuiHeadlessTestPlatform CreatePlatform() =>
		new(0x1000, 0x20000, 0x4000, APTR.FromPointer(0x1000));
}
