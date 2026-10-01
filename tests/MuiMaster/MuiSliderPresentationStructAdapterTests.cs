using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiSliderPresentationStructAdapterTests
{
	[Fact]
	public void SliderPresentationStructAdapterUsesNamedFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiSliderPresentationStateRecord
		{
			Magic = MuiSliderPresentationStateRecord.Cookie,
			Horizontal = 1,
			Quiet = 1,
		};

		Assert.True(MuiSliderPresentationStateRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiSliderPresentationStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiSliderPresentationStateField.Quiet,
			out var quietAddress));
		Assert.Equal(0x3508u, quietAddress.Raw);
		var quietCursor = new MuiSliderPresentationStateFieldCursor
		{
			Record = address,
			Field = MuiSliderPresentationStateField.Quiet,
		};
		Assert.True(MuiSliderPresentationStateFieldCursorCodec.TryGetAddress(
			ref platform, quietCursor, out var cursorQuiet, out var cursorFieldSize));
		Assert.Equal(quietAddress, cursorQuiet);
		Assert.Equal(MuiSliderPresentationStateRecord.FieldSize, cursorFieldSize);
		Assert.True(MuiSliderPresentationStateRecordMemoryCodec.TryGetAddress(
			ref platform, quietCursor, out var memoryQuiet, out var memoryFieldSize));
		Assert.Equal(cursorQuiet, memoryQuiet);
		Assert.Equal(cursorFieldSize, memoryFieldSize);
		Assert.True(MuiSliderPresentationStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiSliderPresentationStateField.Horizontal, 0));
		Assert.True(MuiSliderPresentationStateRecordCodec.TryReadRecord(
			ref platform, address, out var decoded));
		Assert.Equal(0u, decoded.Horizontal);
		Assert.Equal(1u, decoded.Quiet);
		Assert.False(MuiSliderPresentationStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.FromPointer(0x30FF5),
			MuiSliderPresentationStateField.Magic, out _));
		Assert.False(MuiSliderPresentationStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiSliderPresentationStateField.Magic, out _));
		quietCursor.Field = (MuiSliderPresentationStateField)255;
		Assert.False(MuiSliderPresentationStateFieldCursorCodec.TryGetAddress(
			ref platform, quietCursor, out _, out _));
		quietCursor.Record = APTR.Null;
		quietCursor.Field = MuiSliderPresentationStateField.Quiet;
		Assert.False(MuiSliderPresentationStateFieldCursorCodec.TryGetAddress(
			ref platform, quietCursor, out _, out _));
	}
}
