using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaPresentationPolicyAdmissionTests
{
	[Fact]
	public void AreaPresentationPoliciesRoundTripThroughNamedStructs()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var doubleClickAddress = APTR.FromPointer(0x1500);
		var shortHelpAddress = APTR.FromPointer(0x1520);
		var textColorAddress = APTR.FromPointer(0x1540);
		var doubleClick = new MuiAreaDoubleClickStateRecord
		{
			Magic = MuiAreaDoubleClickStateRecord.Cookie,
			Value = -2,
			Generation = 3,
		};
		var shortHelp = new MuiAreaShortHelpStateRecord
		{
			Magic = MuiAreaShortHelpStateRecord.Cookie,
			Text = APTR.FromPointer(0x1900),
			Generation = 5,
		};
		var textColor = new MuiAreaTextColorStateRecord
		{
			Magic = MuiAreaTextColorStateRecord.Cookie,
			Color = 0x00C0FFEE,
			Active = 1,
			Generation = 7,
		};

		Assert.True(MuiAreaDoubleClickStateRecordCodec.Write(ref platform,
			doubleClickAddress, doubleClick));
		Assert.True(MuiAreaShortHelpStateRecordCodec.Write(ref platform,
			shortHelpAddress, shortHelp));
		Assert.True(MuiAreaTextColorStateRecordCodec.Write(ref platform,
			textColorAddress, textColor));
		Assert.True(MuiAreaDoubleClickStateRecordCodec.TryRead(ref platform,
			doubleClickAddress, out var doubleClickRead));
		Assert.True(MuiAreaShortHelpStateRecordCodec.TryRead(ref platform,
			shortHelpAddress, out var shortHelpRead));
		Assert.True(MuiAreaTextColorStateRecordCodec.TryRead(ref platform,
			textColorAddress, out var textColorRead));
		Assert.Equal(doubleClick.Value, doubleClickRead.Value);
		Assert.Equal(shortHelp.Text, shortHelpRead.Text);
		Assert.Equal(textColor.Color, textColorRead.Color);
		Assert.Equal(textColor.Active, textColorRead.Active);
	}

	[Fact]
	public void AreaTextColorStateUsesDedicatedStructCodec()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x1580);
		var value = default(MuiAreaTextColorStateRecord);
		value.Magic = MuiAreaTextColorStateRecord.Cookie;
		value.Color = 0x00C0FFEE;
		value.Active = 1;
		value.Generation = 7;

		Assert.True(MuiAreaTextColorStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiAreaTextColorStateRecordCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Color, decoded.Color);
		Assert.Equal(value.Active, decoded.Active);
		Assert.Equal(value.Generation, decoded.Generation);
		Assert.True(MuiAreaTextColorStateRecordCodec.TryRead(ref platform, address,
			out decoded));
		Assert.False(MuiAreaTextColorStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
	}

	[Fact]
	public void MalformedAreaPresentationPolicyMagicRemainsStructuralButFailsClosed()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var doubleClickAddress = APTR.FromPointer(0x1500);
		var shortHelpAddress = APTR.FromPointer(0x1520);
		var textColorAddress = APTR.FromPointer(0x1540);
		Assert.True(MuiAreaDoubleClickStateRecordCodec.Write(ref platform,
			doubleClickAddress, new MuiAreaDoubleClickStateRecord
			{
				Magic = MuiAreaDoubleClickStateRecord.Cookie,
				Value = -1,
				Generation = 1,
			}));
		Assert.True(MuiAreaShortHelpStateRecordCodec.Write(ref platform,
			shortHelpAddress, new MuiAreaShortHelpStateRecord
			{
				Magic = MuiAreaShortHelpStateRecord.Cookie,
				Generation = 1,
			}));
		Assert.True(MuiAreaTextColorStateRecordCodec.Write(ref platform,
			textColorAddress, new MuiAreaTextColorStateRecord
			{
				Magic = MuiAreaTextColorStateRecord.Cookie,
				Color = 0x00FFFFFF,
				Active = 0,
				Generation = 1,
			}));

		Assert.True(MuiAreaDoubleClickStateFieldCursorCodec.TryWriteUInt32(
			ref platform, doubleClickAddress, MuiAreaDoubleClickStateField.Magic,
			0));
		Assert.True(MuiAreaShortHelpStateFieldCursorCodec.TryWriteUInt32(
			ref platform, shortHelpAddress, MuiAreaShortHelpStateField.Magic, 0));
		Assert.True(MuiAreaTextColorStateFieldCursorCodec.TryWriteUInt32(
			ref platform, textColorAddress, MuiAreaTextColorStateField.Magic, 0));

		Assert.True(MuiAreaDoubleClickStateRecordCodec.TryReadStructural(
			ref platform, doubleClickAddress, out var doubleClick));
		Assert.True(MuiAreaShortHelpStateRecordCodec.TryReadStructural(ref platform,
			shortHelpAddress, out var shortHelp));
		Assert.True(MuiAreaTextColorStateRecordCodec.TryReadStructural(ref platform,
			textColorAddress, out var textColor));
		Assert.Equal(0u, doubleClick.Magic);
		Assert.Equal(0u, shortHelp.Magic);
		Assert.Equal(0u, textColor.Magic);
		Assert.False(MuiAreaDoubleClickStateRecordCodec.TryRead(ref platform,
			doubleClickAddress, out _));
		Assert.False(MuiAreaShortHelpStateRecordCodec.TryRead(ref platform,
			shortHelpAddress, out _));
		Assert.False(MuiAreaTextColorStateRecordCodec.TryRead(ref platform,
			textColorAddress, out _));
	}
}
