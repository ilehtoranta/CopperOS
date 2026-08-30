using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiSliderScaleAdmissionTests
{
	[Fact]
	public void SliderAndScaleRecordsRoundTripThroughNamedStructs()
	{
		var platform = CreatePlatform();
		var sliderAddress = APTR.FromPointer(0x1500);
		var scaleAddress = APTR.FromPointer(0x1530);
		var slider = new MuiSliderPresentationStateRecord
		{
			Magic = MuiSliderPresentationStateRecord.Cookie,
			Horizontal = 1,
			Quiet = 0,
		};
		var scale = new MuiScalePresentationStateRecord
		{
			Magic = MuiScalePresentationStateRecord.Cookie,
			Horizontal = 0,
		};
		Assert.True(MuiSliderPresentationStateRecordCodec.WriteRecord(ref platform,
			sliderAddress, slider));
		Assert.True(MuiScalePresentationStateRecordCodec.WriteRecord(ref platform,
			scaleAddress, scale));
		Assert.True(MuiSliderPresentationStateRecordCodec.TryRead(ref platform,
			sliderAddress, out var sliderRead));
		Assert.True(MuiScalePresentationStateRecordCodec.TryRead(ref platform,
			scaleAddress, out var scaleRead));
		Assert.Equal(slider.Horizontal, sliderRead.Horizontal);
		Assert.Equal(slider.Quiet, sliderRead.Quiet);
		Assert.Equal(scale.Horizontal, scaleRead.Horizontal);
	}

	[Fact]
	public void MalformedSliderAndScaleMagicRemainStructuralButFailClosed()
	{
		var platform = CreatePlatform();
		var sliderAddress = APTR.FromPointer(0x1600);
		var scaleAddress = APTR.FromPointer(0x1630);
		Assert.True(MuiSliderPresentationStateRecordCodec.WriteRecord(ref platform,
			sliderAddress, new MuiSliderPresentationStateRecord
			{
				Magic = MuiSliderPresentationStateRecord.Cookie,
				Horizontal = 0,
				Quiet = 1,
			}));
		Assert.True(MuiScalePresentationStateRecordCodec.WriteRecord(ref platform,
			scaleAddress, new MuiScalePresentationStateRecord
			{
				Magic = MuiScalePresentationStateRecord.Cookie,
				Horizontal = 1,
			}));
		Assert.True(MuiSliderPresentationStateFieldCursorCodec.TryWriteUInt32(
			ref platform, sliderAddress, MuiSliderPresentationStateField.Magic, 0));
		Assert.True(MuiScalePresentationStateFieldCursorCodec.TryWriteUInt32(
			ref platform, scaleAddress, MuiScalePresentationStateField.Magic, 0));
		Assert.True(MuiSliderPresentationStateRecordCodec.TryReadRecord(
			ref platform, sliderAddress, out var slider));
		Assert.True(MuiScalePresentationStateRecordCodec.TryReadRecord(
			ref platform, scaleAddress, out var scale));
		Assert.Equal(0u, slider.Magic);
		Assert.Equal(0u, scale.Magic);
		Assert.False(MuiSliderPresentationStateRecordCodec.TryRead(ref platform,
			sliderAddress, out _));
		Assert.False(MuiScalePresentationStateRecordCodec.TryRead(ref platform,
			scaleAddress, out _));
		Assert.False(MuiSliderPresentationStateAdmission.Validate(slider));
		Assert.False(MuiScalePresentationStateAdmission.Validate(scale));
	}

	[Fact]
	public void ScaleRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x1700);
		var record = new MuiScalePresentationStateRecord
		{
			Magic = MuiScalePresentationStateRecord.Cookie,
			Horizontal = 1,
		};
		Assert.True(MuiScalePresentationStateRecordCodec.WriteRecord(ref platform,
			address, record));
		Assert.True(MuiScalePresentationStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiScalePresentationStateField.Horizontal,
			out var horizontalAddress));
		Assert.Equal(0x1704u, horizontalAddress.Raw);
		Assert.True(MuiScalePresentationStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiScalePresentationStateField.Horizontal,
			out var horizontal));
		Assert.Equal(1u, horizontal);
		Assert.True(MuiScalePresentationStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiScalePresentationStateField.Horizontal, 0));
		Assert.True(MuiScalePresentationStateRecordCodec.TryReadRecord(
			ref platform, address, out var updated));
		Assert.Equal(0u, updated.Horizontal);
		Assert.False(MuiScalePresentationStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, (MuiScalePresentationStateField)255, out _));
		Assert.False(MuiScalePresentationStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiScalePresentationStateField.Magic, out _));
		Assert.False(MuiScalePresentationStateRecordCodec.TryReadRecord(
			ref platform, APTR.Null, out _));
	}

	[Fact]
	public void SliderPresentationRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x1A60);
		var record = new MuiSliderPresentationStateRecord
		{
			Magic = MuiSliderPresentationStateRecord.Cookie,
			Horizontal = 1,
			Quiet = 1,
		};
		Assert.True(MuiSliderPresentationStateRecordCodec.WriteRecord(ref platform, address,
			record));
		Assert.True(MuiSliderPresentationStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiSliderPresentationStateField.Quiet,
			out var quietAddress));
		Assert.Equal(0x1A68u, quietAddress.Raw);
		Assert.True(MuiSliderPresentationStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiSliderPresentationStateField.Horizontal,
			out var horizontal));
		Assert.Equal(1u, horizontal);
		Assert.True(MuiSliderPresentationStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiSliderPresentationStateField.Quiet, 0));
		Assert.True(MuiSliderPresentationStateRecordCodec.TryReadRecord(ref platform,
			address, out var updated));
		Assert.Equal(0u, updated.Quiet);
		Assert.False(MuiSliderPresentationStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, (MuiSliderPresentationStateField)255, out _));
		Assert.False(MuiSliderPresentationStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiSliderPresentationStateField.Magic, out _));
		Assert.False(MuiSliderPresentationStateRecordCodec.TryReadRecord(ref platform,
			APTR.Null, out _));
	}

	private static MuiHeadlessTestPlatform CreatePlatform() =>
		new(0x1000, 0x20000, 0x4000, APTR.FromPointer(0x1000));
}
