using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiMenuAdmissionTests
{
	[Fact]
	public void MenuSpecialistStateRoundTripsThroughNamedStruct()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x1500);
		var value = new MuiMenuSpecialistState
		{
			Magic = MuiMenuSpecialistState.Cookie,
			Class = (uint)MuiMenuSpecialistClass.Menu,
			ChangeDepth = 2,
			TitleOwned = APTR.Null,
			TitleOwnedSize = 0,
			ShortcutOwned = APTR.Null,
			ShortcutOwnedSize = 0,
			Flags = MuiMenuSpecialistLayout.FlagCaseSensitive,
			Trigger = 0x80421234,
			NotifyAttribute = 1,
			NotifyValue = 2,
			NotifyCount = 3,
			Reserved0 = 0,
		};
		Assert.True(MuiMenuSpecialistStateCodec.Write(ref platform, address, value));
		Assert.True(MuiMenuSpecialistStateCodec.TryRead(ref platform, address,
			out var read));
		Assert.Equal(value.Magic, read.Magic);
		Assert.Equal(value.Class, read.Class);
		Assert.Equal(value.ChangeDepth, read.ChangeDepth);
		Assert.Equal(value.Flags, read.Flags);
		Assert.Equal(value.NotifyCount, read.NotifyCount);
	}

	[Fact]
	public void MalformedMenuMagicRemainsStructuralButFailsClosed()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x1580);
		var value = new MuiMenuSpecialistState
		{
			Magic = MuiMenuSpecialistState.Cookie,
			Class = (uint)MuiMenuSpecialistClass.Menuitem,
		};
		Assert.True(MuiMenuSpecialistStateCodec.Write(ref platform, address, value));
		Assert.True(MuiMenuRecordFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiMenuRecordField.Magic, 0));
		Assert.True(MuiMenuSpecialistStateCodec.TryReadStructural(ref platform,
			address, out var structural));
		Assert.Equal(0u, structural.Magic);
		Assert.False(MuiMenuSpecialistStateCodec.TryRead(ref platform, address,
			out _));
		Assert.False(MuiMenuSpecialistStateAdmission.Validate(ref platform,
			structural));
	}
}
