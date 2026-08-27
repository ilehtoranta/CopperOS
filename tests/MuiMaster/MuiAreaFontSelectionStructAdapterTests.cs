using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaFontSelectionStructAdapterTests
{
	[Fact]
	public void AreaFontSelectionStructAdapterUsesNamedFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3C00);
		var value = new MuiAreaFontSelectionStateRecord
		{
			Magic = MuiAreaFontSelectionStateRecord.Cookie,
			Active = (uint)MuiAreaFontSelectionKind.CustomFont,
			Source = APTR.FromPointer(0x1A00),
			Generation = 7,
		};

		Assert.True(MuiAreaFontSelectionStateRecordCodec.Write(ref platform,
			address, value));
		Assert.True(MuiAreaFontSelectionStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiAreaFontSelectionStateField.Source,
			out var sourceAddress));
		Assert.Equal(0x3C08u, sourceAddress.Raw);
		Assert.True(MuiAreaFontSelectionStateRecordCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(value.Source, decoded.Source);
		Assert.Equal(value.Active, decoded.Active);
		Assert.False(MuiAreaFontSelectionStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, (MuiAreaFontSelectionStateField)255, out _));
		Assert.False(MuiAreaFontSelectionStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiAreaFontSelectionStateField.Magic, out _));
		Assert.False(MuiAreaFontSelectionStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
	}
}
