using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListtreeHeaderPresentationAdmissionTests
{
	[Fact]
	public void ListtreeHeaderAndPresentationRoundTripThroughNamedStructs()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var headerAddress = APTR.FromPointer(0x1900);
		var presentationAddress = APTR.FromPointer(0x1940);
		var header = new MuiListtreeCore.MuiListtreeHeaderState
		{
			Magic = MuiListtreeCore.MuiListtreeHeaderState.Cookie,
			RootFirst = APTR.FromPointer(0x1A00),
			RootLast = APTR.FromPointer(0x1A40),
			RootCount = 2,
			Total = 5,
			Redraw = 1,
			Dirty = 1,
			DropEntry = -3,
			DropValue = 4,
			Reserved0 = 0x10,
			Reserved1 = 0x20,
			Reserved2 = 0x30,
		};
		var presentation = new MuiListtreePresentationStateRecord
		{
			Magic = MuiListtreePresentationStateRecord.Cookie,
			EmptyNodes = 1,
			Format = APTR.FromPointer(0x1A80),
			MultiSelect = 1,
			NList = 0,
			Title = 3,
			TreeColumn = 2,
		};
		Assert.True(MuiListtreeCore.MuiListtreeHeaderCodec.Write(ref platform,
			headerAddress, header));
		Assert.True(MuiListtreePresentationStateRecordCodec.Write(ref platform,
			presentationAddress, presentation));
		Assert.True(MuiListtreeCore.MuiListtreeHeaderCodec.TryRead(ref platform,
			headerAddress, out var readHeader));
		Assert.Equal(header.RootFirst, readHeader.RootFirst);
		Assert.Equal(header.DropEntry, readHeader.DropEntry);
		Assert.Equal(header.Reserved2, readHeader.Reserved2);
		Assert.True(MuiListtreePresentationStateRecordCodec.TryRead(ref platform,
			presentationAddress, out var readPresentation));
		Assert.Equal(presentation.Format, readPresentation.Format);
		Assert.Equal(1u, readPresentation.Title);
	}

	[Fact]
	public void MalformedListtreeHeaderAndPresentationMagicRemainStructural()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var headerAddress = APTR.FromPointer(0x1980);
		var presentationAddress = APTR.FromPointer(0x19C0);
		Assert.True(MuiListtreeCore.MuiListtreeHeaderCodec.Write(ref platform,
			headerAddress, new MuiListtreeCore.MuiListtreeHeaderState
			{
				Magic = MuiListtreeCore.MuiListtreeHeaderState.Cookie,
				RootCount = 2,
			}));
		Assert.True(MuiListtreePresentationStateRecordCodec.Write(ref platform,
			presentationAddress, new MuiListtreePresentationStateRecord
			{
				Magic = MuiListtreePresentationStateRecord.Cookie,
				Title = 1,
			}));
		Assert.True(MuiListtreeCore.MuiListtreeHeaderFieldCursorCodec.TryWriteUInt32(
			ref platform, headerAddress, MuiListtreeCore.MuiListtreeHeaderField.Magic,
			0));
		Assert.True(MuiListtreePresentationFieldCursorCodec.TryWriteUInt32(ref platform,
			presentationAddress, MuiListtreePresentationField.Magic, 0));
		Assert.True(MuiListtreeCore.MuiListtreeHeaderCodec.TryReadStructural(ref platform,
			headerAddress, out var header));
		Assert.Equal(0u, header.Magic);
		Assert.Equal(2u, header.RootCount);
		Assert.False(MuiListtreeCore.MuiListtreeHeaderCodec.TryRead(ref platform,
			headerAddress, out _));
		Assert.True(MuiListtreePresentationStateRecordCodec.TryReadStructural(
			ref platform, presentationAddress, out var presentation));
		Assert.Equal(0u, presentation.Magic);
		Assert.Equal(1u, presentation.Title);
		Assert.False(MuiListtreePresentationStateRecordCodec.TryRead(ref platform,
			presentationAddress, out _));
	}
}
