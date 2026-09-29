using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListStateFieldCursorStructAdapterTests
{
	[Fact]
	public void AggregateCursorDispatchesToNamedRecord()
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
		var cursor = new MuiListCore.MuiListStateFieldCursor
		{
			Address = address,
			Record = MuiListCore.MuiListStateRecordKind.PresentationPolicy,
			Field = MuiListCore.MuiListStateField.MinLineHeight,
		};
		Assert.True(MuiListCore.MuiListStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var fieldAddress, out var fieldSize));
		Assert.Equal(0x352Cu, fieldAddress.Raw);
		Assert.Equal(4u, fieldSize);
		Assert.True(MuiListCore.MuiListStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiListCore.MuiListStateRecordKind.PresentationPolicy,
			MuiListCore.MuiListStateField.DragType, 18));
		Assert.True(MuiListCore.MuiListStateFieldCursorCodec.TryReadUInt32(
			ref platform, address, MuiListCore.MuiListStateRecordKind.PresentationPolicy,
			MuiListCore.MuiListStateField.Stripes, out var stripes));
		Assert.Equal(value.Stripes, stripes);
		Assert.True(MuiListCore.MuiListPresentationPolicyStateCodec.TryReadRecord(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Editable, decoded.Editable);
		Assert.Equal(18u, decoded.DragType);
		Assert.Equal(value.MinLineHeight, decoded.MinLineHeight);
	}

	[Fact]
	public void AggregateCursorRejectsUnknownAndIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListCore.MuiListStateFieldCursorCodec.TryGetAddress(
			ref platform, new MuiListCore.MuiListStateFieldCursor
			{
				Address = address,
				Record = MuiListCore.MuiListStateRecordKind.PresentationPolicy,
				Field = (MuiListCore.MuiListStateField)255,
			}, out _, out _));
		Assert.False(MuiListCore.MuiListStateFieldCursorCodec.TryGetAddress(
			ref platform, new MuiListCore.MuiListStateFieldCursor
			{
				Address = APTR.FromPointer(0x30FF9),
				Record = MuiListCore.MuiListStateRecordKind.PresentationPolicy,
				Field = MuiListCore.MuiListStateField.Magic,
			}, out _, out _));
		Assert.False(MuiListCore.MuiListStateMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, MuiListCore.MuiListStateRecordKind.PresentationPolicy,
			MuiListCore.MuiListStateField.Magic, out _, out _));
	}
}
