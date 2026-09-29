using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListviewExternalScrollerConnectionFieldCursorStructAdapterTests
{
	[Fact]
	public void CursorWalksNamedConnectionRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListviewCore.MuiListviewExternalScrollerConnectionState
		{
			Magic = MuiListviewCore.MuiListviewExternalScrollerConnectionState.Cookie,
			Prop = APTR.FromPointer(0x123456),
		};
		Assert.True(MuiListviewCore.MuiListviewExternalScrollerConnectionStateCodec
			.WriteRecord(ref platform, address, value));
		var cursor = new MuiListviewCore.MuiListviewExternalScrollerConnectionFieldCursor
		{
			Record = address,
			Field = MuiListviewCore.MuiListviewExternalScrollerConnectionField.Prop,
		};
		Assert.True(MuiListviewCore.MuiListviewExternalScrollerConnectionFieldCursorCodec
			.TryGetAddress(ref platform, cursor, out var fieldAddress,
				out var fieldSize));
		Assert.Equal(0x3504u, fieldAddress.Raw);
		Assert.Equal(4u, fieldSize);
		Assert.True(MuiListviewCore.MuiListviewExternalScrollerConnectionFieldCursorCodec
			.TryWriteUInt32(ref platform, address,
				MuiListviewCore.MuiListviewExternalScrollerConnectionField.Prop,
				0x345678));
		Assert.True(MuiListviewCore.MuiListviewExternalScrollerConnectionFieldCursorCodec
			.TryReadUInt32(ref platform, address,
				MuiListviewCore.MuiListviewExternalScrollerConnectionField.Prop,
				out var prop));
		Assert.Equal(0x345678u, prop);
		Assert.True(MuiListviewCore.MuiListviewExternalScrollerConnectionStateCodec
			.TryReadStructural(ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(0x345678u, decoded.Prop.Raw);
	}

	[Fact]
	public void CursorRejectsUnknownAndIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListviewCore.MuiListviewExternalScrollerConnectionFieldCursorCodec
			.TryGetAddress(ref platform,
				new MuiListviewCore.MuiListviewExternalScrollerConnectionFieldCursor
				{
					Record = address,
					Field = (MuiListviewCore.MuiListviewExternalScrollerConnectionField)255,
				}, out _, out _));
		Assert.False(MuiListviewCore.MuiListviewExternalScrollerConnectionFieldCursorCodec
			.TryGetAddress(ref platform,
				new MuiListviewCore.MuiListviewExternalScrollerConnectionFieldCursor
				{
					Record = APTR.FromPointer(0x30FFC),
					Field = MuiListviewCore.MuiListviewExternalScrollerConnectionField.Magic,
				}, out _, out _));
		Assert.False(MuiListviewCore.MuiListviewExternalScrollerConnectionMemoryCodec
			.TryGetAddress(ref platform, APTR.Null,
				MuiListviewCore.MuiListviewExternalScrollerConnectionField.Magic,
				out _, out _));
	}
}
