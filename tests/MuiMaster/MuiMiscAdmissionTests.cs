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

	[Fact]
	public void MiscRecordMemoryCodecUsesNamedRecordKinds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var headerAddress = APTR.FromPointer(0x1600);
		Assert.True(MuiMiscRecordMemoryCodec.TryWriteUInt32(ref platform,
			headerAddress, MuiMiscRecordKind.Header,
			MuiMiscRecordField.NotifyCount, 7));
		Assert.True(MuiMiscRecordMemoryCodec.TryReadUInt32(ref platform,
			headerAddress, MuiMiscRecordKind.Header,
			MuiMiscRecordField.NotifyCount, out var count));
		Assert.Equal(7u, count);
		Assert.True(MuiMiscRecordMemoryCodec.TryGetAddress(ref platform,
			headerAddress, MuiMiscRecordKind.Header,
			MuiMiscRecordField.NotifyCount, out var countAddress));
		Assert.Equal(headerAddress.Raw + 20u, countAddress.Raw);

		var titleAddress = APTR.FromPointer(0x1640);
		Assert.True(MuiMiscRecordMemoryCodec.TryWriteUInt32(ref platform,
			titleAddress, MuiMiscRecordKind.Title,
			MuiMiscRecordField.Position, unchecked((uint)-3)));
		Assert.True(MuiMiscRecordMemoryCodec.TryReadUInt32(ref platform,
			titleAddress, MuiMiscRecordKind.Title,
			MuiMiscRecordField.Position, out var position));
		Assert.Equal(-3, unchecked((int)position));
		Assert.False(MuiMiscRecordMemoryCodec.TryGetAddress(ref platform,
			titleAddress, MuiMiscRecordKind.Title,
			(MuiMiscRecordField)0xFF, out _));
		Assert.False(MuiMiscRecordMemoryCodec.TryReadUInt32(ref platform,
			APTR.FromPointer(0x20FF4), MuiMiscRecordKind.Title,
			MuiMiscRecordField.Position, out _));
	}

	[Fact]
	public void MiscStateMemoryCodecResolvesNamedRegions()
	{
		var instance = APTR.FromPointer(0x1800);
		Assert.True(MuiMiscStateMemoryCodec.TryGetAddress(instance,
			MuiMiscStateRegion.Title, out var title));
		Assert.Equal(instance.Raw + (uint)MuiMiscSpecialistLayout.TitleStateOffset,
			title.Raw);
		Assert.True(MuiMiscStateMemoryCodec.TryGetAddress(instance,
			MuiMiscStateRegion.FilepanelService, out var filepanel));
		Assert.Equal(instance.Raw +
			(uint)MuiMiscSpecialistLayout.FilepanelServiceStateOffset,
			filepanel.Raw);
		Assert.False(MuiMiscStateMemoryCodec.TryGetAddress(APTR.Null,
			MuiMiscStateRegion.Title, out _));
		Assert.False(MuiMiscStateMemoryCodec.TryGetAddress(instance,
			(MuiMiscStateRegion)0xFF, out _));
	}

	[Fact]
	public void MiscOwnedStringMemoryCodecResolvesNamedSlots()
	{
		var instance = APTR.FromPointer(0x1900);
		Assert.True(MuiMiscOwnedStringMemoryCodec.TryGetAddress(instance,
			MuiMiscOwnedStringField.Key, out var key));
		Assert.Equal(instance.Raw + 24u, key.Raw);
		Assert.True(MuiMiscOwnedStringMemoryCodec.TryGetAddress(instance,
			MuiMiscOwnedStringField.FilepanelRejectPattern, out var reject));
		Assert.Equal(instance.Raw + 136u, reject.Raw);
		Assert.False(MuiMiscOwnedStringMemoryCodec.TryGetAddress(APTR.Null,
			MuiMiscOwnedStringField.Key, out _));
		Assert.False(MuiMiscOwnedStringMemoryCodec.TryGetAddress(instance,
			(MuiMiscOwnedStringField)0xFF, out _));
	}
}
