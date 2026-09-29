using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListtreePresentationStructAdapterTests
{
	[Fact]
	public void ListtreePresentationFieldsRoundTripThroughNamedRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListtreePresentationStateRecord
		{
			Magic = MuiListtreePresentationStateRecord.Cookie,
			EmptyNodes = 1,
			Format = APTR.FromPointer(0x3600),
			MultiSelect = 2,
			NList = 3,
			Title = 4,
			TreeColumn = 5,
		};

		Assert.True(MuiListtreePresentationStateRecordCodec.WriteRecord(
			ref platform, address, value));
		Assert.True(MuiListtreePresentationMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiListtreePresentationField.EmptyNodes, 10));
		Assert.True(MuiListtreePresentationMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiListtreePresentationField.Format, 0x3700));
		Assert.True(MuiListtreePresentationMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiListtreePresentationField.TreeColumn, 11));

		Assert.True(MuiListtreePresentationStateRecordCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(10u, decoded.EmptyNodes);
		Assert.Equal(0x3700u, decoded.Format.Raw);
		Assert.Equal(value.MultiSelect, decoded.MultiSelect);
		Assert.Equal(value.NList, decoded.NList);
		Assert.Equal(value.Title, decoded.Title);
		Assert.Equal(11u, decoded.TreeColumn);

		Assert.True(MuiListtreePresentationMemoryCodec.TryReadUInt32(ref platform,
			address, MuiListtreePresentationField.Format, out var format));
		Assert.Equal(0x3700u, format);
		Assert.True(MuiListtreePresentationMemoryCodec.TryGetAddress(ref platform,
			address, MuiListtreePresentationField.TreeColumn, out var treeColumn));
		Assert.Equal(0x3518u, treeColumn.Raw);
		var fieldCursor = new MuiListtreePresentationFieldCursor
		{
			Record = address,
			Field = MuiListtreePresentationField.Format,
		};
		Assert.True(MuiListtreePresentationFieldCursorCodec.TryGetAddress(
			ref platform, fieldCursor, out var formatAddress, out var formatSize));
		Assert.Equal(0x3508u, formatAddress.Raw);
		Assert.Equal(4u, formatSize);
	}

	[Fact]
	public void ListtreePresentationAdapterRejectsInvalidOrIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListtreePresentationMemoryCodec.TryReadUInt32(ref platform,
			address, (MuiListtreePresentationField)0xFF, out _));
		Assert.False(MuiListtreePresentationMemoryCodec.TryWriteUInt32(ref platform,
			address, (MuiListtreePresentationField)0xFF, 1));
		Assert.False(MuiListtreePresentationMemoryCodec.TryReadUInt32(ref platform,
			APTR.FromPointer(0x30FF0),
			MuiListtreePresentationField.TreeColumn, out _));
		Assert.False(MuiListtreePresentationMemoryCodec.TryWriteUInt32(ref platform,
			APTR.Null, MuiListtreePresentationField.Magic, 1));
	}
}
