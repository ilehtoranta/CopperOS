using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListFormatPolicyStateFieldCursorStructAdapterTests
{
	[Fact]
	public void CursorWalksNamedFormatPolicyRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListCore.MuiListFormatPolicyState
		{
			Magic = MuiListCore.MuiListFormatPolicyState.Cookie,
			Format = APTR.FromPointer(0x4567),
			MaxColumns = 32,
			Columns = 7,
		};
		Assert.True(MuiListCore.MuiListFormatPolicyStateCodec.WriteRecord(
			ref platform, address, value));
		var cursor = new MuiListCore.MuiListFormatPolicyStateFieldCursor
		{
			Address = address,
			Field = MuiListCore.MuiListFormatPolicyStateField.Columns,
		};
		Assert.True(MuiListCore.MuiListFormatPolicyStateFieldCursorCodec
			.TryGetAddress(ref platform, cursor, out var fieldAddress, out var fieldSize));
		Assert.Equal(0x350Cu, fieldAddress.Raw);
		Assert.Equal(4u, fieldSize);
		Assert.True(MuiListCore.MuiListFormatPolicyStateFieldCursorCodec
			.TryWriteUInt32(ref platform, address,
				MuiListCore.MuiListFormatPolicyStateField.Columns, 9));
		Assert.True(MuiListCore.MuiListFormatPolicyStateFieldCursorCodec
			.TryReadUInt32(ref platform, address,
				MuiListCore.MuiListFormatPolicyStateField.Columns, out var columns));
		Assert.Equal(9u, columns);
		Assert.True(MuiListCore.MuiListFormatPolicyStateCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Format, decoded.Format);
		Assert.Equal(value.MaxColumns, decoded.MaxColumns);
		Assert.Equal(9u, decoded.Columns);
	}

	[Fact]
	public void CursorRejectsUnknownAndIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListCore.MuiListFormatPolicyStateFieldCursorCodec
			.TryGetAddress(ref platform,
				new MuiListCore.MuiListFormatPolicyStateFieldCursor
				{
					Address = address,
					Field = (MuiListCore.MuiListFormatPolicyStateField)255,
				}, out _, out _));
		Assert.False(MuiListCore.MuiListFormatPolicyStateFieldCursorCodec
			.TryGetAddress(ref platform,
				new MuiListCore.MuiListFormatPolicyStateFieldCursor
				{
					Address = APTR.FromPointer(0x30FF9),
					Field = MuiListCore.MuiListFormatPolicyStateField.Magic,
				}, out _, out _));
		Assert.False(MuiListCore.MuiListFormatPolicyStateMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiListCore.MuiListFormatPolicyStateField.Magic,
			out _, out _));
	}
}
