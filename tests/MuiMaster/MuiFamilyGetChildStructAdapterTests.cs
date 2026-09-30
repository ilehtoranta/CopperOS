using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiFamilyGetChildStructAdapterTests
{
	[Fact]
	public void FamilyGetChildFieldsRoundTripThroughNamedRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var packet = new MuiFamilyGetChildMessage
		{
			MethodId = MuiFamilyGetChildMessageCodec.Method,
			Number = -4,
			Reference = APTR.FromPointer(0x36200),
		};

		Assert.True(MuiFamilyGetChildMessageStructCodec.WriteRecord(ref platform,
			address, packet));
		Assert.True(MuiFamilyGetChildMessageMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiFamilyGetChildPacketField.Number,
			unchecked((uint)-7)));
		Assert.True(MuiFamilyGetChildMessageMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiFamilyGetChildPacketField.Reference, 0x36400));
		Assert.True(MuiFamilyGetChildMessageMemoryCodec.TryReadUInt32(
			ref platform, address, MuiFamilyGetChildPacketField.Number,
			out var number));
		Assert.Equal(unchecked((uint)-7), number);
		Assert.True(MuiFamilyGetChildMessageStructCodec.TryRead(ref platform,
			address, out var decoded));
		Assert.Equal(packet.MethodId, decoded.MethodId);
		Assert.Equal(-7, decoded.Number);
		Assert.Equal(0x36400u, decoded.Reference.Raw);

		Assert.True(MuiFamilyGetChildMessageMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiFamilyGetChildPacketField.MethodId,
			0xF1234567u));
		Assert.True(MuiFamilyGetChildMessageStructCodec.TryRead(ref platform,
			address, out decoded));
		Assert.Equal(0xF1234567u, decoded.MethodId);
		Assert.Equal(-7, decoded.Number);
		Assert.Equal(0x36400u, decoded.Reference.Raw);
	}

	[Fact]
	public void FamilyGetChildStructAdapterRejectsIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var truncated = APTR.FromPointer(0x30FFC);
		Assert.False(MuiFamilyGetChildMessageStructCodec.TryRead(ref platform,
			truncated, out _));
		Assert.False(MuiFamilyGetChildMessageMemoryCodec.TryReadUInt32(
			ref platform, truncated, MuiFamilyGetChildPacketField.Number, out _));
		Assert.False(MuiFamilyGetChildMessageMemoryCodec.TryWriteUInt32(
			ref platform, APTR.Null, MuiFamilyGetChildPacketField.Reference, 1));
		Assert.False(MuiFamilyGetChildMessageMemoryCodec.TryReadUInt32(
			ref platform, APTR.FromPointer(0x3500),
			(MuiFamilyGetChildPacketField)0xFF, out _));
	}
}
