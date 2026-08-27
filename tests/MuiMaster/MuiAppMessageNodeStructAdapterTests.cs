using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAppMessageNodeStructAdapterTests
{
	[Fact]
	public void AppMessageNodeUsesDedicatedMixedWidthStructAdapter()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x2E80);
		var value = new MuiAppMessageNodeState
		{
			Successor = APTR.FromPointer(0x2F00),
			Predecessor = APTR.FromPointer(0x2F20),
			Type = 7,
			Priority = -3,
			Name = APTR.FromPointer(0x2F40),
			ReplyPort = APTR.FromPointer(0x2F60),
			Length = 0x1234,
		};

		Assert.True(MuiAppMessageNodeCodec.Write(ref platform, address, value));
		Assert.True(MuiAppMessageNodeMemoryCodec.TryGetAddress(ref platform,
			address, MuiAppMessageNodeField.Name, out var nameField,
			out var nameSize));
		Assert.Equal(APTR.FromPointer(0x2E8A), nameField);
		Assert.Equal(4u, nameSize);
		Assert.True(MuiAppMessageNodeMemoryCodec.TryGetAddress(ref platform,
			address, MuiAppMessageNodeField.Length, out var lengthField,
			out var lengthSize));
		Assert.Equal(APTR.FromPointer(0x2E92), lengthField);
		Assert.Equal(2u, lengthSize);
		Assert.True(MuiAppMessageNodeMemoryCodec.TryReadUInt8(ref platform,
			address, MuiAppMessageNodeField.Priority, out var priority));
		Assert.Equal(unchecked((byte)-3), priority);
		Assert.True(MuiAppMessageNodeMemoryCodec.TryWriteUInt16(ref platform,
			address, MuiAppMessageNodeField.Length, 0x4321));
		Assert.True(MuiAppMessageNodeCodec.TryRead(ref platform, address,
			out var decoded));
		Assert.Equal(value.Successor, decoded.Successor);
		Assert.Equal(value.Name, decoded.Name);
		Assert.Equal(value.Priority, decoded.Priority);
		Assert.Equal((ushort)0x4321, decoded.Length);
		Assert.False(MuiAppMessageNodeMemoryCodec.TryGetAddress(ref platform,
			APTR.FromPointer(0x30FF0), MuiAppMessageNodeField.Type, out _, out _));
		Assert.False(MuiAppMessageNodeMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, MuiAppMessageNodeField.Successor, out _, out _));
	}
}
