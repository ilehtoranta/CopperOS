using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiPopAdmissionTests
{
	[Fact]
	public void PopSpecialistStateRoundTripsThroughNamedStruct()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x1500);
		var value = new MuiPopSpecialistState
		{
			Magic = MuiPopSpecialistState.Cookie,
			Class = (uint)MuiPopSpecialistClass.Popstring,
			Flags = MuiPopSpecialistLayout.FlagToggle,
			StringChild = APTR.Null,
			ButtonChild = APTR.Null,
			ArrayCount = 0,
			AslType = 3,
			Selected = 7,
			NotifyAttribute = 1,
			NotifyValue = 2,
			NotifyCount = 3,
		};
		Assert.True(MuiPopSpecialistStateCodec.Write(ref platform, address, value));
		Assert.True(MuiPopSpecialistStateCodec.TryRead(ref platform, address,
			out var read));
		Assert.Equal(value.Magic, read.Magic);
		Assert.Equal(value.Class, read.Class);
		Assert.Equal(value.Flags, read.Flags);
		Assert.Equal(value.AslType, read.AslType);
		Assert.Equal(value.Selected, read.Selected);
		Assert.Equal(value.NotifyCount, read.NotifyCount);
	}

	[Fact]
	public void MalformedPopMagicRemainsStructuralButFailsClosed()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x1600);
		Assert.True(MuiPopSpecialistStateCodec.Write(ref platform, address,
			new MuiPopSpecialistState
			{
				Magic = MuiPopSpecialistState.Cookie,
				Class = (uint)MuiPopSpecialistClass.Popobject,
				Flags = MuiPopSpecialistLayout.FlagVolatile,
			}));
		Assert.True(MuiPopSpecialistRecordFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiPopSpecialistRecordField.Magic, 0));
		Assert.True(MuiPopSpecialistStateCodec.TryReadStructural(ref platform,
			address, out var structural));
		Assert.Equal(0u, structural.Magic);
		Assert.False(MuiPopSpecialistStateCodec.TryRead(ref platform, address,
			out _));
		Assert.False(MuiPopSpecialistAdmission.Validate(ref platform, structural));
	}
}
