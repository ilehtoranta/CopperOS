using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiFamilyDoChildMethodsStructAdapterTests
{
	[Fact]
	public void FamilyDoChildMethodsFieldUsesNamedMethodRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiFamilyDoChildMethodsMessage
		{
			MethodId = MuiFamilyDoChildMethodsCore.Method,
		};
		Assert.True(MuiFamilyDoChildMethodsMessageStructCodec.WriteRecord(
			ref platform, address, value));
		Assert.True(MuiFamilyDoChildMethodsMessageMemoryCodec.TryWriteUInt32(
			ref platform, address,
			MuiFamilyDoChildMethodsPacketField.MethodId, 0xF1234567u));
		Assert.True(MuiFamilyDoChildMethodsMessageMemoryCodec.TryReadUInt32(
			ref platform, address,
			MuiFamilyDoChildMethodsPacketField.MethodId, out var method));
		Assert.Equal(0xF1234567u, method);
		Assert.True(MuiFamilyDoChildMethodsMessageStructCodec.TryRead(ref platform,
			address, out var decoded));
		Assert.Equal(0xF1234567u, decoded.MethodId);
	}

	[Fact]
	public void FamilyDoChildMethodsStructAdapterRejectsIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		Assert.False(MuiFamilyDoChildMethodsMessageMemoryCodec.TryReadUInt32(
			ref platform, APTR.FromPointer(0x30FFF),
			MuiFamilyDoChildMethodsPacketField.MethodId, out _));
		Assert.False(MuiFamilyDoChildMethodsMessageMemoryCodec.TryWriteUInt32(
			ref platform, APTR.Null,
			MuiFamilyDoChildMethodsPacketField.MethodId, 1));
		Assert.False(MuiFamilyDoChildMethodsMessageMemoryCodec.TryReadUInt32(
			ref platform, APTR.FromPointer(0x3500),
			(MuiFamilyDoChildMethodsPacketField)0xFF, out _));
	}
}
