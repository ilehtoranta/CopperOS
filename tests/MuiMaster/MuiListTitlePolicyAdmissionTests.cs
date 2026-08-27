using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListTitlePolicyAdmissionTests
{
	[Fact]
	public void ListTitleSelectionFormatAndFontRecordsRoundTripThroughStructs()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var titleAddress = APTR.FromPointer(0x3100);
		var selectionAddress = APTR.FromPointer(0x3120);
		var formatAddress = APTR.FromPointer(0x3140);
		var fontAddress = APTR.FromPointer(0x3160);
		var title = new MuiListCore.MuiListTitleState
		{
			Magic = MuiListCore.MuiListTitleState.Cookie,
			Value = 1,
		};
		var selection = new MuiListCore.MuiListSelectionSignalState
		{
			Magic = MuiListCore.MuiListSelectionSignalState.Cookie,
			Value = 1,
		};
		var format = new MuiListCore.MuiListFormatPolicyState
		{
			Magic = MuiListCore.MuiListFormatPolicyState.Cookie,
			Format = APTR.Null,
			MaxColumns = 8,
			Columns = 3,
		};
		var font = new MuiListCore.MuiListFontState
		{
			Magic = MuiListCore.MuiListFontState.Cookie,
			Font = APTR.Null,
		};

		Assert.True(MuiListCore.MuiListTitleStateCodec.Write(ref platform, titleAddress, title));
		Assert.True(MuiListCore.MuiListSelectionSignalStateCodec.Write(ref platform,
			selectionAddress, selection));
		Assert.True(MuiListCore.MuiListFormatPolicyStateCodec.Write(ref platform, formatAddress,
			format));
		Assert.True(MuiListCore.MuiListFontStateCodec.Write(ref platform, fontAddress, font));

		Assert.True(MuiListCore.MuiListTitleStateCodec.TryRead(ref platform, titleAddress,
			out var readTitle));
		Assert.Equal(title.Value, readTitle.Value);
		Assert.True(MuiListCore.MuiListSelectionSignalStateCodec.TryRead(ref platform,
			selectionAddress, out var readSelection));
		Assert.Equal(selection.Value, readSelection.Value);
		Assert.True(MuiListCore.MuiListFormatPolicyStateCodec.TryRead(ref platform, formatAddress,
			out var readFormat));
		Assert.Equal(format.MaxColumns, readFormat.MaxColumns);
		Assert.Equal(format.Columns, readFormat.Columns);
		Assert.Equal(format.Format, readFormat.Format);
		Assert.True(MuiListCore.MuiListFontStateCodec.TryRead(ref platform, fontAddress,
			out var readFont));
		Assert.Equal(font.Font, readFont.Font);
	}

	[Fact]
	public void MalformedListTitlePolicyMagicRemainsStructuralButFailsClosed()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var titleAddress = APTR.FromPointer(0x3180);
		var selectionAddress = APTR.FromPointer(0x31A0);
		var formatAddress = APTR.FromPointer(0x31C0);
		var fontAddress = APTR.FromPointer(0x31E0);
		Assert.True(MuiListCore.MuiListTitleStateCodec.Write(ref platform, titleAddress,
			new MuiListCore.MuiListTitleState
			{
				Magic = MuiListCore.MuiListTitleState.Cookie,
				Value = 1,
			}));
		Assert.True(MuiListCore.MuiListSelectionSignalStateCodec.Write(ref platform,
			selectionAddress, new MuiListCore.MuiListSelectionSignalState
			{
				Magic = MuiListCore.MuiListSelectionSignalState.Cookie,
				Value = 1,
			}));
		Assert.True(MuiListCore.MuiListFormatPolicyStateCodec.Write(ref platform, formatAddress,
			new MuiListCore.MuiListFormatPolicyState
			{
				Magic = MuiListCore.MuiListFormatPolicyState.Cookie,
				MaxColumns = 4,
				Columns = 2,
			}));
		Assert.True(MuiListCore.MuiListFontStateCodec.Write(ref platform, fontAddress,
			new MuiListCore.MuiListFontState
			{
				Magic = MuiListCore.MuiListFontState.Cookie,
			}));
		Assert.True(MuiListCore.MuiListTitleStateFieldCursorCodec.TryWriteUInt32(ref platform,
			titleAddress, MuiListCore.MuiListTitleStateField.Magic, 0));
		Assert.True(MuiListCore.MuiListSelectionSignalStateFieldCursorCodec.TryWriteUInt32(
			ref platform, selectionAddress,
			MuiListCore.MuiListSelectionSignalStateField.Magic, 0));
		Assert.True(MuiListCore.MuiListFormatPolicyStateFieldCursorCodec.TryWriteUInt32(
			ref platform, formatAddress, MuiListCore.MuiListFormatPolicyStateField.Magic, 0));
		Assert.True(MuiListCore.MuiListFontStateFieldCursorCodec.TryWriteUInt32(ref platform,
			fontAddress, MuiListCore.MuiListFontStateField.Magic, 0));

		Assert.True(MuiListCore.MuiListTitleStateCodec.TryReadStructural(ref platform,
			titleAddress, out var structuralTitle));
		Assert.Equal(0u, structuralTitle.Magic);
		Assert.Equal(1u, structuralTitle.Value);
		Assert.False(MuiListCore.MuiListTitleStateCodec.TryRead(ref platform, titleAddress,
			out _));
		Assert.True(MuiListCore.MuiListTitleStateCodec.TryReadStorage(ref platform, titleAddress,
			out _));

		Assert.True(MuiListCore.MuiListSelectionSignalStateCodec.TryReadStructural(ref platform,
			selectionAddress, out var structuralSelection));
		Assert.Equal(0u, structuralSelection.Magic);
		Assert.False(MuiListCore.MuiListSelectionSignalStateCodec.TryRead(ref platform,
			selectionAddress, out _));
		Assert.True(MuiListCore.MuiListSelectionSignalStateCodec.TryReadStorage(ref platform,
			selectionAddress, out _));

		Assert.True(MuiListCore.MuiListFormatPolicyStateCodec.TryReadStructural(ref platform,
			formatAddress, out var structuralFormat));
		Assert.Equal(0u, structuralFormat.Magic);
		Assert.Equal(4u, structuralFormat.MaxColumns);
		Assert.False(MuiListCore.MuiListFormatPolicyStateCodec.TryRead(ref platform,
			formatAddress, out _));
		Assert.True(MuiListCore.MuiListFormatPolicyStateCodec.TryReadStorage(ref platform,
			formatAddress, out _));

		Assert.True(MuiListCore.MuiListFontStateCodec.TryReadStructural(ref platform,
			fontAddress, out var structuralFont));
		Assert.Equal(0u, structuralFont.Magic);
		Assert.False(MuiListCore.MuiListFontStateCodec.TryRead(ref platform, fontAddress,
			out _));
		Assert.True(MuiListCore.MuiListFontStateCodec.TryReadStorage(ref platform, fontAddress,
			out _));
	}
}
