using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListtreeDisplayColumnStructAdapterTests
{
	[Fact]
	public void DisplayColumnTextRoundTripsThroughNamedRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListtreeCore.MuiListtreeDisplayColumnRecord
		{
			Text = APTR.FromPointer(0x36200),
		};

		Assert.True(MuiListtreeCore.MuiListtreeDisplayColumnCodec.Write(
			ref platform, address, value));
		Assert.True(MuiListtreeCore.MuiListtreeDisplayColumnMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListtreeCore.MuiListtreeDisplayColumnField.Text,
			0x36400));
		Assert.True(MuiListtreeCore.MuiListtreeDisplayColumnMemoryCodec.TryReadUInt32(
			ref platform, address, MuiListtreeCore.MuiListtreeDisplayColumnField.Text,
			out var text));
		Assert.Equal(0x36400u, text);
		Assert.True(MuiListtreeCore.MuiListtreeDisplayColumnCodec.TryRead(
			ref platform, address, out var decoded));
		Assert.Equal(0x36400u, decoded.Text.Raw);
		Assert.True(MuiListtreeCore.MuiListtreeDisplayColumnMemoryCodec.TryGetAddress(
			ref platform, address, MuiListtreeCore.MuiListtreeDisplayColumnField.Text,
			out var textAddress));
		Assert.Equal(0x3500u, textAddress.Raw);
		var fieldCursor = new MuiListtreeCore.MuiListtreeDisplayColumnFieldCursor
		{
			Address = address,
			Field = MuiListtreeCore.MuiListtreeDisplayColumnField.Text,
		};
		Assert.True(MuiListtreeCore.MuiListtreeDisplayColumnFieldCursorCodec
			.TryGetAddress(ref platform, fieldCursor, out var cursorAddress,
				out var cursorSize));
		Assert.Equal(0x3500u, cursorAddress.Raw);
		Assert.Equal(4u, cursorSize);
		var vector = APTR.FromPointer(0x3700);
		Assert.True(MuiListtreeCore.MuiListtreeDisplayColumnVectorMemoryCodec
			.TryGetEntry(ref platform, vector, 2, out var thirdAddress));
		Assert.Equal(0x3708u, thirdAddress.Raw);
	}

	[Fact]
	public void DisplayColumnAdapterRejectsInvalidOrIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListtreeCore.MuiListtreeDisplayColumnMemoryCodec.TryReadUInt32(
			ref platform, address, (MuiListtreeCore.MuiListtreeDisplayColumnField)0xFF,
			out _));
		Assert.False(MuiListtreeCore.MuiListtreeDisplayColumnMemoryCodec.TryWriteUInt32(
			ref platform, address, (MuiListtreeCore.MuiListtreeDisplayColumnField)0xFF,
			1));
		Assert.False(MuiListtreeCore.MuiListtreeDisplayColumnMemoryCodec.TryReadUInt32(
			ref platform, APTR.FromPointer(0x30FFE),
			MuiListtreeCore.MuiListtreeDisplayColumnField.Text, out _));
		Assert.False(MuiListtreeCore.MuiListtreeDisplayColumnMemoryCodec.TryWriteUInt32(
			ref platform, APTR.Null, MuiListtreeCore.MuiListtreeDisplayColumnField.Text,
			1));
		Assert.False(MuiListtreeCore.MuiListtreeDisplayColumnVectorMemoryCodec
			.TryGetEntry(ref platform, address,
				MuiListtreeCore.MuiListtreeDisplayColumnCursor.MaximumEntries, out _));
	}
}
