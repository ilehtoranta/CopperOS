using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListviewInteractionPolicyFieldCursorStructAdapterTests
{
	[Fact]
	public void CursorWalksNamedPolicyRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListviewCore.MuiListviewInteractionPolicyState
		{
			Magic = MuiListviewCore.MuiListviewInteractionPolicyState.Cookie,
			Input = 1,
			MultiSelect = 2,
			ScrollerPos = 3,
			DragType = 4,
		};
		Assert.True(MuiListviewCore.MuiListviewInteractionPolicyStateCodec.WriteRecord(
			ref platform, address, value));
		var cursor = new MuiListviewCore.MuiListviewInteractionPolicyFieldCursor
		{
			Record = address,
			Field = MuiListviewCore.MuiListviewInteractionPolicyField.DragType,
		};
		Assert.True(MuiListviewCore.MuiListviewInteractionPolicyFieldCursorCodec
			.TryGetAddress(ref platform, cursor, out var fieldAddress,
				out var fieldSize));
		Assert.Equal(0x3510u, fieldAddress.Raw);
		Assert.Equal(4u, fieldSize);
		Assert.True(MuiListviewCore.MuiListviewInteractionPolicyFieldCursorCodec
			.TryWriteUInt32(ref platform, address,
				MuiListviewCore.MuiListviewInteractionPolicyField.DragType, 9));
		Assert.True(MuiListviewCore.MuiListviewInteractionPolicyFieldCursorCodec
			.TryReadUInt32(ref platform, address,
				MuiListviewCore.MuiListviewInteractionPolicyField.DragType,
				out var dragType));
		Assert.Equal(9u, dragType);
		Assert.True(MuiListviewCore.MuiListviewInteractionPolicyStateCodec
			.TryReadStructural(ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Input, decoded.Input);
		Assert.Equal(9u, decoded.DragType);
	}

	[Fact]
	public void CursorRejectsUnknownAndIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListviewCore.MuiListviewInteractionPolicyFieldCursorCodec
			.TryGetAddress(ref platform,
				new MuiListviewCore.MuiListviewInteractionPolicyFieldCursor
				{
					Record = address,
					Field = (MuiListviewCore.MuiListviewInteractionPolicyField)255,
				}, out _, out _));
		Assert.False(MuiListviewCore.MuiListviewInteractionPolicyFieldCursorCodec
			.TryGetAddress(ref platform,
				new MuiListviewCore.MuiListviewInteractionPolicyFieldCursor
				{
					Record = APTR.FromPointer(0x30FF0),
					Field = MuiListviewCore.MuiListviewInteractionPolicyField.Magic,
				}, out _, out _));
		Assert.False(MuiListviewCore.MuiListviewInteractionPolicyMemoryCodec
			.TryGetAddress(ref platform, APTR.Null,
				MuiListviewCore.MuiListviewInteractionPolicyField.Magic, out _, out _));
	}
}
