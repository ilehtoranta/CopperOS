using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaFontAdmissionTests
{
	[Fact]
	public void AreaFontRecordsRoundTripThroughNamedStructs()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var builtinAddress = APTR.FromPointer(0x1500);
		var selectionAddress = APTR.FromPointer(0x1520);
		var builtin = new MuiAreaBuiltinFontStateRecord
		{
			Magic = MuiAreaBuiltinFontStateRecord.Cookie,
			Selector = unchecked((uint)-7),
			Present = 1,
			Generation = 3,
		};
		var selection = new MuiAreaFontSelectionStateRecord
		{
			Magic = MuiAreaFontSelectionStateRecord.Cookie,
			Active = (uint)MuiAreaFontSelectionKind.CustomFont,
			Source = APTR.FromPointer(0x1900),
			Generation = 5,
		};
		Assert.True(MuiAreaBuiltinFontStateRecordCodec.Write(ref platform,
			builtinAddress, builtin));
		Assert.True(MuiAreaFontSelectionStateRecordCodec.Write(ref platform,
			selectionAddress, selection));
		Assert.True(MuiAreaBuiltinFontStateRecordCodec.TryRead(ref platform,
			builtinAddress, out var builtinRead));
		Assert.True(MuiAreaFontSelectionStateRecordCodec.TryRead(ref platform,
			selectionAddress, out var selectionRead));
		Assert.Equal(builtin.Selector, builtinRead.Selector);
		Assert.Equal(builtin.Present, builtinRead.Present);
		Assert.Equal(selection.Active, selectionRead.Active);
		Assert.Equal(selection.Source, selectionRead.Source);
	}

	[Fact]
	public void MalformedAreaFontMagicRemainsStructuralButFailsClosed()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var builtinAddress = APTR.FromPointer(0x1500);
		var selectionAddress = APTR.FromPointer(0x1520);
		Assert.True(MuiAreaBuiltinFontStateRecordCodec.Write(ref platform,
			builtinAddress, new MuiAreaBuiltinFontStateRecord
			{
				Magic = MuiAreaBuiltinFontStateRecord.Cookie,
				Selector = unchecked((uint)-7),
				Present = 1,
				Generation = 1,
			}));
		Assert.True(MuiAreaFontSelectionStateRecordCodec.Write(ref platform,
			selectionAddress, new MuiAreaFontSelectionStateRecord
			{
				Magic = MuiAreaFontSelectionStateRecord.Cookie,
				Active = (uint)MuiAreaFontSelectionKind.Font,
				Source = APTR.FromPointer(0x1900),
				Generation = 1,
			}));
		Assert.True(MuiAreaBuiltinFontStateFieldCursorCodec.TryWriteUInt32(
			ref platform, builtinAddress, MuiAreaBuiltinFontStateField.Magic, 0));
		Assert.True(MuiAreaFontSelectionStateFieldCursorCodec.TryWriteUInt32(
			ref platform, selectionAddress, MuiAreaFontSelectionStateField.Magic, 0));
		Assert.True(MuiAreaBuiltinFontStateRecordCodec.TryReadStructural(
			ref platform, builtinAddress, out var builtin));
		Assert.True(MuiAreaFontSelectionStateRecordCodec.TryReadStructural(
			ref platform, selectionAddress, out var selection));
		Assert.Equal(0u, builtin.Magic);
		Assert.Equal(0u, selection.Magic);
		Assert.False(MuiAreaBuiltinFontStateRecordCodec.TryRead(ref platform,
			builtinAddress, out _));
		Assert.False(MuiAreaFontSelectionStateRecordCodec.TryRead(ref platform,
			selectionAddress, out _));
	}
}
