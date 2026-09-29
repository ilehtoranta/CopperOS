/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiDataspaceIffStructAdapterTests
{
	[Fact]
	public void IffPacketFieldsUseNamedRecordsAndPreserveSiblings()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var read = APTR.FromPointer(0x2400);
		Assert.True(MuiDataspaceIffMessageCore.WriteReadIffRecord(ref platform,
			read, APTR.FromPointer(0x3000)));
		Assert.True(MuiDataspaceReadIffMessageMemoryCodec.TryWrite(ref platform,
			read, MuiDataspaceReadIffField.Handle, 0x3100));
		Assert.True(MuiDataspaceReadIffMessageMemoryCodec.TryRead(ref platform,
			read, MuiDataspaceReadIffField.Handle, out var handle));
		Assert.Equal(0x3100u, handle);
		const uint replacement = 0xF1234567u;
		Assert.True(MuiDataspaceReadIffMessageMemoryCodec.TryWrite(ref platform,
			read, MuiDataspaceReadIffField.MethodId, replacement));
		Assert.True(MuiDataspaceReadIffMessageMemoryCodec.TryRead(ref platform,
			read, MuiDataspaceReadIffField.Handle, out handle));
		Assert.Equal(0x3100u, handle);
		Assert.False(MuiDataspaceIffMessageCodec.TryReadReadIff(ref platform,
			read, out _));

		var write = APTR.FromPointer(0x2400);
		Assert.True(MuiDataspaceIffMessageCore.WriteWriteIffRecord(ref platform,
			write, APTR.FromPointer(0x3200), 0x464F524D, 0x44415441));
		Assert.True(MuiDataspaceWriteIffMessageMemoryCodec.TryWrite(ref platform,
			write, MuiDataspaceWriteIffField.Handle, 0x3300));
		Assert.True(MuiDataspaceWriteIffMessageMemoryCodec.TryWrite(ref platform,
			write, MuiDataspaceWriteIffField.Type, 0x494C4953));
		Assert.True(MuiDataspaceWriteIffMessageMemoryCodec.TryRead(ref platform,
			write, MuiDataspaceWriteIffField.Handle, out handle));
		Assert.Equal(0x3300u, handle);
		Assert.True(MuiDataspaceWriteIffMessageMemoryCodec.TryRead(ref platform,
			write, MuiDataspaceWriteIffField.Type, out var type));
		Assert.Equal(0x494C4953u, type);
		Assert.True(MuiDataspaceWriteIffMessageMemoryCodec.TryRead(ref platform,
			write, MuiDataspaceWriteIffField.Id, out var id));
		Assert.Equal(0x44415441u, id);
	}

	[Fact]
	public void IffStructAdaptersRejectBadRangesAndPreserveHeaderFields()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var header = APTR.FromPointer(0x2400);
		Assert.True(MuiDataspaceIffEntryHeaderCodec.Write(ref platform, header,
			new MuiDataspaceIffEntryHeader { Id = 1, Length = 2 }));
		Assert.True(MuiDataspaceIffEntryHeaderFieldCursorCodec.TryWrite(ref platform,
			header, MuiDataspaceIffEntryHeaderField.Length, 37));
		Assert.True(MuiDataspaceIffEntryHeaderFieldCursorCodec.TryRead(ref platform,
			header, MuiDataspaceIffEntryHeaderField.Id, out var id));
		Assert.Equal(1u, id);
		Assert.True(MuiDataspaceIffEntryHeaderFieldCursorCodec.TryRead(ref platform,
			header, MuiDataspaceIffEntryHeaderField.Length, out var length));
		Assert.Equal(37u, length);
		Assert.False(MuiDataspaceIffEntryHeaderFieldCursorCodec.TryRead(ref platform,
			APTR.FromPointer(0x20FFC), MuiDataspaceIffEntryHeaderField.Id, out _));
		Assert.False(MuiDataspaceReadIffMessageMemoryCodec.TryRead(ref platform,
			APTR.Null, MuiDataspaceReadIffField.Handle, out _));
		Assert.False(MuiDataspaceWriteIffMessageMemoryCodec.TryWrite(ref platform,
			APTR.FromPointer(0x20FF5), MuiDataspaceWriteIffField.Id, 1));
		Assert.False(MuiDataspaceIffMethodFieldCursorCodec.TryWrite(ref platform,
			header, (MuiDataspaceIffMethodField)255, 1));
	}
}
