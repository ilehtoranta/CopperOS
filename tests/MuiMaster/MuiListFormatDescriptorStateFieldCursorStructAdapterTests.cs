using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListFormatDescriptorStateFieldCursorStructAdapterTests
{
	[Fact]
	public void CursorWalksNamedFormatDescriptorStateRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListCore.MuiListFormatDescriptorState
		{
			Magic = MuiListCore.MuiListFormatDescriptorState.Cookie,
			Columns = 4,
			Values = APTR.FromPointer(0x1234),
		};
		Assert.True(MuiListCore.MuiListFormatDescriptorStateCodec.WriteRecord(
			ref platform, address, value));
		var cursor = new MuiListCore.MuiListFormatDescriptorStateFieldCursor
		{
			Address = address,
			Field = MuiListCore.MuiListFormatDescriptorStateField.Values,
		};
		Assert.True(MuiListCore.MuiListFormatDescriptorStateFieldCursorCodec
			.TryGetAddress(ref platform, cursor, out var fieldAddress, out var fieldSize));
		Assert.Equal(0x3508u, fieldAddress.Raw);
		Assert.Equal(4u, fieldSize);
		Assert.True(MuiListCore.MuiListFormatDescriptorStateFieldCursorCodec
			.TryWriteUInt32(ref platform, address,
				MuiListCore.MuiListFormatDescriptorStateField.Columns, 8));
		Assert.True(MuiListCore.MuiListFormatDescriptorStateFieldCursorCodec
			.TryReadUInt32(ref platform, address,
				MuiListCore.MuiListFormatDescriptorStateField.Values, out var values));
		Assert.Equal(value.Values.Raw, values);
		Assert.True(MuiListCore.MuiListFormatDescriptorStateCodec.TryReadRecord(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(8u, decoded.Columns);
		Assert.Equal(value.Values.Raw, decoded.Values.Raw);
	}

	[Fact]
	public void CursorRejectsUnknownAndIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListCore.MuiListFormatDescriptorStateFieldCursorCodec
			.TryGetAddress(ref platform,
				new MuiListCore.MuiListFormatDescriptorStateFieldCursor
				{
					Address = address,
					Field = (MuiListCore.MuiListFormatDescriptorStateField)255,
				}, out _, out _));
		Assert.False(MuiListCore.MuiListFormatDescriptorStateFieldCursorCodec
			.TryGetAddress(ref platform,
				new MuiListCore.MuiListFormatDescriptorStateFieldCursor
				{
					Address = APTR.FromPointer(0x30FF9),
					Field = MuiListCore.MuiListFormatDescriptorStateField.Magic,
				}, out _, out _));
		Assert.False(MuiListCore.MuiListFormatDescriptorStateMemoryCodec.TryGetAddress(
			ref platform, APTR.Null,
			MuiListCore.MuiListFormatDescriptorStateField.Magic, out _, out _));
	}
}
