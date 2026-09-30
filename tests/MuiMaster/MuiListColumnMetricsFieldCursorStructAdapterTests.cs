using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListColumnMetricsFieldCursorStructAdapterTests
{
	[Fact]
	public void CursorWalksNamedColumnMetricsRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListCore.MuiListColumnMetricsState
		{
			Magic = MuiListCore.MuiListColumnMetricsState.Cookie,
			Width = 640,
			Columns = 4,
			Values = APTR.FromPointer(0x1234),
		};
		Assert.True(MuiListCore.MuiListColumnMetricsStateCodec.WriteRecord(
			ref platform, address, value));
		var cursor = new MuiListCore.MuiListColumnMetricsFieldCursor
		{
			Address = address,
			Field = MuiListCore.MuiListColumnMetricsField.Values,
		};
		Assert.True(MuiListCore.MuiListColumnMetricsFieldCursorCodec
			.TryGetAddress(ref platform, cursor, out var fieldAddress, out var fieldSize));
		Assert.Equal(0x350Cu, fieldAddress.Raw);
		Assert.Equal(4u, fieldSize);
		Assert.True(MuiListCore.MuiListColumnMetricsFieldCursorCodec
			.TryWriteUInt32(ref platform, address,
				MuiListCore.MuiListColumnMetricsField.Width, 800));
		Assert.True(MuiListCore.MuiListColumnMetricsFieldCursorCodec
			.TryReadUInt32(ref platform, address,
				MuiListCore.MuiListColumnMetricsField.Columns, out var columns));
		Assert.Equal(value.Columns, columns);
		Assert.True(MuiListCore.MuiListColumnMetricsStateCodec.TryReadRecord(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(800u, decoded.Width);
		Assert.Equal(value.Columns, decoded.Columns);
		Assert.Equal(value.Values.Raw, decoded.Values.Raw);
	}

	[Fact]
	public void CursorRejectsUnknownAndIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListCore.MuiListColumnMetricsFieldCursorCodec
			.TryGetAddress(ref platform,
				new MuiListCore.MuiListColumnMetricsFieldCursor
				{
					Address = address,
					Field = (MuiListCore.MuiListColumnMetricsField)255,
				}, out _, out _));
		Assert.False(MuiListCore.MuiListColumnMetricsFieldCursorCodec
			.TryGetAddress(ref platform,
				new MuiListCore.MuiListColumnMetricsFieldCursor
				{
					Address = APTR.FromPointer(0x30FF9),
					Field = MuiListCore.MuiListColumnMetricsField.Magic,
				}, out _, out _));
		Assert.False(MuiListCore.MuiListColumnMetricsMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiListCore.MuiListColumnMetricsField.Magic,
			out _, out _));
	}
}
