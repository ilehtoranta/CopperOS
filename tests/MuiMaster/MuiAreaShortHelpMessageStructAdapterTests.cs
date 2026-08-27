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
			checkAddress, check.Help, check.MouseX, check.MouseY));
		Assert.True(MuiAreaShortHelpMessageMemoryCodec.TryReadUInt32(ref platform,
			checkAddress, MuiAreaShortHelpPacketKind.Check,
			MuiAreaShortHelpMessageField.MouseY, out var mouseY));
		Assert.Equal(34, unchecked((int)mouseY));
		Assert.True(MuiAreaShortHelpMessageMemoryCodec.TryGetAddress(ref platform,
			checkAddress, MuiAreaShortHelpPacketKind.Check,
			MuiAreaShortHelpMessageField.Help, out var helpAddress));
		Assert.Equal(APTR.FromPointer(0x3004), helpAddress);
		Assert.True(MuiAreaShortHelpMessageCodec.TryReadCheck(ref platform,
			checkAddress, out var decodedCheck));
		Assert.Equal(check.Help, decodedCheck.Help);
		Assert.Equal(check.MouseX, decodedCheck.MouseX);
		Assert.Equal(check.MouseY, decodedCheck.MouseY);

		var createAddress = APTR.FromPointer(0x3040);
		Assert.True(MuiAreaShortHelpMessageMemoryCodec.TryWriteUInt32(ref platform,
			createAddress, MuiAreaShortHelpPacketKind.Create,
			MuiAreaShortHelpMessageField.MethodId,
			MuiAreaShortHelpMessageCodec.CreateShortHelp));
		Assert.True(MuiAreaShortHelpMessageMemoryCodec.TryWriteUInt32(ref platform,
			createAddress, MuiAreaShortHelpPacketKind.Create,
			MuiAreaShortHelpMessageField.MouseX, unchecked((uint)-3)));
		Assert.True(MuiAreaShortHelpMessageMemoryCodec.TryWriteUInt32(ref platform,
			createAddress, MuiAreaShortHelpPacketKind.Create,
			MuiAreaShortHelpMessageField.MouseY, 8));
		Assert.True(MuiAreaShortHelpMessageCodec.TryReadCreate(ref platform,
			createAddress, out var decodedCreate));
		Assert.Equal(-3, decodedCreate.MouseX);
		Assert.Equal(8, decodedCreate.MouseY);

		var deleteAddress = APTR.FromPointer(0x3080);
		Assert.True(MuiAreaShortHelpMessageMemoryCodec.TryWriteUInt32(ref platform,
			deleteAddress, MuiAreaShortHelpPacketKind.Delete,
			MuiAreaShortHelpMessageField.MethodId,
			MuiAreaShortHelpMessageCodec.DeleteShortHelp));
		Assert.True(MuiAreaShortHelpMessageMemoryCodec.TryWriteUInt32(ref platform,
			deleteAddress, MuiAreaShortHelpPacketKind.Delete,
			MuiAreaShortHelpMessageField.Help, 0x3500));
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
	}
}
