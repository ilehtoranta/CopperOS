using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiWorkbenchArgumentStructAdapterTests
{
	[Fact]
	public void WorkbenchArgumentRecordUsesDedicatedNamedStructAdapter()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3100);
		var value = new MuiWorkbenchArgumentRecord
		{
			Lock = BPTR.FromRaw(0x12345678u),
			Name = STRPTR.FromPointer(0x2F00),
		};

		Assert.True(MuiWorkbenchArgumentRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiWorkbenchArgumentRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiWorkbenchArgumentField.Lock,
			out var lockField));
		Assert.Equal(APTR.FromPointer(0x3100), lockField);
		Assert.True(MuiWorkbenchArgumentRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiWorkbenchArgumentField.Name,
			out var nameField));
		Assert.Equal(APTR.FromPointer(0x3104), nameField);
		Assert.True(MuiWorkbenchArgumentRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiWorkbenchArgumentField.Lock,
			out var rawLock));
		Assert.Equal(value.Lock.Raw, rawLock);
		Assert.True(MuiWorkbenchArgumentRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiWorkbenchArgumentField.Name, 0x2FA0u));
		Assert.True(MuiWorkbenchArgumentRecordCodec.TryRead(ref platform, address,
			out var decoded));
		Assert.Equal(value.Lock, decoded.Lock);
		Assert.Equal(STRPTR.FromPointer(0x2FA0), decoded.Name);
		Assert.False(MuiWorkbenchArgumentRecordMemoryCodec.TryGetAddress(ref platform,
			APTR.FromPointer(0x30FFC), MuiWorkbenchArgumentField.Lock, out _));
		Assert.False(MuiWorkbenchArgumentRecordMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, MuiWorkbenchArgumentField.Name, out _));
	}

	[Fact]
	public void WorkbenchArgumentVectorBridgeUsesCompleteNamedRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var vector = APTR.FromPointer(0x3200);
		var expected = new MuiWorkbenchArgumentRecord
		{
			Lock = BPTR.FromRaw(0xF1234567u),
			Name = STRPTR.FromPointer(0xE2345678u),
		};

		Assert.True(MuiWorkbenchArgumentVectorCodec.TryWrite(ref platform,
			vector, 3, expected));
		Assert.True(MuiWorkbenchArgumentVectorCodec.TryRead(ref platform,
			vector, 3, out var actual));
		Assert.Equal(expected.Lock, actual.Lock);
		Assert.Equal(expected.Name, actual.Name);

		Assert.False(MuiWorkbenchArgumentVectorCodec.TryRead(ref platform,
			vector, MuiWorkbenchArgumentVectorCursor.MaximumEntries, out _));
		Assert.False(MuiWorkbenchArgumentVectorCodec.TryRead(ref platform,
			APTR.FromPointer(0x30FFCu), 0, out _));
		Assert.False(MuiWorkbenchArgumentVectorCodec.TryWrite(ref platform,
			APTR.Null, 0, expected));
	}
}
