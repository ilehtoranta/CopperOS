using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiGaugeLevelmeterAdmissionTests
{
	[Fact]
	public void GaugeAndLevelmeterRecordsRoundTripThroughNamedStructs()
	{
		var platform = CreatePlatform();
		var gaugeAddress = APTR.FromPointer(0x1500);
		var levelmeterAddress = APTR.FromPointer(0x1530);
		var gauge = new MuiGaugeStateRecord
		{
			Magic = MuiGaugeStateRecord.Cookie,
			Maximum = 100,
			Current = 35,
			Divide = 2,
			Horizontal = 1,
		};
		var levelmeter = new MuiLevelmeterPresentationStateRecord
		{
			Magic = MuiLevelmeterPresentationStateRecord.Cookie,
			Horizontal = 0,
		};
		Assert.True(MuiGaugeStateRecordCodec.Write(ref platform, gaugeAddress,
			gauge));
		Assert.True(MuiLevelmeterPresentationStateRecordCodec.WriteRecord(ref platform,
			levelmeterAddress, levelmeter));
		Assert.True(MuiGaugeStateRecordCodec.TryRead(ref platform, gaugeAddress,
			out var gaugeRead));
		Assert.True(MuiLevelmeterPresentationStateRecordCodec.TryRead(ref platform,
			levelmeterAddress, out var levelmeterRead));
		Assert.Equal(gauge.Maximum, gaugeRead.Maximum);
		Assert.Equal(gauge.Current, gaugeRead.Current);
		Assert.Equal(gauge.Divide, gaugeRead.Divide);
		Assert.Equal(gauge.Horizontal, gaugeRead.Horizontal);
		Assert.Equal(levelmeter.Horizontal, levelmeterRead.Horizontal);
	}

	[Fact]
	public void LevelmeterPresentationRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x1BE0);
		var value = new MuiLevelmeterPresentationStateRecord
		{
			Magic = MuiLevelmeterPresentationStateRecord.Cookie,
			Horizontal = 1,
		};

		Assert.True(MuiLevelmeterPresentationStateRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiLevelmeterPresentationStateRecordCodec.TryReadRecord(
			ref platform, address, out var structural));
		Assert.Equal(value.Magic, structural.Magic);
		Assert.Equal(value.Horizontal, structural.Horizontal);
		Assert.True(MuiLevelmeterPresentationStateRecordCodec.TryRead(
			ref platform, address, out _));
		Assert.True(MuiLevelmeterPresentationStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, 4, out var horizontalField));
		Assert.Equal(address.Raw + 4, horizontalField.Raw);
		Assert.True(MuiLevelmeterPresentationStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, 4, out var horizontal));
		Assert.Equal(value.Horizontal, horizontal);
		Assert.False(MuiLevelmeterPresentationStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiLevelmeterPresentationStateRecord.Size, out _));
		Assert.False(MuiLevelmeterPresentationStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, 0, out _));
		Assert.False(MuiLevelmeterPresentationStateRecordCodec.TryReadRecord(
			ref platform, APTR.Null, out _));
	}

	[Fact]
	public void GaugeRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x1BC0);
		var value = new MuiGaugeStateRecord
		{
			Magic = MuiGaugeStateRecord.Cookie,
			Maximum = uint.MaxValue,
			Current = 0x80000000u,
			Divide = uint.MaxValue,
			Horizontal = 1,
		};

		Assert.True(MuiGaugeStateRecordCodec.Write(ref platform, address, value));
		Assert.True(MuiGaugeStateRecordCodec.TryReadStructural(ref platform,
			address, out var structural));
		Assert.Equal(value.Magic, structural.Magic);
		Assert.Equal(value.Maximum, structural.Maximum);
		Assert.Equal(value.Current, structural.Current);
		Assert.Equal(value.Divide, structural.Divide);
		Assert.Equal(value.Horizontal, structural.Horizontal);
		Assert.True(MuiGaugeStateRecordCodec.TryRead(ref platform, address,
			out _));
		Assert.True(MuiGaugeStateRecordMemoryCodec.TryGetAddress(ref platform,
			address, MuiGaugeStateField.Horizontal, out var horizontalField));
		Assert.Equal(address.Raw + MuiGaugeStateRecord.HorizontalOffset,
			horizontalField.Raw);
		Assert.True(MuiGaugeStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiGaugeStateField.Maximum, out var maximum));
		Assert.Equal(value.Maximum, maximum);
		Assert.False(MuiGaugeStateRecordMemoryCodec.TryGetAddress(ref platform,
			address, (MuiGaugeStateField)255, out _));
		Assert.False(MuiGaugeStateRecordMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, MuiGaugeStateField.Magic, out _));
		Assert.False(MuiGaugeStateRecordCodec.TryReadStructural(ref platform,
			APTR.Null, out _));
	}

	[Fact]
	public void LevelmeterLabelRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x1C20);
		var label = APTR.FromPointer(0x1D20);
		platform.WriteCString(label, "Progress");
		var value = new MuiLevelmeterLabelStateRecord
		{
			Magic = MuiLevelmeterLabelStateRecord.Cookie,
			Label = label,
		};

		Assert.True(MuiLevelmeterLabelStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiLevelmeterLabelStateRecordCodec.TryReadStructural(
			ref platform, address, out var structural));
		Assert.Equal(value.Magic, structural.Magic);
		Assert.Equal(value.Label, structural.Label);
		Assert.True(MuiLevelmeterLabelStateRecordCodec.TryRead(ref platform,
			address, out _));
		Assert.True(MuiLevelmeterLabelStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, 4, out var labelField));
		Assert.Equal(address.Raw + 4, labelField.Raw);
		Assert.True(MuiLevelmeterLabelStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, 4, out var labelRaw));
		Assert.Equal(value.Label.Raw, labelRaw);
		Assert.False(MuiLevelmeterLabelStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiLevelmeterLabelStateRecord.Size, out _));
		Assert.False(MuiLevelmeterLabelStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, 0, out _));
		Assert.False(MuiLevelmeterLabelStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
	}

	[Fact]
	public void MalformedGaugeAndLevelmeterMagicRemainStructuralButFailClosed()
	{
		var platform = CreatePlatform();
		var gaugeAddress = APTR.FromPointer(0x1600);
		var levelmeterAddress = APTR.FromPointer(0x1630);
		Assert.True(MuiGaugeStateRecordCodec.Write(ref platform, gaugeAddress,
			new MuiGaugeStateRecord
			{
				Magic = MuiGaugeStateRecord.Cookie,
				Maximum = 1,
				Current = 1,
				Divide = 1,
				Horizontal = 0,
			}));
		Assert.True(MuiLevelmeterPresentationStateRecordCodec.WriteRecord(ref platform,
			levelmeterAddress, new MuiLevelmeterPresentationStateRecord
			{
				Magic = MuiLevelmeterPresentationStateRecord.Cookie,
				Horizontal = 1,
			}));
		Assert.True(MuiGaugeStateFieldCursorCodec.TryWriteUInt32(ref platform,
			gaugeAddress, MuiGaugeStateField.Magic, 0));
		Assert.True(MuiLevelmeterPresentationStateFieldCursorCodec.TryWriteUInt32(
			ref platform, levelmeterAddress,
			MuiLevelmeterPresentationStateField.Magic, 0));
		Assert.True(MuiGaugeStateRecordCodec.TryReadStructural(ref platform,
			gaugeAddress, out var gauge));
		Assert.True(MuiLevelmeterPresentationStateRecordCodec.TryReadRecord(
			ref platform, levelmeterAddress, out var levelmeter));
		Assert.Equal(0u, gauge.Magic);
		Assert.Equal(0u, levelmeter.Magic);
		Assert.False(MuiGaugeStateRecordCodec.TryRead(ref platform, gaugeAddress,
			out _));
		Assert.False(MuiLevelmeterPresentationStateRecordCodec.TryRead(ref platform,
			levelmeterAddress, out _));
		Assert.False(MuiGaugeStateAdmission.Validate(gauge));
		Assert.False(MuiLevelmeterPresentationStateAdmission.Validate(levelmeter));
	}

	private static MuiHeadlessTestPlatform CreatePlatform() =>
		new(0x1000, 0x20000, 0x4000, APTR.FromPointer(0x1000));
}
