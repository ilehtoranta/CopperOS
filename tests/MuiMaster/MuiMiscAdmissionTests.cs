using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiMiscAdmissionTests
{
	[Fact]
	public void MiscSpecialistHeaderRoundTripsThroughNamedStruct()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x1500);
		var value = new MuiMiscSpecialistHeader
		{
			Magic = MuiMiscSpecialistHeader.Cookie,
			Class = (uint)MuiMiscSpecialistClass.Keyadjust,
			Flags = MuiMiscSpecialistLayout.FlagKaMultipleKeys,
			NotifyAttribute = 1,
			NotifyValue = 2,
			NotifyCount = 3,
		};
		Assert.True(MuiMiscSpecialistHeaderCodec.Write(ref platform, address, value));
		Assert.True(MuiMiscSpecialistHeaderCodec.TryRead(ref platform, address,
			out var read));
		Assert.Equal(value.Magic, read.Magic);
		Assert.Equal(value.Class, read.Class);
		Assert.Equal(value.Flags, read.Flags);
		Assert.Equal(value.NotifyCount, read.NotifyCount);
	}

	[Fact]
	public void MalformedMiscMagicRemainsStructuralButFailsClosed()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x1580);
		var value = new MuiMiscSpecialistHeader
		{
			Magic = MuiMiscSpecialistHeader.Cookie,
			Class = (uint)MuiMiscSpecialistClass.Keyadjust,
		};
		Assert.True(MuiMiscSpecialistHeaderCodec.Write(ref platform, address, value));
		Assert.True(MuiMiscRecordFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiMiscRecordKind.Header, MuiMiscRecordField.Magic, 0));
		Assert.True(MuiMiscSpecialistHeaderCodec.TryReadStructural(ref platform,
			address, out var structural));
		Assert.Equal(0u, structural.Magic);
		Assert.False(MuiMiscSpecialistHeaderCodec.TryRead(ref platform, address,
			out _));
		Assert.False(MuiMiscSpecialistAdmission.ValidateHeader(structural));
	}
}
