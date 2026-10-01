using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaShortHelpMessageStructAdapterTests
{
	[Fact]
	public void ShortHelpMessageRecordsUseNamedFieldsAndCompleteBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var checkAddress = APTR.FromPointer(0x3000);
		var check = default(MuiAreaCheckShortHelpMessage);
		check.MethodId = MuiAreaShortHelpMessageCodec.CheckShortHelp;
		check.Help = APTR.FromPointer(0x3400);
		check.MouseX = -12;
		check.MouseY = 34;
		Assert.True(MuiAreaShortHelpMessageCodec.WriteCheck(ref platform,
			checkAddress, check));
		Assert.True(MuiAreaShortHelpMessageMemoryCodec.TryReadUInt32(ref platform,
			checkAddress, MuiAreaShortHelpPacketKind.Check,
			MuiAreaShortHelpMessageField.MouseY, out var mouseY));
		Assert.Equal(34, unchecked((int)mouseY));
		Assert.True(MuiAreaShortHelpMessageMemoryCodec.TryGetAddress(ref platform,
			checkAddress, MuiAreaShortHelpPacketKind.Check,
			MuiAreaShortHelpMessageField.Help, out var helpAddress));
		Assert.Equal(APTR.FromPointer(0x3004), helpAddress);
		var cursor = new MuiAreaShortHelpMessageFieldCursor
		{
			Message = checkAddress,
			Packet = MuiAreaShortHelpPacketKind.Check,
			Field = MuiAreaShortHelpMessageField.Help,
		};
		Assert.True(MuiAreaShortHelpMessageFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var typedHelpAddress, out var typedHelpSize));
		Assert.Equal(helpAddress, typedHelpAddress);
		Assert.Equal(MuiAreaCheckShortHelpMessage.FieldSize, typedHelpSize);
		Assert.True(MuiAreaShortHelpMessageMemoryCodec.TryGetAddress(ref platform,
			cursor, out var memoryHelpAddress, out var memoryHelpSize));
		Assert.Equal(typedHelpAddress, memoryHelpAddress);
		Assert.Equal(typedHelpSize, memoryHelpSize);
		Assert.True(MuiAreaShortHelpMessageCodec.TryReadCheck(ref platform,
			checkAddress, out var decodedCheck));
		Assert.Equal(check.Help, decodedCheck.Help);
		Assert.Equal(check.MouseX, decodedCheck.MouseX);
		Assert.Equal(check.MouseY, decodedCheck.MouseY);

		var createAddress = APTR.FromPointer(0x3040);
		var create = new MuiAreaCreateShortHelpMessage
		{
			MethodId = MuiAreaShortHelpMessageCodec.CreateShortHelp,
			MouseX = -3,
			MouseY = 8,
		};
		Assert.True(MuiAreaShortHelpMessageCodec.WriteCreate(ref platform,
			createAddress, create));
		Assert.True(MuiAreaShortHelpMessageCodec.TryReadCreate(ref platform,
			createAddress, out var decodedCreate));
		Assert.Equal(-3, decodedCreate.MouseX);
		Assert.Equal(8, decodedCreate.MouseY);

		var deleteAddress = APTR.FromPointer(0x3080);
		var delete = new MuiAreaDeleteShortHelpMessage
		{
			MethodId = MuiAreaShortHelpMessageCodec.DeleteShortHelp,
			Help = APTR.FromPointer(0x3500),
		};
		Assert.True(MuiAreaShortHelpMessageCodec.WriteDelete(ref platform,
			deleteAddress, delete));
		Assert.True(MuiAreaShortHelpMessageCodec.TryReadDelete(ref platform,
			deleteAddress, out var decodedDelete));
		Assert.Equal(APTR.FromPointer(0x3500), decodedDelete.Help);

		Assert.False(MuiAreaShortHelpMessageMemoryCodec.TryGetAddress(ref platform,
			APTR.FromPointer(0x20FF8), MuiAreaShortHelpPacketKind.Check,
			MuiAreaShortHelpMessageField.MouseY, out _));
		Assert.False(MuiAreaShortHelpMessageMemoryCodec.TryGetAddress(ref platform,
			checkAddress, MuiAreaShortHelpPacketKind.Check,
			(MuiAreaShortHelpMessageField)255, out _));
		Assert.False(MuiAreaShortHelpMessageMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, MuiAreaShortHelpPacketKind.Delete,
			MuiAreaShortHelpMessageField.Help, out _));
		cursor.Field = (MuiAreaShortHelpMessageField)255;
		Assert.False(MuiAreaShortHelpMessageFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _, out _));
		cursor.Message = APTR.Null;
		cursor.Field = MuiAreaShortHelpMessageField.Help;
		Assert.False(MuiAreaShortHelpMessageFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _, out _));
	}

	[Fact]
	public void ShortHelpFieldAccessUsesCompleteNamedPacketRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var checkAddress = APTR.FromPointer(0x3100);
		var check = new MuiAreaCheckShortHelpMessage
		{
			MethodId = MuiAreaShortHelpMessageCodec.CheckShortHelp,
			Help = APTR.FromPointer(0x3500),
			MouseX = -41,
			MouseY = 27,
		};
		Assert.True(MuiAreaShortHelpMessageCodec.WriteCheck(ref platform,
			checkAddress, check));
		Assert.True(MuiAreaShortHelpMessageMemoryCodec.TryWriteUInt32(ref platform,
			checkAddress, MuiAreaShortHelpPacketKind.Check,
			MuiAreaShortHelpMessageField.MouseX, unchecked((uint)123)));
		Assert.True(MuiAreaShortHelpMessageMemoryCodec.TryWriteUInt32(ref platform,
			checkAddress, MuiAreaShortHelpPacketKind.Check,
			MuiAreaShortHelpMessageField.Help, 0x3600));
		Assert.True(MuiAreaShortHelpMessageCodec.TryReadCheck(ref platform,
			checkAddress, out var updatedCheck));
		Assert.Equal(check.MethodId, updatedCheck.MethodId);
		Assert.Equal(123, updatedCheck.MouseX);
		Assert.Equal(check.MouseY, updatedCheck.MouseY);
		Assert.Equal(APTR.FromPointer(0x3600), updatedCheck.Help);
		Assert.True(MuiAreaShortHelpMessageMemoryCodec.TryReadUInt32(ref platform,
			checkAddress, MuiAreaShortHelpPacketKind.Check,
			MuiAreaShortHelpMessageField.MouseY, out var mouseY));
		Assert.Equal(check.MouseY, unchecked((int)mouseY));

		var createAddress = APTR.FromPointer(0x3140);
		var create = new MuiAreaCreateShortHelpMessage
		{
			MethodId = MuiAreaShortHelpMessageCodec.CreateShortHelp,
			MouseX = -8,
			MouseY = 19,
		};
		Assert.True(MuiAreaShortHelpMessageCodec.WriteCreate(ref platform,
			createAddress, create));
		Assert.True(MuiAreaShortHelpMessageMemoryCodec.TryWriteUInt32(ref platform,
			createAddress, MuiAreaShortHelpPacketKind.Create,
			MuiAreaShortHelpMessageField.MouseY, unchecked((uint)-77)));
		Assert.True(MuiAreaShortHelpMessageCodec.TryReadCreate(ref platform,
			createAddress, out var updatedCreate));
		Assert.Equal(create.MethodId, updatedCreate.MethodId);
		Assert.Equal(create.MouseX, updatedCreate.MouseX);
		Assert.Equal(-77, updatedCreate.MouseY);

		var deleteAddress = APTR.FromPointer(0x3180);
		var delete = new MuiAreaDeleteShortHelpMessage
		{
			MethodId = MuiAreaShortHelpMessageCodec.DeleteShortHelp,
			Help = APTR.FromPointer(0x3700),
		};
		Assert.True(MuiAreaShortHelpMessageCodec.WriteDelete(ref platform,
			deleteAddress, delete));
		Assert.True(MuiAreaShortHelpMessageMemoryCodec.TryWriteUInt32(ref platform,
			deleteAddress, MuiAreaShortHelpPacketKind.Delete,
			MuiAreaShortHelpMessageField.Help, 0x3800));
		Assert.True(MuiAreaShortHelpMessageCodec.TryReadDelete(ref platform,
			deleteAddress, out var updatedDelete));
		Assert.Equal(delete.MethodId, updatedDelete.MethodId);
		Assert.Equal(APTR.FromPointer(0x3800), updatedDelete.Help);

		Assert.False(MuiAreaShortHelpMessageMemoryCodec.TryReadUInt32(ref platform,
			APTR.FromPointer(0x20FF4), MuiAreaShortHelpPacketKind.Check,
			MuiAreaShortHelpMessageField.MouseX, out _));
		Assert.False(MuiAreaShortHelpMessageMemoryCodec.TryWriteUInt32(ref platform,
			APTR.FromPointer(0x20FF8), MuiAreaShortHelpPacketKind.Create,
			MuiAreaShortHelpMessageField.MouseY, 1));
		Assert.False(MuiAreaShortHelpMessageMemoryCodec.TryReadUInt32(ref platform,
			checkAddress, MuiAreaShortHelpPacketKind.Delete,
			MuiAreaShortHelpMessageField.MouseX, out _));
		Assert.False(MuiAreaShortHelpMessageMemoryCodec.TryWriteUInt32(ref platform,
			checkAddress, MuiAreaShortHelpPacketKind.Check,
			(MuiAreaShortHelpMessageField)255, 1));
	}
}
