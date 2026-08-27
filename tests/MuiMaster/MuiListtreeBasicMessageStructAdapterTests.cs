using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListtreeBasicMessageStructAdapterTests
{
	[Fact]
	public void ListtreeBasicPacketsUseNamedFieldsAndCompleteBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var methodAddress = APTR.FromPointer(0x3000);
		Assert.True(MuiListtreeMethodMessageCodec.TryWrite(ref platform,
			methodAddress, MuiListtreeMessageCodec.Get));
		Assert.True(MuiListtreeMethodMessageCodec.TryRead(ref platform,
			methodAddress, out var method));
		Assert.Equal(MuiListtreeMessageCodec.Get, method.MethodId);
		Assert.True(MuiListtreeMessageMemoryCodec.TryGetAddress(ref platform,
			methodAddress, MuiListtreePacketKind.Method,
			MuiListtreeField.MethodId, out var methodField));
		Assert.Equal(methodAddress, methodField);

		var setAddress = APTR.FromPointer(0x3020);
		Assert.True(MuiListtreeMessageCodec.WriteSet(ref platform, setAddress,
			MuiListtreeMessageCodec.Set, 0x120, 0x456));
		Assert.True(MuiListtreeMessageCodec.TryReadSet(ref platform, setAddress,
			MuiListtreeMessageCodec.Set, out var set));
		Assert.Equal(0x120u, set.Attribute);
		Assert.Equal(0x456u, set.Value);

		var getAddress = APTR.FromPointer(0x3040);
		Assert.True(MuiListtreeMessageCodec.WriteGet(ref platform, getAddress,
			0x220, 0x3500));
		Assert.True(MuiListtreeMessageCodec.TryReadGet(ref platform, getAddress,
			out var get));
		Assert.Equal(0x3500u, get.Storage);
		Assert.True(MuiListtreeMessageMemoryCodec.TryGetAddress(ref platform,
			getAddress, MuiListtreePacketKind.Get,
			MuiListtreeField.Storage, out var storageField));
		Assert.Equal(APTR.FromPointer(0x3048), storageField);

		var entryAddress = APTR.FromPointer(0x3060);
		Assert.True(MuiListtreeMessageCodec.WriteGetEntry(ref platform,
			entryAddress, 0x3600, 7, 3));
		Assert.True(MuiListtreeMessageCodec.TryReadGetEntry(ref platform,
			entryAddress, out var entry));
		Assert.Equal(0x3600u, entry.Node);
		Assert.Equal(7u, entry.Position);
		Assert.Equal(3u, entry.Flags);

		Assert.False(MuiListtreeMessageMemoryCodec.TryGetAddress(ref platform,
			APTR.FromPointer(0x20FF4), MuiListtreePacketKind.GetEntry,
			MuiListtreeField.Flags, out _));
		Assert.False(MuiListtreeMessageMemoryCodec.TryGetAddress(ref platform,
			setAddress, MuiListtreePacketKind.Method,
			MuiListtreeField.Attribute, out _));
		Assert.False(MuiListtreeMessageMemoryCodec.TryGetAddress(ref platform,
			setAddress, (MuiListtreePacketKind)255,
			MuiListtreeField.MethodId, out _));
		Assert.False(MuiListtreeMessageMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, MuiListtreePacketKind.Get,
			MuiListtreeField.Storage, out _));
	}
}
