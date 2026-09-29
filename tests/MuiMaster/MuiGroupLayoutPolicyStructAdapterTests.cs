using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiGroupLayoutPolicyStructAdapterTests
{
	[Fact]
	public void GroupLayoutPolicySequentialRecordPreservesSignedSpacingAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3D00);
		var value = new MuiGroupLayoutPolicyStateRecord
		{
			Magic = MuiGroupLayoutPolicyStateRecord.Cookie,
			Horizontal = 1,
			HorizontalSpacing = unchecked((uint)-100),
			VerticalSpacing = unchecked((uint)-1),
			SameWidth = 1,
			SameHeight = 0,
			PageMode = 1,
		};

		Assert.True(MuiGroupLayoutPolicyStateRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiGroupLayoutPolicyStateRecordCodec.TryReadRecord(ref platform,
			address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Horizontal, decoded.Horizontal);
		Assert.Equal(value.HorizontalSpacing, decoded.HorizontalSpacing);
		Assert.Equal(value.VerticalSpacing, decoded.VerticalSpacing);
		Assert.Equal(value.SameWidth, decoded.SameWidth);
		Assert.Equal(value.SameHeight, decoded.SameHeight);
		Assert.Equal(value.PageMode, decoded.PageMode);
		var policyCursor = new MuiGroupLayoutPolicyFieldCursor
		{
			Address = address,
			Field = MuiGroupLayoutPolicyField.PageMode,
		};
		Assert.True(MuiGroupLayoutPolicyFieldCursorCodec.TryGetAddress(
			ref platform, policyCursor, out var cursorPageModeAddress,
			out var cursorFieldSize));
		Assert.Equal(APTR.FromPointer(0x3D18), cursorPageModeAddress);
		Assert.Equal(MuiGroupLayoutPolicyStateRecord.FieldSize, cursorFieldSize);
		Assert.True(MuiGroupLayoutPolicyStateRecordMemoryCodec.TryGetAddress(
			ref platform, policyCursor, out var memoryPageModeAddress,
			out var memoryFieldSize));
		Assert.Equal(cursorPageModeAddress, memoryPageModeAddress);
		Assert.Equal(cursorFieldSize, memoryFieldSize);
		policyCursor.Field = (MuiGroupLayoutPolicyField)255;
		Assert.False(MuiGroupLayoutPolicyFieldCursorCodec.TryGetAddress(
			ref platform, policyCursor, out _, out _));
		policyCursor.Address = APTR.Null;
		policyCursor.Field = MuiGroupLayoutPolicyField.PageMode;
		Assert.False(MuiGroupLayoutPolicyFieldCursorCodec.TryGetAddress(
			ref platform, policyCursor, out _, out _));

		var crossingEnd = APTR.FromPointer(0x30FF1);
		Assert.False(MuiGroupLayoutPolicyStateRecordCodec.WriteRecord(ref platform,
			crossingEnd, value));
		Assert.False(MuiGroupLayoutPolicyStateRecordCodec.TryReadRecord(ref platform,
			crossingEnd, out _));
	}
}
