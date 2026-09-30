using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListtreeHeaderStructAdapterTests
{
	[Fact]
	public void HeaderFieldsRoundTripThroughNamedRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListtreeCore.MuiListtreeHeaderState
		{
			Magic = MuiListtreeCore.MuiListtreeHeaderState.Cookie,
			RootFirst = APTR.FromPointer(0x36200),
			RootLast = APTR.FromPointer(0x36240),
			RootCount = 2,
			Total = 5,
			Redraw = 7,
			Dirty = 1,
			DropEntry = -3,
			DropValue = 4,
			Reserved0 = 0x10,
			Reserved1 = 0x20,
			Reserved2 = 0x30,
		};

		Assert.True(MuiListtreeCore.MuiListtreeHeaderCodec.WriteRecord(
			ref platform, address, value));
		Assert.True(MuiListtreeCore.MuiListtreeHeaderMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListtreeCore.MuiListtreeHeaderField.RootCount, 3));
		Assert.True(MuiListtreeCore.MuiListtreeHeaderMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListtreeCore.MuiListtreeHeaderField.DropEntry,
			unchecked((uint)-7)));
		Assert.True(MuiListtreeCore.MuiListtreeHeaderMemoryCodec.TryReadUInt32(
			ref platform, address, MuiListtreeCore.MuiListtreeHeaderField.DropEntry,
			out var dropEntry));
		Assert.Equal(unchecked((uint)-7), dropEntry);

		Assert.True(MuiListtreeCore.MuiListtreeHeaderCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.RootFirst.Raw, decoded.RootFirst.Raw);
		Assert.Equal(value.RootLast.Raw, decoded.RootLast.Raw);
		Assert.Equal(3u, decoded.RootCount);
		Assert.Equal(value.Total, decoded.Total);
		Assert.Equal(value.Redraw, decoded.Redraw);
		Assert.Equal(value.Dirty, decoded.Dirty);
		Assert.Equal(-7, decoded.DropEntry);
		Assert.Equal(value.DropValue, decoded.DropValue);
		Assert.Equal(value.Reserved0, decoded.Reserved0);
		Assert.Equal(value.Reserved1, decoded.Reserved1);
		Assert.Equal(value.Reserved2, decoded.Reserved2);
		Assert.True(MuiListtreeCore.MuiListtreeHeaderMemoryCodec.TryGetAddress(
			ref platform, address, MuiListtreeCore.MuiListtreeHeaderField.DropValue,
			out var dropValueAddress));
		Assert.Equal(0x3520u, dropValueAddress.Raw);
	}

	[Fact]
	public void HeaderAdapterRejectsInvalidOrIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListtreeCore.MuiListtreeHeaderMemoryCodec.TryReadUInt32(
			ref platform, address, (MuiListtreeCore.MuiListtreeHeaderField)0xFF,
			out _));
		Assert.False(MuiListtreeCore.MuiListtreeHeaderMemoryCodec.TryWriteUInt32(
			ref platform, address, (MuiListtreeCore.MuiListtreeHeaderField)0xFF, 1));
		Assert.False(MuiListtreeCore.MuiListtreeHeaderMemoryCodec.TryReadUInt32(
			ref platform, APTR.FromPointer(0x30FF0),
			MuiListtreeCore.MuiListtreeHeaderField.RootCount, out _));
		Assert.False(MuiListtreeCore.MuiListtreeHeaderMemoryCodec.TryWriteUInt32(
			ref platform, APTR.Null, MuiListtreeCore.MuiListtreeHeaderField.Magic, 1));
	}
}
