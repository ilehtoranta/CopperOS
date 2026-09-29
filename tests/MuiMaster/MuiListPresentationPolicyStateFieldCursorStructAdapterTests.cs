using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListPresentationPolicyStateFieldCursorStructAdapterTests
{
	[Fact]
	public void CursorWalksNamedPresentationPolicyRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListCore.MuiListPresentationPolicyState
		{
			Magic = MuiListCore.MuiListPresentationPolicyState.Cookie,
			Editable = 1,
			Quiet = 2,
			AdjustHeight = 3,
			AdjustWidth = 4,
			Stripes = 5,
			ShowDropMarks = 6,
			DragSortable = 7,
			DragType = 8,
			AutoVisible = 9,
			AutoLineHeight = 10,
			MinLineHeight = 11,
		};
		Assert.True(MuiListCore.MuiListPresentationPolicyStateCodec.WriteRecord(
			ref platform, address, value));
		var cursor = new MuiListCore.MuiListPresentationPolicyStateFieldCursor
		{
			Address = address,
			Field = MuiListCore.MuiListPresentationPolicyStateField.MinLineHeight,
		};
		Assert.True(MuiListCore.MuiListPresentationPolicyStateFieldCursorCodec
			.TryGetAddress(ref platform, cursor, out var fieldAddress, out var fieldSize));
		Assert.Equal(0x352Cu, fieldAddress.Raw);
		Assert.Equal(4u, fieldSize);
		Assert.True(MuiListCore.MuiListPresentationPolicyStateFieldCursorCodec
			.TryWriteUInt32(ref platform, address,
				MuiListCore.MuiListPresentationPolicyStateField.DragType, 18));
		Assert.True(MuiListCore.MuiListPresentationPolicyStateFieldCursorCodec
			.TryReadUInt32(ref platform, address,
				MuiListCore.MuiListPresentationPolicyStateField.Stripes, out var stripes));
		Assert.Equal(value.Stripes, stripes);
		Assert.True(MuiListCore.MuiListPresentationPolicyStateCodec.TryReadRecord(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Editable, decoded.Editable);
		Assert.Equal(value.Quiet, decoded.Quiet);
		Assert.Equal(value.AdjustHeight, decoded.AdjustHeight);
		Assert.Equal(value.AdjustWidth, decoded.AdjustWidth);
		Assert.Equal(value.Stripes, decoded.Stripes);
		Assert.Equal(value.ShowDropMarks, decoded.ShowDropMarks);
		Assert.Equal(value.DragSortable, decoded.DragSortable);
		Assert.Equal(18u, decoded.DragType);
		Assert.Equal(value.AutoVisible, decoded.AutoVisible);
		Assert.Equal(value.AutoLineHeight, decoded.AutoLineHeight);
		Assert.Equal(value.MinLineHeight, decoded.MinLineHeight);
	}

	[Fact]
	public void CursorRejectsUnknownAndIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListCore.MuiListPresentationPolicyStateFieldCursorCodec
			.TryGetAddress(ref platform,
				new MuiListCore.MuiListPresentationPolicyStateFieldCursor
				{
					Address = address,
					Field = (MuiListCore.MuiListPresentationPolicyStateField)255,
				}, out _, out _));
		Assert.False(MuiListCore.MuiListPresentationPolicyStateFieldCursorCodec
			.TryGetAddress(ref platform,
				new MuiListCore.MuiListPresentationPolicyStateFieldCursor
				{
					Address = APTR.FromPointer(0x30FF9),
					Field = MuiListCore.MuiListPresentationPolicyStateField.Magic,
				}, out _, out _));
		Assert.False(MuiListCore.MuiListPresentationPolicyStateMemoryCodec.TryGetAddress(
			ref platform, APTR.Null,
			MuiListCore.MuiListPresentationPolicyStateField.Magic, out _, out _));
	}
}
