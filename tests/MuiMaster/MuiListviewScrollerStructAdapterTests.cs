using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListviewScrollerStructAdapterTests
{
	[Fact]
	public void ScrollerFieldsRoundTripThroughNamedRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListviewCore.MuiListviewScrollerState
		{
			Magic = MuiListviewCore.MuiListviewScrollerState.Cookie,
			Entries = 100,
			Visible = 12,
			First = 4,
			MaxFirst = 88,
		};

		Assert.True(MuiListviewCore.MuiListviewScrollerStateCodec.WriteRecord(
			ref platform, address, value));
		Assert.True(MuiListviewCore.MuiListviewScrollerMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListviewCore.MuiListviewScrollerField.First, 9));
		Assert.True(MuiListviewCore.MuiListviewScrollerMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListviewCore.MuiListviewScrollerField.MaxFirst,
			90));
		Assert.True(MuiListviewCore.MuiListviewScrollerMemoryCodec.TryReadUInt32(
			ref platform, address, MuiListviewCore.MuiListviewScrollerField.MaxFirst,
			out var maxFirst));
		Assert.Equal(90u, maxFirst);

		Assert.True(MuiListviewCore.MuiListviewScrollerStateCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Entries, decoded.Entries);
		Assert.Equal(value.Visible, decoded.Visible);
		Assert.Equal(9u, decoded.First);
		Assert.Equal(90u, decoded.MaxFirst);
		Assert.True(MuiListviewCore.MuiListviewScrollerMemoryCodec.TryGetAddress(
			ref platform, address, MuiListviewCore.MuiListviewScrollerField.MaxFirst,
			out var maxFirstAddress));
		Assert.Equal(0x3510u, maxFirstAddress.Raw);
	}

	[Fact]
	public void ScrollerAdapterRejectsInvalidOrIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListviewCore.MuiListviewScrollerMemoryCodec.TryReadUInt32(
			ref platform, address, (MuiListviewCore.MuiListviewScrollerField)0xFF,
			out _));
		Assert.False(MuiListviewCore.MuiListviewScrollerMemoryCodec.TryWriteUInt32(
			ref platform, address, (MuiListviewCore.MuiListviewScrollerField)0xFF, 1));
		Assert.False(MuiListviewCore.MuiListviewScrollerMemoryCodec.TryReadUInt32(
			ref platform, APTR.FromPointer(0x30FF0),
			MuiListviewCore.MuiListviewScrollerField.MaxFirst, out _));
		Assert.False(MuiListviewCore.MuiListviewScrollerMemoryCodec.TryWriteUInt32(
			ref platform, APTR.Null, MuiListviewCore.MuiListviewScrollerField.Magic, 1));
	}
}
